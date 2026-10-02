using System.Text.Json;

namespace DockWindows.Core.Models;

public class Preferencias
{
    public int SchemaVersion { get; set; } = 4;
    public string AmbienteAtivoId { get; set; } = string.Empty;
    public TemaModo Tema { get; set; } = TemaModo.Escuro;
    public EstiloTema EstiloTema { get; set; } = EstiloTema.Escuro;
    public TamanhoIcone TamanhoIcones { get; set; } = TamanhoIcone.Medio;
    public double AlturaBarra { get; set; } = 64.0;
    public double OpacidadeDock { get; set; } = 0.92;
    public double RaioCantosDock { get; set; } = 20.0;
    public bool EfeitoDesfoque { get; set; } = true;
    public string VelocidadeAnimacao { get; set; } = "Normal";
    public bool SempreNoTopo { get; set; } = true;
    public bool OcultarAutomaticamente { get; set; } = false;
    public bool IniciarComWindows { get; set; } = false;
    public AtalhoConfig AtalhoDock { get; set; } = new() { Control = true, Alt = true, Tecla = "D" };
    public List<Ambiente> Ambientes { get; set; } = new();

    // Propriedades Modo Substituição e Exibição
    public bool UsarComoBarraPrincipal { get; set; } = false;
    public int? EstadoAnteriorBarraTarefas { get; set; } = null;
    public bool ExibirSeletorAmbientes { get; set; } = true;
    public bool ExibirItensFixados { get; set; } = true;
    public bool ExibirBotoesAcao { get; set; } = false;
    public bool ExibirContagemColecoes { get; set; } = false;
    public bool ExibirClima { get; set; } = true;
    public bool ExibirMidia { get; set; } = true;
    public bool ExibirLixeira { get; set; } = false;
    public bool AbrirPlayerAoDuploClique { get; set; } = true;
    public string GitHubUsuario { get; set; } = string.Empty;
    public string LocalizacaoClima { get; set; } = string.Empty;
    public bool DesativarAnimacoes { get; set; } = false;
    public int EspacamentoItens { get; set; } = 6;

    // Área Permanente de Aplicativos e Janelas Abertas
    public bool AppsFixadosGlobais { get; set; } = true;
    public List<ItemFixado> AppsPermanentes { get; set; } = CriarAppsPermanentesPadrao();
    public List<ConfigSecaoDock> OrdemSecoes { get; set; } = CriarOrdemSecoesPadrao();

    // Coleções, Espaçadores e Widgets V1.3
    public List<ColecaoApp> ColecoesGlobais { get; set; } = CriarColecoesGlobaisPadrao();
    public List<EspacadorConfig> Espacadores { get; set; } = CriarEspacadoresPadrao();
    public List<CompromissoLocal> CompromissosLocais { get; set; } = CriarCompromissosPadrao();
    public string UrlIcal { get; set; } = string.Empty;
    public List<WidgetInstanceConfig> WidgetsGlobais { get; set; } = CriarWidgetsPadrao();

    public Preferencias Clonar()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<Preferencias>(json) ?? CriarPadrao();
    }

    public static List<ConfigSecaoDock> CriarOrdemSecoesPadrao() => new()
    {
        new ConfigSecaoDock { Tipo = TipoSecaoDock.IniciarPesquisa, Nome = "Iniciar e Pesquisa", Visivel = true, Ordem = 0 },
        new ConfigSecaoDock { Tipo = TipoSecaoDock.ClimaInline, Nome = "Clima Inline", Visivel = true, Ordem = 1 },
        new ConfigSecaoDock { Tipo = TipoSecaoDock.MidiaInline, Nome = "Mídia Inline", Visivel = true, Ordem = 2 },
        new ConfigSecaoDock { Tipo = TipoSecaoDock.Apps, Nome = "Aplicativos Fixados e Abertos", Visivel = true, Ordem = 3 },
        new ConfigSecaoDock { Tipo = TipoSecaoDock.Colecoes, Nome = "Coleções de Aplicativos", Visivel = true, Ordem = 4 },
        new ConfigSecaoDock { Tipo = TipoSecaoDock.ItensAmbiente, Nome = "Itens do Ambiente Atual", Visivel = true, Ordem = 5 },
        new ConfigSecaoDock { Tipo = TipoSecaoDock.Widgets, Nome = "Widgets (Pomodoro e Calendário)", Visivel = true, Ordem = 6 },
        new ConfigSecaoDock { Tipo = TipoSecaoDock.RelogioControles, Nome = "Relógio e Controles", Visivel = true, Ordem = 7 }
    };

    public static List<ItemFixado> CriarAppsPermanentesPadrao() => new()
    {
        new ItemFixado { Titulo = "Explorador de Arquivos", CaminhoOuUrl = "explorer.exe", Tipo = TipoItem.Aplicativo, Ordem = 0 },
        new ItemFixado { Titulo = "Navegador Web", CaminhoOuUrl = "https://www.google.com.br", Tipo = TipoItem.WebUrl, Ordem = 1 },
        new ItemFixado { Titulo = "Bloco de Notas", CaminhoOuUrl = "notepad.exe", Tipo = TipoItem.Aplicativo, Ordem = 2 },
        new ItemFixado { Titulo = "Terminal", CaminhoOuUrl = "cmd.exe", Tipo = TipoItem.Aplicativo, Ordem = 3 }
    };

    public static List<ColecaoApp> CriarColecoesGlobaisPadrao() => new()
    {
        new ColecaoApp
        {
            Id = "col-utilitarios",
            Nome = "Utilitários",
            Icone = "🛠️",
            EhGlobal = true,
            Ordem = 0,
            Itens = new List<ItemFixado>
            {
                new() { Titulo = "Calculadora", CaminhoOuUrl = "calc.exe", Tipo = TipoItem.Aplicativo, Ordem = 0 },
                new() { Titulo = "Bloco de Notas", CaminhoOuUrl = "notepad.exe", Tipo = TipoItem.Aplicativo, Ordem = 1 },
                new() { Titulo = "Terminal", CaminhoOuUrl = "cmd.exe", Tipo = TipoItem.Aplicativo, Ordem = 2 }
            }
        }
    };

    public static List<EspacadorConfig> CriarEspacadoresPadrao() => new()
    {
        new EspacadorConfig { Id = "esp-apps", Nome = "Divisor Apps", Estilo = EstiloEspacador.Linha, Largura = 8, Visivel = true, Ordem = 0 },
        new EspacadorConfig { Id = "esp-colecoes", Nome = "Divisor Coleções", Estilo = EstiloEspacador.Linha, Largura = 8, Visivel = true, Ordem = 1 },
        new EspacadorConfig { Id = "esp-itens", Nome = "Divisor Itens", Estilo = EstiloEspacador.Linha, Largura = 8, Visivel = true, Ordem = 2 },
        new EspacadorConfig { Id = "esp-widgets", Nome = "Divisor Widgets", Estilo = EstiloEspacador.Linha, Largura = 8, Visivel = true, Ordem = 3 }
    };

            public static List<WidgetInstanceConfig> CriarWidgetsPadrao() => new()
    {
        new WidgetInstanceConfig { Id = "wgt-relogio", Tipo = TipoWidget.Relogio, Nome = "Relógio Digital", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 0 },
        new WidgetInstanceConfig { Id = "wgt-pomodoro", Tipo = TipoWidget.Pomodoro, Nome = "Pomodoro de Foco", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 1 },
        new WidgetInstanceConfig { Id = "wgt-calendario", Tipo = TipoWidget.CalendarioCompromissos, Nome = "Calendário e Compromissos", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 2 },
        new WidgetInstanceConfig { Id = "wgt-github", Tipo = TipoWidget.GitHubContribuicoes, Nome = "Integração GitHub", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 3 },
        new WidgetInstanceConfig { Id = "wgt-clima", Tipo = TipoWidget.Clima, Nome = "Clima e Tempo", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 4 },
        new WidgetInstanceConfig { Id = "wgt-whatsapp", Tipo = TipoWidget.WhatsAppNotificacoes, Nome = "WhatsApp", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 5 },
        new WidgetInstanceConfig { Id = "wgt-teams", Tipo = TipoWidget.TeamsStatus, Nome = "Microsoft Teams", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 6 },
        new WidgetInstanceConfig { Id = "wgt-discord", Tipo = TipoWidget.DiscordVoz, Nome = "Discord", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 7 }
    };

    public static List<CompromissoLocal> CriarCompromissosPadrao() => new();

    public static Preferencias CriarPadrao()
    {
        var prefs = new Preferencias
        {
            SchemaVersion = 4,
            Tema = TemaModo.Escuro,
            EstiloTema = EstiloTema.Escuro,
            TamanhoIcones = TamanhoIcone.Medio,
            AlturaBarra = 64.0,
            OpacidadeDock = 0.92,
            RaioCantosDock = 20.0,
            EfeitoDesfoque = true,
            VelocidadeAnimacao = "Normal",
            SempreNoTopo = true,
            OcultarAutomaticamente = false,
            IniciarComWindows = false,
            UsarComoBarraPrincipal = false,
            ExibirSeletorAmbientes = true,
            ExibirItensFixados = true,
            ExibirBotoesAcao = false,
            ExibirClima = true,
            ExibirMidia = true,
            ExibirLixeira = false,
            DesativarAnimacoes = false,
            EspacamentoItens = 6,
            AppsFixadosGlobais = true,
            AppsPermanentes = CriarAppsPermanentesPadrao(),
            OrdemSecoes = CriarOrdemSecoesPadrao(),
            ColecoesGlobais = CriarColecoesGlobaisPadrao(),
            Espacadores = CriarEspacadoresPadrao(),
            CompromissosLocais = CriarCompromissosPadrao(),
            WidgetsGlobais = CriarWidgetsPadrao(),
            AtalhoDock = new AtalhoConfig { Control = true, Alt = true, Tecla = "D" }
        };

        var ambTrabalho = CriarAmbienteTrabalhoPadrao();
        var ambEstudos = CriarAmbienteEstudosPadrao();
        var ambPessoal = CriarAmbientePessoalPadrao();

        prefs.Ambientes.Add(ambTrabalho);
        prefs.Ambientes.Add(ambEstudos);
        prefs.Ambientes.Add(ambPessoal);
        prefs.AmbienteAtivoId = ambTrabalho.Id;

        return prefs;
    }

    public static Ambiente CriarAmbienteTrabalhoPadrao() => new()
    {
        Id = "ambiente-trabalho",
        Nome = "Trabalho",
        CorHex = "#0078D4",
        Icone = "💼",
        Widgets = new WidgetConfig { RelogioHabilitado = true, PomodoroHabilitado = true },
        WidgetsInstalados = CriarWidgetsPadrao(),
        Colecoes = new List<ColecaoApp>(),
        Itens = new List<ItemFixado>
        {
            new() { Titulo = "Navegador Web", CaminhoOuUrl = "https://www.google.com.br", Tipo = TipoItem.WebUrl, Ordem = 0 },
            new() { Titulo = "Explorador de Arquivos", CaminhoOuUrl = "explorer.exe", Tipo = TipoItem.Aplicativo, Ordem = 1 },
            new() { Titulo = "Bloco de Notas", CaminhoOuUrl = "notepad.exe", Tipo = TipoItem.Aplicativo, Ordem = 2 },
            new() { Titulo = "Terminal", CaminhoOuUrl = "cmd.exe", Tipo = TipoItem.Aplicativo, Ordem = 3 }
        }
    };

    public static Ambiente CriarAmbienteEstudosPadrao() => new()
    {
        Id = "ambiente-estudos",
        Nome = "Estudos",
        CorHex = "#107C41",
        Icone = "📚",
        Widgets = new WidgetConfig { RelogioHabilitado = true, PomodoroHabilitado = true, DuracaoFocoMinutos = 30 },
        WidgetsInstalados = CriarWidgetsPadrao(),
        Colecoes = new List<ColecaoApp>
        {
            new()
            {
                Id = "col-estudos",
                Nome = "Material",
                Icone = "📚",
                EhGlobal = false,
                Ordem = 0,
                Itens = new List<ItemFixado>
                {
                    new() { Titulo = "Wikipédia", CaminhoOuUrl = "https://pt.wikipedia.org", Tipo = TipoItem.WebUrl, Ordem = 0 },
                    new() { Titulo = "Calculadora", CaminhoOuUrl = "calc.exe", Tipo = TipoItem.Aplicativo, Ordem = 1 }
                }
            }
        },
        Itens = new List<ItemFixado>
        {
            new() { Titulo = "Wikipédia", CaminhoOuUrl = "https://pt.wikipedia.org", Tipo = TipoItem.WebUrl, Ordem = 0 },
            new() { Titulo = "Calculadora", CaminhoOuUrl = "calc.exe", Tipo = TipoItem.Aplicativo, Ordem = 1 },
            new() { Titulo = "Documentos", CaminhoOuUrl = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Tipo = TipoItem.Pasta, Ordem = 2 }
        }
    };

    public static Ambiente CriarAmbientePessoalPadrao() => new()
    {
        Id = "ambiente-pessoal",
        Nome = "Pessoal",
        CorHex = "#8764B8",
        Icone = "🎮",
        Widgets = new WidgetConfig { RelogioHabilitado = true, PomodoroHabilitado = false },
        WidgetsInstalados = CriarWidgetsPadrao(),
        Colecoes = new List<ColecaoApp>(),
        Itens = new List<ItemFixado>
        {
            new() { Titulo = "YouTube", CaminhoOuUrl = "https://www.youtube.com", Tipo = TipoItem.WebUrl, Ordem = 0 },
            new() { Titulo = "Imagens", CaminhoOuUrl = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Tipo = TipoItem.Pasta, Ordem = 1 }
        }
    };

    public void RestaurarAmbientePadrao(string ambienteId)
    {
        var ambiente = Ambientes.FirstOrDefault(a => a.Id == ambienteId);
        if (ambiente == null) return;

        Ambiente padrao = ambiente.Nome.ToLowerInvariant() switch
        {
            "estudos" => CriarAmbienteEstudosPadrao(),
            "pessoal" => CriarAmbientePessoalPadrao(),
            _ => CriarAmbienteTrabalhoPadrao()
        };

        ambiente.Nome = padrao.Nome;
        ambiente.CorHex = padrao.CorHex;
        ambiente.Icone = padrao.Icone;
        ambiente.Widgets = padrao.Widgets;
        ambiente.WidgetsInstalados = padrao.WidgetsInstalados;
        ambiente.Colecoes = padrao.Colecoes;
        ambiente.Itens = padrao.Itens;
    }
}









