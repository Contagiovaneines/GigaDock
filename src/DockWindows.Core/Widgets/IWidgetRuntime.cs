namespace DockWindows.Core.Widgets;

public interface IWidgetRuntime : IDisposable
{
    string InstanceId { get; }
    Models.TipoWidget Tipo { get; }
    bool Ativo { get; }
    void Ativar(bool visual, bool segundoPlano = false);
    void Suspender();
}
