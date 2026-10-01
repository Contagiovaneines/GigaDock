using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionNotasInline : UserControl
{
    public SectionNotasInline()
    {
        InitializeComponent();
    }

    private void AbrirPainel_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.Notas != null)
        {
            mainVm.Notas.PainelAberto = !mainVm.Notas.PainelAberto;
        }
    }
}
