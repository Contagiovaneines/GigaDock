using DockWindows.Infrastructure.Windows;

namespace DockWindows.Infrastructure.Persistence;

/// <summary>Adaptador de compatibilidade Windows; formato e migrações pertencem aos serviços comuns.</summary>
public class JsonSettingsRepository : GigaDock.Services.Persistence.JsonSettingsRepository
{
    public JsonSettingsRepository(string? customFolder = null)
        : base(customFolder ?? new WindowsAppDirectories().Configuracoes)
    {
    }
}