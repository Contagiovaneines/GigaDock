namespace GigaDock.Infrastructure.Linux;

/// <summary>Lock de arquivo mantido aberto, liberado pelo SO após falha. Isolamento por diretório de configurações.</summary>
public sealed class SingleInstance : IDisposable
{
    private readonly FileStream _lock;
    private SingleInstance(FileStream file) => _lock = file;
    public static SingleInstance? TryAcquire(string state)
    {
        Directory.CreateDirectory(state);
        try { return new SingleInstance(new FileStream(Path.Combine(state, "instance.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)); }
        catch (IOException) { return null; }
    }
    public void Dispose() => _lock.Dispose();
}
