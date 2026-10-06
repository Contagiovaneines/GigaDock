namespace DockWindows.Core.Models;

public class CompromissoLocal
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titulo { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.Today.AddHours(14);
    public string? Descricao { get; set; }
    public string? Local { get; set; }
    public bool DiaInteiro { get; set; }
}
