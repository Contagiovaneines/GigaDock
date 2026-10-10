using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.ViewModels;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DockWindows.App.Views.Sections;

public partial class SectionWidgets : UserControl
{
    public SectionWidgets()
    {
        InitializeComponent();
    }

    private void Tarefas_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel main || main.AmbienteAtivo is null || sender is not Button button) return;
        if (button.Tag is Popup old) old.IsOpen = false;
        var popup = new Popup { PlacementTarget = button, Placement = PlacementMode.Top, StaysOpen = false, AllowsTransparency = true,
            Child = new Border { Background = new SolidColorBrush(Color.FromRgb(25, 28, 33)), Padding = new Thickness(16),
                CornerRadius = new CornerRadius(12), Child = new TarefasPanel(main.AmbienteAtivo.Id) } };
        popup.KeyDown += (_, key) => { if (key.Key == Key.Escape) { popup.IsOpen = false; key.Handled = true; } };
        popup.Opened += (_, _) => ((FrameworkElement)popup.Child).MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        System.ComponentModel.PropertyChangedEventHandler closeOnEnvironment = (_, changed) =>
        {
            if (changed.PropertyName == nameof(main.AmbienteAtivo) || changed.PropertyName == nameof(main.TarefasHabilitadas)) popup.IsOpen = false;
        };
        main.PropertyChanged += closeOnEnvironment;
        popup.Closed += (_, _) => { main.PropertyChanged -= closeOnEnvironment; button.Unloaded -= CloseWhenUnloaded; button.Tag = null; button.Focus(); };
        button.Unloaded += CloseWhenUnloaded;
        void CloseWhenUnloaded(object source, RoutedEventArgs args) { popup.IsOpen = false; button.Unloaded -= CloseWhenUnloaded; }
        button.Tag = popup; popup.IsOpen = true;
    }

    private void AudioSistemaButton_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not MainViewModel main) return;
        main.AudioSistema.AjustarVolumeMestre(e.Delta > 0 ? 2 : -2);
        e.Handled = true;
    }

    private void AudioSistemaButton_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || DataContext is not MainViewModel main) return;
        main.AudioSistema.AlternarMudoMestre();
        e.Handled = true;
    }

    private void VolumeMestre_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        => AudioSistemaButton_OnPreviewMouseWheel(sender, e);

    private void VolumeMestre_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        => AudioSistemaButton_OnPreviewMouseDown(sender, e);

    private static SessaoAudioViewModel? ObterSessao(DependencyObject origem)
    {
        if (origem is FrameworkElement elemento && elemento.DataContext is SessaoAudioViewModel sessao) return sessao;
        return null;
    }

    private void SessaoAudio_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var sessao = ObterSessao((DependencyObject)sender);
        if (sessao is null) return;
        sessao.AjustarVolume(e.Delta > 0 ? 2 : -2);
        e.Handled = true;
    }

    private void SessaoAudio_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        var sessao = ObterSessao((DependencyObject)sender);
        if (sessao is null) return;
        sessao.AlternarMudo();
        e.Handled = true;
    }
}
