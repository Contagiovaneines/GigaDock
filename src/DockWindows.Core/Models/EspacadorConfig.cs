namespace DockWindows.Core.Models;

public class EspacadorConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = "Divisor";
    public EstiloEspacador Estilo { get; set; } = EstiloEspacador.Linha;
    public int Largura { get; set; } = 8;
    public bool Visivel { get; set; } = true;
    public int Ordem { get; set; }
}
