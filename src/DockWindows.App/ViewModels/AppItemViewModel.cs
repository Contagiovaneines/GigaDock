using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class AppItemViewModel : ObservableObject
{
    private readonly IWindowTrackingService _windowService;
    private readonly IIconExtractionService _iconService;
    private readonly Action<AppItemViewModel> _onExecutar;
    private readonly Action<AppItemViewModel> _onAlternarFixado;
    private readonly Action<AppItemViewModel>? _onMoverEsquerda;
    private readonly Action<AppItemViewModel>? _onMoverDireita;

    private string _titulo;
    private string _caminhoExecutavel;
    private ImageSource? _icone;
    private bool _estaFixado;
    private bool _estaAberto;
    private bool _estaAtivo;
    private int _quantidadeJanelas;
    private bool _menuJanelasAberto;

    public AppItemViewModel(
        ItemFixado model,
        IWindowTrackingService windowService,
        IIconExtractionService iconService,
        Action<AppItemViewModel> onExecutar,
        Action<AppItemViewModel> onAlternarFixado,
        Action<AppItemViewModel>? onMoverEsquerda = null,
        Action<AppItemViewModel>? onMoverDireita = null)
    {
        _windowService = windowService;
        _iconService = iconService;
        _onExecutar = onExecutar;
        _onAlternarFixado = onAlternarFixado;
        _onMoverEsquerda = onMoverEsquerda;
        _onMoverDireita = onMoverDireita;

        Id = model.Id;
        _titulo = model.Titulo;
        _caminhoExecutavel = model.CaminhoOuUrl;
        Tipo = model.Tipo;
        _estaFixado = true;
        Janelas = new ObservableCollection<JanelaInfo>();

        CarregarIcone();

        ClicarCommand = new RelayCommand(Clicar);
        AtivarJanelaCommand = new RelayCommand<JanelaInfo>(AtivarJanela);
        FecharJanelaCommand = new RelayCommand<JanelaInfo>(FecharJanela);
        FecharTodasJanelasCommand = new RelayCommand(FecharTodasJanelas);
        AbrirNovaJanelaCommand = new RelayCommand(() => _onExecutar(this));
        FixarDesafixarCommand = new RelayCommand(() => _onAlternarFixado(this));
        MoverEsquerdaCommand = new RelayCommand(() => _onMoverEsquerda?.Invoke(this));
        MoverDireitaCommand = new RelayCommand(() => _onMoverDireita?.Invoke(this));
        MoverParaAmbienteCommand = new RelayCommand(() => OnMoverParaAmbiente?.Invoke(this));
    }

    public AppItemViewModel(
        JanelaInfo primeiraJanela,
        IWindowTrackingService windowService,
        IIconExtractionService iconService,
        Action<AppItemViewModel> onExecutar,
        Action<AppItemViewModel> onAlternarFixado,
        Action<AppItemViewModel>? onMoverEsquerda = null,
        Action<AppItemViewModel>? onMoverDireita = null)
    {
        _windowService = windowService;
        _iconService = iconService;
        _onExecutar = onExecutar;
        _onAlternarFixado = onAlternarFixado;
        _onMoverEsquerda = onMoverEsquerda;
        _onMoverDireita = onMoverDireita;

        Id = "app-open-" + Guid.NewGuid().ToString("N")[..8];
        _titulo = string.IsNullOrWhiteSpace(primeiraJanela.NomeProcesso) ? primeiraJanela.Titulo : primeiraJanela.NomeProcesso;
        _caminhoExecutavel = primeiraJanela.CaminhoExecutavel;
        Tipo = TipoItem.Aplicativo;
        _estaFixado = false;
        _estaAberto = true;
        _estaAtivo = primeiraJanela.EstaAtiva;
        _quantidadeJanelas = 1;
        Janelas = new ObservableCollection<JanelaInfo> { primeiraJanela };

        CarregarIcone();

        ClicarCommand = new RelayCommand(Clicar);
        AtivarJanelaCommand = new RelayCommand<JanelaInfo>(AtivarJanela);
        FecharJanelaCommand = new RelayCommand<JanelaInfo>(FecharJanela);
        FecharTodasJanelasCommand = new RelayCommand(FecharTodasJanelas);
        AbrirNovaJanelaCommand = new RelayCommand(() => _onExecutar(this));
        FixarDesafixarCommand = new RelayCommand(() => _onAlternarFixado(this));
        MoverEsquerdaCommand = new RelayCommand(() => _onMoverEsquerda?.Invoke(this));
        MoverDireitaCommand = new RelayCommand(() => _onMoverDireita?.Invoke(this));
        MoverParaAmbienteCommand = new RelayCommand(() => OnMoverParaAmbiente?.Invoke(this));
    }

    public string Id { get; }
    public TipoItem Tipo { get; }

    public string Titulo
    {
        get => _titulo;
        set => SetProperty(ref _titulo, value);
    }

    public string CaminhoExecutavel
    {
        get => _caminhoExecutavel;
        set
        {
            if (SetProperty(ref _caminhoExecutavel, value))
            {
                CarregarIcone();
            }
        }
    }

    public ImageSource? Icone
    {
        get => _icone;
        set => SetProperty(ref _icone, value);
    }

    public bool EstaFixado
    {
        get => _estaFixado;
        set => SetProperty(ref _estaFixado, value);
    }

    public bool EstaAberto
    {
        get => _estaAberto;
        set
        {
            if (SetProperty(ref _estaAberto, value))
            {
                OnPropertyChanged(nameof(TextoDica));
            }
        }
    }

    public bool EstaAtivo
    {
        get => _estaAtivo;
        set => SetProperty(ref _estaAtivo, value);
    }

    public int QuantidadeJanelas
    {
        get => _quantidadeJanelas;
        set
        {
            if (SetProperty(ref _quantidadeJanelas, value))
            {
                OnPropertyChanged(nameof(TemMultiplasJanelas));
                OnPropertyChanged(nameof(TextoDica));
            }
        }
    }

    public bool TemMultiplasJanelas => QuantidadeJanelas > 1;

    public bool MenuJanelasAberto
    {
        get => _menuJanelasAberto;
        set => SetProperty(ref _menuJanelasAberto, value);
    }

    public ObservableCollection<JanelaInfo> Janelas { get; }

    public string NomeProcesso => Janelas.FirstOrDefault()?.NomeProcesso ?? (string.IsNullOrWhiteSpace(CaminhoExecutavel) ? string.Empty : Path.GetFileNameWithoutExtension(CaminhoExecutavel));

    public string TextoDica
    {
        get
        {
            if (!EstaAberto) return Titulo;
            if (QuantidadeJanelas <= 1) return $"{Titulo} (Aberto)";
            return $"{Titulo} ({QuantidadeJanelas} janelas abertas)";
        }
    }

    public Action<AppItemViewModel>? OnMoverParaAmbiente { get; set; }

    public ICommand ClicarCommand { get; }
    public ICommand AtivarJanelaCommand { get; }
    public ICommand FecharJanelaCommand { get; }
    public ICommand FecharTodasJanelasCommand { get; }
    public ICommand AbrirNovaJanelaCommand { get; }
    public ICommand FixarDesafixarCommand { get; }
    public ICommand MoverEsquerdaCommand { get; }
    public ICommand MoverDireitaCommand { get; }
    public ICommand MoverParaAmbienteCommand { get; }

    private void Clicar()
    {
        if (!EstaAberto)
        {
            _onExecutar(this);
            return;
        }

        if (QuantidadeJanelas == 1)
        {
            var win = Janelas.FirstOrDefault();
            if (win != null)
            {
                if (EstaAtivo && !win.EstaMinimizada)
                {
                    _windowService.MinimizarJanela(win.Hwnd);
                }
                else
                {
                    _windowService.AtivarJanela(win.Hwnd);
                }
            }
        }
        else
        {
            // Alterna o flyout de seleção de janela
            MenuJanelasAberto = !MenuJanelasAberto;
        }
    }

    private void AtivarJanela(JanelaInfo? janela)
    {
        if (janela != null)
        {
            _windowService.AtivarJanela(janela.Hwnd);
            MenuJanelasAberto = false;
        }
    }

    private void FecharJanela(JanelaInfo? janela)
    {
        if (janela != null)
        {
            _windowService.FecharJanela(janela.Hwnd);
        }
    }

    private void FecharTodasJanelas()
    {
        foreach (var jan in Janelas.ToList())
        {
            _windowService.FecharJanela(jan.Hwnd);
        }
        MenuJanelasAberto = false;
    }

    public void SincronizarJanelas(List<JanelaInfo> janelasCorrespondentes)
    {
        Janelas.Clear();
        foreach (var j in janelasCorrespondentes)
        {
            Janelas.Add(j);
        }

        QuantidadeJanelas = Janelas.Count;
        EstaAberto = QuantidadeJanelas > 0;
        EstaAtivo = Janelas.Any(j => j.EstaAtiva);

        if (!EstaAberto)
        {
            MenuJanelasAberto = false;
        }

        // Se o título for genérico e tiver janela aberta, atualiza com título da janela se mais informativo
        if (string.IsNullOrWhiteSpace(_titulo) && janelasCorrespondentes.Count > 0)
        {
            Titulo = janelasCorrespondentes[0].NomeProcesso;
        }

        // Se ícone ainda não carregou, tenta recarregar com o caminho da janela ou com o HWND
        if (_icone == null && janelasCorrespondentes.Count > 0)
        {
            if (!string.IsNullOrEmpty(janelasCorrespondentes[0].CaminhoExecutavel))
            {
                _caminhoExecutavel = janelasCorrespondentes[0].CaminhoExecutavel;
            }
            CarregarIcone();
        }
    }

    private void CarregarIcone()
    {
        try
        {
            bool ehAppFrameHost = !string.IsNullOrEmpty(_caminhoExecutavel) &&
                _caminhoExecutavel.EndsWith("ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase);

            if (ehAppFrameHost && Janelas != null && Janelas.Count > 0)
            {
                foreach (var j in Janelas)
                {
                    if (j.Hwnd != IntPtr.Zero)
                    {
                        var iconJanela = _iconService.ObterIconeJanela(j.Hwnd);
                        if (iconJanela != null)
                        {
                            Icone = iconJanela;
                            return;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(_caminhoExecutavel))
            {
                Icone = _iconService.ObterIcone(_caminhoExecutavel, Tipo);
            }

            if (Icone == null && Janelas != null && Janelas.Count > 0)
            {
                foreach (var j in Janelas)
                {
                    if (j.Hwnd != IntPtr.Zero)
                    {
                        var iconJanela = _iconService.ObterIconeJanela(j.Hwnd);
                        if (iconJanela != null)
                        {
                            Icone = iconJanela;
                            break;
                        }
                    }
                }
            }
        }
        catch { }
    }

    public ItemFixado ToModel(int ordem = 0)
    {
        return new ItemFixado
        {
            Id = Id,
            Titulo = Titulo,
            CaminhoOuUrl = CaminhoExecutavel,
            Tipo = Tipo,
            Ordem = ordem
        };
    }
}
