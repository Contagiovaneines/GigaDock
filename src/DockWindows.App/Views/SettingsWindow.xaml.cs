using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;

namespace DockWindows.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.FecharJanela = Close;

        SelecionarTemaNaCombo();
        SelecionarTamanhoNaCombo();
    }

    private void SelecionarTemaNaCombo()
    {
        foreach (ComboBoxItem item in CmbTema.Items)
        {
            if (item.Tag is TemaModo modo && modo == _viewModel.Tema)
            {
                CmbTema.SelectedItem = item;
                break;
            }
        }
    }

    private void SelecionarTamanhoNaCombo()
    {
        foreach (ComboBoxItem item in CmbTamanho.Items)
        {
            if (item.Tag is TamanhoIcone tam && tam == _viewModel.TamanhoIcones)
            {
                CmbTamanho.SelectedItem = item;
                break;
            }
        }
    }

    private void CmbTema_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTema?.SelectedItem is ComboBoxItem item && item.Tag is TemaModo modo)
        {
            _viewModel.Tema = modo;
        }
    }

    private void CmbTamanho_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTamanho?.SelectedItem is ComboBoxItem item && item.Tag is TamanhoIcone tam)
        {
            _viewModel.TamanhoIcones = tam;
        }
    }

    private void Cor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex && _viewModel.AmbienteSelecionado != null)
        {
            _viewModel.AmbienteSelecionado.CorHex = hex;
        }
    }
}
