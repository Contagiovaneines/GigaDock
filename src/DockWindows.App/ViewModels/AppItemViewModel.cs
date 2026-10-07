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
    private int _numeroNotificacoes;
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
        set
        {
            if (SetProperty(ref _estaAtivo, value))
            {
                if (value) 
                {
                    NumeroNotificacoes = 0;
                    OnPropertyChanged(nameof(NumeroNotificacoes)); // Força atualização para zerar widgets mesmo se já for 0
                }
            }
        }
    }

        public int QuantidadeJanelas
    {
        get => _quantidadeJanelas;
        set
        {
            int oldVal = _quantidadeJanelas;
            if (SetProperty(ref _quantidadeJanelas, value))
            {
                OnPropertyChanged(nameof(TemMultiplasJanelas));
                OnPropertyChanged(nameof(TextoDica));

                // Detecção Heurística de Ligação do WhatsApp
                if (value > oldVal && value > 1)
                {
                    string t = (Titulo ?? "").ToLowerInvariant();
                    string exec = (CaminhoExecutavel ?? "").ToLowerInvariant();
                    if (t.Contains("whatsapp") || exec.Contains("whatsapp"))
                    {
                        // WhatsApp abriu uma segunda janela. Normalmente é a janela de chamada!
                        // Aciona um evento ou dispara o alerta global via Messenger/Delegates
                        // Aqui não temos referência direta ao MainViewModel, então podemos usar um Action opcional ou Application.Current.MainWindow.DataContext
                        try {
                            var mw = System.Windows.Application.Current?.MainWindow;
                            if (mw != null && mw.DataContext != null)
                            {
                                var mvmType = mw.DataContext.GetType();
                                var dispararMethod = mvmType.GetMethod("DispararAlertaGlobal");
                                if (dispararMethod != null)
                                {
                                    // Invoca DispararAlertaGlobal("#25D366", true) para WhatsApp
                                    dispararMethod.Invoke(mw.DataContext, new object[] { "#25D366", true });
                                }
                            }
                        } catch { }
                    }
                }
            }
        }
    }

    public int NumeroNotificacoes
    {
        get => _numeroNotificacoes;
        set
        {
            if (SetProperty(ref _numeroNotificacoes, value))
            {
                OnPropertyChanged(nameof(TemNotificacao));
            }
        }
    }

    public bool TemNotificacao => _numeroNotificacoes > 0;

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

        var janela = Janelas.FirstOrDefault(j => j.EstaAtiva)
                     ?? Janelas.FirstOrDefault(j => !j.EstaMinimizada)
                     ?? Janelas.FirstOrDefault();

        if (janela != null)
        {
            if (!_windowService.AtivarJanela(janela.Hwnd))
            {
                // A janela rastreada pode ter sido substituída entre a enumeração e
                // o clique. Atualiza o estado e tenta iniciar o item fixado.
                SincronizarJanelas(Janelas.Where(j => j.Hwnd != janela.Hwnd).ToList());
                if (EstaFixado) _onExecutar(this);
            }
            MenuJanelasAberto = false;
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
        if (janela != null && _windowService.FecharJanela(janela.Hwnd))
        {
            SincronizarJanelas(Janelas.Where(j => j.Hwnd != janela.Hwnd).ToList());
        }
    }

    private void FecharTodasJanelas()
    {
        var restantes = Janelas.ToList();
        foreach (var jan in Janelas.ToList())
        {
            if (_windowService.FecharJanela(jan.Hwnd)) restantes.RemoveAll(j => j.Hwnd == jan.Hwnd);
        }
        SincronizarJanelas(restantes);
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



