path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Core\Models\Preferencias.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Replace all Visivel = true to Visivel = false inside CriarWidgetsPadrao
bad_block = """    public static List<WidgetInstanceConfig> CriarWidgetsPadrao() => new()
    {
        new WidgetInstanceConfig { Id = "wgt-relogio", Tipo = TipoWidget.Relogio, Nome = "Relógio Digital", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 0 },
        new WidgetInstanceConfig { Id = "wgt-pomodoro", Tipo = TipoWidget.Pomodoro, Nome = "Pomodoro de Foco", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 1 },
        new WidgetInstanceConfig { Id = "wgt-calendario", Tipo = TipoWidget.CalendarioCompromissos, Nome = "Calendário e Compromissos", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 2 },
        new WidgetInstanceConfig { Id = "wgt-github", Tipo = TipoWidget.GitHubContribuicoes, Nome = "Integração GitHub", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 3 },
        new WidgetInstanceConfig { Id = "wgt-clima", Tipo = TipoWidget.Clima, Nome = "Clima e Tempo", Formato = FormatoWidget.Compacto, Visivel = true, Ordem = 4 },"""

good_block = """    public static List<WidgetInstanceConfig> CriarWidgetsPadrao() => new()
    {
        new WidgetInstanceConfig { Id = "wgt-relogio", Tipo = TipoWidget.Relogio, Nome = "Relógio Digital", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 0 },
        new WidgetInstanceConfig { Id = "wgt-pomodoro", Tipo = TipoWidget.Pomodoro, Nome = "Pomodoro de Foco", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 1 },
        new WidgetInstanceConfig { Id = "wgt-calendario", Tipo = TipoWidget.CalendarioCompromissos, Nome = "Calendário e Compromissos", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 2 },
        new WidgetInstanceConfig { Id = "wgt-github", Tipo = TipoWidget.GitHubContribuicoes, Nome = "Integração GitHub", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 3 },
        new WidgetInstanceConfig { Id = "wgt-clima", Tipo = TipoWidget.Clima, Nome = "Clima e Tempo", Formato = FormatoWidget.Compacto, Visivel = false, Ordem = 4 },"""

# Since we might have encoding issues with the accents (Relógio vs Relgio), let's use regex
import re
def replace_visivel(m):
    return m.group(0).replace("Visivel = true", "Visivel = false")

c = re.sub(r'public static List<WidgetInstanceConfig> CriarWidgetsPadrao\(\).*?wgt-discord.*?\}', replace_visivel, c, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
