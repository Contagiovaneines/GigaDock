using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class ColecaoAppViewModel : ObservableObject
{
    private readonly ColecaoApp _model;
    private readonly ILauncherService _launcher;
    private readonly IIconExtractionService _iconService;
    private readonly Action<ColecaoAppViewModel>? _onEditarColecao;
    private readonly Action<string>? _notificarErro;

    private bool _painelAberto;

    public ColecaoAppViewModel(
        ColecaoApp model,
        ILauncherService launcher,
        IIconExtractionService iconService,
        Action<ColecaoAppViewModel>? onEditarColecao = null,
        Action<string>? notificarErro = null,
        Action<ColecaoAppViewModel>? onRemoverColecao = null)
    {
        _model = model;
        _launcher = launcher;
        _iconService = iconService;
        _onEditarColecao = onEditarColecao;
        _notificarErro = notificarErro;

        Itens = new ObservableCollection<ItemViewModel>();
        RecarregarItens();

        AlternarPainelCommand = new RelayCommand(AlternarPainel);
        FecharPainelCommand = new RelayCommand(FecharPainel);
        ExecutarItemCommand = new RelayCommand<ItemViewModel>(ExecutarItem);
        EditarColecaoCommand = new RelayCommand(EditarColecao);
        RemoverColecaoCommand = new RelayCommand(() => onRemoverColecao?.Invoke(this));
    }

    public ColecaoApp Model => _model;
    public string Id => _model.Id;

    public string Nome
    {
        get => _model.Nome;
        set
        {
            if (_model.Nome != value)
            {
                _model.Nome = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoDica));
            }
        }
    }

    public string Icone
    {
        get => _model.Icone;
        set
        {
            if (_model.Icone != value)
            {
                _model.Icone = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EhGlobal
    {
        get => _model.EhGlobal;
        set
        {
            if (_model.EhGlobal != value)
            {
                _model.EhGlobal = value;
                OnPropertyChanged();
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

    public bool PainelAberto
    {
        get => _painelAberto;
        set => SetProperty(ref _painelAberto, value);
    }

    public ObservableCollection<ItemViewModel> Itens { get; }

    public int QuantidadeItens => Itens.Count;
    public string TextoDica => $"{Nome} ({Itens.Count} apps)\nClique para abrir coleção";

    public ICommand AlternarPainelCommand { get; }
    public ICommand FecharPainelCommand { get; }
    public ICommand ExecutarItemCommand { get; }
    public ICommand RemoverColecaoCommand { get; }
    public ICommand EditarColecaoCommand { get; }

    public void RecarregarItens()
    {
        Itens.Clear();
        foreach (var item in _model.Itens.OrderBy(i => i.Ordem))
        {
            var itemVm = new ItemViewModel(
                item,
                _launcher,
                _iconService,
                onEditar: vm => { },
                onRemover: vm => RemoverItem(vm),
                onMoverEsquerda: vm => { },
                onMoverDireita: vm => { },
                notificarErro: msg => _notificarErro?.Invoke(msg));

            Itens.Add(itemVm);
        }
        OnPropertyChanged(nameof(QuantidadeItens));
        OnPropertyChanged(nameof(TextoDica));
        OnPropertyChanged(nameof(TemItens));
        OnPropertyChanged(nameof(Miniatura1));
        OnPropertyChanged(nameof(Miniatura2));
        OnPropertyChanged(nameof(Miniatura3));
        OnPropertyChanged(nameof(Miniatura4));
    }

    public bool TemItens => Itens.Count > 0;
    public System.Windows.Media.ImageSource? Miniatura1 => Itens.Count > 0 ? Itens[0].Icone : null;
    public System.Windows.Media.ImageSource? Miniatura2 => Itens.Count > 1 ? Itens[1].Icone : null;
    public System.Windows.Media.ImageSource? Miniatura3 => Itens.Count > 2 ? Itens[2].Icone : null;
    public System.Windows.Media.ImageSource? Miniatura4 => Itens.Count > 3 ? Itens[3].Icone : null;

    public void AdicionarItem(ItemFixado item)
    {
        item.Ordem = _model.Itens.Count;
        _model.Itens.Add(item);
        RecarregarItens();
    }

    public void RemoverItem(ItemViewModel itemVm)
    {
        _model.Itens.Remove(itemVm.Model);
        RecarregarItens();
    }

    private void AlternarPainel()
    {
        PainelAberto = !PainelAberto;
    }

    private void FecharPainel()
    {
        PainelAberto = false;
    }

    private void ExecutarItem(ItemViewModel? itemVm)
    {
        if (itemVm == null) return;
        FecharPainel();
        itemVm.ExecutarCommand.Execute(null);
    }

    private void EditarColecao()
    {
        FecharPainel();
        _onEditarColecao?.Invoke(this);
    }
}


