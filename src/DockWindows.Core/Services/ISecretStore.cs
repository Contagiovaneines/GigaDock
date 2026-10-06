namespace DockWindows.Core.Services;

public interface ISecretStore
{
    string? Ler(string alvo);
    bool Salvar(string alvo, string segredo);
    bool Remover(string alvo);
}
