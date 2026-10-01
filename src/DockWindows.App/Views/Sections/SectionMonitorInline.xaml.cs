using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionMonitorInline : UserControl
{
    public SectionMonitorInline()
    {
        InitializeComponent();
    }

    private void AbrirPainel_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.MonitorSistema != null)
        {
            mainVm.MonitorSistema.PainelAberto = !mainVm.MonitorSistema.PainelAberto;
        }
    }
}
