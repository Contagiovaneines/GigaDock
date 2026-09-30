using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public interface ISettingsRepository
{
    Preferencias Carregar();
    void Salvar(Preferencias prefs);
    Task SalvarAsync(Preferencias prefs);
    string ObterCaminhoConfiguracoes();
}
