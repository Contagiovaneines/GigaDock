using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;
namespace GigaDock.App.Linux;
public sealed class MainWindow : Window
{
    private readonly LinuxApplicationSession _session;
    private readonly Border _dock = new();
    private SettingsWindow? _settings;
    private Window? _diagnostics;
    private readonly WidgetPanels _widgets;
    private readonly Avalonia.Threading.DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly List<(TextBlock Text, WidgetInstanceConfig Widget, string Environment)> _liveWidgets = [];
    private readonly LinuxSystemServices _system = new();
    private int _sampleTick;
    private string? _draggedItem;
    private string? _draggedEnvironment;
    public MainWindow(LinuxApplicationSession session)
    {
        _session = session; _widgets = new WidgetPanels(session, this); Title = "GigaDock"; WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        _widgets.SummaryChanged += (id, text, tooltip) =>
        {
            foreach (var live in _liveWidgets.Where(live => live.Widget.Id == id))
            { live.Text.Text = text; ToolTip.SetTip(live.Text, tooltip); }
        };
        CanResize = false; ShowInTaskbar = false; Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent]; SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        _dock.Margin = new Thickness(20); _dock.Padding = new Thickness(10, 0); _dock.BorderThickness = new Thickness(1);
        _dock.BoxShadow = BoxShadows.Parse("0 5 20 0 #80000000"); Content = _dock;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F2) { ShowSettings(); e.Handled = true; }
            if (e.Key == Key.Escape)
            {
                foreach (var button in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(_dock).OfType<Button>())
                    if (button.Tag is Avalonia.Controls.Primitives.Popup popup) popup.IsOpen = false;
                e.Handled = true;
            }
            if (e.KeyModifiers == KeyModifiers.Control && e.Key is Key.PageDown or Key.PageUp)
            {
                try { _session.CycleEnvironment(e.Key == Key.PageDown ? 1 : -1); Refresh(); _settings?.RefreshSection(); }
                catch { ShowMessage("Não foi possível salvar a troca de ambiente."); }
                e.Handled = true;
            }
        };
        Opened += (_, _) => { Screens.Changed += OnScreensChanged; PlaceDock(); };
        SizeChanged += (_, _) => { if (IsVisible) Avalonia.Threading.Dispatcher.UIThread.Post(PlaceDock); };
        Closed += (_, _) => { Screens.Changed -= OnScreensChanged; _timer.Stop(); _settings?.Close(); _diagnostics?.Close(); };
        _timer.Tick += (_, _) =>
        {
            _widgets.Tick();
            SystemSnapshot? system = null;
            if (++_sampleTick % 3 == 0 && _liveWidgets.Any(live => live.Widget.Tipo is TipoWidget.Bateria or TipoWidget.Conectividade or TipoWidget.MonitorSistema))
                system = _system.Read();
            string? sensors = null;
            if (_sampleTick % 3 == 0 && _liveWidgets.Any(live => live.Widget.Tipo == TipoWidget.SensoresLinux)) sensors = _widgets.HardwareSummary();
            foreach (var live in _liveWidgets)
            {
                if (live.Widget.Tipo == TipoWidget.Relogio) live.Text.Text = DateTime.Now.ToString("HH:mm");
                else if (live.Widget.Tipo == TipoWidget.Pomodoro) live.Text.Text = _widgets.Pomodoro(live.Environment).TempoFormatado;
                else if (live.Widget.Tipo == TipoWidget.SensoresLinux && sensors is not null) live.Text.Text = sensors;
                else if (system is not null) live.Text.Text = live.Widget.Tipo switch
                {
                    TipoWidget.Bateria => system.Battery,
                    TipoWidget.Conectividade => system.Network,
                    TipoWidget.MonitorSistema => $"CPU {(system.CpuPercent.HasValue ? system.CpuPercent.Value.ToString("0") + "%" : "—")} · RAM {(system.MemoryPercent.HasValue ? system.MemoryPercent.Value.ToString("0") + "%" : "—")}",
                    _ => live.Text.Text
                };
            }
        };
        _timer.Start(); _session.Windows.Iniciar(); Refresh();
    }
    private void PlaceDock()
    {
        var index = _session.Preferences.MonitorDockLinux;
        var screen = index >= 0 && index < Screens.All.Count ? Screens.All[index] : Screens.Primary;
        if (screen is null) return;
        MaxWidth = Math.Max(1, screen.WorkingArea.Width / screen.Scaling);
        Position = new PixelPoint(screen.WorkingArea.X + (screen.WorkingArea.Width - (int)(Bounds.Width * RenderScaling)) / 2, screen.WorkingArea.Bottom - (int)(Bounds.Height * RenderScaling));
    }
    private void OnScreensChanged(object? sender, EventArgs args) => Avalonia.Threading.Dispatcher.UIThread.Post(PlaceDock);
    internal void Refresh()
    {
        if (Content is Grid previousSurface)
        {
            previousSurface.Children.Remove(_dock);
            Content = _dock;
        }
        _dock.Margin = new Thickness(20);
        Grid.SetRow(_dock, 0);
        if (_dock.Child is not null)
            foreach (var button in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(_dock.Child).OfType<Button>())
                if (button.Tag is Avalonia.Controls.Primitives.Popup popup) popup.IsOpen = false;
        var prefs = _session.Preferences; var theme = TemaDefinicao.ObterPorEstilo(prefs.EstiloTema);
        Topmost = prefs.SempreNoTopo; _liveWidgets.Clear();
        _dock.Height = Math.Clamp(prefs.AlturaBarra, 48, 120); _dock.CornerRadius = new CornerRadius(Math.Clamp(prefs.RaioCantosDock, 0, 40));
        _dock.Opacity = Math.Clamp(prefs.OpacidadeDock, .35, 1); _dock.Background = prefs.EstiloTema == EstiloTema.Areia ? Visuals.AreiaBackground() : Visuals.Brush(theme.FundoDockColor); _dock.BorderBrush = Visuals.Brush(theme.BordaDockColor);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Math.Clamp(prefs.EspacamentoItens, 0, 30), VerticalAlignment = VerticalAlignment.Center };
        var launcher = Visuals.Button("Buscar e abrir aplicativos", () => new ItemEditorWindow(_session, () => { Refresh(); _settings?.RefreshSection(); }).Show(this), Visuals.Icon("Aplicativo", theme.TextoPrincipalColor, size: 28));
        launcher.Classes.Add("dock"); row.Children.Add(launcher);
        foreach (var env in prefs.ExibirSeletorAmbientes ? prefs.Ambientes : [])
        {
            var id = env.Id;
            var button = Visuals.Button("Mudar para " + env.Nome, () => { try { _session.SelectEnvironment(id); Refresh(); _settings?.RefreshSection(); } catch { ShowMessage("Não foi possível salvar o ambiente. Confira as permissões da pasta de configurações."); } }, Visuals.Icon("Ambientes", env.Id == prefs.AmbienteAtivoId ? env.CorHex : "#9A9AA0", 26));
            button.Classes.Add("dock"); button.Width = 44; button.Height = 44; button.Padding = new Thickness(8); button.CornerRadius = new CornerRadius(12);
            button.PointerWheelChanged += (_, e) =>
            {
                if (e.Delta.Y == 0) return;
                try { _session.CycleEnvironment(e.Delta.Y < 0 ? 1 : -1); Refresh(); _settings?.RefreshSection(); }
                catch { ShowMessage("Não foi possível salvar a troca de ambiente."); }
                e.Handled = true;
            };
            var scale = new ScaleTransform(1, 1);
            button.RenderTransform = scale;
            button.RenderTransformOrigin = new RelativePoint(.5, .5, RelativeUnit.Relative);
            if (!prefs.DesativarAnimacoes)
            {
                scale.Transitions = new Avalonia.Animation.Transitions
                {
                    new Avalonia.Animation.DoubleTransition { Property = ScaleTransform.ScaleXProperty, Duration = TimeSpan.FromMilliseconds(120) },
                    new Avalonia.Animation.DoubleTransition { Property = ScaleTransform.ScaleYProperty, Duration = TimeSpan.FromMilliseconds(120) }
                };
                button.PointerEntered += (_, _) => { scale.ScaleX = 1.18; scale.ScaleY = 1.18; };
                button.PointerExited += (_, _) => { scale.ScaleX = 1; scale.ScaleY = 1; };
            }
            if (env.Id == prefs.AmbienteAtivoId) button.Background = Visuals.Brush("#22FFFFFF"); row.Children.Add(button);
        }
        row.Children.Add(new Border { Width = 1, Height = 28, Background = Visuals.Brush(prefs.EstiloTema == EstiloTema.Areia ? "#40A18D72" : "#35FFFFFF"), Margin = new Thickness(6, 0) });
        row.Children.Add(new TextBlock { Text = _session.ActiveEnvironment.Nome, Foreground = Visuals.Brush(theme.TextoPrincipalColor), VerticalAlignment = VerticalAlignment.Center });
        foreach (var item in prefs.AppsPermanentes.Concat(_session.ActiveEnvironment.Itens).OrderBy(item => item.Ordem))
        {
            var button = Visuals.Button(item.Titulo, async () =>
            {
                try { await new LinuxLauncher().LaunchAsync(item); }
                catch (Exception error) { ShowMessage(error.Message); }
            }, Visuals.ItemIcon(item, prefs.EstiloTema == EstiloTema.Areia ? theme.HighlightColor : "#D5DAE1"));
            button.Classes.Add("dock");
            if (prefs.EstiloTema == EstiloTema.Areia) { button.Background = Visuals.Brush("#FAFFFFFF"); button.BorderBrush = Visuals.Brush("#E2D8C9"); }
            row.Children.Add(button);
            if (_session.ActiveEnvironment.Itens.Any(existing => existing.Id == item.Id)) EnableItemDrag(button, item.Id);
            var remove = new MenuItem { Header = "Desafixar" };
            remove.Click += (_, _) => { try { _session.RemoveItem(item.Id); Refresh(); _settings?.RefreshSection(); } catch (Exception error) { ShowMessage(error.Message); } };
            button.ContextMenu = new ContextMenu { ItemsSource = new[] { remove } };
        }
        foreach (var collection in prefs.ColecoesGlobais.Concat(_session.ActiveEnvironment.Colecoes).OrderBy(c => c.Ordem))
        {
            var button = Visuals.Button(collection.Nome, () => { }, Visuals.Icon("Pasta", prefs.EstiloTema == EstiloTema.Areia ? theme.HighlightColor : "#D5DAE1", size: 28));
            var menu = collection.Itens.OrderBy(item => item.Ordem).Select(item =>
            {
                var entry = new MenuItem { Header = item.Titulo };
                entry.Click += async (_, _) => { try { await new LinuxLauncher().LaunchAsync(item); } catch (Exception error) { ShowMessage(error.Message); } };
                return entry;
            }).ToArray();
            button.Flyout = new MenuFlyout { ItemsSource = menu }; button.Classes.Add("dock"); row.Children.Add(button);
        }
        foreach (var divider in prefs.Espacadores.Where(d => d.Visivel).OrderBy(d => d.Ordem))
            row.Children.Add(new Border { Width = Math.Clamp(divider.Largura, 2, 40), Height = 28, BorderBrush = Visuals.Brush("#35FFFFFF"), BorderThickness = new Thickness(1, 0, 0, 0) });
        var pets = new List<WidgetInstanceConfig>();
        foreach (var widget in prefs.WidgetsGlobais.Concat(_session.ActiveEnvironment.WidgetsInstalados).Where(w => w.Visivel && LinuxWidgetCatalog.Supported.Contains(w.Tipo)).OrderBy(w => w.Ordem))
        {
            Control content;
            if (widget.Tipo == TipoWidget.MascotePokemon) { pets.Add(widget); continue; }
            else if (widget.Tipo is TipoWidget.Relogio or TipoWidget.Pomodoro ||
                widget.Formato == FormatoWidget.Expandido && widget.Tipo is TipoWidget.Bateria or TipoWidget.Conectividade or TipoWidget.MonitorSistema or TipoWidget.SensoresLinux or TipoWidget.AplicativosFlatpak or TipoWidget.WorkspacesLinux or TipoWidget.ScriptLocalLinux)
            {
                var text = Visuals.Text(widget.Tipo switch { TipoWidget.Relogio => DateTime.Now.ToString("HH:mm"), TipoWidget.Pomodoro => _widgets.Pomodoro(_session.ActiveEnvironment.Id).TempoFormatado, _ => _widgets.Summary(widget) }, widget.Formato == FormatoWidget.Expandido ? 13 : 18);
                text.MaxWidth = 180; text.MaxHeight = 40; text.TextTrimming = TextTrimming.CharacterEllipsis;
                _liveWidgets.Add((text, widget, _session.ActiveEnvironment.Id)); content = text;
            }
            else content = Visuals.Icon("Widgets", size: 28);
            var button = Visuals.Button(LinuxWidgetCatalog.Name(widget.Tipo), () => { }, content);
            button.Click += (_, _) => _widgets.Open(button, widget);
            if (prefs.EstiloTema == EstiloTema.Areia && widget.Tipo != TipoWidget.MascotePokemon) button.Background = Visuals.Brush(theme.FundoCardColor);
            button.Classes.Add("dock"); row.Children.Add(button);
        }
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        var add = Visuals.Button("Adicionar ao ambiente", () => new ItemEditorWindow(_session, () => { Refresh(); _settings?.RefreshSection(); }).Show(this), Visuals.Icon("Adicionar", theme.TextoPrincipalColor, size: 18));
        var settings = Visuals.Button("Ajustes e Personalização (F2)", () => ShowSettings(), Visuals.Icon("Geral", theme.TextoPrincipalColor, size: 18));
        foreach (var control in new[] { add, settings })
        {
            control.Classes.Add("dock"); control.Width = 32; control.Height = 32; control.Padding = new Thickness(0);
            control.CornerRadius = new CornerRadius(10); control.Background = Visuals.Brush(prefs.EstiloTema == EstiloTema.Areia ? "#24A18D72" : "#14FFFFFF"); actions.Children.Add(control);
        }
        row.Children.Add(actions);
        _dock.Child = new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, Content = row };
        var decorationColors = DecoracoesDock.Cores(prefs.DecoracaoDock);
        if (decorationColors.Length > 0)
        {
            var dockContent = _dock.Child!; _dock.Child = null;
            var surface = new Grid(); surface.Children.Add(dockContent);
            var lights = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false, Margin = new Thickness(12, 2) };
            for (var index = 0; index < 8; index++) lights.Children.Add(new Border { Width = 5, Height = 5, CornerRadius = new CornerRadius(3), Background = Visuals.Brush(decorationColors[index % decorationColors.Length]) });
            surface.Children.Add(lights); _dock.Child = surface;
        }
        if (pets.Count > 0)
        {
            var petHeight = pets.Any(p => int.TryParse(p.ObterConfiguracao("PokemonId", "1"), out var id) &&
                DockWindows.App.Common.PokemonLocomotion.KindFor(id) != DockWindows.App.Common.PokemonMovementKind.Ground) ? 60 : 44;
            var surface = new Grid { RowDefinitions = new RowDefinitions($"{petHeight},*") };
            var petLayer = new Grid { Height = petHeight, Margin = new Thickness(30, 0, 30, -2), IsHitTestVisible = false, ClipToBounds = true };
            foreach (var pet in pets)
                petLayer.Children.Add(new PokemonSprite(pet, prefs.DesativarAnimacoes || prefs.ModoEconomico, roam: true));
            _dock.Margin = new Thickness(20, 0, 20, 20);
            Content = null;
            Grid.SetRow(_dock, 1); surface.Children.Add(petLayer); surface.Children.Add(_dock); Content = surface;
        }
        var change = new MenuItem { Header = "Mudar de Ambiente" };
        change.ItemsSource = prefs.Ambientes.Select(env => { var item = new MenuItem { Header = env.Nome }; var id = env.Id; item.Click += (_, _) => { try { _session.SelectEnvironment(id); Refresh(); _settings?.RefreshSection(); } catch { ShowMessage("Não foi possível salvar o ambiente."); } }; return item; }).ToArray();
        var adjust = new MenuItem { Header = "Ajustes e Personalização..." }; adjust.Click += (_, _) => ShowSettings();
        var exit = new MenuItem { Header = "Sair do GigaDock" }; exit.Click += (_, _) => Close();
        _dock.ContextMenu = new ContextMenu { ItemsSource = new Control[] { change, new Separator(), adjust, exit } };
        if (IsVisible) Avalonia.Threading.Dispatcher.UIThread.Post(PlaceDock);
    }
    private void EnableItemDrag(Button button, string id)
    {
        ToolTip.SetTip(button, button.GetValue(Avalonia.Automation.AutomationProperties.NameProperty) + " · Alt + arrastar para reordenar");
        DragDrop.SetAllowDrop(button, true);
        button.AddHandler(DragDrop.DragOverEvent, (_, args) =>
        {
            args.DragEffects = _draggedItem is not null && _draggedEnvironment == _session.ActiveEnvironment.Id
                ? DragDropEffects.Move : DragDropEffects.None;
            args.Handled = true;
        });
        button.AddHandler(DragDrop.DropEvent, (_, args) =>
        {
            if (_draggedItem is null || _draggedEnvironment != _session.ActiveEnvironment.Id) return;
            try { _session.MoveItemBefore(_draggedItem, id); Refresh(); _settings?.RefreshSection(); }
            catch (Exception error) { ShowMessage(error.Message); }
            args.Handled = true;
        });
        button.AddHandler(InputElement.PointerPressedEvent, async (_, args) =>
        {
            if (args.KeyModifiers != KeyModifiers.Alt || !args.GetCurrentPoint(button).Properties.IsLeftButtonPressed) return;
            args.Handled = true;
            _draggedItem = id; _draggedEnvironment = _session.ActiveEnvironment.Id;
            try
            {
                var data = new DataTransfer(); data.Add(DataTransferItem.CreateText(id));
                await DragDrop.DoDragDropAsync(args, data, DragDropEffects.Move);
            }
            catch (Exception error) { ShowMessage(error.Message); }
            finally { _draggedItem = null; _draggedEnvironment = null; }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    internal SettingsWindow ShowSettings()
    {
        if (_settings is not null) { _settings.Activate(); return _settings; }
        _settings = new SettingsWindow(_session, Refresh, ShowDiagnostics); _settings.Closed += (_, _) => _settings = null; _settings.Show(this); return _settings;
    }
    private void ShowMessage(string message) => new Window { Title = "GigaDock", Width = 420, Height = 160, Content = Visuals.Text(message), Padding = new Thickness(20) }.Show(this);
    private void ShowDiagnostics()
    {
        if (_diagnostics is not null) { _diagnostics.Activate(); return; }
        _diagnostics = new Window { Title = "Diagnóstico do GigaDock", Width = 680, Height = 440, Content = new TextBox { Text = _session.Describe(), IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(16) } };
        _diagnostics.Closed += (_, _) => _diagnostics = null; _diagnostics.Show(this);
    }
}

