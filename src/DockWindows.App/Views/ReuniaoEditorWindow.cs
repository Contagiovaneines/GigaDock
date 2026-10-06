using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DockWindows.Core.Models;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views;

public class ReuniaoEditorWindow : Window
{
    public CompromissoLocal? Resultado { get; private set; }
    public ReuniaoEditorWindow(CompromissoLocal? atual)
    {
        Title = "Configurar reunião"; Width = 440; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = new SolidColorBrush(Color.FromRgb(24, 28, 31)); Foreground = Brushes.White;
        var root = new StackPanel { Margin = new Thickness(22) };
        root.Children.Add(new TextBlock { Text = "Reunião local", FontSize = 22 });
        TextBox Campo(string label, string text)
        {
            root.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 14, 0, 5) });
            var field = new TextBox { Text = text, Padding = new Thickness(8) };
            System.Windows.Automation.AutomationProperties.SetName(field, label);
            root.Children.Add(field); return field;
        }
        var title = Campo("Título", atual?.Titulo ?? "");
        var data = Campo("Data e hora (dd/MM/yyyy HH:mm)", (atual?.DataHora ?? DateTime.Now.AddHours(1)).ToString("dd/MM/yyyy HH:mm"));
        var link = Campo("Link HTTPS da reunião (opcional)", CalendarioWidgetViewModel.LinkReuniao(atual) ?? "");
        var error = new TextBlock { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) }; root.Children.Add(error);
        var save = new Button { Content = "Salvar reunião", IsDefault = true, Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 16, 0, 0) };
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(title.Text)) { error.Text = "Informe o título da reunião."; return; }
            if (!DateTime.TryParseExact(data.Text.Trim(), "dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var date)) { error.Text = "Use a data e hora no formato dd/MM/yyyy HH:mm."; return; }
            var value = new CompromissoLocal { Id = atual?.Id ?? Guid.NewGuid().ToString(), Titulo = title.Text.Trim(), DataHora = date, Local = link.Text.Trim(), Descricao = atual?.Descricao };
            if (!string.IsNullOrWhiteSpace(value.Local) && CalendarioWidgetViewModel.LinkReuniao(value) == null) { error.Text = "Informe um link HTTPS válido, sem usuário ou senha."; return; }
            Resultado = value; DialogResult = true;
        };
        root.Children.Add(save); root.Children.Add(new Button { Content = "Cancelar", IsCancel = true, Margin = new Thickness(0, 8, 0, 0), Padding = new Thickness(8) });
        Content = root;
    }
}
