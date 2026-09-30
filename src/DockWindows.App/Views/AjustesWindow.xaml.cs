using System.Windows;
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

        _viewModel.FecharJanela = () =>
        {
            DialogResult = true;
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
}
