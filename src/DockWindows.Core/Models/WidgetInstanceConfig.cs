namespace DockWindows.Core.Models;

public class WidgetInstanceConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public TipoWidget Tipo { get; set; } = TipoWidget.Relogio;
    public string Nome { get; set; } = "Relógio";
    public FormatoWidget Formato { get; set; } = FormatoWidget.Compacto;
    public string Estilo { get; set; } = "";
    public bool Visivel { get; set; } = true;
    public int Ordem { get; set; }
}
