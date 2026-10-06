using System.Windows;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Common;

/// <summary>Um token por elemento Loaded; vários elementos podem representar o mesmo widget.</summary>
public static class AtividadeVisual
{
    public static readonly DependencyProperty ComponenteProperty = DependencyProperty.RegisterAttached(
        "Componente", typeof(string), typeof(AtividadeVisual), new PropertyMetadata(null, Configurar));
    private static readonly DependencyProperty RegistroProperty = DependencyProperty.RegisterAttached(
        "Registro", typeof(IDisposable), typeof(AtividadeVisual));
    public static void SetComponente(DependencyObject element, string value) => element.SetValue(ComponenteProperty, value);
    public static string GetComponente(DependencyObject element) => (string)element.GetValue(ComponenteProperty);
    private static MainViewModel? ObterMain(FrameworkElement element) => element.DataContext as MainViewModel ?? Window.GetWindow(element)?.DataContext as MainViewModel;
    private static void Configurar(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement element) return;
        element.Loaded -= Loaded; element.Unloaded -= Unloaded; element.IsVisibleChanged -= Visible; element.DataContextChanged -= Context;
        Liberar(element);
        if (args.NewValue is not string) return;
        element.Loaded += Loaded; element.Unloaded += Unloaded; element.IsVisibleChanged += Visible; element.DataContextChanged += Context;
        if (element.IsLoaded) Registrar(element);
    }
    private static void Loaded(object sender, RoutedEventArgs e) => Registrar((FrameworkElement)sender);
    private static void Unloaded(object sender, RoutedEventArgs e) => Liberar((FrameworkElement)sender);
    private static void Context(object sender, DependencyPropertyChangedEventArgs e)
    { var element = (FrameworkElement)sender; Liberar(element); if (element.IsLoaded) Registrar(element); }
    private static void Visible(object sender, DependencyPropertyChangedEventArgs e)
    { var element = (FrameworkElement)sender; ObterMain(element)?.Atividade.Visibilidade(GetComponente(element), element, element.IsVisible); }
    private static void Registrar(FrameworkElement element)
    {
        Liberar(element);
        if (ObterMain(element) is { } main)
            element.SetValue(RegistroProperty, main.Atividade.Renderizar(GetComponente(element), element, element.IsVisible));
    }
    private static void Liberar(FrameworkElement element)
    { (element.GetValue(RegistroProperty) as IDisposable)?.Dispose(); element.ClearValue(RegistroProperty); }
}
