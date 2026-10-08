using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;

namespace DockWindows.App.Views.Sections;

public partial class SectionApps : UserControl
{
    private Point _inicioArraste;
    public SectionApps()
    {
        InitializeComponent();
        _hover = new(() => _main?.ModoAberturaPaineis == "Mouse", anchor =>
        {
            return AbrirVisualizacao(anchor) ? _visualizacao : null;
        }, () => _main?.AtrasoAbrirPreviaMs ?? 350, () => _main?.AtrasoFecharPreviaMs ?? 200);
        Loaded += Section_Loaded;
    }

    private readonly DockWindows.App.Views.HoverFlyout _hover;
    private void App_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement anchor) _hover.Entrar(anchor);
        AtualizarMagnificacao(sender as Button);
    }
    private void App_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _hover.Sair();
        AtualizarMagnificacao(null);
    }
    private void App_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e) => AtualizarMagnificacao(sender as Button);
    private void App_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e) => AtualizarMagnificacao(null);

    private static Button? EncontrarBotao(DependencyObject? root)
    {
        if (root is Button button) return button;
        if (root == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (EncontrarBotao(VisualTreeHelper.GetChild(root, i)) is { } child) return child;
        return null;
    }
    private void AtualizarMagnificacao(Button? alvo, bool imediato = false)
    {
        var buttons = new List<Button>();
        for (int i = 0; i < AppsItems.Items.Count; i++)
            if (EncontrarBotao(AppsItems.ItemContainerGenerator.ContainerFromIndex(i)) is { } button) buttons.Add(button);
        int selected = alvo == null ? -1 : buttons.IndexOf(alvo);
        bool animar = !imediato && _main?.DesativarAnimacoes != true && SystemParameters.ClientAreaAnimation;
        for (int i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            if (button.Template?.FindName("AppScale", button) is not ScaleTransform scale) continue;
            var distancia = selected < 0 ? int.MaxValue : i - selected;
            double destino = animar ? Math.Abs(distancia) switch
            {
                0 => 1.38,
                1 => 1.16,
                2 => 1.06,
                _ => 1.0
            } : 1.0;
            double deslocamento = animar ? distancia switch
            {
                -1 => -6.0,
                1 => 6.0,
                -2 => -3.0,
                2 => 3.0,
                _ => 0.0
            } : 0.0;
            if (button.Parent is UIElement container) Panel.SetZIndex(container, i == selected ? 2 : 0);
            foreach (var property in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
            {
                if (!animar) { scale.BeginAnimation(property, null); scale.SetValue(property, 1.0); }
                else if (scale.HasAnimatedProperties || Math.Abs((double)scale.GetValue(property) - destino) > .001)
                    scale.BeginAnimation(property, new DoubleAnimation(destino, TimeSpan.FromMilliseconds(180))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }, HandoffBehavior.SnapshotAndReplace);
            }
            if (button.Template.FindName("AppShift", button) is TranslateTransform shift)
            {
                if (!animar) { shift.BeginAnimation(TranslateTransform.XProperty, null); shift.X = 0; }
                else shift.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(deslocamento, TimeSpan.FromMilliseconds(190))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } },
                    HandoffBehavior.SnapshotAndReplace);
            }
        }
    }
    private Window? _visualizacao;
    private MainViewModel? _main;
    private void Section_Loaded(object sender, RoutedEventArgs e)
    {
        if (_main != null) _main.PropertyChanged -= Main_PropertyChanged;
        _main = DataContext as MainViewModel;
        if (_main != null) _main.PropertyChanged += Main_PropertyChanged;
        Unloaded -= Section_Unloaded;
        Unloaded += Section_Unloaded;
    }
    private void Main_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.DesativarAnimacoes) or nameof(MainViewModel.AmbienteAtivo) or nameof(MainViewModel.VisualAtivo))
            AtualizarMagnificacao(null, imediato: true);
        if (e.PropertyName is nameof(MainViewModel.AmbienteAtivo) or nameof(MainViewModel.PreviaJanelas) or nameof(MainViewModel.PreviaPastas) or nameof(MainViewModel.ModoAberturaPaineis) or nameof(MainViewModel.VisualAtivo))
        { _hover.Parar(); _visualizacao?.Close(); }
    }
    private void App_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _inicioArraste = e.GetPosition(this);
        // Não consome o clique: o comando do botão sempre deve ativar ou abrir o app.
        _hover.Parar();
        _visualizacao?.Close();
    }

    private void App_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not FrameworkElement { DataContext: AppItemViewModel app } origem || !app.EstaFixado) return;
        var atual = e.GetPosition(this);
        if (Math.Abs(atual.X - _inicioArraste.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(atual.Y - _inicioArraste.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _hover.Parar();
        DragDrop.DoDragDrop(origem, new DataObject("GigaDock.AppFixado", app), DragDropEffects.Move);
    }

    private void App_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("GigaDock.AppFixado") && sender is FrameworkElement { DataContext: AppItemViewModel { EstaFixado: true } }
            ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void App_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel main && sender is FrameworkElement { DataContext: AppItemViewModel destino } && e.Data.GetData("GigaDock.AppFixado") is AppItemViewModel origem)
            main.ReordenarAplicativo(origem, destino);
        e.Handled = true;
    }

    private void App_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        button.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle, () =>
        {
            var escopo = System.Windows.Input.FocusManager.GetFocusScope(button);
            System.Windows.Input.FocusManager.SetFocusedElement(escopo, null);
            System.Windows.Input.Keyboard.ClearFocus();
        });
    }
    private bool AbrirVisualizacao(object sender, bool isClick = false)
    {
        if (DataContext is not MainViewModel main || sender is not FrameworkElement { DataContext: AppItemViewModel app } anchor) return false;

        // Se for um clique e houver apenas 1 janela aberta, não abre a visualização (deixa o clique ativar a janela)
        if (isClick && app.Tipo == TipoItem.Aplicativo && app.Janelas.Count == 1) return false;

        DockWindows.App.Views.DockFlyoutWindow? visual = null;
        if (main.PreviaPastas && app.Tipo == TipoItem.Pasta)
            visual = new DockWindows.App.Views.PastaPreviewWindow(app.CaminhoExecutavel);
        else if (main.PreviaJanelas && app.Janelas.Count > 0)
            visual = new DockWindows.App.Views.JanelasPreviewWindow(app) { AtrasoAposRemoverMs = main.AtrasoAposRemoverPreviaMs };
        if (visual == null) return false;
        visual.FecharAoClicarFora = main.FecharPreviaAoClicarFora;
        _visualizacao?.Close();
        _visualizacao = visual;
        Unloaded -= Section_Unloaded;
        Unloaded += Section_Unloaded;
        visual.MostrarPerto(anchor);
        return true;
    }
    private void Section_Unloaded(object sender, RoutedEventArgs e)
    {
        AtualizarMagnificacao(null, imediato: true);
        _hover.Parar();
        _visualizacao?.Close();
        if (_main != null) _main.PropertyChanged -= Main_PropertyChanged;
        _main = null;
    }
    private void Apps_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Apps_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is MainViewModel mainVm)
        {
            var arquivos = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (arquivos != null)
            {
                foreach (var caminho in arquivos)
                {
                    var isDir = Directory.Exists(caminho);
                    var novo = new ItemFixado
                    {
                        Titulo = isDir ? Path.GetFileName(caminho) : Path.GetFileNameWithoutExtension(caminho),
                        CaminhoOuUrl = caminho,
                        Tipo = isDir ? TipoItem.Pasta : (Path.GetExtension(caminho).Equals(".exe", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(caminho).Equals(".lnk", StringComparison.OrdinalIgnoreCase) ? TipoItem.Aplicativo : TipoItem.Arquivo)
                    };
                    if (string.IsNullOrWhiteSpace(novo.Titulo))
                    {
                        novo.Titulo = caminho;
                    }
                    mainVm.AdicionarAppPermanenteDireto(novo);
                }
            }
            e.Handled = true;
        }
    }
}
