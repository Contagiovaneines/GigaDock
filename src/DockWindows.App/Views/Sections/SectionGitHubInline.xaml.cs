using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionGitHubInline : UserControl
{
    public SectionGitHubInline()
    {
        InitializeComponent();
    }

    private void AbrirPainel_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.GitHub.AbrirPerfilCommand.CanExecute(null))
        {
            mainVm.GitHub.AbrirPerfilCommand.Execute(null);
        }
    }
}
