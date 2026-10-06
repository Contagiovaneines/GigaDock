namespace DockWindows.Core.Models;

public static class WidgetCapabilities
{
    public static bool PermiteMultiplasInstancias(TipoWidget tipo) =>
        tipo is TipoWidget.Relogio or TipoWidget.Notas;
}
