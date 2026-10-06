using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public interface IAplicativosSegundoPlanoService
{
    IReadOnlyList<AplicativoSegundoPlanoInfo> ObterAplicativos();
}
