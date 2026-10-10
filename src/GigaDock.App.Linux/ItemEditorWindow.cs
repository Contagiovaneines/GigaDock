using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.App.Linux;

internal sealed class ItemEditorWindow : Window
{
    public ItemEditorWindow(LinuxApplicationSession session, Action changed, string? collection = null)
    {
        Title = "Fixar item — GigaDock"; Width = 620; Height = 650; MinWidth = 420; MinHeight = 400;
        Background = Visuals.Brush("#191C21"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Spacing = 10, Margin = new Thickness(20) };
        panel.Children.Add(Visuals.Text("Aplicativos instalados", 20));
        var search = new TextBox { PlaceholderText = "Buscar aplicativo…" }; AutomationProperties.SetName(search, "Buscar aplicativo instalado"); panel.Children.Add(search);
        var apps = new ListBox { Height = 180 }; AutomationProperties.SetName(apps, "Aplicativos disponíveis"); panel.Children.Add(apps);
        var title = new TextBox { PlaceholderText = "Nome do item", MaxLength = 200 };
        var target = new TextBox { PlaceholderText = "Caminho absoluto ou URL HTTP/HTTPS" };
        AutomationProperties.SetName(title, "Nome do item"); AutomationProperties.SetName(target, "Caminho ou endereço do item");
        var types = new[] { "Aplicativo", "Arquivo", "Pasta", "Site" };
        var type = new ComboBox { ItemsSource = types, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetName(type, "Tipo do item");
        panel.Children.Add(Visuals.Text("Ou informe um item", 16)); panel.Children.Add(type); panel.Children.Add(title); panel.Children.Add(target);
        var message = Visuals.Text("", 12, "#A9B8C9"); panel.Children.Add(message);
        panel.Children.Add(Visuals.Button("Fixar no ambiente", () =>
        {
            try
            {
                var selected = (apps.SelectedItem as ListBoxItem)?.Tag as DesktopApplication;
                session.AddItem(new ItemFixado { Titulo = title.Text ?? "", CaminhoOuUrl = target.Text ?? "", Tipo = (TipoItem)type.SelectedIndex,
                    IconeCustomizado = selected is not null && selected.DesktopFile == target.Text ? selected.Icon : null }, collection);
                changed(); Close();
            }
            catch (Exception error) { message.Text = error.Message; }
        }));
        panel.Children.Add(Visuals.Button("Cancelar", Close));
        panel.Children.Add(Visuals.Button("Abrir sem fixar", async () =>
        {
            try { await new LinuxLauncher().LaunchAsync(new ItemFixado { Titulo = title.Text ?? "", CaminhoOuUrl = target.Text ?? "", Tipo = (TipoItem)type.SelectedIndex }); }
            catch (Exception error) { message.Text = error.Message; }
        }));
        Content = new ScrollViewer { Content = panel };
        IReadOnlyList<DesktopApplication> catalog = [];
        void Filter()
        {
            var text = search.Text ?? "";
            apps.ItemsSource = catalog.Where(app => app.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase))
                .Select(app => new ListBoxItem { Content = app.Name, Tag = app }).ToArray();
        }
        search.TextChanged += (_, _) => Filter();
        apps.SelectionChanged += (_, _) =>
        {
            if (apps.SelectedItem is not ListBoxItem { Tag: DesktopApplication app }) return;
            title.Text = app.Name; target.Text = app.DesktopFile; type.SelectedIndex = 0;
        };
        Opened += async (_, _) =>
        {
            if (!OperatingSystem.IsLinux()) { message.Text = "A prévia Windows não possui o catálogo de aplicativos Linux."; return; }
            catalog = await Task.Run(() => new DesktopCatalog().Scan());
            Filter(); message.Text = catalog.Count == 0 ? "Nenhum aplicativo encontrado. Informe um caminho ou URL." : $"{catalog.Count} aplicativos disponíveis.";
        };
    }
}

