namespace DockWindows.Infrastructure.Windows;

/// <summary>Capacidade ainda indisponível; não conectar sem handshake/autorização RPC.</summary>
public sealed class DiscordIpcService : IDisposable
{
    private bool _iniciado, _disposed;
    public event Action<string>? EstadoAlterado;
    public void Iniciar()
    {
        if (_disposed || _iniciado) return;
        _iniciado = true;
        EstadoAlterado?.Invoke("Integração de voz indisponível");
    }
    public void Parar() => _iniciado = false;
    public void Dispose() { _disposed = true; Parar(); EstadoAlterado = null; }
}
