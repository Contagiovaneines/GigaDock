using System.Diagnostics;

namespace DockWindows.Infrastructure.Windows;

/// <summary>Observa somente o processo local. Dados de voz exigem autorização pelo SDK oficial.</summary>
public sealed class DiscordIpcService : IDisposable
{
    private readonly Timer _timer;
    private bool _iniciado;
    private bool _disposed;
    private string? _ultimoEstado;

    public DiscordIpcService() => _timer = new(_ => Atualizar(), null, Timeout.Infinite, Timeout.Infinite);
    public event Action<string>? EstadoAlterado;

    public void Iniciar()
    {
        if (_disposed || _iniciado) return;
        _iniciado = true;
        Atualizar();
        _timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private void Atualizar()
    {
        if (!_iniciado || _disposed) return;
        string estado;
        try
        {
            var sessaoAtual = Process.GetCurrentProcess().SessionId;
            estado = Process.GetProcessesByName("Discord").Any(processo =>
            {
                using (processo) return processo.SessionId == sessaoAtual;
            }) ? "Discord aberto · voz requer autorização oficial" : "Discord fechado";
        }
        catch { estado = "Estado do Discord indisponível"; }

        if (estado == _ultimoEstado) return;
        _ultimoEstado = estado;
        EstadoAlterado?.Invoke(estado);
    }

    public void Parar()
    {
        _iniciado = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Parar();
        _timer.Dispose();
        EstadoAlterado = null;
    }
}
