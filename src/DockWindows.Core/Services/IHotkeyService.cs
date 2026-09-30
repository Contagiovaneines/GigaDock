using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public record HotkeyRegistrationResult(bool Sucesso, string? MensagemErro = null)
{
    public static HotkeyRegistrationResult Ok() => new(true);
    public static HotkeyRegistrationResult Falha(string mensagem) => new(false, mensagem);
}

public interface IHotkeyService : IDisposable
{
    HotkeyRegistrationResult Registrar(int id, AtalhoConfig config, Action acao);
    void Desregistrar(int id);
    void DesregistrarTodos();
}
