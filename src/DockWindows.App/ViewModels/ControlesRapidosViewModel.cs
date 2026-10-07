using System.Collections.ObjectModel;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class ControleRapidoViewModel : ObservableObject
{
    private readonly ControleRapidoConfig _config;
    private readonly Action _salvar;
    public TipoControleRapido Tipo => _config.Tipo;
    public string Nome { get; }
    public string Icone { get; }
    public string Cor { get; }
    public string Ajuda { get; }
    public ICommand ExecutarCommand { get; }
    public string Status => MostrarNaDock ? "Na dock" : "Oculto da dock";
    public bool MostrarNaDock
    {
        get => _config.MostrarNaDock;
        set
        {
            if (_config.MostrarNaDock == value) return;
            _config.MostrarNaDock = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Status));
            _salvar();
        }
    }

    public ControleRapidoViewModel(ControleRapidoConfig config, Action salvar, Action<TipoControleRapido> executar)
    {
        _config = config;
        _salvar = salvar;
        (Nome, Icone, Cor, Ajuda) = config.Tipo switch
        {
            TipoControleRapido.Wifi => ("Wi-Fi", "", "#3488E7", "Abrir configurações de Wi-Fi"),
            TipoControleRapido.Bluetooth => ("Bluetooth", "", "#3488E7", "Abrir configurações de Bluetooth"),
            TipoControleRapido.ModoEscuro => ("Modo escuro", "", "#A675EE", "Escolher modo claro ou escuro nas configurações do Windows"),
            TipoControleRapido.Foco => ("Foco", "", "#ECA342", "Abrir configurações de foco do Windows"),
            TipoControleRapido.BloquearTeclado => ("Bloquear teclado", "", "#54C5B2", "Bloquear por 30 segundos. F12 ou outro clique libera."),
            TipoControleRapido.BloquearTela => ("Bloquear tela", "", "#829DA9", "Bloquear sua sessão do Windows"),
            TipoControleRapido.Suspender => ("Suspender", "", "#9894E5", "Suspender o computador"),
            _ => throw new ArgumentOutOfRangeException(nameof(config))
        };
        ExecutarCommand = new RelayCommand(() => executar(Tipo));
    }
}

public class ControlesRapidosViewModel : ObservableObject, IDisposable
{
    private readonly IControlesRapidosService _service;
    private readonly Action _salvar;
    private readonly Action<string> _erro;
    private readonly Func<bool> _confirmarSuspensao;
    private bool _aberto;
    private string? _paginaDetalhe;
    private Ambiente? _ambiente;
    public string[] EstilosDisponiveis { get; } = { "Compacto", "Anéis" };
    public string Estilo
    {
        get => _ambiente?.EstiloControlesRapidos == "Anéis" ? "Anéis" : "Compacto";
        set
        {
            if (_ambiente == null || !EstilosDisponiveis.Contains(value) || Estilo == value) return;
            _ambiente.EstiloControlesRapidos = value;
            OnPropertyChanged();
            _salvar();
        }
    }
    public ObservableCollection<ControleRapidoViewModel> Itens { get; } = new();
    public ObservableCollection<ControleRapidoViewModel> Fixados { get; } = new();
    public int Quantidade => Fixados.Count;
    public bool Aberto { get => _aberto; set => SetProperty(ref _aberto, value); }
    public ConectividadeViewModel Conectividade { get; }
    public bool ExibindoInicio => _paginaDetalhe == null;
    public bool ExibindoWifi => _paginaDetalhe == "wifi";
    public bool ExibindoBluetooth => _paginaDetalhe == "bluetooth";
    public bool TecladoBloqueado => _service.TecladoBloqueado;
    public ICommand AbrirCommand { get; }
    public ICommand FecharCommand { get; }
    public ICommand LiberarTecladoCommand { get; }
    public ICommand VoltarCommand { get; }

    public ControlesRapidosViewModel(Action salvar, Action<string> erro, Func<bool> confirmarSuspensao,
        IControlesRapidosService? service = null, ConectividadeViewModel? conectividade = null)
    {
        _salvar = salvar;
        _erro = erro;
        _confirmarSuspensao = confirmarSuspensao;
        Conectividade = conectividade ?? new ConectividadeViewModel(new ConectividadeService());
        _service = service ?? new ControlesRapidosService();
        _service.BloqueioAlterado += BloqueioAlterado;
        AbrirCommand = new RelayCommand(() => { if (!Aberto) MostrarPagina(null); Aberto = !Aberto; });
        FecharCommand = new RelayCommand(() => Aberto = false);
        LiberarTecladoCommand = new RelayCommand(() => Executar(TipoControleRapido.BloquearTeclado), () => TecladoBloqueado);
        VoltarCommand = new RelayCommand(() => MostrarPagina(null));
    }

    public void Carregar(Ambiente ambiente)
    {
        _ambiente = ambiente;
        OnPropertyChanged(nameof(Estilo));
        _service.LiberarTeclado();
        Aberto = false;
        Itens.Clear();
        ambiente.ControlesRapidos ??= ControleRapidoConfig.CriarPadrao();
        foreach (var tipo in Enum.GetValues<TipoControleRapido>())
        {
            var config = ambiente.ControlesRapidos.FirstOrDefault(c => c.Tipo == tipo);
            if (config == null)
            {
                config = ControleRapidoConfig.CriarPadrao().Single(c => c.Tipo == tipo);
                ambiente.ControlesRapidos.Add(config);
            }
            Itens.Add(new(config, () => { AtualizarFixados(); _salvar(); }, Executar));
        }
        AtualizarFixados();
    }

    private void AtualizarFixados()
    {
        Fixados.Clear();
        foreach (var item in Itens.Where(i => i.MostrarNaDock)) Fixados.Add(item);
        OnPropertyChanged(nameof(Quantidade));
    }

    private void Executar(TipoControleRapido tipo)
    {
        try
        {
            if (tipo == TipoControleRapido.Wifi)
            {
                Conectividade.Habilitado = true;
                MostrarPagina("wifi");
                Aberto = true;
                Conectividade.AtualizarCommand.Execute(null);
                return;
            }
            if (tipo == TipoControleRapido.Bluetooth)
            {
                Conectividade.Habilitado = true;
                MostrarPagina("bluetooth");
                Aberto = true;
                Conectividade.AtualizarCommand.Execute(null);
                return;
            }
            Aberto = false;
            if (tipo == TipoControleRapido.Suspender && !_confirmarSuspensao()) return;
            _service.Executar(tipo);
        }
        catch { _erro("Não foi possível executar esse controle. Verifique as permissões e a disponibilidade do recurso no Windows."); }
    }

    private void MostrarPagina(string? pagina)
    {
        _paginaDetalhe = pagina;
        OnPropertyChanged(nameof(ExibindoInicio));
        OnPropertyChanged(nameof(ExibindoWifi));
        OnPropertyChanged(nameof(ExibindoBluetooth));
    }

    private void BloqueioAlterado() => OnPropertyChanged(nameof(TecladoBloqueado));
    public void Dispose()
    {
        _service.BloqueioAlterado -= BloqueioAlterado;
        _service.Dispose();
    }
}
