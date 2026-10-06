using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Services;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class AreaTransferenciaViewModel : ObservableObject, IAtividadeWidget
{
    private readonly IClipboardNotificationService _service;
    private bool _disposed;
    private bool _habilitado;
    private bool _painelAberto;
    private string _estado = "Histórico vazio";

    public AreaTransferenciaViewModel(IClipboardNotificationService service)
    {
        _service = service;
        _service.ClipboardAlterada += AoAlterarClipboard;
        LimparCommand = new RelayCommand(Limpar);
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
    }

    public ObservableCollection<ItemClipboardViewModel> Itens { get; } = new();
    public bool? EmExecucao => _service.EstaAtivo;
    public SaudeWidget Saude => _service.EstaAtivo || !Habilitado ? SaudeWidget.Disponivel : SaudeWidget.Erro;
    public string? MotivoEstado => Estado;
    public bool Habilitado { get => _habilitado; set { if (SetProperty(ref _habilitado, value)) AtualizarServico(); } }
    public bool PainelAberto { get => _painelAberto; set => SetProperty(ref _painelAberto, value); }
    public string Estado { get => _estado; private set => SetProperty(ref _estado, value); }
    public ICommand LimparCommand { get; }
    public ICommand AlternarPainelCommand { get; }

    public void DefinirAtividade(EstadoAtividade estado)
    {
        if (_disposed) return;
        if (!estado.Visual) PainelAberto = false;
        AtualizarServico();
    }

    private void AtualizarServico()
    {
        if (_disposed || !Habilitado) { _service.Parar(); return; }
        Estado = _service.Iniciar() ? (Itens.Count == 0 ? "Histórico vazio" : $"{Itens.Count} itens") : "O Windows recusou o monitor da área de transferência.";
    }

    private void AoAlterarClipboard()
    {
        if (_disposed || !Habilitado) return;
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (!Clipboard.ContainsText()) return;
                var texto = Clipboard.GetText().Trim();
                if (string.IsNullOrEmpty(texto)) return;
                if (texto.Length > 4000) texto = texto[..4000];
                if (Itens.FirstOrDefault()?.Texto == texto) return;
                Itens.Insert(0, new ItemClipboardViewModel(texto, Copiar, Remover));
                while (Itens.Count > 20) Itens.RemoveAt(Itens.Count - 1);
                Estado = $"{Itens.Count} itens · somente nesta sessão";
            }
            catch { Estado = "Não foi possível ler o conteúdo copiado."; }
        });
    }

    private void Copiar(ItemClipboardViewModel item)
    {
        try { Clipboard.SetText(item.Texto); } catch { Estado = "Não foi possível copiar este item."; }
    }

    private void Remover(ItemClipboardViewModel item)
    {
        Itens.Remove(item);
        Estado = Itens.Count == 0 ? "Histórico vazio" : $"{Itens.Count} itens · somente nesta sessão";
    }

    private void Limpar()
    {
        Itens.Clear();
        Estado = "Histórico vazio";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _service.ClipboardAlterada -= AoAlterarClipboard;
        _service.Dispose();
        Itens.Clear();
    }
}

public sealed class ItemClipboardViewModel
{
    public ItemClipboardViewModel(string texto, Action<ItemClipboardViewModel> copiar, Action<ItemClipboardViewModel> remover)
    {
        Texto = texto;
        CopiarCommand = new RelayCommand(() => copiar(this));
        RemoverCommand = new RelayCommand(() => remover(this));
    }
    public string Texto { get; }
    public string Resumo => Texto.ReplaceLineEndings(" ").Length > 80 ? Texto.ReplaceLineEndings(" ")[..77] + "…" : Texto.ReplaceLineEndings(" ");
    public ICommand CopiarCommand { get; }
    public ICommand RemoverCommand { get; }
}
