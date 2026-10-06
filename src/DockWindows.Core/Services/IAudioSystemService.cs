using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public interface IAudioSystemService : IDisposable
{
    IReadOnlyList<SessaoAudioInfo> Sessoes { get; }
    IReadOnlyList<DispositivoAudioInfo> Saidas { get; }
    bool MicrofoneMudo { get; }
    string? Erro { get; }
    Task AtualizarAsync();
    bool DefinirVolumeSessao(string id, float volume);
    bool DefinirMudoSessao(string id, bool mudo);
    bool DefinirMicrofoneMudo(bool mudo);
}
