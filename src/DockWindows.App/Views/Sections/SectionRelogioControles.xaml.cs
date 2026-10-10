using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;

namespace DockWindows.App.Views.Sections;

public partial class SectionRelogioControles : UserControl
{
    private readonly DispatcherTimer _trayIdle = new() { Interval = TimeSpan.FromSeconds(15) };
    private Window? _owner;
    public SectionRelogioControles()
    {
        InitializeComponent();
        _trayIdle.Tick += (_, _) => BandejaDockPopup.SetCurrentValue(System.Windows.Controls.Primitives.Popup.IsOpenProperty, false);
        Unloaded += (_, _) => { BandejaDockPopup.SetCurrentValue(System.Windows.Controls.Primitives.Popup.IsOpenProperty, false); DetachOwner(); _trayIdle.Stop(); };
    }

    private void BandejaDock_Opened(object sender, EventArgs e)
    {
        DetachOwner();
        _owner = Window.GetWindow(this);
        if (_owner != null) _owner.Deactivated += Owner_Deactivated;
        _trayIdle.Start();
    }
    private void BandejaDock_Closed(object sender, EventArgs e)
    { _trayIdle.Stop(); DetachOwner(); }
    private void Owner_Deactivated(object? sender, EventArgs e) => BandejaDockPopup.SetCurrentValue(System.Windows.Controls.Primitives.Popup.IsOpenProperty, false);
    private void DetachOwner()
    {
        if (_owner != null) _owner.Deactivated -= Owner_Deactivated;
        _owner = null;
    }
    private void BandejaDock_Interaction(object sender, RoutedEventArgs e)
    { _trayIdle.Stop(); _trayIdle.Start(); }
    private void BandejaDock_Close(object sender, RoutedEventArgs e) => BandejaDockPopup.SetCurrentValue(System.Windows.Controls.Primitives.Popup.IsOpenProperty, false);
}
