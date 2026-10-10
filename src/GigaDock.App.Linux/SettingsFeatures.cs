using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using DockWindows.Core.Models;
using DockWindows.Core.Widgets;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.App.Linux;

public sealed partial class SettingsWindow
{
    private void Apply(Action edit)
    {
        try { edit(); _changed(); RefreshSection(); }
        catch (Exception error) { _details.Children.Add(Visuals.Text(error.Message, color: "#FF8D86")); }
    }

    private void BuildItems()
    {
        var launcher = new LinuxLauncher();
        var add = Visuals.Button("+ Adicionar item", async () => await new ItemEditorWindow(_session, () => { _changed(); RefreshSection(); }).ShowDialog(this));
        add.Background = Visuals.Brush("#0A84FF"); _details.Children.Add(add);
        foreach (var item in _session.Preferences.AppsPermanentes.Concat(_session.ActiveEnvironment.Itens).OrderBy(item => item.Ordem))
        {
            var content = new StackPanel { Spacing = 6 }; content.Children.Add(Visuals.Text(item.Titulo, 15));
            var validation = LinuxLauncher.Validate(item);
            if (!validation.Valido) content.Children.Add(Visuals.Text(validation.MensagemErro ?? "Reassocie este item.", 11, "#FFB4A2"));
            var row = new WrapPanel();
            row.Children.Add(Visuals.Button("Abrir", async () => { try { await launcher.LaunchAsync(item); } catch (Exception error) { content.Children.Add(Visuals.Text(error.Message, 11, "#FFB4A2")); } }));
            if (_session.ActiveEnvironment.Itens.Any(existing => existing.Id == item.Id))
            {
                row.Children.Add(Visuals.Button("←", () => Apply(() => _session.MoveItem(item.Id, -1))));
                row.Children.Add(Visuals.Button("→", () => Apply(() => _session.MoveItem(item.Id, 1))));
            }
            row.Children.Add(Visuals.Button("Remover", () => Apply(() => _session.RemoveItem(item.Id))));
            content.Children.Add(row); _details.Children.Add(Visuals.Card(content));
        }
        _details.Children.Add(Visuals.Text("Coleções", 16));
        var name = new TextBox { PlaceholderText = "Nome da coleção", MaxLength = 60 }; _details.Children.Add(name);
        _details.Children.Add(Visuals.Button("+ Criar coleção", () => Apply(() =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) throw new ArgumentException("Informe um nome para a coleção.");
            _session.Update(prefs => prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).Colecoes.Add(new ColecaoApp { Nome = name.Text.Trim(), EhGlobal = false, Itens = [] }));
        })));
        foreach (var collection in _session.Preferences.ColecoesGlobais.Concat(_session.ActiveEnvironment.Colecoes))
        {
            var panel = new StackPanel { Spacing = 6 }; panel.Children.Add(Visuals.Text(collection.Nome, 16));
            panel.Children.Add(Visuals.Button("+ Item na coleção", async () => await new ItemEditorWindow(_session, () => { _changed(); RefreshSection(); }, collection.Id).ShowDialog(this)));
            foreach (var item in collection.Itens)
                panel.Children.Add(Visuals.Button("Remover " + item.Titulo, () => Apply(() => _session.RemoveItem(item.Id, collection.Id))));
            panel.Children.Add(Visuals.Button("Excluir coleção", () => Apply(() => _session.Update(prefs =>
            {
                prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).Colecoes.RemoveAll(c => c.Id == collection.Id);
                prefs.ColecoesGlobais.RemoveAll(c => c.Id == collection.Id);
            }))));
            _details.Children.Add(Visuals.Card(panel));
        }
        var environmentName = new TextBox { PlaceholderText = "Nome do ambiente", Text = _session.ActiveEnvironment.Nome, MaxLength = 60 };
        _master.Children.Add(environmentName);
        _master.Children.Add(Visuals.Button("Renomear ambiente", () => Apply(() =>
        {
            if (string.IsNullOrWhiteSpace(environmentName.Text)) throw new ArgumentException("Informe um nome.");
            _session.Update(prefs => prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).Nome = environmentName.Text.Trim());
        })));
        _master.Children.Add(Visuals.Button("+ Novo ambiente", () => Apply(() => _session.AddEnvironment("Novo ambiente"))));
        _master.Children.Add(Visuals.Button("Duplicar ambiente", () => Apply(() =>
            _session.DuplicateEnvironment(_session.ActiveEnvironment.Id, "Cópia de " + _session.ActiveEnvironment.Nome[..Math.Min(51, _session.ActiveEnvironment.Nome.Length)]))));
        var order = new WrapPanel();
        order.Children.Add(Visuals.Button("Mover ambiente para cima", () => Apply(() => _session.MoveEnvironment(_session.ActiveEnvironment.Id, -1))));
        order.Children.Add(Visuals.Button("Mover ambiente para baixo", () => Apply(() => _session.MoveEnvironment(_session.ActiveEnvironment.Id, 1))));
        _master.Children.Add(order);
        var color = new TextBox { Text = _session.ActiveEnvironment.CorHex, MaxLength = 7 };
        Avalonia.Automation.AutomationProperties.SetName(color, "Cor do ambiente em hexadecimal");
        _master.Children.Add(Visuals.Text("Cor do ambiente (#RRGGBB)", 12)); _master.Children.Add(color);
        _master.Children.Add(Visuals.Button("Aplicar cor do ambiente", () => Apply(() =>
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(color.Text ?? "", "^#[0-9a-fA-F]{6}$"))
                throw new ArgumentException("Informe uma cor no formato #RRGGBB.");
            _session.Update(prefs => prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).CorHex = color.Text!);
        })));
        var delete = Visuals.Button("Excluir ambiente", () => Apply(() => _session.Update(prefs =>
        {
            if (prefs.Ambientes.Count <= 1) throw new ArgumentException("Mantenha pelo menos um ambiente.");
            prefs.Ambientes.RemoveAll(a => a.Id == prefs.AmbienteAtivoId); prefs.AmbienteAtivoId = prefs.Ambientes[0].Id;
        })));
        delete.IsEnabled = _session.Preferences.Ambientes.Count > 1; _master.Children.Add(delete);
    }

    private void BuildWidgets()
    {
        _master.Children.Add(Visuals.Text("Cada ambiente tem seus widgets.", color: "#A9B8C9"));
        var search = new TextBox { PlaceholderText = "Buscar widget…" }; _master.Children.Add(search);
        var list = new StackPanel { Spacing = 10 }; _details.Children.Add(list);
        void Filter()
        {
            list.Children.Clear();
            var kinds = _section == "Pokédex" ? new[] { TipoWidget.MascotePokemon } : Enum.GetValues<TipoWidget>();
            foreach (var kind in kinds.Where(kind => WidgetStoreCatalog.Get(kind, WidgetPlatform.Linux).Name.Contains(search.Text ?? "", StringComparison.CurrentCultureIgnoreCase)))
            {
                var access = LinuxWidgetAvailability.Get(kind);
                var info = access.Entry;
                var active = _session.ActiveEnvironment.WidgetsInstalados.Any(widget => widget.Tipo == kind && widget.Visivel);
                var card = new StackPanel { Spacing = 8 };
                card.Children.Add(Visuals.Text(info.Name, 16));
                card.Children.Add(Visuals.Text(access.Status, 12, access.CanInstall ? "#72D99C" : "#EAC17F"));
                card.Children.Add(Visuals.Text(info.Description, 13, "#D5DAE1"));
                card.Children.Add(Visuals.Text("Como funciona", 12));
                card.Children.Add(Visuals.Text(info.HowTo, 12, "#C0C8D4"));
                card.Children.Add(Visuals.Text("Requisitos e limites", 12));
                card.Children.Add(Visuals.Text(access.Reason, 12, "#A9B3C2"));
                var checkbox = new CheckBox { Content = "Ativar na dock", IsChecked = active, IsEnabled = access.CanInstall || active };
                Avalonia.Automation.AutomationProperties.SetName(checkbox, "Ativar widget " + info.Name);
                checkbox.IsCheckedChanged += (_, _) => Apply(() => _session.SetWidget(kind, checkbox.IsChecked == true));
                card.Children.Add(checkbox);
                if (!access.CanInstall) card.Children.Add(Visuals.Text("Instalação bloqueada" + (active ? "; você pode desativar a instalação antiga." : "."), 12, "#EAC17F"));
                list.Children.Add(Visuals.Card(card));
                var installed = _session.ActiveEnvironment.WidgetsInstalados.FirstOrDefault(widget => widget.Tipo == kind);
                if (installed is not null && kind is TipoWidget.Relogio or TipoWidget.Pomodoro or TipoWidget.Bateria or TipoWidget.Conectividade or TipoWidget.MonitorSistema or TipoWidget.SensoresLinux or TipoWidget.AplicativosFlatpak or TipoWidget.WorkspacesLinux or TipoWidget.ScriptLocalLinux)
                {
                    var expanded = new CheckBox { Content = "Mostrar informações na barra", IsChecked = installed.Formato == FormatoWidget.Expandido };
                    expanded.IsCheckedChanged += (_, _) => Apply(() => _session.Update(prefs =>
                        prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).WidgetsInstalados.First(widget => widget.Id == installed.Id).Formato = expanded.IsChecked == true ? FormatoWidget.Expandido : FormatoWidget.Compacto));
                    list.Children.Add(expanded);
                }
            }
        }
        search.TextChanged += (_, _) => Filter(); Filter();
        if (_section == "Pokédex")
        {
            _details.Children.Add(Visuals.Text("Sprites PMDCollab com créditos preservados.", 12, "#A9B8C9"));
            var species = new NumericUpDown { Minimum = 1, Maximum = 151, Value = 1, FormatString = "0" }; _details.Children.Add(species);
            _details.Children.Add(Visuals.Button("Escolher Pokémon", () => Apply(() => _session.Update(prefs =>
            {
                var widget = prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.MascotePokemon);
                if (widget is null) throw new ArgumentException("Ative a Pokédex primeiro.");
                widget.DefinirConfiguracao("PokemonId", ((int)(species.Value ?? 1)).ToString());
            }))));
        }
    }

    private void BuildDividers()
    {
        _details.Children.Add(Visuals.Button("+ Adicionar divisor", () => Apply(() => _session.Update(prefs => prefs.Espacadores.Add(new EspacadorConfig { Ordem = prefs.Espacadores.Count })))));
        foreach (var divider in _session.Preferences.Espacadores)
            _details.Children.Add(Visuals.Button("Remover " + divider.Nome, () => Apply(() => _session.Update(prefs => prefs.Espacadores.RemoveAll(d => d.Id == divider.Id)))));
    }

    private void BuildDisplay()
    {
        _details.Children.Add(Visuals.Text("Tela da dock", 16));
        var screens = new ComboBox { ItemsSource = new[] { "Acompanhar tela principal" }.Concat(Screens.All.Select((screen, index) =>
            $"Tela {index + 1} — {screen.Bounds.Width} × {screen.Bounds.Height}")),
            SelectedIndex = _session.Preferences.MonitorDockLinux >= 0 && _session.Preferences.MonitorDockLinux < Screens.All.Count
                ? _session.Preferences.MonitorDockLinux + 1 : 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        Avalonia.Automation.AutomationProperties.SetName(screens, "Tela da dock"); _details.Children.Add(screens);
        _details.Children.Add(Visuals.Button("Aplicar tela da dock", () => Apply(() =>
            _session.Update(prefs => prefs.MonitorDockLinux = screens.SelectedIndex - 1))));
        var selector = new CheckBox { Content = "Mostrar seletor de ambientes", IsChecked = _session.Preferences.ExibirSeletorAmbientes };
        selector.IsCheckedChanged += (_, _) => Apply(() => _session.Update(prefs => prefs.ExibirSeletorAmbientes = selector.IsChecked == true)); _details.Children.Add(selector);
        var top = new CheckBox { Content = "Manter a dock acima das janelas", IsChecked = _session.Preferences.SempreNoTopo };
        top.IsCheckedChanged += (_, _) => Apply(() => _session.Update(prefs => prefs.SempreNoTopo = top.IsChecked == true)); _details.Children.Add(top);
        _details.Children.Add(Visuals.Text("A posição junto à borda e a sobreposição dependem do compositor. Área exclusiva, ocultação por tela cheia e miniaturas não são universais no Linux.", 12, "#A9B8C9"));
        var windows = _session.Windows;
        if (!windows.Capacidades.ListarJanelas)
            _details.Children.Add(Visuals.Text("Janelas abertas: disponível em X11 com wmctrl. Em Wayland, esta integração fica desativada.", 12, "#A9B8C9"));
        else
        {
            _details.Children.Add(Visuals.Button("Atualizar lista de janelas", RefreshSection));
            foreach (var window in windows.ObterJanelasAbertas())
            {
                var activate = Visuals.Button("Ativar " + window.Titulo, async () =>
                {
                    if (!await Task.Run(() => windows.Ativar(window.Id))) _details.Children.Add(Visuals.Text("O desktop não ativou a janela.", 12, "#FFB4A2"));
                });
                _details.Children.Add(activate);
            }
        }
    }

    private void BuildGeneral()
    {
        _details.Children.Add(Visuals.Button("Conhecer o GigaDock", () => new GuideWindow(SelectSection).Show(this)));
        if (!OperatingSystem.IsLinux()) return;
        var start = new CheckBox { Content = "Abrir ao entrar no Linux", IsChecked = LinuxAutostart.Enabled };
        var adjusting = false;
        start.IsCheckedChanged += (_, _) =>
        {
            if (adjusting) return;
            try { LinuxAutostart.Set(start.IsChecked == true, Environment.ProcessPath ?? ""); }
            catch (Exception error) { _details.Children.Add(Visuals.Text(error.Message, color: "#FF8D86")); }
            adjusting = true;
            start.IsChecked = LinuxAutostart.Enabled;
            adjusting = false;
        };
        _details.Children.Add(start);
        _details.Children.Add(Visuals.Text("A instalação e atualização são por usuário; os dados permanecem nas pastas XDG. Não há consulta automática a um servidor de atualização.", 12, "#A9B8C9"));
    }

    private void BuildUtilities()
    {
        _details.Children.Add(Visuals.Text("Rede e som usam os serviços disponíveis no seu Linux. As credenciais continuam nas ferramentas do sistema.", color: "#A9B8C9"));
        _details.Children.Add(Visuals.Button("Diagnóstico de capacidades", _diagnostics));
        _details.Children.Add(Visuals.Text("OBS, notificações de terceiros, Wi-Fi/Bluetooth com pareamento, captura de janelas e histórico global de clipboard exigem integrações adicionais específicas do desktop.", 12, "#A9B8C9"));
    }
}
