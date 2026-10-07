using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public class Win32WindowTrackingService : IWindowTrackingService
{
        public event Action? JanelasAlteradas;
    public event Action<IntPtr>? JanelaAtivada;
    public event Action<bool>? TelaCheiaAlterada;

    private readonly System.Timers.Timer _pollTimer;
    private IntPtr _hHookForeground = IntPtr.Zero;
    private IntPtr _hHookWindow = IntPtr.Zero;
    private WinEventDelegate? _procForeground;
    private WinEventDelegate? _procWindow;
    private bool _isDisposed;
    private IntPtr _ultimaJanelaAtiva = IntPtr.Zero;
    private bool _ultimoEstadoTelaCheia = false;
    private readonly object _lock = new();

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint EVENT_OBJECT_DESTROY = 0x8001;
    private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
    private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const uint GW_OWNER = 4;
    private const int DWMWA_CLOAKED = 14;

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;
    private const int SW_MINIMIZE = 6;
    private const uint WM_CLOSE = 0x0010;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : (IntPtr)GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, StringBuilder text, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    public Win32WindowTrackingService()
    {
        _pollTimer = new System.Timers.Timer(600)
        {
            AutoReset = true
        };
        _pollTimer.Elapsed += (s, e) => VerificarMudancas();
    }

    public void Iniciar()
    {
        lock (_lock)
        {
            if (_procForeground == null)
            {
                _procForeground = OnForegroundChanged;
                _hHookForeground = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _procForeground,
                    0, 0,
                    WINEVENT_OUTOFCONTEXT);
            }

            if (_procWindow == null)
            {
                _procWindow = OnWindowChanged;
                _hHookWindow = SetWinEventHook(
                    EVENT_OBJECT_CREATE,
                    EVENT_OBJECT_DESTROY,
                    IntPtr.Zero,
                    _procWindow,
                    0, 0,
                    WINEVENT_OUTOFCONTEXT);
            }

            _pollTimer.Start();
        }
    }

    public void Parar()
    {
        lock (_lock)
        {
            _pollTimer.Stop();

            if (_hHookForeground != IntPtr.Zero)
            {
                UnhookWinEvent(_hHookForeground);
                _hHookForeground = IntPtr.Zero;
            }

            if (_hHookWindow != IntPtr.Zero)
            {
                UnhookWinEvent(_hHookWindow);
                _hHookWindow = IntPtr.Zero;
            }
        }
    }

    private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (idObject != 0 || hwnd == IntPtr.Zero) return;
        ProcessarNovoForeground(hwnd);
    }

    private void OnWindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (idObject != 0 || hwnd == IntPtr.Zero) return;
        InvocarSeguro(JanelasAlteradas);
    }

    private void VerificarMudancas()
    {
        try
        {
            var foreground = GetForegroundWindow();
            ProcessarNovoForeground(foreground);
        }
        catch { }
    }

        private void ProcessarNovoForeground(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;

        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == Environment.ProcessId)
        {
            return;
        }

        if (_ultimaJanelaAtiva != hwnd)
        {
            _ultimaJanelaAtiva = hwnd;
            InvocarSeguro(JanelaAtivada, hwnd);
            InvocarSeguro(JanelasAlteradas);
        }

        bool ehTelaCheia = false;
        try
        {
            if (GetWindowRect(hwnd, out RECT rect))
            {
                IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (hMonitor != IntPtr.Zero)
                {
                    MONITORINFO mi = new MONITORINFO();
                    mi.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFO));
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        if (rect.Left <= mi.rcMonitor.Left &&
                            rect.Top <= mi.rcMonitor.Top &&
                            rect.Right >= mi.rcMonitor.Right &&
                            rect.Bottom >= mi.rcMonitor.Bottom)
                        {
                            var sb = new System.Text.StringBuilder(256);
                            GetClassName(hwnd, sb, sb.Capacity);
                            string className = sb.ToString();
                                                        if (className != "WorkerW" && className != "Progman" && className != "ScreenClippingHost" && className != "SnippingTool" && className != "Credential Dialog Xaml Host" && className != "ForegroundStaging")
                            {
                                var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
                                const long WS_EX_TOOLWINDOW = 0x00000080L;
                                const long WS_EX_TRANSPARENT = 0x00000020L;
                                
                                if ((exStyle & WS_EX_TOOLWINDOW) == 0 && (exStyle & WS_EX_TRANSPARENT) == 0)
                                {
                                    var titulo = new StringBuilder(512);
                                    GetWindowText(hwnd, titulo, titulo.Capacity);
                                    var executavel = ObterCaminhoProcesso(pid);
                                    ehTelaCheia = DockWindows.Core.Services.DeteccaoVideoTelaCheia.Reconhecer(executavel, titulo.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }

        if (_ultimoEstadoTelaCheia != ehTelaCheia)
        {
            _ultimoEstadoTelaCheia = ehTelaCheia;
            InvocarSeguro(TelaCheiaAlterada, ehTelaCheia);
        }
    }

    private static void InvocarSeguro(Action? evento)
    {
        if (evento == null) return;
        foreach (Action handler in evento.GetInvocationList()) try { handler(); } catch { }
    }

    private static void InvocarSeguro<T>(Action<T>? evento, T valor)
    {
        if (evento == null) return;
        foreach (Action<T> handler in evento.GetInvocationList()) try { handler(valor); } catch { }
    }

    public IntPtr ObterJanelaAtiva()
    {
        // Em vez de chamar GetForegroundWindow() agora, retorna o Ãºltimo que rastreamos (ignora a Dock)
        return _ultimaJanelaAtiva;
    }

    public IReadOnlyList<JanelaInfo> ObterJanelasAbertas()
    {
        var lista = new List<JanelaInfo>();
        var foreground = GetForegroundWindow();
        int meuPid = Environment.ProcessId;

        EnumWindows((hWnd, lParam) =>
        {
            if (!EhJanelaValida(hWnd, meuPid)) return true;

            var titulo = ObterTextoJanela(hWnd);
            if (string.IsNullOrWhiteSpace(titulo)) return true;

            GetWindowThreadProcessId(hWnd, out uint pid);
            var caminhoExe = ObterCaminhoProcesso(pid);
            var nomeProcesso = Path.GetFileNameWithoutExtension(caminhoExe);
            if (string.IsNullOrEmpty(nomeProcesso))
            {
                try
                {
                    using var processo = Process.GetProcessById((int)pid);
                    nomeProcesso = processo.ProcessName;
                }
                catch { }
            }

            bool estaAtiva = (hWnd == foreground);
            bool estaMinimizada = IsIconic(hWnd);

            lista.Add(new JanelaInfo
            {
                Hwnd = hWnd,
                Titulo = titulo,
                CaminhoExecutavel = caminhoExe,
                NomeProcesso = nomeProcesso,
                ProcessId = (int)pid,
                EstaAtiva = estaAtiva,
                EstaMinimizada = estaMinimizada
            });

            return true;
        }, IntPtr.Zero);

        return lista;
    }

    private static bool EhJanelaValida(IntPtr hWnd, int meuPid)
    {
        if (!IsWindowVisible(hWnd)) return false;

        // Verifica se a janela estÃ¡ oculta/suspensa pelo DWM (Windows 10/11)
        int cloaked = 0;
        DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
        if (cloaked != 0) return false;

        // Ignora a nossa prÃ³pria aplicaÃ§Ã£o Dock Windows
        GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == meuPid) return false;

        // Estilos de janela
        var exStyle = (long)GetWindowLongPtr(hWnd, GWL_EXSTYLE);
        if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
        {
            return false;
        }

        // Janela proprietÃ¡ria (owned) geralmente nÃ£o Ã© janela de aplicativo de topo
        var owner = GetWindow(hWnd, GW_OWNER);
        if (owner != IntPtr.Zero && (exStyle & WS_EX_APPWINDOW) == 0)
        {
            return false;
        }

        // Classes de sistema a ignorar
        var sbClasse = new StringBuilder(256);
        GetClassName(hWnd, sbClasse, sbClasse.Capacity);
        var classe = sbClasse.ToString();

        if (classe == "Shell_TrayWnd" ||
            classe == "Progman" ||
            classe == "WorkerW" ||
            classe == "Shell_SecondaryTrayWnd" ||
            classe == "DV2ControlHost")
        {
            return false;
        }

        return true;
    }

    private static string ObterTextoJanela(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length == 0) return string.Empty;

        var sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string ObterCaminhoProcesso(uint pid)
    {
        IntPtr hProc = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
        if (hProc != IntPtr.Zero)
        {
            try
            {
                int size = 1024;
                var sb = new StringBuilder(size);
                if (QueryFullProcessImageName(hProc, 0, sb, ref size))
                {
                    return sb.ToString();
                }
            }
            finally
            {
                CloseHandle(hProc);
            }
        }

        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return proc.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool AtivarJanela(IntPtr hWnd)
    {
        try
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return false;

            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
            }
            else
            {
                ShowWindow(hWnd, SW_SHOW);
            }

            BringWindowToTop(hWnd);
            // O clique do usuário na dock permite a transferência de primeiro plano.
            keybd_event(0, 0, 0, 0);
            SetForegroundWindow(hWnd);
            return GetForegroundWindow() == hWnd || !IsIconic(hWnd);
        }
        catch
        {
            return false;
        }
    }

    public bool MinimizarJanela(IntPtr hWnd)
    {
        try
        {
            return ShowWindow(hWnd, SW_MINIMIZE);
        }
        catch
        {
            return false;
        }
    }

    public bool FecharJanela(IntPtr hWnd)
    {
        try
        {
            return PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            Parar();
            _pollTimer.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}








