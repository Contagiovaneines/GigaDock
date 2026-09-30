namespace DockWindows.Core.Services;

public interface IAutostartService
{
    bool EstaHabilitado();
    bool Configurar(bool habilitar);
}
