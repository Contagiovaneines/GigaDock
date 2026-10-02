using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace DockWindows.Infrastructure.Windows;

public class DiscordIpcService
{
    private NamedPipeClientStream? _pipe;

    public event Action<string>? OnCanalVozAlterado;
    public event Action<string, string, string, bool>? OnUsuarioFlando;

    public void Iniciar()
    {
        _ = ConectarPipeAsync();
    }

    private async Task ConectarPipeAsync()
    {
        try
        {
            _pipe = new NamedPipeClientStream(".", "discord-ipc-0", PipeDirection.InOut, PipeOptions.Asynchronous);
            await _pipe.ConnectAsync(5000);

            // Neste ponto, seria necessário enviar o Handshake (Opcode 0) em JSON 
            // e depois escutar os eventos do Discord RPC.
            // Para produção real, o pacote 'DiscordRichPresence' (Lachee) é o recomendado via NuGet.
            
            // Mock de dados simulando a recepção pelo Pipe:
            OnCanalVozAlterado?.Invoke("Design e Front-end");
            OnUsuarioFlando?.Invoke("Giovane", "G", "#5865F2", true);
            OnUsuarioFlando?.Invoke("Alex", "A", "#ED4245", false);
        }
        catch
        {
            // Falhou ao conectar (Discord fechado ou erro no IPC)
        }
    }
}
