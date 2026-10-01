using System.Collections.ObjectModel;
using System.Linq;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class EnvironmentViewModel : ObservableObject
{
    private readonly Ambiente _model;
    private readonly ILauncherService _launcher;
    private readonly IIconExtractionService _iconService;
    private readonly Action<ItemViewModel> _onEditarItem;
    private readonly Action<ItemViewModel> _onRemoverItem;
    private readonly Action<ItemViewModel> _onMoverEsquerda;
    private readonly Action<ItemViewModel> _onMoverDireita;
    private readonly Action<string> _notificarErro;
    private readonly Action<ItemViewModel>? _onMoverParaGlobal;
    private readonly Action<ColecaoAppViewModel>? _onEditarColecao;
    private readonly Action<ColecaoAppViewModel>? _onRemoverColecao;

    private bool _estaAtivo;

    public EnvironmentViewModel(
        Ambiente model,
        ILauncherService launcher,
        IIconExtractionService iconService,
        Action<ItemViewModel> onEditarItem,
        Action<ItemViewModel> onRemoverItem,
        Action<ItemViewModel> onMoverEsquerda,
        Action<ItemViewModel> onMoverDireita,
        Action<string> notificarErro,
        Action<ItemViewModel>? onMoverParaGlobal = null,
        Action<ColecaoAppViewModel>? onEditarColecao = null, Action<ColecaoAppViewModel>? onRemoverColecao = null)
    {
        _model = model;
        _launcher = launcher;
        _iconService = iconService;
        _onEditarItem = onEditarItem;
        _onRemoverItem = onRemoverItem;
        _onMoverEsquerda = onMoverEsquerda;
        _onMoverDireita = onMoverDireita;
        _notificarErro = notificarErro;
        _onMoverParaGlobal = onMoverParaGlobal;
        _onEditarColecao = onEditarColecao;
        _onRemoverColecao = onRemoverColecao;

        Itens = new ObservableCollection<ItemViewModel>();
        Colecoes = new ObservableCollection<ColecaoAppViewModel>();
        WidgetsInstalados = new ObservableCollection<WidgetInstanceConfig>(_model.WidgetsInstalados.OrderBy(w => w.Ordem));

        RecarregarItens();
        RecarregarColecoes();
    }

    public Ambiente Model => _model;

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
            }
        }
    }

    public string CorHex
    {
        get => _model.CorHex;
        set
        {
            if (_model.CorHex != value)
            {
                _model.CorHex = value;
                OnPropertyChanged();
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

    public string CorIndicadorApps
    {
        get => _model.CorIndicadorApps;
        set
        {
            if (_model.CorIndicadorApps != value)
            {
                _model.CorIndicadorApps = value;
                OnPropertyChanged();
            }
        }
    }

    public string EstiloIndicadorApps
    {
        get => _model.EstiloIndicadorApps;
        set
        {
            if (_model.EstiloIndicadorApps != value)
            {
                _model.EstiloIndicadorApps = value;
                OnPropertyChanged();
            }
        }
    }

    public WidgetConfig Widgets => _model.Widgets;

    public bool EstaAtivo
    {
        get => _estaAtivo;
        set => SetProperty(ref _estaAtivo, value);
    }

    public ObservableCollection<ItemViewModel> Itens { get; }
    public ObservableCollection<ColecaoAppViewModel> Colecoes { get; }
    public ObservableCollection<WidgetInstanceConfig> WidgetsInstalados { get; }

    public void RecarregarItens()
    {
        Itens.Clear();
        foreach (var item in _model.Itens.OrderBy(i => i.Ordem))
        {
            var itemVm = new ItemViewModel(
                item,
                _launcher,
                _iconService,
                _onEditarItem,
                _onRemoverItem,
                _onMoverEsquerda,
                _onMoverDireita,
                _notificarErro)
            {
                OnMoverParaGlobal = _onMoverParaGlobal
            };
            Itens.Add(itemVm);
        }
    }

    public void RecarregarColecoes()
    {
        Colecoes.Clear();
        foreach (var col in _model.Colecoes.OrderBy(c => c.Ordem))
        {
            var colVm = new ColecaoAppViewModel(col, _launcher, _iconService, _onEditarColecao, _notificarErro, _onRemoverColecao);
            Colecoes.Add(colVm);
        }
    }

    public void SincronizarOrdemItens()
    {
        for (int i = 0; i < Itens.Count; i++)
        {
            Itens[i].Ordem = i;
        }
        _model.Itens = Itens.Select(vm => vm.Model).ToList();
    }

    public void SincronizarColecoes()
    {
        for (int i = 0; i < Colecoes.Count; i++)
        {
            Colecoes[i].Ordem = i;
        }
        _model.Colecoes = Colecoes.Select(vm => vm.Model).ToList();
    }
}

