namespace DockWindows.Core.Models;

public class ItemFixado
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = string.Empty;
    public string CaminhoOuUrl { get; set; } = string.Empty;
    public string? Argumentos { get; set; }
    public TipoItem Tipo { get; set; } = TipoItem.Aplicativo;
    public string? IconeCustomizado { get; set; }
    public int Ordem { get; set; }
}
