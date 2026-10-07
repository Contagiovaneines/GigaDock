using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.Views;

public partial class AppGalleryWindow : Window
{
    private readonly IIconExtractionService _iconService;
    private readonly List<ItemFixado> _itensJaFixados;
    private List<AppGalleryItemViewModel> _catalogoCompleto = new();

    public List<ItemFixado> ItensSelecionadosFinal { get; private set; } = new();

    private readonly System.Threading.CancellationTokenSource _fechamento = new();
    public AppGalleryWindow(IIconExtractionService iconService, IEnumerable<ItemFixado> itensFixadosAtuais)
    {
        InitializeComponent();
        Closed += (_, _) => { _fechamento.Cancel(); _fechamento.Dispose(); };
        _iconService = iconService;
        _itensJaFixados = itensFixadosAtuais.ToList();
        ItensSelecionadosFinal.AddRange(_itensJaFixados);

        CarregarAppsAsync();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true; // Sempre retorna true para salvar o que foi ativado/desativado
        Close();
    }

    private async void CarregarAppsAsync()
    {
        try
        {
            TxtTotalBadge.Text = "Carregando biblioteca...";
            var instalados = await InstalledAppsScanner.ListarAsync();
            if (_fechamento.IsCancellationRequested) return;
        
        _catalogoCompleto = instalados.Select(app =>
        {
            var vm = new AppGalleryItemViewModel
            {
                Nome = app.Nome,
                CaminhoExecucao = app.CaminhoExecucao,
                ParsingName = app.ParsingName,
                EhAppModerno = app.EhAppModerno
            };
            
            // Verifica se este app já está no ambiente atual
            vm.JaAdicionado = _itensJaFixados.Any(fixado => 
                string.Equals(fixado.CaminhoOuUrl, app.CaminhoExecucao, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fixado.Titulo, app.Nome, StringComparison.OrdinalIgnoreCase));
            
            return vm;
        }).ToList();

        // Carregar ícones em background (para não travar a abertura inicial)
        _ = CarregarIconesAsync(_fechamento.Token);

            TxtTotalBadge.Text = $"{_catalogoCompleto.Count} ITENS";
            AplicarFiltros();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_fechamento.IsCancellationRequested)
            {
                TxtTotalBadge.Text = "Não foi possível carregar os aplicativos";
                MessageBox.Show(this, ex.Message, "Galeria de aplicativos", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async Task CarregarIconesAsync(System.Threading.CancellationToken token)
    {
        try
        {
            foreach (var item in _catalogoCompleto)
            {
                if (token.IsCancellationRequested) break;
                await Dispatcher.InvokeAsync(() => { if (!token.IsCancellationRequested) item.Icone = _iconService.ObterIcone(item.CaminhoExecucao, TipoItem.Aplicativo); }, System.Windows.Threading.DispatcherPriority.Background, token);
            }
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private void TxtBusca_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_catalogoCompleto == null) return;
        var termo = TxtBusca.Text.ToLowerInvariant();
        var filtrados = string.IsNullOrWhiteSpace(termo) 
            ? _catalogoCompleto 
            : _catalogoCompleto.Where(x => x.Nome.IndexOf(termo, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(filtrados);
        ListaApps.ItemsSource = view;
    }

    private void BtnToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AppGalleryItemViewModel item)
        {
            item.JaAdicionado = !item.JaAdicionado;

            if (item.JaAdicionado)
            {
                // Adiciona na lista final
                if (!ItensSelecionadosFinal.Any(x => x.CaminhoOuUrl == item.CaminhoExecucao))
                {
                    ItensSelecionadosFinal.Add(new ItemFixado
                    {
                        Id = Guid.NewGuid().ToString(),
                        Titulo = item.Nome,
                        CaminhoOuUrl = item.CaminhoExecucao,
                        Tipo = TipoItem.Aplicativo
                    });
                }
            }
            else
            {
                // Remove da lista final
                ItensSelecionadosFinal.RemoveAll(x => 
                    string.Equals(x.CaminhoOuUrl, item.CaminhoExecucao, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Titulo, item.Nome, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}

public class AppGalleryItemViewModel : ObservableObject
{
    private bool _jaAdicionado;
    private ImageSource? _icone;

    public string Nome { get; init; } = string.Empty;
    public string CaminhoExecucao { get; init; } = string.Empty;
    public string ParsingName { get; init; } = string.Empty;
    public bool EhAppModerno { get; init; }

    public string CaminhoCurto
    {
        get
        {
            if (EhAppModerno) return "Microsoft Store App / UWP";
            if (CaminhoExecucao.Length > 60) return CaminhoExecucao.Substring(0, 57) + "...";
            return CaminhoExecucao;
        }
    }

    public ImageSource? Icone
    {
        get => _icone;
        set => SetProperty(ref _icone, value);
    }

    public bool JaAdicionado
    {
        get => _jaAdicionado;
        set
        {
            if (SetProperty(ref _jaAdicionado, value))
            {
                OnPropertyChanged(nameof(TextoBotao));
                OnPropertyChanged(nameof(CorBotao));
                OnPropertyChanged(nameof(CorBordaBotao));
                OnPropertyChanged(nameof(CorTextoBotao));
            }
        }
    }

    public string TextoBotao => JaAdicionado ? "Desativar" : "+ Adicionar";
    public string CorBotao => JaAdicionado ? "Transparent" : "#0A84FF"; 
    public string CorBordaBotao => JaAdicionado ? "#1C1E26" : "#0A84FF";
    public string CorTextoBotao => JaAdicionado ? "#8E8E93" : "#FFFFFF";
}
