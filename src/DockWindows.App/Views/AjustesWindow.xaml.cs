using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views;

public partial class AjustesWindow : Window
{
    private readonly AjustesViewModel _viewModel;

    public AjustesWindow(AjustesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        SizeChanged += (_, _) => AtualizarLayoutResponsivo();
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Width = Math.Max(MinWidth, Math.Min(Width, area.Width - 32));
            Height = Math.Max(MinHeight, Math.Min(Height, area.Height - 32));
            AtualizarLayoutResponsivo();
        };
        _viewModel.PropertyChanged += SecaoAlterada;
        Closed += (_, _) =>
        {
            _viewModel.PropertyChanged -= SecaoAlterada;
            _viewModel.FecharJanela = null;
            _viewModel.MostrarAlerta = null;
            _viewModel.ConfirmarAcao = null;
            _viewModel.PedirTexto = null;
            _viewModel.AbrirDialogoItem = null;
        };

        _viewModel.FecharJanela = () =>
        {
            Close();
        };

        _viewModel.MostrarAlerta = (titulo, msg) =>
        {
            MessageBox.Show(this, msg, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
        };

        _viewModel.ConfirmarAcao = (titulo, msg) =>
        {
            return MessageBox.Show(this, msg, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        };

        _viewModel.PedirTexto = (titulo, prompt) =>
        {
            var dlg = new InputPromptDialog(titulo, prompt) { Owner = this };
            return dlg.ShowDialog() == true ? dlg.ValorResultante : null;
        };

        _viewModel.AbrirDialogoItem = itemExistente =>
        {
            var dlg = new ItemEditDialog(itemExistente) { Owner = this };
            return dlg.ShowDialog() == true ? dlg.ItemResultante : null;
        };

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        };
    }

    private void SecaoAlterada(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(AjustesViewModel.SecaoAtiva)) AtualizarLayoutResponsivo();
    }

    private void AtualizarLayoutResponsivo()
    {
        if (_viewModel == null) return;
        bool compacto = ActualWidth < 1050;
        bool empilhado = ActualWidth < 760;
        bool lista = _viewModel.EhSecaoAmbientes || _viewModel.EhSecaoWidgets || _viewModel.EhSecaoEspacadores;
        NavigationPanel.Visibility = compacto ? Visibility.Collapsed : Visibility.Visible;
        CompactNavigation.Visibility = compacto ? Visibility.Visible : Visibility.Collapsed;
        CompactHeaderRow.Height = compacto ? GridLength.Auto : new GridLength(0);
        NavigationColumn.Width = compacto ? new GridLength(0) : new GridLength(210);
        MasterPanel.Visibility = lista ? Visibility.Visible : Visibility.Collapsed;
        MasterColumn.Width = lista && !empilhado ? new GridLength(compacto ? 245 : 290) : new GridLength(0);
        StackedDetailsRow.Height = empilhado && lista ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        LayoutRoot.RowDefinitions[1].Height = empilhado && lista ? new GridLength(210) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(DetailsPanel, empilhado && lista ? 2 : 1);
        Grid.SetColumn(MasterPanel, empilhado ? 2 : 1);
        Grid.SetColumn(DetailsPanel, lista ? 2 : 1);
        Grid.SetColumnSpan(DetailsPanel, lista ? 1 : 2);
        Grid.SetRow(VisualizationsPanel, 1);
        Grid.SetColumn(VisualizationsPanel, 1);
        Grid.SetColumnSpan(VisualizationsPanel, 2);
    }
}
