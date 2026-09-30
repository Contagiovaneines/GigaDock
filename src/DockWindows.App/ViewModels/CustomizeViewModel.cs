using System.Collections.ObjectModel;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.App.ViewModels;

public class CustomizeViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;
    private readonly ISettingsRepository _repo;
    private readonly Preferencias _workingPrefs;

    private Ambiente? _ambienteSelecionado;
    private ItemFixado? _itemSelecionado;

    public CustomizeViewModel(MainViewModel mainVm, ISettingsRepository repo)
    {
        _mainVm = mainVm;
        _repo = repo;

        // Trabalha com um clone independente para permitir Live Preview e Cancelar sem efeitos colaterais
        _workingPrefs = _mainVm.Preferencias.Clonar();

        Secoes = new ObservableCollection<ConfigSecaoDock>();
        foreach (var sec in _workingPrefs.OrdemSecoes.OrderBy(s => s.Ordem))
        {
            sec.PropertyChanged += (o, e) => AtualizarPreview();
            Secoes.Add(sec);
        }

        AppsPermanentes = new ObservableCollection<ItemFixado>(_workingPrefs.AppsPermanentes.OrderBy(a => a.Ordem));

        Ambientes = new ObservableCollection<Ambiente>(_workingPrefs.Ambientes);
        ItensDoAmbiente = new ObservableCollection<ItemFixado>();
        AmbienteSelecionado = Ambientes.FirstOrDefault(a => a.Id == _workingPrefs.AmbienteAtivoId)
                           ?? Ambientes.FirstOrDefault();
        CarregarItensDoAmbiente();

        SalvarCommand = new RelayCommand(SalvarEAplicar);
        CancelarCommand = new RelayCommand(() => FecharJanela?.Invoke());
        RestaurarPadraoCommand = new RelayCommand(RestaurarAmbientePadrao);

        MoverSecaoCimaCommand = new RelayCommand(MoverSecaoCima, () => SecaoSelecionada != null && Secoes.IndexOf(SecaoSelecionada) > 0);
        MoverSecaoBaixoCommand = new RelayCommand(MoverSecaoBaixo, () => SecaoSelecionada != null && Secoes.IndexOf(SecaoSelecionada) < Secoes.Count - 1);
        RestaurarOrdemSecoesPadraoCommand = new RelayCommand(RestaurarOrdemSecoesPadrao);

        MoverAppCimaCommand = new RelayCommand(MoverAppCima, () => AppPermanenteSelecionado != null && AppsPermanentes.IndexOf(AppPermanenteSelecionado) > 0);
        MoverAppBaixoCommand = new RelayCommand(MoverAppBaixo, () => AppPermanenteSelecionado != null && AppsPermanentes.IndexOf(AppPermanenteSelecionado) < AppsPermanentes.Count - 1);
        RemoverAppPermanenteCommand = new RelayCommand(RemoverAppPermanente, () => AppPermanenteSelecionado != null);
        AdicionarAppPermanenteCommand = new RelayCommand(AdicionarAppPermanente);
        EditarAppPermanenteCommand = new RelayCommand(EditarAppPermanente, () => AppPermanenteSelecionado != null);
        RestaurarAppsPadraoCommand = new RelayCommand(RestaurarAppsPadrao);

        MoverItemCimaCommand = new RelayCommand(MoverItemCima, () => ItemSelecionado != null && ItensDoAmbiente.IndexOf(ItemSelecionado) > 0);
        MoverItemBaixoCommand = new RelayCommand(MoverItemBaixo, () => ItemSelecionado != null && ItensDoAmbiente.IndexOf(ItemSelecionado) < ItensDoAmbiente.Count - 1);
        RemoverItemCommand = new RelayCommand(RemoverItem, () => ItemSelecionado != null);
        AdicionarItemCommand = new RelayCommand(AdicionarItem);
        EditarItemCommand = new RelayCommand(EditarItem, () => ItemSelecionado != null);

        RestaurarBarraWindowsCommand = new RelayCommand(() =>
        {
            UsarComoBarraPrincipal = false;
            _mainVm.RestaurarBarraWindows();
        });
    }

    private ConfigSecaoDock? _secaoSelecionada;
    private ItemFixado? _appPermanenteSelecionado;

    public ObservableCollection<ConfigSecaoDock> Secoes { get; }
    public ObservableCollection<ItemFixado> AppsPermanentes { get; }

    public ConfigSecaoDock? SecaoSelecionada
    {
        get => _secaoSelecionada;
        set => SetProperty(ref _secaoSelecionada, value);
    }

    public ItemFixado? AppPermanenteSelecionado
    {
        get => _appPermanenteSelecionado;
        set => SetProperty(ref _appPermanenteSelecionado, value);
    }

    public bool AppsFixadosGlobais
    {
        get => _workingPrefs.AppsFixadosGlobais;
        set
        {
            if (_workingPrefs.AppsFixadosGlobais != value)
            {
                _workingPrefs.AppsFixadosGlobais = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<Ambiente> Ambientes { get; }
    public ObservableCollection<ItemFixado> ItensDoAmbiente { get; }

    public Ambiente? AmbienteSelecionado
    {
        get => _ambienteSelecionado;
        set
        {
            if (SetProperty(ref _ambienteSelecionado, value))
            {
                CarregarItensDoAmbiente();
                AtualizarPreview();
                OnPropertyChanged(nameof(RelogioHabilitado));
                OnPropertyChanged(nameof(PomodoroHabilitado));
                OnPropertyChanged(nameof(DuracaoFocoMinutos));
                OnPropertyChanged(nameof(DuracaoPausaCurtaMinutos));
                OnPropertyChanged(nameof(DuracaoPausaLongaMinutos));
            }
        }
    }

    public ItemFixado? ItemSelecionado
    {
        get => _itemSelecionado;
        set => SetProperty(ref _itemSelecionado, value);
    }

    // Seções
    public bool ExibirSeletorAmbientes
    {
        get => _workingPrefs.ExibirSeletorAmbientes;
        set
        {
            if (_workingPrefs.ExibirSeletorAmbientes != value)
            {
                _workingPrefs.ExibirSeletorAmbientes = value;
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    public bool ExibirItensFixados
    {
        get => _workingPrefs.ExibirItensFixados;
        set
        {
            if (_workingPrefs.ExibirItensFixados != value)
            {
                _workingPrefs.ExibirItensFixados = value;
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    public bool ExibirBotoesAcao
    {
        get => _workingPrefs.ExibirBotoesAcao;
        set
        {
            if (_workingPrefs.ExibirBotoesAcao != value)
            {
                _workingPrefs.ExibirBotoesAcao = value;
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    // Posição e Comportamento
    public bool UsarComoBarraPrincipal
    {
        get => _workingPrefs.UsarComoBarraPrincipal;
        set
        {
            if (_workingPrefs.UsarComoBarraPrincipal != value)
            {
                _workingPrefs.UsarComoBarraPrincipal = value;
                OnPropertyChanged();
            }
        }
    }

    public bool OcultarAutomaticamente
    {
        get => _workingPrefs.OcultarAutomaticamente;
        set
        {
            if (_workingPrefs.OcultarAutomaticamente != value)
            {
                _workingPrefs.OcultarAutomaticamente = value;
                OnPropertyChanged();
            }
        }
    }

    public bool SempreNoTopo
    {
        get => _workingPrefs.SempreNoTopo;
        set
        {
            if (_workingPrefs.SempreNoTopo != value)
            {
                _workingPrefs.SempreNoTopo = value;
                OnPropertyChanged();
            }
        }
    }

    public bool DesativarAnimacoes
    {
        get => _workingPrefs.DesativarAnimacoes;
        set
        {
            if (_workingPrefs.DesativarAnimacoes != value)
            {
                _workingPrefs.DesativarAnimacoes = value;
                OnPropertyChanged();
            }
        }
    }

    // Aparência
    public TemaModo Tema
    {
        get => _workingPrefs.Tema;
        set
        {
            if (_workingPrefs.Tema != value)
            {
                _workingPrefs.Tema = value;
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    public TamanhoIcone TamanhoIcones
    {
        get => _workingPrefs.TamanhoIcones;
        set
        {
            if (_workingPrefs.TamanhoIcones != value)
            {
                _workingPrefs.TamanhoIcones = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewIconeSize));
                AtualizarPreview();
            }
        }
    }

    public int EspacamentoItens
    {
        get => _workingPrefs.EspacamentoItens;
        set
        {
            if (_workingPrefs.EspacamentoItens != value)
            {
                _workingPrefs.EspacamentoItens = Math.Clamp(value, 2, 24);
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    public double OpacidadeDock
    {
        get => _workingPrefs.OpacidadeDock;
        set
        {
            if (Math.Abs(_workingPrefs.OpacidadeDock - value) > 0.01)
            {
                _workingPrefs.OpacidadeDock = Math.Clamp(value, 0.5, 1.0);
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    // Widgets do ambiente ativo
    public bool RelogioHabilitado
    {
        get => AmbienteSelecionado?.Widgets.RelogioHabilitado ?? true;
        set
        {
            if (AmbienteSelecionado != null && AmbienteSelecionado.Widgets.RelogioHabilitado != value)
            {
                AmbienteSelecionado.Widgets.RelogioHabilitado = value;
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    public bool PomodoroHabilitado
    {
        get => AmbienteSelecionado?.Widgets.PomodoroHabilitado ?? true;
        set
        {
            if (AmbienteSelecionado != null && AmbienteSelecionado.Widgets.PomodoroHabilitado != value)
            {
                AmbienteSelecionado.Widgets.PomodoroHabilitado = value;
                OnPropertyChanged();
                AtualizarPreview();
            }
        }
    }

    public int DuracaoFocoMinutos
    {
        get => AmbienteSelecionado?.Widgets.DuracaoFocoMinutos ?? 25;
        set
        {
            if (AmbienteSelecionado != null)
            {
                AmbienteSelecionado.Widgets.DuracaoFocoMinutos = Math.Clamp(value, 1, 120);
                OnPropertyChanged();
            }
        }
    }

    public int DuracaoPausaCurtaMinutos
    {
        get => AmbienteSelecionado?.Widgets.DuracaoPausaCurtaMinutos ?? 5;
        set
        {
            if (AmbienteSelecionado != null)
            {
                AmbienteSelecionado.Widgets.DuracaoPausaCurtaMinutos = Math.Clamp(value, 1, 60);
                OnPropertyChanged();
            }
        }
    }

    public int DuracaoPausaLongaMinutos
    {
        get => AmbienteSelecionado?.Widgets.DuracaoPausaLongaMinutos ?? 15;
        set
        {
            if (AmbienteSelecionado != null)
            {
                AmbienteSelecionado.Widgets.DuracaoPausaLongaMinutos = Math.Clamp(value, 1, 60);
                OnPropertyChanged();
            }
        }
    }

    // Propriedades do Live Preview
    public bool PreviewEhEscuro => Tema switch
    {
        TemaModo.Escuro => true,
        TemaModo.Claro => false,
        _ => _mainVm.EhTemaEscuro
    };

    public string PreviewFundoColor => PreviewEhEscuro ? "#E61C1C1E" : "#E6F5F5F7";
    public string PreviewBordaColor => PreviewEhEscuro ? "#30FFFFFF" : "#25000000";
    public string PreviewTextoColor => PreviewEhEscuro ? "#FFFFFF" : "#111113";
    public string PreviewCardColor => PreviewEhEscuro ? "#202024" : "#FFFFFF";
    public double PreviewIconeSize => (double)TamanhoIcones;

    public bool PreviewSecaoIniciarPesquisaVisivel => Secoes.FirstOrDefault(s => s.Tipo == TipoSecaoDock.IniciarPesquisa)?.Visivel ?? true;
    public bool PreviewSecaoAppsVisivel => Secoes.FirstOrDefault(s => s.Tipo == TipoSecaoDock.Apps)?.Visivel ?? true;
    public bool PreviewSecaoItensAmbienteVisivel => Secoes.FirstOrDefault(s => s.Tipo == TipoSecaoDock.ItensAmbiente)?.Visivel ?? true;
    public bool PreviewSecaoWidgetsVisivel => Secoes.FirstOrDefault(s => s.Tipo == TipoSecaoDock.Widgets)?.Visivel ?? true;
    public bool PreviewSecaoRelogioControlesVisivel => Secoes.FirstOrDefault(s => s.Tipo == TipoSecaoDock.RelogioControles)?.Visivel ?? true;

    // Callbacks
    public Action? FecharJanela { get; set; }
    public Func<string, string, bool>? ConfirmarAcao { get; set; }
    public Func<ItemFixado?, ItemFixado?>? AbrirDialogoItem { get; set; }

    // Comandos
    public ICommand SalvarCommand { get; }
    public ICommand CancelarCommand { get; }
    public ICommand RestaurarPadraoCommand { get; }

    public ICommand MoverSecaoCimaCommand { get; }
    public ICommand MoverSecaoBaixoCommand { get; }
    public ICommand RestaurarOrdemSecoesPadraoCommand { get; }

    public ICommand MoverAppCimaCommand { get; }
    public ICommand MoverAppBaixoCommand { get; }
    public ICommand RemoverAppPermanenteCommand { get; }
    public ICommand AdicionarAppPermanenteCommand { get; }
    public ICommand EditarAppPermanenteCommand { get; }
    public ICommand RestaurarAppsPadraoCommand { get; }

    public ICommand MoverItemCimaCommand { get; }
    public ICommand MoverItemBaixoCommand { get; }
    public ICommand RemoverItemCommand { get; }
    public ICommand AdicionarItemCommand { get; }
    public ICommand EditarItemCommand { get; }
    public ICommand RestaurarBarraWindowsCommand { get; }

    private void MoverSecaoCima()
    {
        if (SecaoSelecionada == null) return;
        int idx = Secoes.IndexOf(SecaoSelecionada);
        if (idx > 0)
        {
            Secoes.Move(idx, idx - 1);
            SincronizarOrdemSecoes();
        }
    }

    private void MoverSecaoBaixo()
    {
        if (SecaoSelecionada == null) return;
        int idx = Secoes.IndexOf(SecaoSelecionada);
        if (idx >= 0 && idx < Secoes.Count - 1)
        {
            Secoes.Move(idx, idx + 1);
            SincronizarOrdemSecoes();
        }
    }

    private void SincronizarOrdemSecoes()
    {
        for (int i = 0; i < Secoes.Count; i++)
        {
            Secoes[i].Ordem = i;
        }
        _workingPrefs.OrdemSecoes = Secoes.ToList();
        AtualizarPreview();
    }

    private void RestaurarOrdemSecoesPadrao()
    {
        Secoes.Clear();
        foreach (var s in Preferencias.CriarOrdemSecoesPadrao())
        {
            s.PropertyChanged += (o, e) => AtualizarPreview();
            Secoes.Add(s);
        }
        SincronizarOrdemSecoes();
    }

    private void MoverAppCima()
    {
        if (AppPermanenteSelecionado == null) return;
        int idx = AppsPermanentes.IndexOf(AppPermanenteSelecionado);
        if (idx > 0)
        {
            AppsPermanentes.Move(idx, idx - 1);
            SincronizarAppsPermanentes();
        }
    }

    private void MoverAppBaixo()
    {
        if (AppPermanenteSelecionado == null) return;
        int idx = AppsPermanentes.IndexOf(AppPermanenteSelecionado);
        if (idx >= 0 && idx < AppsPermanentes.Count - 1)
        {
            AppsPermanentes.Move(idx, idx + 1);
            SincronizarAppsPermanentes();
        }
    }

    private void RemoverAppPermanente()
    {
        if (AppPermanenteSelecionado == null) return;
        AppsPermanentes.Remove(AppPermanenteSelecionado);
        AppPermanenteSelecionado = null;
        SincronizarAppsPermanentes();
    }

    private void AdicionarAppPermanente()
    {
        var novo = AbrirDialogoItem?.Invoke(null);
        if (novo != null)
        {
            novo.Ordem = AppsPermanentes.Count;
            AppsPermanentes.Add(novo);
            SincronizarAppsPermanentes();
        }
    }

    private void EditarAppPermanente()
    {
        if (AppPermanenteSelecionado == null) return;
        var editado = AbrirDialogoItem?.Invoke(AppPermanenteSelecionado);
        if (editado != null)
        {
            AppPermanenteSelecionado.Titulo = editado.Titulo;
            AppPermanenteSelecionado.CaminhoOuUrl = editado.CaminhoOuUrl;
            AppPermanenteSelecionado.Tipo = editado.Tipo;
            AppPermanenteSelecionado.Argumentos = editado.Argumentos;
            SincronizarAppsPermanentes();
        }
    }

    private void RestaurarAppsPadrao()
    {
        AppsPermanentes.Clear();
        foreach (var app in Preferencias.CriarAppsPermanentesPadrao())
        {
            AppsPermanentes.Add(app);
        }
        SincronizarAppsPermanentes();
    }

    private void SincronizarAppsPermanentes()
    {
        for (int i = 0; i < AppsPermanentes.Count; i++)
        {
            AppsPermanentes[i].Ordem = i;
        }
        _workingPrefs.AppsPermanentes = AppsPermanentes.ToList();
        AtualizarPreview();
    }

    private void CarregarItensDoAmbiente()
    {
        ItensDoAmbiente.Clear();
        if (AmbienteSelecionado != null)
        {
            foreach (var item in AmbienteSelecionado.Itens.OrderBy(i => i.Ordem))
            {
                ItensDoAmbiente.Add(item);
            }
        }
    }

    private void SincronizarItensNoAmbiente()
    {
        if (AmbienteSelecionado != null)
        {
            for (int i = 0; i < ItensDoAmbiente.Count; i++)
            {
                ItensDoAmbiente[i].Ordem = i;
            }
            AmbienteSelecionado.Itens = ItensDoAmbiente.ToList();
            AtualizarPreview();
        }
    }

    private void MoverItemCima()
    {
        if (ItemSelecionado == null) return;
        int idx = ItensDoAmbiente.IndexOf(ItemSelecionado);
        if (idx > 0)
        {
            ItensDoAmbiente.Move(idx, idx - 1);
            SincronizarItensNoAmbiente();
        }
    }

    private void MoverItemBaixo()
    {
        if (ItemSelecionado == null) return;
        int idx = ItensDoAmbiente.IndexOf(ItemSelecionado);
        if (idx >= 0 && idx < ItensDoAmbiente.Count - 1)
        {
            ItensDoAmbiente.Move(idx, idx + 1);
            SincronizarItensNoAmbiente();
        }
    }

    private void RemoverItem()
    {
        if (ItemSelecionado == null) return;
        ItensDoAmbiente.Remove(ItemSelecionado);
        ItemSelecionado = null;
        SincronizarItensNoAmbiente();
    }

    private void AdicionarItem()
    {
        var novo = AbrirDialogoItem?.Invoke(null);
        if (novo != null)
        {
            novo.Ordem = ItensDoAmbiente.Count;
            ItensDoAmbiente.Add(novo);
            SincronizarItensNoAmbiente();
        }
    }

    private void EditarItem()
    {
        if (ItemSelecionado == null) return;
        var editado = AbrirDialogoItem?.Invoke(ItemSelecionado);
        if (editado != null)
        {
            ItemSelecionado.Titulo = editado.Titulo;
            ItemSelecionado.CaminhoOuUrl = editado.CaminhoOuUrl;
            ItemSelecionado.Tipo = editado.Tipo;
            ItemSelecionado.Argumentos = editado.Argumentos;
            SincronizarItensNoAmbiente();
            CarregarItensDoAmbiente();
        }
    }

    private void RestaurarAmbientePadrao()
    {
        if (AmbienteSelecionado == null) return;

        bool confirmou = ConfirmarAcao?.Invoke(
            "Restaurar Ambiente",
            $"Tem certeza que deseja restaurar as configurações e itens padrão do ambiente '{AmbienteSelecionado.Nome}'?") ?? false;

        if (confirmou)
        {
            _workingPrefs.RestaurarAmbientePadrao(AmbienteSelecionado.Id);
            CarregarItensDoAmbiente();
            AtualizarPreview();
        }
    }

    private void AtualizarPreview()
    {
        OnPropertyChanged(nameof(PreviewEhEscuro));
        OnPropertyChanged(nameof(PreviewFundoColor));
        OnPropertyChanged(nameof(PreviewBordaColor));
        OnPropertyChanged(nameof(PreviewTextoColor));
        OnPropertyChanged(nameof(PreviewCardColor));
        OnPropertyChanged(nameof(PreviewIconeSize));
        OnPropertyChanged(nameof(PreviewSecaoIniciarPesquisaVisivel));
        OnPropertyChanged(nameof(PreviewSecaoAppsVisivel));
        OnPropertyChanged(nameof(PreviewSecaoItensAmbienteVisivel));
        OnPropertyChanged(nameof(PreviewSecaoWidgetsVisivel));
        OnPropertyChanged(nameof(PreviewSecaoRelogioControlesVisivel));
    }

    private void SalvarEAplicar()
    {
        _workingPrefs.Ambientes = Ambientes.ToList();
        _workingPrefs.OrdemSecoes = Secoes.ToList();
        _workingPrefs.AppsPermanentes = AppsPermanentes.ToList();
        _mainVm.AtualizarPreferencias(_workingPrefs);
        FecharJanela?.Invoke();
    }
}
