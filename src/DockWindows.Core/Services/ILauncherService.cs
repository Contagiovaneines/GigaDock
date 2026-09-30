using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public record LaunchResult(bool Sucesso, string? MensagemErro = null)
{
    public static LaunchResult Ok() => new(true);
    public static LaunchResult Falha(string mensagem) => new(false, mensagem);
}

public interface ILauncherService
{
    LaunchResult Executar(ItemFixado item);
    LaunchResult AbrirLocal(ItemFixado item);
    LaunchResult ExecutarCaminho(string caminho, string? argumentos = null);
}
