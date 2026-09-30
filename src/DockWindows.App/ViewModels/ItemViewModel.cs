using System.Windows.Input;
using System.Windows.Media;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class ItemViewModel : ObservableObject
{
    private readonly ItemFixado _model;
    private readonly ILauncherService _launcher;
    private readonly IIconExtractionService _iconService;
    private readonly Action<ItemViewModel> _onEditar;
    private readonly Action<ItemViewModel> _onRemover;
    private readonly Action<ItemViewModel> _onMoverEsquerda;
    private readonly Action<ItemViewModel> _onMoverDireita;
    private readonly Action<string> _notificarErro;

    private ImageSource? _icone;

    public ItemViewModel(
        ItemFixado model,
        ILauncherService launcher,
        IIconExtractionService iconService,
        Action<ItemViewModel> onEditar,
        Action<ItemViewModel> onRemover,
        Action<ItemViewModel> onMoverEsquerda,
        Action<ItemViewModel> onMoverDireita,
        Action<string> notificarErro)
    {
        _model = model;
        _launcher = launcher;
        _iconService = iconService;
        _onEditar = onEditar;
        _onRemover = onRemover;
        _onMoverEsquerda = onMoverEsquerda;
        _onMoverDireita = onMoverDireita;
        _notificarErro = notificarErro;

        ExecutarCommand = new RelayCommand(Executar);
        AbrirLocalCommand = new RelayCommand(AbrirLocal);
        EditarCommand = new RelayCommand(() => _onEditar(this));
        RemoverCommand = new RelayCommand(() => _onRemover(this));
        MoverEsquerdaCommand = new RelayCommand(() => _onMoverEsquerda(this));
        MoverDireitaCommand = new RelayCommand(() => _onMoverDireita(this));
        MoverParaGlobalCommand = new RelayCommand(() => OnMoverParaGlobal?.Invoke(this));

        CarregarIcone();
    }

    public ItemFixado Model => _model;

    public string Id => _model.Id;

    public string Titulo
    {
        get => _model.Titulo;
        set
        {
            if (_model.Titulo != value)
            {
                _model.Titulo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoDica));
            }
        }
    }

    public string CaminhoOuUrl
    {
        get => _model.CaminhoOuUrl;
        set
        {
            if (_model.CaminhoOuUrl != value)
            {
                _model.CaminhoOuUrl = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoDica));
                CarregarIcone();
            }
        }
    }

    public TipoItem Tipo
    {
        get => _model.Tipo;
        set
        {
            if (_model.Tipo != value)
            {
                _model.Tipo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoTipo));
                CarregarIcone();
            }
        }
    }

    public int Ordem
    {
        get => _model.Ordem;
        set
        {
            if (_model.Ordem != value)
            {
                _model.Ordem = value;
                OnPropertyChanged();
            }
        }
    }

    public ImageSource? Icone
    {
        get => _icone;
        private set => SetProperty(ref _icone, value);
    }

    public string TextoTipo => Tipo switch
    {
        TipoItem.Aplicativo => "Aplicativo",
        TipoItem.Pasta => "Pasta",
        TipoItem.Arquivo => "Arquivo",
        TipoItem.WebUrl => "Endereço Web",
        _ => "Item"
    };

    public string TextoDica => $"{Titulo}\n{CaminhoOuUrl}";

    public Action<ItemViewModel>? OnMoverParaGlobal { get; set; }

    public ICommand ExecutarCommand { get; }
    public ICommand AbrirLocalCommand { get; }
    public ICommand EditarCommand { get; }
    public ICommand RemoverCommand { get; }
    public ICommand MoverEsquerdaCommand { get; }
    public ICommand MoverDireitaCommand { get; }
    public ICommand MoverParaGlobalCommand { get; }

    public void CarregarIcone()
    {
        try
        {
            Icone = _iconService.ObterIcone(_model);
        }
        catch
        {
            Icone = null;
        }
    }

    private void Executar()
    {
        var res = _launcher.Executar(_model);
        if (!res.Sucesso)
        {
            _notificarErro(res.MensagemErro ?? "Não foi possível abrir o item selecionado.");
        }
    }

    private void AbrirLocal()
    {
        var res = _launcher.AbrirLocal(_model);
        if (!res.Sucesso)
        {
            _notificarErro(res.MensagemErro ?? "Não foi possível abrir a pasta do item.");
        }
    }
}
