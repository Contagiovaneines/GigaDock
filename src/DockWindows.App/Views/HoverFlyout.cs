using System.Windows;
using System.Windows.Threading;

namespace DockWindows.App.Views;

// Timer apenas durante a aproximação/saída; não mant?m polling em repouso.
internal sealed class HoverFlyout
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly Func<FrameworkElement, Window?> _abrir;
    private readonly Func<bool> _habilitado;
    private FrameworkElement? _anchor;
    private Window? _window;
    private bool _fechando;

    public HoverFlyout(Func<bool> habilitado, Func<FrameworkElement, Window?> abrir)
    {
        _habilitado = habilitado;
        _abrir = abrir;
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            if (_fechando)
            {
                if (_anchor?.IsMouseOver != true && _window?.IsMouseOver != true) Parar();
            }
            else if (_habilitado() && _anchor?.IsMouseOver == true)
            {
                _window = _abrir(_anchor);
                if (_window != null)
                {
                    _window.MouseEnter += Janela_Entrou;
                    _window.MouseLeave += Janela_Saiu;
                    _window.Closed += Janela_Fechou;
                }
            }
        };
    }
    private void Janela_Entrou(object sender, System.Windows.Input.MouseEventArgs e) => _timer.Stop();
    private void Janela_Saiu(object sender, System.Windows.Input.MouseEventArgs e) => Sair();
    private void Janela_Fechou(object? sender, EventArgs e)
    {
        if (sender is not Window window) return;
        window.MouseEnter -= Janela_Entrou; window.MouseLeave -= Janela_Saiu; window.Closed -= Janela_Fechou;
        if (ReferenceEquals(_window, window)) { _window = null; _anchor = null; _timer.Stop(); }
    }
    public void Entrar(FrameworkElement anchor)
    {
        if (!_habilitado()) return;
        Parar();
        _anchor = anchor;
        _fechando = false;
        _timer.Start();
    }
    public void Sair()
    {
        if (_anchor == null) return;
        _timer.Stop();
        _fechando = true;
        _timer.Start();
    }
    public void Parar()
    {
        _timer.Stop();
        _window?.Close();
        _window = null;
        _anchor = null;
    }
}
