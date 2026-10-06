using System.Windows.Automation;

namespace DockWindows.Infrastructure.Windows;

public sealed class BandejaOcultaService
{
    public string? Abrir()
    {
        try
        {
            var barras = AutomationElement.RootElement.FindAll(TreeScope.Children,
                new OrCondition(new PropertyCondition(AutomationElement.ClassNameProperty, "Shell_TrayWnd"),
                    new PropertyCondition(AutomationElement.ClassNameProperty, "Shell_SecondaryTrayWnd")));
            foreach (AutomationElement barra in barras)
            {
                var botoes = barra.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                foreach (AutomationElement botao in botoes)
                {
                    var nome = botao.Current.Name;
                    if (!nome.Contains("ícones ocultos", StringComparison.OrdinalIgnoreCase) &&
                        !nome.Contains("hidden icons", StringComparison.OrdinalIgnoreCase)) continue;
                    // Auto-ocultação pode marcar o botão como fora da tela; Invoke
                    // não depende de coordenadas nem exige simular um clique.
                    if (!botao.Current.IsEnabled) continue;
                    if (botao.TryGetCurrentPattern(InvokePattern.Pattern, out var invocar))
                    {
                        ((InvokePattern)invocar).Invoke();
                        return null;
                    }
                    if (botao.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandir))
                    {
                        ((ExpandCollapsePattern)expandir).Expand();
                        return null;
                    }
                }
            }
            return "O Windows não disponibilizou o botão de ícones ocultos. Verifique se a barra de tarefas está visível e se há ícones ocultos na bandeja.";
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        {
            return "Não foi possível abrir os ícones ocultos do Windows. " + ex.Message;
        }
    }
}
