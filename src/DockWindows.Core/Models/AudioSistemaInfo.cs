namespace DockWindows.Core.Models;

public sealed record SessaoAudioInfo(string Id, uint ProcessId, string Nome, float Volume, bool Mudo);
public sealed record DispositivoAudioInfo(string Id, string Nome, bool Padrao, bool Captura);
