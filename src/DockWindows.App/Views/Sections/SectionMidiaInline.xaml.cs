using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionMidiaInline : UserControl
{
    private readonly DispatcherTimer _abrir = new() { Interval = TimeSpan.FromMilliseconds(220) };
    private readonly DispatcherTimer _fechar = new() { Interval = TimeSpan.FromMilliseconds(260) };
    private bool _mouseHost, _mousePopup, _foco;

    public SectionMidiaInline()
    {
        InitializeComponent();
        _abrir.Tick += (_, _) =>
        {
            _abrir.Stop();
            if ((_mouseHost || _foco) && EhExpansivel()) ExpandedMediaPopup.IsOpen = true;
        };
        _fechar.Tick += (_, _) =>
        {
            _fechar.Stop();
            if (!_mouseHost && !_mousePopup && !_foco) ExpandedMediaPopup.IsOpen = false;
        };
    }

    private bool EhExpansivel() => DataContext is MainViewModel main && main.Midia.Estilo == "expansivel";
    private void AgendarAbertura()
    {
        _fechar.Stop();
        if (!EhExpansivel()) { ExpandedMediaPopup.IsOpen = false; return; }
        _abrir.Stop();
        _abrir.Start();
    }
    private void AgendarFechamento()
    {
        _abrir.Stop();
        _fechar.Stop();
        _fechar.Start();
    }
    private void ModernMediaHost_MouseEnter(object sender, MouseEventArgs e) { _mouseHost = true; AgendarAbertura(); }
    private void ModernMediaHost_MouseLeave(object sender, MouseEventArgs e) { _mouseHost = false; AgendarFechamento(); }
    private void ExpandedMediaPopup_MouseEnter(object sender, MouseEventArgs e) { _mousePopup = true; _fechar.Stop(); }
    private void ExpandedMediaPopup_MouseLeave(object sender, MouseEventArgs e) { _mousePopup = false; AgendarFechamento(); }
    private void ModernMediaHost_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) { _foco = true; AgendarAbertura(); }
    private void ModernMediaHost_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) { _foco = false; AgendarFechamento(); }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _abrir.Stop();
        _fechar.Stop();
        ExpandedMediaPopup.IsOpen = false;
        _mouseHost = _mousePopup = _foco = false;
    }
}
