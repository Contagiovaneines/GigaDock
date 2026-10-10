namespace DockWindows.Core.Models;

/// <summary>Identidade opaca. O valor só pode ser interpretado pelo backend que o emitiu.</summary>
public readonly record struct DesktopWindowId(string Backend, string Value);

public sealed record DesktopWindow(
    DesktopWindowId Id, string Titulo, string CaminhoExecutavel,
    string NomeProcesso, int ProcessId, bool EstaAtiva, bool EstaMinimizada);

/// <summary>Capacidades independentes; disponibilidade de uma não implica as demais.</summary>
public sealed record DesktopWindowCapabilities(
    bool ListarJanelas, bool AtivarJanela, bool MinimizarJanela, bool FecharJanela);
