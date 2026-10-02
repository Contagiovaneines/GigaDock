using System.Windows.Controls;
using System.Windows.Input;

namespace DockWindows.App.Views.Sections;

public partial class SectionGitHubInline : UserControl
{
    public SectionGitHubInline()
    {
        InitializeComponent();
    }

    private void AbrirPainel_Click(object sender, MouseButtonEventArgs e)
    {
        PopupGitHub.IsOpen = !PopupGitHub.IsOpen;
    }
}
