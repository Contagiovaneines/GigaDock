using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionControlesRapidos : UserControl
{
    public SectionControlesRapidos() => InitializeComponent();

    private void PanelPopup_Opened(object? sender, EventArgs e)
    {
        Panel.Width = Math.Min(360, Math.Max(240, SystemParameters.WorkArea.Width - 24));
        OptionsScroller.MaxHeight = Math.Min(420, Math.Max(100, SystemParameters.WorkArea.Height - 260));
        Panel.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    private void Panel_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (DataContext is MainViewModel vm) vm.ControlesRapidos.Aberto = false;
        OpenButton.Focus();
        e.Handled = true;
    }
}
