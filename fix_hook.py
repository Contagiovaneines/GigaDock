import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\MainWindow.xaml.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

dll_imports = """using System.Runtime.InteropServices;

namespace DockWindows.App;

public partial class MainWindow : Window
{
    [DllImport("user32.dll", EntryPoint = "RegisterShellHookWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterShellHookWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessage")]
    public static extern uint RegisterWindowMessage(string lpString);

    private uint _shellHookMessage;
    private const int HSHELL_FLASH = 0x8006;
"""

content = re.sub(r"namespace DockWindows.App;\s*public partial class MainWindow : Window\s*\{", dll_imports, content, flags=re.DOTALL)

loaded_pattern = r"private void MainWindow_Loaded\(object sender, RoutedEventArgs e\)\s*\{"
loaded_replacement = """private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        RegisterShellHookWindow(hwnd);
        _shellHookMessage = RegisterWindowMessage("SHELLHOOK");
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
"""
content = re.sub(loaded_pattern, loaded_replacement, content)

wndproc = """
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == _shellHookMessage)
        {
            if (wParam.ToInt32() == HSHELL_FLASH)
            {
                _viewModel.IncrementarNotificacaoApp(lParam);
            }
        }
        return IntPtr.Zero;
    }
}
"""
content = re.sub(r"\}\s*$", wndproc, content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
