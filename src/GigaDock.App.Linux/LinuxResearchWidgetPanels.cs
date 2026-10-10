using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.App.Linux;

internal sealed partial class WidgetPanels
{
    private readonly LinuxHardwareSensors _hardware = new();
    private readonly LinuxFlatpakService _flatpak = new();
    private readonly LinuxWorkspaceService _workspaces = new();
    private readonly LinuxLocalScriptWidget _script = new();
    private readonly Dictionary<string, (string Text, string Tooltip)> _summaries = new();
    public event Action<string, string, string>? SummaryChanged;
    public string Summary(WidgetInstanceConfig widget) => _summaries.TryGetValue(widget.Id, out var value) ? value.Text : LinuxWidgetCatalog.Name(widget.Tipo);
    public string HardwareSummary() => LinuxHardwareSensors.Summary(_hardware.Read());
    private void Publish(WidgetInstanceConfig widget, string text, string tooltip = "")
    {
        _summaries[widget.Id] = (text, tooltip); SummaryChanged?.Invoke(widget.Id, text, tooltip);
    }

    private void BuildResearchPanel(LinuxApplicationSession session, Window owner, WidgetInstanceConfig widget, string environment,
        StackPanel panel, TextBlock message, Popup popup)
    {
        var lifetime = new CancellationTokenSource(); var token = lifetime.Token;
        popup.Closed += (_, _) => { lifetime.Cancel(); lifetime.Dispose(); };
        var busy = false;
        async Task Run(Func<Task> action)
        {
            if (busy) return;
            busy = true; message.Text = "Consultando…";
            try { await action(); }
            catch (OperationCanceledException) { }
            catch (Exception error) { if (!token.IsCancellationRequested) message.Text = error.Message; }
            finally { busy = false; }
        }

        if (widget.Tipo == TipoWidget.SensoresLinux)
        {
            panel.Children.Add(Visuals.Text("Temperaturas e RPM disponibilizados pelo seu hardware. Nenhuma configuração das ventoinhas é alterada.", 12, "#A9B8C9"));
            var list = new StackPanel { Spacing = 8 }; panel.Children.Add(list);
            void Refresh()
            {
                var sensors = _hardware.Read(); list.Children.Clear();
                foreach (var sensor in sensors)
                    list.Children.Add(Visuals.Card(Visuals.Text($"{sensor.Chip} · {sensor.Label}\n{sensor.Formatted}")));
                message.Text = sensors.Count == 0 ? "Nenhum sensor acessível. Isso pode acontecer em máquinas virtuais ou hardware sem suporte." : $"{sensors.Count} leituras · atualização a cada 3 segundos.";
                Publish(widget, LinuxHardwareSensors.Summary(sensors));
            }
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (_, _) => Refresh(); popup.Closed += (_, _) => timer.Stop();
            Refresh(); timer.Start();
        }
        else if (widget.Tipo == TipoWidget.AplicativosFlatpak)
        {
            panel.Children.Add(Visuals.Text("Aplicativos instalados no usuário ou no sistema. Abrir um aplicativo não instala nem atualiza pacotes.", 12, "#A9B8C9"));
            var search = new TextBox { PlaceholderText = "Buscar aplicativo Flatpak" }; panel.Children.Add(search);
            Avalonia.Automation.AutomationProperties.SetName(search, "Buscar aplicativo Flatpak");
            var list = new StackPanel { Spacing = 8 }; IReadOnlyList<FlatpakApplication> apps = [];
            void Draw()
            {
                list.Children.Clear();
                foreach (var app in apps.Where(app => app.Name.Contains(search.Text ?? "", StringComparison.CurrentCultureIgnoreCase) || app.Id.Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase)).Take(100))
                {
                    var card = new StackPanel { Spacing = 6 };
                    card.Children.Add(Visuals.Text(app.Name, 15));
                    card.Children.Add(Visuals.Text($"{app.Id}\n{app.Version} · {app.Branch} · {(app.UserInstallation ? "usuário" : "sistema")}", 11, "#A9B8C9"));
                    card.Children.Add(Visuals.Button("Abrir " + app.Name, async () => await Run(async () => { await _flatpak.LaunchAsync(app, token); message.Text = "Abertura solicitada: " + app.Name; })));
                    list.Children.Add(Visuals.Card(card));
                }
            }
            search.TextChanged += (_, _) => Draw();
            panel.Children.Add(Visuals.Button("Atualizar aplicativos Flatpak", async () => await Run(async () =>
            {
                apps = await _flatpak.ListAsync(token); if (token.IsCancellationRequested) return;
                Draw(); message.Text = apps.Count == 0 ? "Nenhum aplicativo Flatpak instalado." : $"{apps.Count} aplicativos · até 100 resultados por busca.";
                Publish(widget, $"Flatpak · {apps.Count}");
            })));
            panel.Children.Add(list); message.Text = "Clique em Atualizar para consultar. Este recurso exige Flatpak instalado.";
        }
        else if (widget.Tipo == TipoWidget.WorkspacesLinux)
        {
            panel.Children.Add(Visuals.Text("Áreas de trabalho do desktop, separadas dos ambientes internos do GigaDock. Integração com Sway e Hyprland.", 12, "#A9B8C9"));
            var list = new StackPanel { Spacing = 8 };
            async Task Refresh()
            {
                var workspaces = await _workspaces.ListAsync(token); if (token.IsCancellationRequested) return;
                list.Children.Clear();
                foreach (var workspace in workspaces)
                {
                    var button = Visuals.Button((workspace.Active ? "✓ " : "") + workspace.Name, async () => await Run(async () =>
                    {
                        await _workspaces.SwitchAsync(workspace, token); await Refresh();
                    }));
                    ToolTip.SetTip(button, workspace.Monitor); list.Children.Add(button);
                }
                var current = workspaces.FirstOrDefault(workspace => workspace.Active)?.Name ?? "Nenhuma ativa";
                message.Text = $"{_workspaces.Compositor} · ativa: {current}"; Publish(widget, "Área · " + current);
            }
            panel.Children.Add(Visuals.Button("Atualizar áreas de trabalho", async () => await Run(Refresh)));
            panel.Children.Add(list);
            message.Text = _workspaces.Compositor == LinuxCompositor.Unsupported ? "A sessão atual não é Sway ou Hyprland. GNOME/KDE ainda não têm integração com este widget." : "Clique em Atualizar para consultar as áreas de trabalho.";
        }
        else
        {
            panel.Children.Add(Visuals.Text("Escolha um programa local de sua confiança. Ele roda com as permissões do seu usuário, somente quando você clicar em Executar.", 12, "#A9B8C9"));
            var path = new TextBox { Text = widget.ObterConfiguracao("ScriptPath"), PlaceholderText = "/caminho/absoluto/meu-widget.sh" };
            Avalonia.Automation.AutomationProperties.SetName(path, "Caminho do script local"); panel.Children.Add(path);
            panel.Children.Add(Visuals.Button("Escolher script local", async () => await Run(async () =>
            {
                var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Escolher script local", AllowMultiple = false });
                if (token.IsCancellationRequested) return;
                var selected = files.FirstOrDefault()?.TryGetLocalPath(); if (selected is not null) path.Text = selected;
                message.Text = "Salve o caminho para usar novamente, ou execute para consultar.";
            })));
            panel.Children.Add(Visuals.Button("Salvar caminho do script", () =>
            {
                try
                {
                    var selected = string.IsNullOrWhiteSpace(path.Text) ? "" : LinuxLocalScriptWidget.ValidatePath(path.Text);
                    session.Update(prefs => prefs.WidgetsGlobais.Concat(prefs.Ambientes.First(a => a.Id == environment).WidgetsInstalados).First(w => w.Id == widget.Id).DefinirConfiguracao("ScriptPath", selected));
                    widget.DefinirConfiguracao("ScriptPath", selected);
                    message.Text = selected.Length == 0 ? "Caminho removido." : "Caminho salvo. O script não foi executado.";
                }
                catch (Exception error) { message.Text = error.Message; }
            }));
            panel.Children.Add(Visuals.Text("Saída esperada: {\"text\":\"Meu status\",\"tooltip\":\"Detalhes\"}\nLimites: 3 segundos, 4096 caracteres; sem argumentos ou execução automática.", 12, "#A9B8C9"));
            panel.Children.Add(Visuals.Button("Executar script local", async () => await Run(async () =>
            {
                var result = await _script.ExecuteAsync(path.Text ?? "", token); if (token.IsCancellationRequested) return;
                Publish(widget, result.Text, result.Tooltip); message.Text = result.Text + (result.Tooltip.Length > 0 ? "\n" + result.Tooltip : "");
            })));
            message.Text = "Nenhum script é executado ao abrir o GigaDock ou trocar de ambiente.";
        }
    }
}
