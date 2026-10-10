using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace GigaDock.Linux.DesktopProbe;

public sealed class ProbeWindow : Window
{
    private readonly ComboBox _monitors = new() { MinWidth = 130, MaxWidth = 180 };
    private readonly TextBlock _status = new() { Text = "Selecione um ambiente.", VerticalAlignment = VerticalAlignment.Center };
    private readonly StackPanel _controls = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly DispatcherTimer _hideTimer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    private readonly CheckBox _autoHide = new() { Content = "Recolher ao sair", VerticalAlignment = VerticalAlignment.Center };
    private Window? _diagnostics;
    private bool _collapsed;

    public ProbeWindow()
    {
        Title = "GigaDock — protótipo Linux";
        Width = 860;
        Height = 88;
        CanResize = false;
        WindowDecorations = WindowDecorations.None;
        Background = new SolidColorBrush(Color.Parse("#252B33"));
        // Janela normal: não impõe Topmost nem remove acesso pela barra do desktop.
        ShowInTaskbar = true;
        var row = new StackPanel { Spacing = 6, Margin = new Thickness(12, 8) };
        _controls.Children.Add(new TextBlock { Text = "GigaDock · teste", VerticalAlignment = VerticalAlignment.Center });
        foreach (var environment in new[] { "Trabalho", "Estudos", "Pessoal" })
            _controls.Children.Add(Button(environment, () => _status.Text = $"Ambiente: {environment} (demonstração)"));
        _controls.Children.Add(Button("Ajustes", ShowSettings));
        _controls.Children.Add(Button("Diagnóstico", ShowDiagnostics));
        _controls.Children.Add(Button("Sair", Close));
        row.Children.Add(new ScrollViewer { Content = _controls, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
        row.Children.Add(_status);
        Content = row;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { Expand(); e.Handled = true; }
        };
        PointerEntered += (_, _) => Expand();
        PointerExited += (_, _) => { if (_autoHide.IsChecked == true) _hideTimer.Start(); };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            if (IsActive || _diagnostics?.IsVisible == true || _autoHide.IsChecked != true) return;
            _collapsed = true;
            _controls.IsVisible = false;
            _status.Text = "GigaDock — passe o mouse para expandir";
            Height = 32;
            PlaceDock();
        };
        Opened += (_, _) => RefreshMonitors();
        ScalingChanged += (_, _) => PlaceDock();
        Screens.Changed += ScreensChanged;
        Closed += (_, _) =>
        {
            _hideTimer.Stop();
            Screens.Changed -= ScreensChanged;
            _diagnostics?.Close();
        };
    }

    private static Button Button(string label, Action action)
    {
        var button = new Button { Content = label };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => action();
        return button;
    }

    private void ScreensChanged(object? sender, EventArgs e) => RefreshMonitors();

    private void RefreshMonitors()
    {
        _monitors.ItemsSource = Screens.All.Select((screen, index) =>
            $"Monitor {index + 1}{(screen.IsPrimary ? " · principal" : "")}").ToArray();
        _monitors.SelectedIndex = Math.Max(0, Screens.All.ToList().FindIndex(s => s.IsPrimary));
        PlaceDock();
    }

    private void PlaceDock()
    {
        var screen = Screens.All.ElementAtOrDefault(_monitors.SelectedIndex) ?? Screens.Primary;
        if (screen == null) return;
        var area = screen.WorkingArea;
        var scale = screen.Scaling;
        // WorkingArea e Position são pixels; Width/Height são unidades lógicas.
        Width = Math.Min(860, Math.Max(1, area.Width / scale - 24));
        Position = new PixelPoint(area.X + Math.Max(0, (area.Width - (int)Math.Ceiling(Width * scale)) / 2),
            Math.Max(area.Y, area.Bottom - (int)Math.Ceiling(Height * scale) - (int)Math.Ceiling(12 * scale)));
    }

    private void Expand()
    {
        _hideTimer.Stop();
        if (!_collapsed) return;
        _collapsed = false;
        _controls.IsVisible = true;
        _status.Text = "Protótipo expandido.";
        Height = 88;
        PlaceDock();
    }

    private void ShowSettings()
    {
        var content = new StackPanel { Spacing = 10, Margin = new Thickness(12) };
        content.Children.Add(new TextBlock { Text = "Monitor da barra" });
        content.Children.Add(_monitors);
        content.Children.Add(_autoHide);
        content.Children.Add(new TextBlock { Text = "O recolhimento mantém uma faixa visível.\nNão reserva espaço no desktop.", TextWrapping = TextWrapping.Wrap });
        _monitors.SelectionChanged += MonitorSelected;
        var flyout = new Flyout { Content = content };
        flyout.Closed += (_, _) =>
        {
            _monitors.SelectionChanged -= MonitorSelected;
            content.Children.Clear();
        };
        flyout.ShowAt(_controls.Children.OfType<Button>().First(b => Equals(b.Content, "Ajustes")));
    }

    private void MonitorSelected(object? sender, SelectionChangedEventArgs e) => PlaceDock();

    public string CreateReport() => ProbeReport.EnvironmentSummary() + "\n" +
        $"Escala da janela: {RenderScaling}\nPosição solicitada: {Position}\n" +
        string.Join("\n", Screens.All.Select((s, i) => $"Monitor {i + 1}: limites={s.Bounds}; área útil={s.WorkingArea}; escala={s.Scaling}")) +
        "\nPosição efetiva, foco, reserva de área e convivência com painéis exigem conferência no desktop.";

    private void ShowDiagnostics()
    {
        if (_diagnostics != null) { _diagnostics.Activate(); return; }
        _diagnostics = new Window { Title = "Diagnóstico do protótipo", Width = 680, Height = 480 };
        var report = new TextBox { Text = CreateReport(), IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(16) };
        AutomationProperties.SetName(report, "Relatório do ambiente e monitores");
        _diagnostics.Content = report;
        _diagnostics.Closed += (_, _) => _diagnostics = null;
        _diagnostics.Show(this);
    }
}
