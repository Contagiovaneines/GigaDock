using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views;

public partial class AjustesWindow : Window
{
    private readonly AjustesViewModel _viewModel;
    private void Minimizar_OnClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void Maximizar_OnClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }
    private void Fechar_OnClick(object sender, RoutedEventArgs e) => Close();
    private void ConhecerGigaDock_OnClick(object sender, RoutedEventArgs e) =>
        new GuideWindow(_viewModel.NavegarPara, _viewModel.DesativarAnimacoes) { Owner = this }.Show();

    private void BuscarConfiguracoes_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (ResultadosConfiguracoes is null || sender is not TextBox field) return;
        ResultadosConfiguracoes.Items.Clear();
        foreach (var entry in DockWindows.Core.Models.SettingsSearch.Find(field.Text))
            ResultadosConfiguracoes.Items.Add(new ListBoxItem { Content = entry.Title, Tag = entry.WindowsSection });
        ResultadosConfiguracoes.Visibility = string.IsNullOrWhiteSpace(field.Text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ResultadosConfiguracoes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultadosConfiguracoes.SelectedItem is not ListBoxItem { Tag: string section }) return;
        _viewModel.NavegarPara(section); BuscaConfiguracoes.Clear();
    }

    public AjustesWindow(AjustesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        WindowScreenSizing.Attach(this);
        StateChanged += (_, _) =>
        {
            var maximized = WindowState == WindowState.Maximized;
            MaximizeWindowButton.Content = maximized ? "❐" : "□";
            MaximizeWindowButton.ToolTip = maximized ? "Restaurar" : "Maximizar";
            System.Windows.Automation.AutomationProperties.SetName(MaximizeWindowButton, maximized ? "Restaurar ajustes" : "Maximizar ajustes");
        };
        SizeChanged += (_, _) => AtualizarLayoutResponsivo();
        Loaded += (_, _) =>
        {
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
        bool compacto = LayoutRoot.ActualWidth < 1050;
        bool empilhado = LayoutRoot.ActualWidth < 760;
        bool widgets = _viewModel.EhSecaoWidgets;
        bool lista = _viewModel.EhSecaoAmbientes || _viewModel.EhSecaoEspacadores;
        NavigationPanel.Visibility = compacto ? Visibility.Collapsed : Visibility.Visible;
        CompactNavigation.Visibility = compacto ? Visibility.Visible : Visibility.Collapsed;
        CompactHeaderRow.Height = compacto ? GridLength.Auto : new GridLength(0);
        NavigationColumn.Width = compacto ? new GridLength(0) : new GridLength(230);
        MasterPanel.Visibility = lista || widgets ? Visibility.Visible : Visibility.Collapsed;
        DetailsPanel.Visibility = widgets ? Visibility.Collapsed : Visibility.Visible;
        MasterColumn.Width = lista && !empilhado ? new GridLength(compacto ? 260 : 330) : new GridLength(0);
        StackedDetailsRow.Height = empilhado && lista ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        LayoutRoot.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
        Grid.SetRow(DetailsPanel, empilhado && lista ? 2 : 1);
        Grid.SetColumn(MasterPanel, widgets ? 1 : empilhado ? 2 : 1);
        Grid.SetColumnSpan(MasterPanel, widgets ? 2 : 1);
        Grid.SetColumn(DetailsPanel, lista ? 2 : 1);
        Grid.SetColumnSpan(DetailsPanel, lista ? 1 : 2);
        Grid.SetRow(VisualizationsPanel, 1);
        Grid.SetColumn(VisualizationsPanel, 1);
        Grid.SetColumnSpan(VisualizationsPanel, 2);
    }
}
