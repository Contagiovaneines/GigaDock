using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Services;
using DockWindows.Core.Widgets;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public sealed class ObsWidgetViewModel : ObservableObject, IAtividadeWidget
{
    private const string AlvoCredencial = "GigaDock/OBSWebSocket";
    private readonly ISecretStore _segredos;
    private ClientWebSocket? _socket;
    private bool _disposed, _habilitado, _conectado, _estaGravando, _estaTransmitindo;
    private string _estadoIntegracao = "OBS desconectado", _erroIntegracao = string.Empty;
    public ObsWidgetViewModel(ISecretStore? segredos = null)
    {
        _segredos = segredos ?? new WindowsCredentialStore();
        AbrirAppCommand = new RelayCommand(AbrirObs); ConectarCommand = new RelayCommand(() => _ = ConectarAsync());
        AlternarGravacaoCommand = new RelayCommand(() => _ = AlternarGravacaoAsync()); ConfigurarSenhaCommand = new RelayCommand(ConfigurarSenha);
    }
    public bool? EmExecucao => Conectado;
    public SaudeWidget Saude => Conectado ? SaudeWidget.Disponivel : string.IsNullOrEmpty(ErroIntegracao) ? SaudeWidget.Indisponivel : SaudeWidget.Erro;
    public string? MotivoEstado => string.IsNullOrEmpty(ErroIntegracao) ? EstadoIntegracao : ErroIntegracao;
    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public bool Conectado { get => _conectado; private set { if (SetProperty(ref _conectado, value)) OnPropertyChanged(nameof(TextoBotaoGravar)); } }
    public bool EstaGravando { get => _estaGravando; private set { if (SetProperty(ref _estaGravando, value)) { OnPropertyChanged(nameof(TextoBotaoGravar)); OnPropertyChanged(nameof(CorBotaoGravar)); OnPropertyChanged(nameof(CorGlowGravar)); } } }
    public bool EstaTransmitindo { get => _estaTransmitindo; private set => SetProperty(ref _estaTransmitindo, value); }
    public string EstadoIntegracao { get => _estadoIntegracao; private set => SetProperty(ref _estadoIntegracao, value); }
    public string ErroIntegracao { get => _erroIntegracao; private set => SetProperty(ref _erroIntegracao, value); }
    public string TextoBotaoGravar => !Conectado ? "Conectar" : EstaGravando ? "Parar" : "Gravar";
    public string CorBotaoGravar => EstaGravando ? "#FF3B30" : Conectado ? "#34C759" : "#8E8E93";
    public string CorGlowGravar => EstaGravando ? "#FF3B30" : "Transparent";
    public ICommand AbrirAppCommand { get; } public ICommand ConectarCommand { get; } public ICommand AlternarGravacaoCommand { get; } public ICommand ConfigurarSenhaCommand { get; }
    public void DefinirAtividade(EstadoAtividade estado) { if (!estado.Habilitado) _ = DesconectarAsync(); }

    public async Task ConectarAsync()
    {
        if (_disposed || Conectado) return; await DesconectarAsync();
        try
        {
            _socket = new ClientWebSocket(); await _socket.ConnectAsync(new Uri("ws://127.0.0.1:4455"), CancellationToken.None);
            using var hello = await ReceberAsync(); var d = hello.RootElement.GetProperty("d"); string? autenticacao = null;
            if (d.TryGetProperty("authentication", out var auth))
            {
                var senha = _segredos.Ler(AlvoCredencial); if (string.IsNullOrEmpty(senha)) throw new InvalidOperationException("O OBS exige senha. Use Configurar senha.");
                autenticacao = CriarAutenticacao(senha, auth.GetProperty("salt").GetString()!, auth.GetProperty("challenge").GetString()!);
            }
            await EnviarAsync(new { op = 1, d = new { rpcVersion = 1, authentication = autenticacao } });
            using var identificado = await ReceberAsync(); if (identificado.RootElement.GetProperty("op").GetInt32() != 2) throw new InvalidOperationException("O OBS recusou a identificação.");
            Conectado = true; ErroIntegracao = string.Empty; EstadoIntegracao = "OBS conectado"; await AtualizarEstadoAsync();
        }
        catch (Exception ex) { Conectado = false; ErroIntegracao = ex.Message; EstadoIntegracao = "OBS desconectado"; await DesconectarAsync(); }
    }
    private async Task AtualizarEstadoAsync()
    {
        var gravacao = await RequisitarAsync("GetRecordStatus"); EstaGravando = gravacao.TryGetProperty("outputActive", out var rec) && rec.GetBoolean();
        var stream = await RequisitarAsync("GetStreamStatus"); EstaTransmitindo = stream.TryGetProperty("outputActive", out var live) && live.GetBoolean();
        EstadoIntegracao = EstaTransmitindo ? "Transmitindo" : EstaGravando ? "Gravando" : "OBS pronto";
    }
    private async Task AlternarGravacaoAsync() { if (!Conectado) { await ConectarAsync(); return; } try { await RequisitarAsync(EstaGravando ? "StopRecord" : "StartRecord"); await AtualizarEstadoAsync(); } catch (Exception ex) { ErroIntegracao = ex.Message; } }
    private async Task<JsonElement> RequisitarAsync(string tipo)
    {
        var id = Guid.NewGuid().ToString("N"); await EnviarAsync(new { op = 6, d = new { requestType = tipo, requestId = id } }); using var resposta = await ReceberAsync(); var d = resposta.RootElement.GetProperty("d");
        if (!d.GetProperty("requestStatus").GetProperty("result").GetBoolean()) throw new InvalidOperationException(d.GetProperty("requestStatus").GetProperty("comment").GetString() ?? "O OBS recusou o comando.");
        return d.TryGetProperty("responseData", out var dados) ? dados.Clone() : JsonDocument.Parse("{}").RootElement.Clone();
    }
    private async Task EnviarAsync(object valor) { if (_socket?.State != WebSocketState.Open) throw new InvalidOperationException("OBS desconectado."); var bytes = JsonSerializer.SerializeToUtf8Bytes(valor, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }); await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None); }
    private async Task<JsonDocument> ReceberAsync()
    {
        if (_socket == null) throw new InvalidOperationException("OBS desconectado."); using var memoria = new MemoryStream(); var buffer = new byte[8192]; WebSocketReceiveResult resultado;
        do { resultado = await _socket.ReceiveAsync(buffer, CancellationToken.None); memoria.Write(buffer, 0, resultado.Count); } while (!resultado.EndOfMessage);
        if (resultado.MessageType == WebSocketMessageType.Close) throw new InvalidOperationException("O OBS encerrou a conexão."); return JsonDocument.Parse(memoria.ToArray());
    }
    private static string CriarAutenticacao(string senha, string salt, string desafio) { var segredo = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(senha + salt))); return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(segredo + desafio))); }
    private void ConfigurarSenha()
    {
        var caixa = new PasswordBox { Margin = new Thickness(16), MinWidth = 280 }; var janela = new Window { Title = "Senha do OBS WebSocket", Content = caixa, SizeToContent = SizeToContent.WidthAndHeight, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current?.MainWindow };
        caixa.KeyDown += (_, e) => { if (e.Key == Key.Enter) { janela.DialogResult = true; janela.Close(); } };
        if (janela.ShowDialog() == true && !string.IsNullOrEmpty(caixa.Password)) ErroIntegracao = _segredos.Salvar(AlvoCredencial, caixa.Password) ? "Senha salva no Gerenciador de Credenciais." : "Não foi possível salvar a senha.";
    }
    private void AbrirObs() { try { Process.Start(new ProcessStartInfo("obs64.exe") { UseShellExecute = true }); } catch { try { Process.Start(new ProcessStartInfo("obs-studio://") { UseShellExecute = true }); } catch { ErroIntegracao = "OBS não encontrado."; } } }
    private async Task DesconectarAsync() { var socket = _socket; _socket = null; Conectado = false; if (socket != null) { try { if (socket.State == WebSocketState.Open) await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "GigaDock", CancellationToken.None); } catch { } socket.Dispose(); } }
    public void Dispose() { if (_disposed) return; _disposed = true; _ = DesconectarAsync(); }
}
