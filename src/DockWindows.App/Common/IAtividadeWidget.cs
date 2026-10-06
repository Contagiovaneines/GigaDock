using DockWindows.Core.Widgets;

namespace DockWindows.App.Common;

public interface IAtividadeWidget : IDisposable
{
    void DefinirAtividade(EstadoAtividade estado);
    SaudeWidget Saude => SaudeWidget.Disponivel;
    string? MotivoEstado => null;
    bool? EmExecucao => null;
}
