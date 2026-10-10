using DockWindows.Core.Models;
using DockWindows.Core.Services;
using GigaDock.Services.Persistence;

namespace GigaDock.Infrastructure.Linux;

/// <summary>Composição independente da UI. Não inicia integrações nem executa itens armazenados.</summary>
public sealed class LinuxApplicationSession : IDisposable
{
    public IAppDirectories Directories { get; }
    public ISettingsRepository Settings { get; }
    public IDesktopWindowService Windows { get; }
    public Preferencias Preferences { get; private set; }
    public Ambiente ActiveEnvironment => Preferences.Ambientes.First(a => a.Id == Preferences.AmbienteAtivoId);

    public LinuxApplicationSession(IAppDirectories directories, ISettingsRepository? settings = null,
        IDesktopWindowService? windows = null)
    {
        Directories = directories;
        Settings = settings ?? new JsonSettingsRepository(directories.Configuracoes, LinuxPreferencesFactory.CriarPadrao);
        Preferences = Settings.Carregar();
        if (Preferences.Ambientes.Count == 0 || !Preferences.Ambientes.Any(a => a.Id == Preferences.AmbienteAtivoId))
            throw new InvalidOperationException("As configurações não possuem um ambiente ativo válido.");
        Windows = windows ?? X11WindowService.ForCurrentSession();
    }

    public void SelectEnvironment(string id)
    {
        if (!Preferences.Ambientes.Any(a => a.Id == id))
            throw new ArgumentException("O ambiente escolhido não existe.", nameof(id));
        if (Preferences.AmbienteAtivoId == id) return;
        var previous = Preferences.AmbienteAtivoId;
        Preferences.AmbienteAtivoId = id;
        try { Settings.Salvar(Preferences); }
        catch
        {
            Preferences.AmbienteAtivoId = previous;
            throw;
        }
    }

    public void SetAppearance(EstiloTema style, double height, double opacity, double radius, bool reduceMotion)
    {
        if (!Enum.IsDefined(style) || !double.IsFinite(height) || height < 48 || height > 120 ||
            !double.IsFinite(opacity) || opacity < .35 || opacity > 1 ||
            !double.IsFinite(radius) || radius < 0 || radius > 40)
            throw new ArgumentException("Escolha uma aparência com valores válidos.");
        var previous = (Preferences.EstiloTema, Preferences.AlturaBarra, Preferences.OpacidadeDock,
            Preferences.RaioCantosDock, Preferences.DesativarAnimacoes);
        (Preferences.EstiloTema, Preferences.AlturaBarra, Preferences.OpacidadeDock,
            Preferences.RaioCantosDock, Preferences.DesativarAnimacoes) = (style, height, opacity, radius, reduceMotion);
        try { Settings.Salvar(Preferences); }
        catch
        {
            (Preferences.EstiloTema, Preferences.AlturaBarra, Preferences.OpacidadeDock,
                Preferences.RaioCantosDock, Preferences.DesativarAnimacoes) = previous;
            throw;
        }
    }

    public void Update(Action<Preferencias> edit)
    {
        var candidate = Preferences.Clonar();
        edit(candidate);
        if (candidate.Ambientes.Count == 0 || !candidate.Ambientes.Any(a => a.Id == candidate.AmbienteAtivoId))
            throw new ArgumentException("Mantenha pelo menos um ambiente e um ambiente ativo válido.");
        Settings.Salvar(candidate);
        Preferences = candidate;
    }

    public void AddItem(ItemFixado item, string? collectionId = null)
    {
        var validation = LinuxLauncher.Validate(item);
        if (!validation.Valido) throw new ArgumentException(validation.MensagemErro);
        var copy = new ItemFixado { Titulo = item.Titulo.Trim(), CaminhoOuUrl = item.CaminhoOuUrl,
            Tipo = item.Tipo, IconeCustomizado = item.IconeCustomizado };
        Update(prefs =>
        {
            var environment = prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId);
            var list = collectionId is null ? environment.Itens : environment.Colecoes.Concat(prefs.ColecoesGlobais).First(c => c.Id == collectionId).Itens;
            copy.Ordem = list.Count;
            list.Add(copy);
        });
    }

    public void RemoveItem(string id, string? collectionId = null) => Update(prefs =>
    {
        var environment = prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId);
        var list = collectionId is null ? environment.Itens : environment.Colecoes.Concat(prefs.ColecoesGlobais).First(c => c.Id == collectionId).Itens;
        list.RemoveAll(item => item.Id == id);
        if (collectionId is null) prefs.AppsPermanentes.RemoveAll(item => item.Id == id);
    });

    public void MoveItem(string id, int delta) => Update(prefs =>
    {
        var list = prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).Itens.OrderBy(i => i.Ordem).ToList();
        var index = list.FindIndex(i => i.Id == id);
        if (index < 0) throw new ArgumentException("O item não existe.");
        var destination = Math.Clamp(index + delta, 0, list.Count - 1);
        var item = list[index]; list.RemoveAt(index); list.Insert(destination, item);
        for (var i = 0; i < list.Count; i++) list[i].Ordem = i;
        prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId).Itens = list;
    });

    public void MoveItemBefore(string id, string targetId) => Update(prefs =>
    {
        var environment = prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId);
        var items = environment.Itens.OrderBy(item => item.Ordem).ToList();
        var moving = items.FirstOrDefault(item => item.Id == id);
        if (moving is null || !items.Any(item => item.Id == targetId)) throw new ArgumentException("Escolha itens do ambiente ativo.");
        if (id == targetId) return;
        items.Remove(moving); items.Insert(items.FindIndex(item => item.Id == targetId), moving);
        for (var index = 0; index < items.Count; index++) items[index].Ordem = index;
        environment.Itens = items;
    });

    public void AddEnvironment(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 60 || name.Any(char.IsControl)) throw new ArgumentException("Informe um nome de até 60 caracteres sem quebras de linha.");
        Update(prefs =>
        {
            if (prefs.Ambientes.Count >= 60) throw new ArgumentException("O limite é de 60 ambientes.");
            prefs.Ambientes.Add(new Ambiente { Nome = name.Trim(), CorHex = "#0A84FF", Itens = [], Colecoes = [],
                WidgetsInstalados = [], ControlesRapidos = [], Widgets = new WidgetConfig { RelogioHabilitado = false, PomodoroHabilitado = false } });
        });
    }

    public void DuplicateEnvironment(string id, string name) => Update(prefs =>
    {
        if (prefs.Ambientes.Count >= 60) throw new ArgumentException("O limite é de 60 ambientes.");
        var original = prefs.Ambientes.FirstOrDefault(a => a.Id == id)
            ?? throw new ArgumentException("O ambiente não existe.");
        prefs.Ambientes.Add(AmbienteDuplicador.Duplicar(original, name));
    });

    public void MoveEnvironment(string id, int delta) => Update(prefs =>
    {
        var index = prefs.Ambientes.FindIndex(a => a.Id == id);
        if (index < 0) throw new ArgumentException("O ambiente não existe.");
        var destination = (int)Math.Clamp((long)index + delta, 0, prefs.Ambientes.Count - 1);
        var environment = prefs.Ambientes[index];
        prefs.Ambientes.RemoveAt(index); prefs.Ambientes.Insert(destination, environment);
    });

    public void CycleEnvironment(int delta)
    {
        var index = Preferences.Ambientes.FindIndex(a => a.Id == Preferences.AmbienteAtivoId);
        var next = (int)(((long)index + delta % Preferences.Ambientes.Count + Preferences.Ambientes.Count) % Preferences.Ambientes.Count);
        SelectEnvironment(Preferences.Ambientes[next].Id);
    }

    public void SetWidget(TipoWidget kind, bool enabled)
    {
        if (enabled && !LinuxWidgetAvailability.Get(kind).CanInstall)
            throw new ArgumentException(LinuxWidgetAvailability.Get(kind).Reason);
        Update(prefs =>
        {
            var environment = prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId);
            var existing = environment.WidgetsInstalados.FirstOrDefault(w => w.Tipo == kind);
            if (existing is null && enabled) environment.WidgetsInstalados.Add(new WidgetInstanceConfig
                { Tipo = kind, Nome = LinuxWidgetCatalog.Name(kind), Ordem = environment.WidgetsInstalados.Count });
            else if (existing is not null) existing.Visivel = enabled;
        });
    }

    public string Describe() => $"""
        GigaDock — estrutura Linux
        Ambiente ativo: {ActiveEnvironment.Nome}
        Ambientes: {Preferences.Ambientes.Count}
        Configurações: {Settings.ObterCaminhoConfiguracoes()}
        Dados: {Directories.Dados}
        Cache: {Directories.Cache}
        Estado: {Directories.Estado}
        Backend gráfico configurado: Avalonia 12.1.4 / UsePlatformDetect (X11/XWayland no Linux)
        Janelas de outros aplicativos: {(Windows.Capacidades.ListarJanelas ? "disponível" : "não implementado")}
        Abertura de aplicativos/arquivos: GIO (gio launch/open), somente por ação do usuário
        Sessão: {Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "não informada"}
        Desktop: {Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "não informado"}
        Mídia MPRIS: {(LinuxCommands.Find("playerctl") is null ? "playerctl ausente" : "playerctl disponível; player depende da sessão")}
        Áudio: {(LinuxCommands.Find("pactl") is null ? "pactl ausente" : "pactl disponível; servidor depende da sessão")}
        """;

    public void Dispose() => Windows.Dispose();
}
