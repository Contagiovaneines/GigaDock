namespace DockWindows.Core.Services;

public interface ILixeiraDesktopService
{
    // Chamado apenas após uma mudança explícita da opção nos ajustes.
    bool ConfigurarVisibilidade(bool visivel, out string? erro);
}
