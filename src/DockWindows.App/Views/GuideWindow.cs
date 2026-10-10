using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DockWindows.Core.Models;

#if GIGADOCK_INSTALLER
namespace DockWindows.Installer.Views;
#else
namespace DockWindows.App.Views;
#endif

/// <summary>Offline welcome guide shared by the app and installer.</summary>
public sealed class GuideWindow : Window
{
    private readonly Action<string>? _navigate;
    private readonly bool _animate;
    private readonly StackPanel _body = new();
    private readonly WrapPanel _steps = new();
    private readonly TextBlock _progress = Text("", 12, "#ADBBD0");
    private readonly Button _back;
    private readonly Button _next;
    private int _step;

    public GuideWindow(Action<string>? navigate = null, bool reduceMotion = false)
    {
        _navigate = navigate; _animate = !reduceMotion && SystemParameters.ClientAreaAnimation;
        Title = "Conhecer o GigaDock"; Width = 760; Height = 650; MinWidth = 360; MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.SingleBorderWindow; ResizeMode = ResizeMode.CanResize;
        Background = Brush("#101722"); Foreground = Brushes.White; FontFamily = new FontFamily("Segoe UI");
        UseLayoutRounding = true; WindowScreenSizing.Attach(this);
        Resources.Add(typeof(Button), ButtonStyle());
        var root = new DockPanel { LastChildFill = true, Margin = new Thickness(24) };
        var header = new StackPanel(); DockPanel.SetDock(header, Dock.Top);
        header.Children.Add(Text("GIGADOCK  /  DO SEU JEITO", 11, "#A7BAFF"));
        var intro = Text("Um espaço para cada momento.", 27, "#FFFFFF"); intro.FontWeight = FontWeights.SemiBold;
        intro.Margin = new Thickness(0, 8, 0, 8); header.Children.Add(intro);
        header.Children.Add(Text("Explore a dock em cinco passos. Você pode voltar a este guia quando quiser.", 13, "#ADBBD0"));
        _steps.Margin = new Thickness(0, 18, 0, 18); header.Children.Add(_steps); root.Children.Add(header);
        var footer = new StackPanel { Margin = new Thickness(0, 16, 0, 0) }; DockPanel.SetDock(footer, Dock.Bottom);
        footer.Children.Add(_progress);
        var actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        _back = CreateButton("Voltar", () => ChangeStep(_step - 1)); actions.Children.Add(_back);
        if (navigate is not null)
            actions.Children.Add(CreateButton("Abrir esta seção", () => { _navigate?.Invoke(GuideSteps.All[_step].WindowsSection); Close(); }));
        _next = CreateButton("Próximo →", () => { if (_step == GuideSteps.All.Count - 1) Close(); else ChangeStep(_step + 1); }, true);
        actions.Children.Add(_next); actions.Children.Add(CreateButton("Fechar guia", Close)); footer.Children.Add(actions);
        root.Children.Add(footer);
        root.Children.Add(new ScrollViewer { Content = _body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        Content = root;
        KeyDown += (_, key) =>
        {
            if (key.Key == Key.Escape) Close();
            if (key.Key == Key.Right) { ChangeStep(_step + 1); key.Handled = true; }
            if (key.Key == Key.Left) { ChangeStep(_step - 1); key.Handled = true; }
        };
        ChangeStep(0);
    }

    private void ChangeStep(int step)
    {
        if (step < 0 || step >= GuideSteps.All.Count) return;
        _step = step; var current = GuideSteps.All[step]; _steps.Children.Clear();
        for (var i = 0; i < GuideSteps.All.Count; i++)
        {
            var index = i; var button = CreateButton($"{i + 1}  {ShortName(i)}", () => ChangeStep(index), i == step);
            AutomationProperties.SetName(button, $"Passo {i + 1}: {GuideSteps.All[i].Title}"); _steps.Children.Add(button);
        }
        _body.Children.Clear();
        var preview = new Border { CornerRadius = new CornerRadius(20), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 20), MinHeight = 150,
            Background = new LinearGradientBrush(Color.FromRgb(35, 50, 84), Color.FromRgb(49, 34, 70), 25), Child = Preview(step) };
        _body.Children.Add(preview);
        var title = Text(current.Title, 24, "#FFFFFF"); title.FontWeight = FontWeights.SemiBold; _body.Children.Add(title);
        var description = Text(current.Description, 14, "#BDCADB"); description.Margin = new Thickness(0, 10, 0, 14); _body.Children.Add(description);
        _body.Children.Add(new Border { Background = Brush("#1C293B"), Padding = new Thickness(14), CornerRadius = new CornerRadius(12), Child = Text(Tip(step), 12, "#BDD9FF") });
        _progress.Text = $"Passo {step + 1} de {GuideSteps.All.Count} · {ShortName(step)}";
        _back.IsEnabled = step > 0; _next.Content = step == GuideSteps.All.Count - 1 ? "Concluir ✓" : "Próximo →";
        if (_animate) _body.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(180)));
    }

    private static StackPanel Preview(int step)
    {
        var panel = new StackPanel(); panel.Children.Add(Text("PRÉVIA ILUSTRATIVA", 10, "#B9C9E7"));
        var tiles = new WrapPanel { Margin = new Thickness(0, 16, 0, 10) };
        string[] labels = step switch
        {
            0 => ["▦ Trabalho", "▤ Estudos", "✦ Pessoal"],
            1 => ["▣ Apps", "▱ Pastas", "↗ Sites", "▦ Coleções"],
            2 => ["◷ 12:04", "25:00 Foco", "✓ Tarefas", "✎ Notas"],
            3 => ["● Midnight", "● Papel", "● Neon", "✦ Decorações"],
            _ => ["⌂ Dados locais", "✓ Sem conta", "▱ Seu controle"]
        };
        foreach (var label in labels)
            tiles.Children.Add(new Border { Background = Brush("#D91B2234"), CornerRadius = new CornerRadius(13), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 8, 8), BorderBrush = Brush("#536487"), BorderThickness = new Thickness(1), Child = Text(label, 14, "#FFFFFF") });
        panel.Children.Add(tiles); panel.Children.Add(Text("A configuração real é feita nos Ajustes; esta prévia não altera seus dados.", 11, "#C6D1E6"));
        return panel;
    }
    private static string ShortName(int step) => new[] { "Ambientes", "Apps", "Widgets", "Visual", "Privacidade" }[step];
    private static string Tip(int step) => new[]
    {
        "Dica: duplique um ambiente para começar com os mesmos itens e personalizar a cópia.",
        "Dica: organize os itens fixados; a sequência é salva para cada ambiente.",
        "Dica: clique em um widget para abrir seu painel. Alguns serviços precisam de configuração.",
        "Dica: temas e decorações são opcionais. As opções variam entre Windows e Linux.",
        "Dica: o GigaDock convive com a barra do sistema. Recursos online dependem dos serviços consultados."
    }[step];
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
    private static TextBlock Text(string text, double size, string color) => new() { Text = text, FontSize = size, Foreground = Brush(color), TextWrapping = TextWrapping.Wrap };
    private static Button CreateButton(string label, Action action, bool primary = false)
    {
        var button = new Button { Content = label, Background = Brush(primary ? "#5476EA" : "#243148"), Foreground = Brushes.White, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(14, 10, 14, 10), MinHeight = 40 };
        button.Click += (_, _) => action(); return button;
    }
    private static Style ButtonStyle()
    {
        var style = new Style(typeof(Button)); var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border)); border.Name = "Surface";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.BorderBrushProperty, Brush("#B8CEFF")); border.SetValue(Border.BorderThicknessProperty, new Thickness(0));
        border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        var content = new FrameworkElementFactory(typeof(ContentPresenter)); content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content); template.VisualTree = border;
        var hover = new Trigger { Property = IsMouseOverProperty, Value = true }; hover.Setters.Add(new Setter(OpacityProperty, 0.85)); template.Triggers.Add(hover);
        var focus = new Trigger { Property = IsKeyboardFocusedProperty, Value = true }; focus.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "Surface")); template.Triggers.Add(focus);
        style.Setters.Add(new Setter(Control.TemplateProperty, template)); style.Setters.Add(new Setter(CursorProperty, Cursors.Hand));
        var disabled = new Trigger { Property = IsEnabledProperty, Value = false }; disabled.Setters.Add(new Setter(OpacityProperty, 0.4)); style.Triggers.Add(disabled);
        return style;
    }
}
