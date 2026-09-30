using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;

namespace DockWindows.App.Views;

public partial class CustomizeWindow : Window
{
    private readonly CustomizeViewModel _viewModel;

    public CustomizeWindow(CustomizeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.FecharJanela = Close;
        _viewModel.ConfirmarAcao = (titulo, msg) =>
            System.Windows.MessageBox.Show(this, msg, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        _viewModel.AbrirDialogoItem = itemExistente =>
        {
            var dialog = new ItemEditDialog(itemExistente) { Owner = this };
            return dialog.ShowDialog() == true ? dialog.ItemResultante : null;
        };

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
}
