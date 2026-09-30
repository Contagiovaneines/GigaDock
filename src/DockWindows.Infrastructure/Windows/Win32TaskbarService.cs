using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace DockWindows.Infrastructure.Windows;

public interface ITaskbarService : IDisposable
{
    int ObterEstadoAtual();
    bool OcultarBarraNativa(out int estadoAnterior);
    bool RestaurarBarraNativa(int? estadoAnterior = null);
    void GarantirBarraOculta();
}

public class Win32TaskbarService : ITaskbarService
{
    private const int ABM_GETSTATE = 0x00000004;
    private const int ABM_SETSTATE = 0x0000000A;

    private const int ABS_AUTOHIDE = 0x00000001;
    private const int ABS_ALWAYSONTOP = 0x00000002;

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    [DllImport("shell32.dll")]
    private static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowTitle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private readonly System.Timers.Timer _watchdogTimer;
    private bool _watchdogAtivo;
    private int _ultimoEstadoNativo = ABS_ALWAYSONTOP;
    private readonly object _lock = new();

    public Win32TaskbarService()
    {
        _watchdogTimer = new System.Timers.Timer(250)
        {
            AutoReset = true
        };
        _watchdogTimer.Elapsed += (s, e) =>
        {
            if (_watchdogAtivo)
            {
                GarantirBarraOculta();
            }
        };
    }

    public int ObterEstadoAtual()
    {
        try
        {
            var abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = FindWindow("Shell_TrayWnd", null)
            };

            var state = SHAppBarMessage(ABM_GETSTATE, ref abd);
            return state.ToInt32();
        }
        catch
        {
            return ABS_ALWAYSONTOP;
        }
    }

    public bool OcultarBarraNativa(out int estadoAnterior)
    {
        estadoAnterior = ObterEstadoAtual();
        _ultimoEstadoNativo = estadoAnterior;

        lock (_lock)
        {
            try
            {
                var hWndTray = FindWindow("Shell_TrayWnd", null);
                if (hWndTray == IntPtr.Zero) return false;

                // 1. Configura AppBar para ABS_AUTOHIDE para o Explorer recalcular área de trabalho
                var abd = new APPBARDATA
                {
                    cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                    hWnd = hWndTray,
                    lParam = (IntPtr)ABS_AUTOHIDE
                };
                SHAppBarMessage(ABM_SETSTATE, ref abd);

                // 2. Desativa e oculta a janela Shell_TrayWnd e monitores secundários
                SuprimirJanelasBarra();

                // 3. Ativa o watchdog contínuo para evitar que o Explorer ou o clique na borda ressuscite a barra nativa
                _watchdogAtivo = true;
                _watchdogTimer.Start();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public void GarantirBarraOculta()
    {
        lock (_lock)
        {
            if (!_watchdogAtivo) return;
            SuprimirJanelasBarra();
        }
    }

    private static void SuprimirJanelasBarra()
    {
        try
        {
            var hWndTray = FindWindow("Shell_TrayWnd", null);
            if (hWndTray != IntPtr.Zero)
            {
                if (IsWindowVisible(hWndTray))
                {
                    ShowWindow(hWndTray, SW_HIDE);
                }
                EnableWindow(hWndTray, false);
            }

            // Oculta e desativa barras em monitores secundários
            IntPtr hWndSec = IntPtr.Zero;
            while ((hWndSec = FindWindowEx(IntPtr.Zero, hWndSec, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
            {
                if (IsWindowVisible(hWndSec))
                {
                    ShowWindow(hWndSec, SW_HIDE);
                }
                EnableWindow(hWndSec, false);
            }
        }
        catch { }
    }

    public bool RestaurarBarraNativa(int? estadoAnterior = null)
    {
        lock (_lock)
        {
            _watchdogAtivo = false;
            _watchdogTimer.Stop();

            try
            {
                var hWndTray = FindWindow("Shell_TrayWnd", null);
                if (hWndTray != IntPtr.Zero)
                {
                    // Reabilita e exibe a barra principal
                    EnableWindow(hWndTray, true);
                    ShowWindow(hWndTray, SW_SHOW);

                    // Reabilita e exibe barras de monitores secundários
                    IntPtr hWndSec = IntPtr.Zero;
                    while ((hWndSec = FindWindowEx(IntPtr.Zero, hWndSec, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
                    {
                        EnableWindow(hWndSec, true);
                        ShowWindow(hWndSec, SW_SHOW);
                    }

                    int estadoParaRestaurar = estadoAnterior ?? _ultimoEstadoNativo;
                    if (estadoParaRestaurar == 0) estadoParaRestaurar = ABS_ALWAYSONTOP;

                    var abd = new APPBARDATA
                    {
                        cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
                        hWnd = hWndTray,
                        lParam = (IntPtr)estadoParaRestaurar
                    };

                    SHAppBarMessage(ABM_SETSTATE, ref abd);
                    return true;
                }
            }
            catch { }

            return false;
        }
    }

    public void Dispose()
    {
        _watchdogAtivo = false;
        _watchdogTimer?.Stop();
        _watchdogTimer?.Dispose();
    }
}
