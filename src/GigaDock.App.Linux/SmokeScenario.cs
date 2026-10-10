using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DockWindows.Core.Models;
namespace GigaDock.App.Linux;

internal static class SmokeScenario
{
    public static async Task RunAsync(MainWindow window)
    {
        var session = Program.Session;
        session.AddItem(new ItemFixado { Titulo = "Exemplo de site", Tipo = TipoItem.WebUrl, CaminhoOuUrl = "https://example.com" });
        foreach (var kind in new[] { TipoWidget.Relogio, TipoWidget.Pomodoro, TipoWidget.Notas, TipoWidget.Tarefas, TipoWidget.CalendarioCompromissos, TipoWidget.MascotePokemon }) session.SetWidget(kind, true);
        window.Refresh();
        for (var id = 1; id <= 151; id++)
        {
            var config = new WidgetInstanceConfig(); config.DefinirConfiguracao("PokemonId", id.ToString());
            using var sprite = new PokemonSprite(config, true);
            if (!sprite.HasSprite) throw new InvalidOperationException($"Sprite {id} não carregou.");
        }
        await ValidatePokemonMotion(window, session);
        await Task.Delay(400); LinuxApp.Capture(window, "dock-mvp");
        var settings = window.ShowSettings();
        await Task.Delay(300);
        var search = settings.GetVisualDescendants().OfType<TextBox>().Single(field => AutomationProperties.GetName(field) == "Buscar configurações");
        search.Text = "opacidade"; await Task.Delay(100);
        settings.GetVisualDescendants().OfType<Button>().First(button => AutomationProperties.GetName(button) == "Aparência").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (search.Text != "") throw new InvalidOperationException("A busca não navegou para a seção.");
        settings.SelectSection("Ambientes");
        var environment = session.ActiveEnvironment.Id; var environmentCount = session.Preferences.Ambientes.Count;
        settings.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Duplicar ambiente").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (session.Preferences.Ambientes.Count != environmentCount + 1 || session.ActiveEnvironment.Id != environment)
            throw new InvalidOperationException("A duplicação não preservou o ambiente ativo.");
        var itemEditor = new ItemEditorWindow(session, () => { window.Refresh(); settings.RefreshSection(); });
        itemEditor.Show(settings); await Task.Delay(200);
        var itemFields = itemEditor.GetVisualDescendants().OfType<TextBox>().ToArray();
        itemFields.Single(field => AutomationProperties.GetName(field) == "Nome do item").Text = "Site fixado pela interface";
        var address = itemFields.Single(field => AutomationProperties.GetName(field) == "Caminho ou endereço do item");
        address.Text = "https://user:password@example.com";
        itemEditor.GetVisualDescendants().OfType<ComboBox>().Single().SelectedIndex = 3;
        var pin = itemEditor.GetVisualDescendants().OfType<Button>().First(button => AutomationProperties.GetName(button) == "Fixar no ambiente");
        var before = session.ActiveEnvironment.Itens.Count;
        pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (!itemEditor.IsVisible || session.ActiveEnvironment.Itens.Count != before) throw new InvalidOperationException("URL com credencial não foi rejeitada pelo formulário.");
        address.Text = "https://example.com/teste";
        pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (session.ActiveEnvironment.Itens.Count != before + 1) throw new InvalidOperationException("O formulário não fixou o item.");
        await Task.Delay(200); LinuxApp.Capture(settings, "ajustes-ambientes-mvp");
        settings.SelectSection("Aparência"); await Task.Delay(250);
        settings.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Prévia do tema Areia").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        settings.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Aplicar aparência").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (session.Preferences.EstiloTema != EstiloTema.Areia) throw new InvalidOperationException("O tema Areia não foi aplicado.");
        await Task.Delay(250); LinuxApp.Capture(window, "dock-areia");

        var slider = settings.GetVisualDescendants().OfType<Slider>().First(s => AutomationProperties.GetName(s) == "Altura da barra");
        slider.Value = 72;
        var decor = settings.GetVisualDescendants().OfType<ComboBox>().Single(box => AutomationProperties.GetName(box) == "Decoração opcional");
        decor.SelectedItem = DecoracaoDock.Natal;
        var apply = settings.GetVisualDescendants().OfType<Button>().First(button => AutomationProperties.GetName(button) == "Aplicar aparência");
        apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (session.Preferences.AlturaBarra != 72) throw new InvalidOperationException("A aparência não foi aplicada pela interface.");
        if (session.Preferences.DecoracaoDock != DecoracaoDock.Natal) throw new InvalidOperationException("A decoração não foi aplicada.");
        slider.Value = 100;
        settings.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Cancelar prévia").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (session.Preferences.AlturaBarra != 72) throw new InvalidOperationException("Cancelar prévia alterou configurações persistidas.");
        LinuxApp.Capture(window, "dock-decoracao");
        await Task.Delay(250); LinuxApp.Capture(settings, "ajustes-aparencia-mvp");
        settings.SelectSection("Widgets"); await Task.Delay(250);
        foreach (var blocked in new[] { TipoWidget.LembreteAgua, TipoWidget.DiscordVoz, TipoWidget.WhatsAppNotificacoes })
        {
            var name = DockWindows.Core.Widgets.WidgetStoreCatalog.Get(blocked, DockWindows.Core.Widgets.WidgetPlatform.Linux).Name;
            var toggle = settings.GetVisualDescendants().OfType<CheckBox>().Single(check => AutomationProperties.GetName(check) == "Ativar widget " + name);
            if (toggle.IsEnabled) throw new InvalidOperationException("A loja não bloqueou " + name);
        }
        if (!settings.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == "Como funciona")) throw new InvalidOperationException("Instruções da loja ausentes.");
 LinuxApp.Capture(settings, "widgets-mvp");
        settings.SelectSection("Pokédex"); await Task.Delay(250); LinuxApp.Capture(settings, "pokedex-mvp");
        settings.SelectSection("Aparência"); settings.Width = 760; settings.Height = 500;
        await Task.Delay(250); LinuxApp.Capture(settings, "ajustes-compactos-mvp");
        settings.SelectSection("Geral");
        settings.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Conhecer o GigaDock").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(150);
        var guide = settings.OwnedWindows.OfType<GuideWindow>().Single();
        guide.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Próximo").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (!guide.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == "Aplicativos e arquivos")) throw new InvalidOperationException("O guia não avançou.");
        guide.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Abrir esta seção").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (guide.IsVisible) throw new InvalidOperationException("O guia não encerrou ao abrir a seção.");
        settings.Close();
        var notes = window.GetVisualDescendants().OfType<Button>().First(button => AutomationProperties.GetName(button) == "Notas");
        notes.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(250);
        if (notes.Tag is not Avalonia.Controls.Primitives.Popup { IsOpen: true } popup) throw new InvalidOperationException("Painel de notas não abriu.");
        var saveNote = popup.Child!.GetVisualDescendants().OfType<Button>().First(button => AutomationProperties.GetName(button) == "Salvar nota");
        var editor = popup.Child!.GetVisualDescendants().OfType<TextBox>().Single(); editor.Text = "Teste gráfico — notas locais";
        saveNote.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); popup.IsOpen = false;
        if (new GigaDock.Infrastructure.Linux.LocalNotes(session.Directories.Dados).Read(session.ActiveEnvironment.Id) != editor.Text) throw new InvalidOperationException("A nota não foi salva pelo painel.");
        var tasksButton = window.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Tarefas");
        tasksButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(200);
        if (tasksButton.Tag is not Avalonia.Controls.Primitives.Popup { IsOpen: true } taskPopup) throw new InvalidOperationException("Painel de tarefas não abriu.");
        var taskField = taskPopup.Child!.GetVisualDescendants().OfType<TextBox>().Single(); taskField.Text = "Validar tarefas pela interface";
        taskPopup.Child!.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Adicionar tarefa").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var checkTask = taskPopup.Child!.GetVisualDescendants().OfType<CheckBox>().Single(); checkTask.IsChecked = true;
        if (!new GigaDock.Services.Persistence.LocalTaskStore(session.Directories.Dados).Read(environment).Single().Completed)
            throw new InvalidOperationException("A tarefa não foi concluída pelo painel.");
        taskPopup.Child!.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Excluir tarefa Validar tarefas pela interface").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (new GigaDock.Services.Persistence.LocalTaskStore(session.Directories.Dados).Read(environment).Count != 0)
            throw new InvalidOperationException("A tarefa não foi excluída.");
        taskPopup.IsOpen = false;
        await ValidateResearchWidgets(window, session);
        Console.WriteLine(session.Describe());
        Console.WriteLine($"Escala: {window.RenderScaling}; monitores: {window.Screens.All.Count}");
        Console.WriteLine("SMOKE_OK: dock, widgets, 151 sprites, URL, aparência, notas, busca, duplicação, tarefas e widgets Linux elegíveis e bloqueios por requisitos pela interface.");
    }

    private static async Task ValidatePokemonMotion(MainWindow window, GigaDock.Infrastructure.Linux.LinuxApplicationSession session)
    {
        var savedTheme = session.Preferences.EstiloTema;
        session.Update(p => { p.DesativarAnimacoes = false; p.ModoEconomico = false; });
        foreach (var theme in Enum.GetValues<EstiloTema>())
        {
            session.Update(p => p.EstiloTema = theme); window.Refresh(); await Task.Delay(100);
            var pet = window.GetVisualDescendants().OfType<PokemonSprite>().Single();
            var start = pet.WalkPosition; await Task.Delay(250);
            if (!pet.HasSprite || !pet.IsWalking || pet.WalkPosition <= start)
                throw new InvalidOperationException("Pokémon não caminhou no tema " + theme);
            if (pet.IsHitTestVisible) throw new InvalidOperationException("Mascote intercepta cliques dos aplicativos.");
            if (PokemonSprite.LiveSheets != 1) throw new InvalidOperationException("Sprites antigos não foram liberados ao trocar tema.");
            LinuxApp.Capture(window, "pokemon-tema-" + theme);
        }
        session.Update(p => p.DesativarAnimacoes = true); window.Refresh(); await Task.Delay(100);
        var stationary = window.GetVisualDescendants().OfType<PokemonSprite>().Single();
        var position = stationary.WalkPosition; await Task.Delay(150);
        if (stationary.IsWalking || stationary.WalkPosition != position) throw new InvalidOperationException("Redução de animações ignorada.");
        session.Update(p => { p.DesativarAnimacoes = false; p.ModoEconomico = true; }); window.Refresh(); await Task.Delay(100);
        if (window.GetVisualDescendants().OfType<PokemonSprite>().Single().IsWalking) throw new InvalidOperationException("Modo econômico ignorado.");
        session.Update(p => { p.ModoEconomico = false; p.EstiloTema = savedTheme; }); window.Refresh(); await Task.Delay(100);
        foreach (var id in new[] { 6, 151 })
        {
            session.Update(p => p.Ambientes.First(a => a.Id == p.AmbienteAtivoId).WidgetsInstalados
                .Single(w => w.Tipo == TipoWidget.MascotePokemon).DefinirConfiguracao("PokemonId", id.ToString()));
            window.Refresh(); await Task.Delay(150);
            if (!window.GetVisualDescendants().OfType<PokemonSprite>().Single().HasSprite)
                throw new InvalidOperationException("Sprite de voo/flutuação ausente: " + id);
            LinuxApp.Capture(window, "pokemon-voo-" + id);
        }
        session.Update(p => p.Ambientes.First(a => a.Id == p.AmbienteAtivoId).WidgetsInstalados
            .Single(w => w.Tipo == TipoWidget.MascotePokemon).DefinirConfiguracao("PokemonId", "1"));
        window.Refresh();
        for (var i = 0; i < 10; i++) window.Refresh();
        if (PokemonSprite.LiveSheets != 1) throw new InvalidOperationException("Troca repetida reteve sprites antigos.");
        var old = window.GetVisualDescendants().OfType<PokemonSprite>().Single();
        old.Dispose();
        if (old.IsWalking || PokemonSprite.LiveSheets != 0) throw new InvalidOperationException("Dispose não liberou sprite e timer.");
        window.Refresh();
        Console.WriteLine("POKEMON_MOTION_OK: seis temas, deslocamento real, redução de animações, modo econômico e liberação de imagens.");
    }

    private static async Task ValidateResearchWidgets(MainWindow window, GigaDock.Infrastructure.Linux.LinuxApplicationSession session)
    {
        foreach (var kind in new[] { TipoWidget.SensoresLinux, TipoWidget.AplicativosFlatpak, TipoWidget.WorkspacesLinux, TipoWidget.ScriptLocalLinux })
        {
            var access = GigaDock.Infrastructure.Linux.LinuxWidgetAvailability.Get(kind);
            if (access.CanInstall) session.SetWidget(kind, true);
            else
            {
                try { session.SetWidget(kind, true); throw new InvalidOperationException("O widget sem requisito foi instalado."); }
                catch (ArgumentException) { Console.WriteLine("WIDGET_INSTALL_BLOCKED: " + kind + " — " + access.Reason); }
            }
        }
        session.Update(prefs =>
        {
            foreach (var widget in prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).WidgetsInstalados.Where(w => (int)w.Tipo >= 22))
                widget.Formato = FormatoWidget.Expandido;
        });
        window.Refresh(); await Task.Delay(150);
        foreach (var kind in new[] { TipoWidget.SensoresLinux, TipoWidget.AplicativosFlatpak, TipoWidget.WorkspacesLinux, TipoWidget.ScriptLocalLinux })
        {
            if (!GigaDock.Infrastructure.Linux.LinuxWidgetAvailability.Get(kind).CanInstall) continue;
            var name = GigaDock.Infrastructure.Linux.LinuxWidgetCatalog.Name(kind);
            var button = window.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == name);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(100);
            if (button.Tag is not Avalonia.Controls.Primitives.Popup { IsOpen: true } popup) throw new InvalidOperationException("Painel Linux não abriu: " + name);
            var popupContent = popup.Child ?? throw new InvalidOperationException("Painel sem conteúdo: " + name);
            if (!popupContent.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == name)) throw new InvalidOperationException("Título do painel ausente: " + name);
            LinuxApp.Capture(popupContent, "painel-linux-" + (int)kind);
            if (kind == TipoWidget.ScriptLocalLinux && OperatingSystem.IsLinux())
            {
                Directory.CreateDirectory(session.Directories.Dados);
                var script = Path.Combine(session.Directories.Dados, "widget teste local.sh");
                File.WriteAllText(script, "#!/bin/sh\nprintf '%s' '{\"text\":\"Teste local OK\",\"tooltip\":\"Executado pelo botão\"}'\n");
                File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                popupContent.GetVisualDescendants().OfType<TextBox>().Single().Text = script;
                popupContent.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Salvar caminho do script").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var stored = session.ActiveEnvironment.WidgetsInstalados.Single(w => w.Tipo == kind);
                if (stored.ObterConfiguracao("ScriptPath") != script) throw new InvalidOperationException("Caminho do script não foi salvo.");
                if (window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Teste local OK")) throw new InvalidOperationException("O script foi executado antes do clique.");
                popupContent.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetName(b) == "Executar script local").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                for (var attempt = 0; attempt < 40 && !window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Teste local OK"); attempt++) await Task.Delay(50);
                if (!window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Teste local OK")) throw new InvalidOperationException("O resultado do script não chegou à dock.");
                LinuxApp.Capture(popupContent, "painel-linux-script-resultado");
                popup.IsOpen = false; button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(100);
                var reopened = (Avalonia.Controls.Primitives.Popup)button.Tag!;
                if (reopened.Child!.GetVisualDescendants().OfType<TextBox>().Single().Text != script) throw new InvalidOperationException("O painel não recarregou o caminho salvo.");
                reopened.IsOpen = false;
            }
            else popup.IsOpen = false;
        }
        var settings = window.ShowSettings(); settings.SelectSection("Widgets"); await Task.Delay(150);
        LinuxApp.Capture(settings, "widgets-pesquisa-linux"); settings.Close();
        LinuxApp.Capture(window, "dock-pesquisa-linux");
        Console.WriteLine("RESEARCH_WIDGETS_UI_OK: painéis elegíveis; requisitos ausentes bloquearam instalação; script executado manualmente.");
    }
}

