namespace DockWindows.Core.Models;

public class WidgetConfig
{
    public bool RelogioHabilitado { get; set; } = true;
    public bool PomodoroHabilitado { get; set; } = true;
    public int DuracaoFocoMinutos { get; set; } = 25;
    public int DuracaoPausaCurtaMinutos { get; set; } = 5;
    public int DuracaoPausaLongaMinutos { get; set; } = 15;
    public int CiclosAtePausaLonga { get; set; } = 4;
}
