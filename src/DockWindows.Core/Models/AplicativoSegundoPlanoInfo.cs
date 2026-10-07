namespace DockWindows.Core.Models;

public sealed record AplicativoSegundoPlanoInfo(
    string Nome,
    string CaminhoExecutavel,
    bool PossuiJanela,
    IntPtr JanelaPrincipal,
    string? ReferenciaIcone = null);
