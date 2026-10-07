using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionWidgets : UserControl
{
    public SectionWidgets()
    {
        InitializeComponent();
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
