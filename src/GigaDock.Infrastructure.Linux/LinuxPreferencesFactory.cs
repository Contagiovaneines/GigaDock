using DockWindows.Core.Models;

namespace GigaDock.Infrastructure.Linux;

/// <summary>Configuração inicial Linux, sem pressupor nomes de executáveis ou catálogos instalados.</summary>
public static class LinuxPreferencesFactory
{
    public static Preferencias CriarPadrao()
    {
        var preferences = new Preferencias
        {
            SchemaVersion = 8,
            AppsGlobaisMigrados = true,
            AppsPermanentes = [],
            ColecoesGlobais = [],
            Espacadores = [],
            CompromissosLocais = [],
            WidgetsGlobais = [],
            OrdemSecoes = [],
            UsarComoBarraPrincipal = false,
            EstadoAnteriorBarraTarefas = null,
            IniciarComWindows = false,
            PreviaJanelas = false,
            ExibirClima = false,
            ExibirMidia = false,
            ExibirBateria = false,
            ExibirLixeira = false,
            EfeitoDesfoque = false,
            SempreNoTopo = false,
            Ambientes =
            [
                CreateEnvironment("ambiente-trabalho", "Trabalho", "#0078D4", "💼"),
                CreateEnvironment("ambiente-estudos", "Estudos", "#107C41", "📚"),
                CreateEnvironment("ambiente-pessoal", "Pessoal", "#8764B8", "🎮")
            ],
            AmbienteAtivoId = "ambiente-trabalho"
        };
        return preferences;
    }

    private static Ambiente CreateEnvironment(string id, string name, string color, string icon) => new()
    {
        Id = id, Nome = name, CorHex = color, Icone = icon,
        Itens = [], Colecoes = [], WidgetsInstalados = [], ControlesRapidos = [],
        Widgets = new WidgetConfig { RelogioHabilitado = false, PomodoroHabilitado = false }
    };
}
