namespace DockWindows.Core.Models;

public enum TipoControleRapido { Wifi, Bluetooth, ModoEscuro, Foco, BloquearTeclado, BloquearTela, Suspender }

public class ControleRapidoConfig
{
    public TipoControleRapido Tipo { get; set; }
    public bool MostrarNaDock { get; set; }

    public static List<ControleRapidoConfig> CriarPadrao() => Enum.GetValues<TipoControleRapido>()
        .Select(tipo => new ControleRapidoConfig { Tipo = tipo, MostrarNaDock = tipo <= TipoControleRapido.BloquearTeclado })
        .ToList();
}