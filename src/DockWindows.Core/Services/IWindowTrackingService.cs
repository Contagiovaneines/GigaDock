using System;
using System.Collections.Generic;
using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public interface IWindowTrackingService : IDisposable
{
    event Action? JanelasAlteradas;
    event Action<IntPtr>? JanelaAtivada;
    event Action<bool>? TelaCheiaAlterada;

    IReadOnlyList<JanelaInfo> ObterJanelasAbertas();
    IntPtr ObterJanelaAtiva();
    void Iniciar();
    void Parar();
    bool AtivarJanela(IntPtr hWnd);
    bool AtivarAplicativoEmExecucao(string caminhoExecutavel) => false;
    bool MinimizarJanela(IntPtr hWnd);
    bool FecharJanela(IntPtr hWnd);
}
