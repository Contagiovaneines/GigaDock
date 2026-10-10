using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using DockWindows.Core.Models;
using DockWindows.Core.Widgets;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.App.Linux;

internal sealed partial class WidgetPanels(LinuxApplicationSession session, Window owner)
{
    private readonly Dictionary<string, PomodoroEngine> _pomodoros = new();
    private readonly LinuxSystemServices _system = new();
    private readonly LocalNotes _notes = new(session.Directories.Dados);
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false });
    private readonly GigaDock.Services.PublicDataClient _public = new(Http);

    public PomodoroEngine Pomodoro(string environment)
    {
        if (!_pomodoros.TryGetValue(environment, out var engine))
        {
            engine = new PomodoroEngine(session.Preferences.Ambientes.First(a => a.Id == environment).Widgets);
            _pomodoros[environment] = engine;
            engine.CicloConcluido += (_, _) =>
            {
                if (owner.IsVisible) new Window { Title = "Pomodoro — ciclo concluído", Width = 320, Height = 140,
                    Content = Visuals.Text("Ciclo concluído. Próxima fase: " + engine.EstadoTexto), Padding = new Thickness(20) }.Show(owner);
            };
        }
        return engine;
    }

    public void Tick() { foreach (var engine in _pomodoros.Values) engine.Tick(); }

    public void Open(Button anchor, WidgetInstanceConfig widget)
    {
        var environment = session.ActiveEnvironment.Id;
        var panel = new StackPanel { Spacing = 12, Width = 330, Margin = new Thickness(16) };
        panel.Children.Add(Visuals.Text(LinuxWidgetCatalog.Name(widget.Tipo), 20));
        var message = Visuals.Text("", 12, "#A9B8C9");
        var popup = new Popup { PlacementTarget = anchor, Placement = PlacementMode.Top, IsLightDismissEnabled = true,
            Child = new Border { Background = Visuals.Brush("#191C21"), BorderBrush = Visuals.Brush("#424954"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
                Child = new ScrollViewer { MaxHeight = 550, Content = panel } } };
        async Task Run(Func<Task> action)
        {
            try { await action(); }
            catch (Exception error) { message.Text = error.Message; }
        }
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        switch (widget.Tipo)
        {
            case TipoWidget.SensoresLinux:
            case TipoWidget.AplicativosFlatpak:
            case TipoWidget.WorkspacesLinux:
            case TipoWidget.ScriptLocalLinux:
                BuildResearchPanel(session, owner, widget, environment, panel, message, popup);
                break;
            case TipoWidget.Tarefas:
                var tasks = new GigaDock.Services.Persistence.LocalTaskStore(session.Directories.Dados);
                var taskList = new StackPanel { Spacing = 8 }; panel.Children.Add(taskList);
                void DrawTasks()
                {
                    taskList.Children.Clear();
                    foreach (var task in tasks.Read(environment))
                    {
                        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                        var check = new CheckBox { Content = task.Title, IsChecked = task.Completed, MaxWidth = 240 };
                        check.IsCheckedChanged += (_, _) =>
                        {
                            try { tasks.Complete(environment, task.Id, check.IsChecked == true); message.Text = ""; }
                            catch (Exception error) { message.Text = error.Message; }
                            try { DrawTasks(); } catch (Exception error) { message.Text = error.Message; }
                        };
                        row.Children.Add(check);
                        row.Children.Add(Visuals.Button("Excluir tarefa " + task.Title, () =>
                        {
                            try { tasks.Remove(environment, task.Id); DrawTasks(); message.Text = ""; }
                            catch (Exception error) { message.Text = error.Message; }
                        }, Visuals.Text("×")));
                        taskList.Children.Add(row);
                    }
                }
                try { DrawTasks(); } catch (Exception error) { message.Text = error.Message; }
                var taskTitle = new TextBox { PlaceholderText = "Nova tarefa", MaxLength = 200 };
                Avalonia.Automation.AutomationProperties.SetName(taskTitle, "Título da nova tarefa"); panel.Children.Add(taskTitle);
                panel.Children.Add(Visuals.Button("Adicionar tarefa", () =>
                {
                    try { tasks.Add(environment, taskTitle.Text ?? ""); taskTitle.Text = ""; DrawTasks(); message.Text = ""; }
                    catch (Exception error) { message.Text = error.Message; }
                }));
                break;
            case TipoWidget.Relogio:
                var clock = Visuals.Text(DateTime.Now.ToString("HH:mm:ss"), 36); panel.Children.Add(clock);
                panel.Children.Add(Visuals.Text(DateTime.Now.ToString("dddd, dd 'de' MMMM")));
                timer.Tick += (_, _) => clock.Text = DateTime.Now.ToString("HH:mm:ss"); timer.Start();
                break;
            case TipoWidget.Pomodoro:
                var engine = Pomodoro(environment); var time = Visuals.Text(engine.TempoFormatado, 36); panel.Children.Add(time);
                var phase = Visuals.Text(engine.EstadoTexto); panel.Children.Add(phase);
                timer.Tick += (_, _) => { time.Text = engine.TempoFormatado; phase.Text = engine.EstadoTexto; }; timer.Start();
                panel.Children.Add(Visuals.Button("Iniciar / Pausar", engine.Alternar));
                panel.Children.Add(Visuals.Button("Reiniciar", engine.Reiniciar));
                panel.Children.Add(Visuals.Button("Próxima fase", engine.AvancarFase));
                break;
            case TipoWidget.Notas:
                var notes = new TextBox { AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Height = 220 };
                try { notes.Text = _notes.Read(environment); } catch (Exception error) { message.Text = error.Message; }
                Avalonia.Automation.AutomationProperties.SetName(notes, "Texto das notas deste ambiente"); panel.Children.Add(notes);
                panel.Children.Add(Visuals.Button("Salvar nota", () => { try { _notes.Save(environment, notes.Text ?? ""); message.Text = "Nota salva."; } catch (Exception error) { message.Text = error.Message; } }));
                break;
            case TipoWidget.CalendarioCompromissos:
                panel.Children.Add(new Calendar { SelectedDate = DateTime.Today });
                foreach (var appointment in session.Preferences.CompromissosLocais.OrderBy(a => a.DataHora).Take(20))
                {
                    panel.Children.Add(Visuals.Text($"{appointment.DataHora:dd/MM HH:mm} — {appointment.Titulo}"));
                    panel.Children.Add(Visuals.Button("Remover " + appointment.Titulo, () => { try { session.Update(prefs => prefs.CompromissosLocais.RemoveAll(a => a.Id == appointment.Id)); popup.IsOpen = false; } catch (Exception error) { message.Text = error.Message; } }));
                }
                var eventTitle = new TextBox { PlaceholderText = "Título do compromisso", MaxLength = 200 }; var date = new TextBox { PlaceholderText = "dd/MM/aaaa HH:mm", Text = DateTime.Today.AddHours(14).ToString("dd/MM/yyyy HH:mm") };
                panel.Children.Add(eventTitle); panel.Children.Add(date);
                panel.Children.Add(Visuals.Button("Adicionar compromisso", () =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(eventTitle.Text) || !DateTime.TryParseExact(date.Text, "dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), System.Globalization.DateTimeStyles.None, out var when)) throw new ArgumentException("Informe título e data no formato dd/MM/aaaa HH:mm.");
                        session.Update(prefs => prefs.CompromissosLocais.Add(new CompromissoLocal { Titulo = eventTitle.Text.Trim(), DataHora = when })); message.Text = "Compromisso salvo.";
                    }
                    catch (Exception error) { message.Text = error.Message; }
                }));
                break;
            case TipoWidget.MonitorSistema:
            case TipoWidget.Bateria:
            case TipoWidget.Conectividade:
                var snapshot = Visuals.Text(""); panel.Children.Add(snapshot);
                void UpdateSystem()
                {
                    var state = _system.Read(); snapshot.Text = widget.Tipo switch
                    {
                        TipoWidget.Bateria => state.Battery,
                        TipoWidget.Conectividade => state.Network,
                        _ => $"CPU: {(state.CpuPercent.HasValue ? state.CpuPercent.Value.ToString("0") + "%" : "aguardando amostra")}\nMemória: {(state.MemoryPercent.HasValue ? state.MemoryPercent.Value.ToString("0") + "%" : "indisponível") }"
                    };
                }
                UpdateSystem(); timer.Interval = TimeSpan.FromSeconds(3); timer.Tick += (_, _) => UpdateSystem(); timer.Start();
                break;
            case TipoWidget.Midia:
                panel.Children.Add(Visuals.Button("Atualizar faixa", async () => await Run(async () => message.Text = await _system.MediaAsync())));
                foreach (var (label, command) in new[] { ("Anterior", "previous"), ("Tocar / Pausar", "play-pause"), ("Próxima", "next") })
                    panel.Children.Add(Visuals.Button(label, async () => await Run(() => _system.MediaCommandAsync(command))));
                message.Text = LinuxCommands.Find("playerctl") is null ? "Instale playerctl para controlar players MPRIS." : "Clique em Atualizar faixa para consultar o player.";
                break;
            case TipoWidget.AudioSistema:
                var volume = new Slider { Minimum = 0, Maximum = 100, Value = 50 }; panel.Children.Add(volume);
                panel.Children.Add(Visuals.Button("Consultar volume", async () => await Run(async () => message.Text = await _system.VolumeAsync())));
                panel.Children.Add(Visuals.Button("Aplicar volume", async () => await Run(() => _system.SetVolumeAsync((int)volume.Value))));
                message.Text = LinuxCommands.Find("pactl") is null ? "Instale pactl (pulseaudio-utils) para usar o servidor de áudio compatível." : "A mudança de volume acontece somente ao clicar em Aplicar.";
                break;
            case TipoWidget.LembreteAgua:
                message.Text = "Faça uma pausa para beber água. O registro fica somente neste ambiente.";
                panel.Children.Add(Visuals.Button("Bebi água agora", () => { try { session.Update(prefs => prefs.Ambientes.First(a => a.Id == environment).WidgetsInstalados.First(w => w.Id == widget.Id).DefinirConfiguracao("UltimoRegistro", DateTimeOffset.UtcNow.ToString("O"))); message.Text = "Registrado às " + DateTime.Now.ToString("HH:mm"); } catch (Exception error) { message.Text = error.Message; } }));
                break;
            case TipoWidget.MascotePokemon:
                panel.Children.Add(new PokemonSprite(widget, session.Preferences.DesativarAnimacoes || session.Preferences.ModoEconomico) { Width = 120, Height = 120 });
                panel.Children.Add(Visuals.Text("Selecione a espécie em Ajustes › Pokédex. Sprites PMDCollab com atribuição; direitos originais preservados.", 12, "#A9B8C9"));
                break;
            case TipoWidget.Clima:
                var city = new TextBox { PlaceholderText = "Cidade (ex.: Curitiba)", MaxLength = 100 };
                panel.Children.Add(city);
                message.Text = "A consulta envia a cidade informada ao Open-Meteo. Só acontece ao clicar abaixo.";
                panel.Children.Add(Visuals.Button("Consultar previsão de 7 dias", async () => await Run(async () => message.Text = await _public.WeatherAsync(city.Text ?? ""))));
                break;
            case TipoWidget.CotacaoMoedas:
                var from = new TextBox { Text = "USD", MaxLength = 3 }; var to = new TextBox { Text = "BRL", MaxLength = 3 };
                panel.Children.Add(Visuals.Text("Moeda de origem e destino")); panel.Children.Add(from); panel.Children.Add(to);
                message.Text = "Consulta pública ao Frankfurter, somente ao clicar.";
                panel.Children.Add(Visuals.Button("Consultar câmbio", async () => await Run(async () => message.Text = await _public.ExchangeAsync(from.Text ?? "", to.Text ?? ""))));
                break;
            case TipoWidget.GitHubContribuicoes:
                var user = new TextBox { PlaceholderText = "Nome de usuário público", MaxLength = 39 }; panel.Children.Add(user);
                message.Text = "Consulta perfil público no GitHub ao clicar. Não acessa sua conta ou repositórios privados.";
                panel.Children.Add(Visuals.Button("Consultar perfil", async () => await Run(async () => message.Text = await _public.GitHubAsync(user.Text ?? ""))));
                break;
            case TipoWidget.OBSStudio:
                var port = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 4455 };
                var password = new TextBox { PlaceholderText = "Senha do WebSocket (não será salva)", PasswordChar = '●' };
                panel.Children.Add(Visuals.Text("OBS local — 127.0.0.1")); panel.Children.Add(port); panel.Children.Add(password);
                foreach (var (label, request) in new[] { ("Consultar gravação", "GetRecordStatus"), ("Consultar transmissão", "GetStreamStatus"), ("Iniciar gravação", "StartRecord"), ("Parar gravação", "StopRecord") })
                    panel.Children.Add(Visuals.Button(label, async () => await Run(async () => message.Text = await ObsLocalClient.RequestAsync((int)(port.Value ?? 4455), password.Text ?? "", request))));
                popup.Closed += (_, _) => password.Text = "";
                message.Text = "Ative o servidor WebSocket nas ferramentas do OBS. A senha fica somente neste painel enquanto aberto.";
                break;
        }
        panel.Children.Add(message);
        panel.Children.Add(Visuals.Button("Fechar", () => popup.IsOpen = false));
        panel.KeyDown += (_, key) => { if (key.Key == Avalonia.Input.Key.Escape) { popup.IsOpen = false; key.Handled = true; } };
        popup.Opened += (_, _) => Dispatcher.UIThread.Post(() => panel.Children.OfType<TextBox>().FirstOrDefault()?.Focus());
        popup.Closed += (_, _) => { timer.Stop(); anchor.Flyout = null; };
        // Popup não é parte do visual tree; a referência em Tag mantém sua vida até fechar.
        anchor.Tag = popup; popup.Closed += (_, _) => { anchor.Tag = null; anchor.Focus(); }; popup.Open();
    }
}
