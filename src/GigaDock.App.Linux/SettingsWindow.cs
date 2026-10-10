using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;
namespace GigaDock.App.Linux;
public sealed partial class SettingsWindow : Window
{
    private readonly LinuxApplicationSession _session;
    private readonly Action _changed;
    private readonly Action _diagnostics;
    private readonly StackPanel _master = new() { Spacing = 12, Margin = new Thickness(18) };
    private readonly StackPanel _details = new() { Spacing = 16, Margin = new Thickness(24) };
    private readonly TextBlock _active = Visuals.Text("");
    private readonly Dictionary<string, Button> _navigation = new();
    private string _section = "Ambientes";
    public SettingsWindow(LinuxApplicationSession session, Action changed, Action diagnostics)
    {
        _session = session; _changed = changed; _diagnostics = diagnostics;
        Title = "GigaDock — Ajustes e Configurações"; Width = 1180; Height = 760; MinWidth = 760; MinHeight = 500;
        Background = Visuals.Brush("#191C21"); Foreground = Brushes.White; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("230,330,*") };
        var sidebar = new DockPanel();
        var header = new StackPanel { Spacing = 12, Margin = new Thickness(18) };
        var identity = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        identity.Children.Add(new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(10), Background = Visuals.Brush("#625AF0"), Child = new Image { Width = 26, Height = 26, Source = new Bitmap(AssetLoader.Open(new Uri("avares://GigaDock/Assets/gigadock.png"))) } });
        var brand = new StackPanel(); brand.Children.Add(Visuals.Text("GigaDock", 15)); brand.Children.Add(Visuals.Text("Versão " + typeof(SettingsWindow).Assembly.GetName().Version?.ToString(3), 11, "#A9B8C9")); identity.Children.Add(brand);
        header.Children.Add(identity); header.Children.Add(Visuals.Card(_active)); DockPanel.SetDock(header, Dock.Top); sidebar.Children.Add(header);
        var search = new TextBox { PlaceholderText = "Buscar configurações…", MaxLength = 100 };
        AutomationProperties.SetName(search, "Buscar configurações"); header.Children.Add(search);
        var results = new StackPanel { Spacing = 4 }; header.Children.Add(results);
        search.TextChanged += (_, _) =>
        {
            results.Children.Clear();
            foreach (var entry in SettingsSearch.Find(search.Text ?? ""))
                results.Children.Add(Visuals.Button(entry.Title, () => { SelectSection(entry.LinuxSection); search.Text = ""; }));
            if (!string.IsNullOrWhiteSpace(search.Text) && results.Children.Count == 0)
                results.Children.Add(Visuals.Text("Nenhuma seção encontrada.", 12, "#A9B8C9"));
        };
        var finish = Visuals.Button("Concluir e Fechar", Close); finish.Background = Visuals.Brush("#0A84FF"); finish.Margin = new Thickness(18);
        DockPanel.SetDock(finish, Dock.Bottom); sidebar.Children.Add(finish);
        var nav = new StackPanel { Spacing = 4 };
        nav.Children.Add(new TextBlock { Text = "PRINCIPAL", FontSize = 10, FontWeight = FontWeight.Bold, Foreground = Visuals.Brush("#92A6BC"), Margin = new Thickness(16, 8, 0, 4) });
        var sections = new[] { ("Ambientes", "#0A84FF"), ("Widgets", "#FF9F0A"), ("Pokédex", "#72D99C"), ("Divisores", "#32D74B"), ("Aparência", "#BF5AF2"), ("Visualizações", "#7EA8E5"), ("Geral", "#64D2FF"), ("Utilitários", "#FF453A"), ("Sobre", "#94A3B8") };
        foreach (var (name, color) in sections)
        {
            var label = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            label.Children.Add(Visuals.Icon(name, color, 16)); label.Children.Add(Visuals.Text(name));
            if (name == "Pokédex") label.Children.Add(new Border { Background = Visuals.Brush("#4A2016"), CornerRadius = new CornerRadius(6), Padding = new Thickness(5, 2), Child = Visuals.Text("BETA", 8, "#FF9A75") });
            var button = Visuals.Button(name, () => SelectSection(name), label); button.HorizontalAlignment = HorizontalAlignment.Stretch; button.HorizontalContentAlignment = HorizontalAlignment.Left; button.Margin = new Thickness(10, 2);
            nav.Children.Add(button); _navigation.Add(name, button);
        }
        sidebar.Children.Add(new ScrollViewer { Content = nav });
        grid.Children.Add(new Border { Background = Visuals.Brush("#272C33"), BorderBrush = Visuals.Brush("#424954"), BorderThickness = new Thickness(0, 0, 1, 0), Child = sidebar });
        var master = new Border { Background = Visuals.Brush("#20242B"), BorderBrush = Visuals.Brush("#424954"), BorderThickness = new Thickness(0, 0, 1, 0), Child = new ScrollViewer { Content = _master } }; Grid.SetColumn(master, 1); grid.Children.Add(master);
        var details = new ScrollViewer { Content = _details }; Grid.SetColumn(details, 2); grid.Children.Add(details); Content = grid;
        SelectSection("Ambientes");
    }
    internal void SelectSection(string name) { _section = name; RefreshSection(); }
    internal void RefreshSection()
    {
        _active.Text = "Ativo: " + _session.ActiveEnvironment.Nome;
        foreach (var (name, button) in _navigation) { button.Background = Visuals.Brush(name == _section ? "#303D50" : "#00272C33"); button.BorderBrush = Visuals.Brush(name == _section ? "#4B6A94" : "#00272C33"); }
        _master.Children.Clear(); _details.Children.Clear();
        _master.Children.Add(Visuals.Text(_section, 22)); _details.Children.Add(Visuals.Text(_section, 24));
        if (_section == "Ambientes")
        {
            _master.Children.Add(Visuals.Text("Organize seu espaço", 12, "#A9B8C9"));
            foreach (var env in _session.Preferences.Ambientes)
            {
                var id = env.Id; var button = Visuals.Button(env.Nome, () => { try { _session.SelectEnvironment(id); _changed(); RefreshSection(); } catch { _details.Children.Add(Visuals.Text("Não foi possível salvar o ambiente.", color: "#FF8D86")); } });
                button.Background = Visuals.Brush(env.Id == _session.Preferences.AmbienteAtivoId ? "#303D50" : "#202026"); button.BorderBrush = Visuals.Brush(env.CorHex); button.HorizontalAlignment = HorizontalAlignment.Stretch; _master.Children.Add(button);
            }
            _details.Children.Add(Visuals.Card(Visuals.Text(_session.ActiveEnvironment.Nome, 20)));
            _details.Children.Add(Visuals.Text("Aplicativos e itens fixados", 16));
            if (_session.ActiveEnvironment.Itens.Count == 0) _details.Children.Add(Visuals.Text("Este ambiente ainda não possui itens fixados.", color: "#A9B8C9"));
            BuildItems();
        }
        else if (_section == "Aparência") BuildAppearance();
        else if (_section == "Widgets" || _section == "Pokédex") BuildWidgets();
        else if (_section == "Divisores") BuildDividers();
        else if (_section == "Visualizações") BuildDisplay();
        else if (_section == "Utilitários") BuildUtilities();
        else if (_section == "Sobre" || _section == "Geral")
        {
            _master.Children.Add(Visuals.Text("Configurações locais", color: "#A9B8C9"));
            _details.Children.Add(Visuals.Card(Visuals.Text("GigaDock\nTrabalho, Estudos e Pessoal.\nSeus dados ficam neste computador.", 16)));
            _details.Children.Add(Visuals.Button("Diagnóstico", _diagnostics));
            if (_section == "Geral") BuildGeneral();
        }
        else
        {
            _master.Children.Add(Visuals.Text("Disponível em breve", color: "#A9B8C9"));
            _details.Children.Add(Visuals.Card(Visuals.Text("Esta seção ainda não está disponível na versão Linux.", color: "#A9B8C9")));
        }
    }
    private void BuildAppearance()
    {
        var prefs = _session.Preferences;
        var draftTheme = prefs.EstiloTema;
        var preview = new Border { Height = 64, CornerRadius = new CornerRadius(prefs.RaioCantosDock), Background = draftTheme == EstiloTema.Areia ? Visuals.AreiaBackground() : Visuals.Brush(TemaDefinicao.ObterPorEstilo(draftTheme).FundoDockColor),
            BorderBrush = Visuals.Brush("#657084"), BorderThickness = new Thickness(1), Padding = new Thickness(12), Child = Visuals.Text("Prévia · aplicativos · widgets", 14) };
        ((TextBlock)preview.Child!).Foreground = Visuals.Brush(TemaDefinicao.ObterPorEstilo(draftTheme).TextoPrincipalColor);
        _details.Children.Add(preview);
        foreach (var theme in TemaDefinicao.ObterTemasPredefinidos())
        {
            var label = new StackPanel { Spacing = 6 }; label.Children.Add(new Border { Height = 42, Background = Visuals.Brush(theme.CorMiniatura), BorderBrush = Visuals.Brush(theme.CorBordaMiniatura), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Child = new TextBlock { Text = theme.Nome, FontSize = 15, Foreground = Visuals.Brush(theme.TextoPrincipalColor) } });
            var button = Visuals.Button("Prévia do tema " + theme.Nome, () => { draftTheme = theme.Estilo; preview.Background = theme.Estilo == EstiloTema.Areia ? Visuals.AreiaBackground() : Visuals.Brush(theme.FundoDockColor); ((TextBlock)preview.Child!).Foreground = Visuals.Brush(theme.TextoPrincipalColor); }, label);
            button.HorizontalAlignment = HorizontalAlignment.Stretch; button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            if (prefs.EstiloTema == theme.Estilo) button.BorderBrush = Visuals.Brush("#0A84FF"); _master.Children.Add(button);
        }
        _details.Children.Add(Visuals.Text("Personalize sua dock", 16));
        var height = Slider("Altura da barra", 48, 120, Math.Clamp(prefs.AlturaBarra, 48, 120));
        var opacity = Slider("Opacidade", 35, 100, Math.Clamp(prefs.OpacidadeDock * 100, 35, 100));
        var radius = Slider("Cantos arredondados", 0, 40, Math.Clamp(prefs.RaioCantosDock, 0, 40));
        var motion = new CheckBox { Content = "Reduzir animações", IsChecked = prefs.DesativarAnimacoes }; _details.Children.Add(motion);
        var decoration = new ComboBox { ItemsSource = Enum.GetValues<DecoracaoDock>(), SelectedItem = prefs.DecoracaoDock, HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetName(decoration, "Decoração opcional"); _details.Children.Add(Visuals.Text("Decoração opcional", 14)); _details.Children.Add(decoration);
        height.PropertyChanged += (_, e) => { if (e.Property == Avalonia.Controls.Slider.ValueProperty) preview.Height = height.Value; };
        radius.PropertyChanged += (_, e) => { if (e.Property == Avalonia.Controls.Slider.ValueProperty) preview.CornerRadius = new CornerRadius(radius.Value); };
        opacity.PropertyChanged += (_, e) => { if (e.Property == Avalonia.Controls.Slider.ValueProperty) preview.Opacity = opacity.Value / 100; };
        var status = Visuals.Text("", 12, "#A9B8C9");
        var save = Visuals.Button("Aplicar aparência", () => { try { _session.Update(candidate => { candidate.EstiloTema = draftTheme; candidate.AlturaBarra = height.Value; candidate.OpacidadeDock = opacity.Value / 100; candidate.RaioCantosDock = radius.Value; candidate.DesativarAnimacoes = motion.IsChecked == true; candidate.DecoracaoDock = decoration.SelectedItem is DecoracaoDock selected ? selected : DecoracaoDock.Nenhuma; }); _changed(); status.Text = "Aparência salva neste computador."; } catch { status.Text = "Não foi possível salvar. Confira as permissões da pasta de configurações."; } });
        save.Background = Visuals.Brush("#0A84FF"); _details.Children.Add(save); _details.Children.Add(status);
        _details.Children.Add(Visuals.Button("Cancelar prévia", RefreshSection));
        _details.Children.Add(Visuals.Text("Transparência disponível conforme o ambiente gráfico. O desfoque do fundo ainda não está disponível.", 12, "#A9B8C9"));
    }
    private Slider Slider(string label, double min, double max, double value)
    {
        var panel = new StackPanel { Spacing = 8 }; var text = Visuals.Text($"{label}: {value:0}"); panel.Children.Add(text);
        var slider = new Slider { Minimum = min, Maximum = max, Value = value, TickFrequency = 1, IsSnapToTickEnabled = true };
        AutomationProperties.SetName(slider, label); slider.PropertyChanged += (_, e) => { if (e.Property == Avalonia.Controls.Slider.ValueProperty) text.Text = $"{label}: {slider.Value:0}"; };
        panel.Children.Add(slider); _details.Children.Add(Visuals.Card(panel)); return slider;
    }
}

