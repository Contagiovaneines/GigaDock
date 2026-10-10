namespace DockWindows.Core.Services;

/// <summary>Locais próprios do aplicativo; consultar não cria diretórios nem altera o sistema.</summary>
public interface IAppDirectories
{
    string Configuracoes { get; }
    string Dados { get; }
    string Cache { get; }
    string Estado { get; }
}
