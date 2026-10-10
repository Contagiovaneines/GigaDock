namespace DockWindows.Core.Validation;

/// <summary>Validação de caminhos e identificadores de aplicativos por plataforma, sem execução.</summary>
public interface IItemPathValidator
{
    ValidacaoResultado ValidarPasta(string caminho);
    ValidacaoResultado ValidarArquivoOuApp(string caminho);
}
