namespace DockWindows.Core.Models;

public class Ambiente
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = string.Empty;
    public string CorHex { get; set; } = "#0078D4";
    public string Icone { get; set; } = "Work";
    public List<ItemFixado> Itens { get; set; } = new();
    public List<ColecaoApp> Colecoes { get; set; } = new();
    public List<WidgetInstanceConfig> WidgetsInstalados { get; set; } = new();
    public WidgetConfig Widgets { get; set; } = new();
    public string CorIndicadorApps { get; set; } = "#0A84FF";
    public string EstiloIndicadorApps { get; set; } = "Barra";
}
