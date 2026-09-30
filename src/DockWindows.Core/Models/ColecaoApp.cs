namespace DockWindows.Core.Models;

public class ColecaoApp
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = string.Empty;
    public string Icone { get; set; } = "📁";
    public bool EhGlobal { get; set; } = true;
    public int Ordem { get; set; }
    public List<ItemFixado> Itens { get; set; } = new();
}
