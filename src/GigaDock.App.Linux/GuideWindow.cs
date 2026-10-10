using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using DockWindows.Core.Models;

namespace GigaDock.App.Linux;

internal sealed class GuideWindow : Window
{
    public GuideWindow(Action<string> navigate)
    {
        Title = "Conhecer o GigaDock"; Width = 500; Height = 340; MinWidth = 340; MinHeight = 260;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Visuals.Brush("#191C21");
        var panel = new StackPanel { Spacing = 16, Margin = new Thickness(24) };
        var progress = Visuals.Text("", 12, "#A9B8C9"); var title = Visuals.Text("", 22); var description = Visuals.Text("");
        panel.Children.Add(progress); panel.Children.Add(title); panel.Children.Add(description);
        var controls = new WrapPanel(); panel.Children.Add(controls); Content = new ScrollViewer { Content = panel };
        var step = 0;
        void Draw()
        {
            var current = GuideSteps.All[step]; progress.Text = $"Passo {step + 1} de {GuideSteps.All.Count}"; title.Text = current.Title; description.Text = current.Description;
            controls.Children.Clear();
            if (step > 0) controls.Children.Add(Visuals.Button("Voltar", () => { step--; Draw(); }));
            controls.Children.Add(Visuals.Button("Abrir esta seção", () => { navigate(current.LinuxSection); Close(); }));
            if (step < GuideSteps.All.Count - 1) controls.Children.Add(Visuals.Button("Próximo", () => { step++; Draw(); }));
            controls.Children.Add(Visuals.Button(step == GuideSteps.All.Count - 1 ? "Concluir" : "Pular guia", Close));
        }
        KeyDown += (_, key) => { if (key.Key == Key.Escape) Close(); }; Draw();
    }
}
