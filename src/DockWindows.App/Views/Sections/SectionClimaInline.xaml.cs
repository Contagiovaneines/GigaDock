using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionClimaInline : UserControl
{
    private DockFlyoutWindow? _details;
    private readonly HoverFlyout _hover;
    private MainViewModel? _main;
    private void Clima_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    { if (sender is FrameworkElement anchor) _hover.Entrar(anchor); }
    private void Clima_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => _hover.Sair();
    public SectionClimaInline()
    {
        InitializeComponent();
        _hover = new(() => (DataContext as MainViewModel) is { ModoAberturaPaineis: "Mouse", Clima.Habilitado: true, ClimaExpandido: true }, anchor =>
        { ClimaDetalhes_Click(anchor, new RoutedEventArgs()); return _details; });
        Loaded += (_, _) =>
        {
            _main = DataContext as MainViewModel;
            if (_main != null) _main.PropertyChanged += Main_Changed;
        };
        Unloaded += (_, _) =>
        {
            _hover.Parar(); _details?.Close();
            if (_main != null) _main.PropertyChanged -= Main_Changed;
            _main = null;
        };
    }
    private void Main_Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.AmbienteAtivo) or nameof(MainViewModel.ModoAberturaPaineis) or nameof(MainViewModel.VisualAtivo) or nameof(MainViewModel.ClimaExpandido))
        { _hover.Parar(); _details?.Close(); }
    }
    private void ClimaDetalhes_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel { Clima.Habilitado: true, ClimaExpandido: true } main || sender is not FrameworkElement anchor) return;
        _details?.Close();
        _details = new DockFlyoutWindow("Previsão do tempo");
        var panel = (Border)_details.Content;
        var stack = (StackPanel)panel.Child;
        stack.Children.Add(new ClimaDetalhesControl { DataContext = main.Clima });
        _details.MostrarPerto(anchor);
    }
}
