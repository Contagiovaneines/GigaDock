using System.Windows;

namespace DockWindows.App.Views;

public partial class InputPromptDialog : Window
{
    public InputPromptDialog(string titulo, string prompt, string valorPadrao = "")
    {
        InitializeComponent();
        TxtTitulo.Text = titulo;
        TxtPrompt.Text = prompt;
        TxtValor.Text = valorPadrao;
        TxtValor.SelectAll();
        Loaded += (s, e) => TxtValor.Focus();
    }

    public string ValorResultante => TxtValor.Text;

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtValor.Text))
        {
            MessageBox.Show("Por favor, informe um valor não vazio.", "Dock Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
