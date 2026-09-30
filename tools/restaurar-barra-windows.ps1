<#
.SYNOPSIS
    Script de Recuperação de Emergência — Restaurar Barra de Tarefas do Windows
.DESCRIPTION
    Restaura o estado normal (sem auto-hide), reabilita a janela Shell_TrayWnd e garante a visibilidade da barra de tarefas nativa do Windows em todos os monitores.
#>

$signature = @'
[DllImport("shell32.dll")]
public static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

[DllImport("user32.dll", SetLastError = true)]
public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

[DllImport("user32.dll", SetLastError = true)]
public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

[DllImport("user32.dll")]
public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

[DllImport("user32.dll")]
public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct APPBARDATA {
    public int cbSize;
    public IntPtr hWnd;
    public int uCallbackMessage;
    public int uEdge;
    public int rcLeft, rcTop, rcRight, rcBottom;
    public IntPtr lParam;
}
'@

try {
    $type = Add-Type -MemberDefinition $signature -Name "Win32AppBarHelper" -Namespace "DockWindowsRecovery" -PassThru
    $hWnd = [DockWindowsRecovery.Win32AppBarHelper]::FindWindow("Shell_TrayWnd", $null)

    if ($hWnd -ne [IntPtr]::Zero) {
        # Reabilita e exibe
        [DockWindowsRecovery.Win32AppBarHelper]::EnableWindow($hWnd, $true)
        # SW_SHOW = 5
        [DockWindowsRecovery.Win32AppBarHelper]::ShowWindow($hWnd, 5)

        # Trata monitores secundários
        $hWndSec = [IntPtr]::Zero
        while (($hWndSec = [DockWindowsRecovery.Win32AppBarHelper]::FindWindowEx([IntPtr]::Zero, $hWndSec, "Shell_SecondaryTrayWnd", $null)) -ne [IntPtr]::Zero) {
            [DockWindowsRecovery.Win32AppBarHelper]::EnableWindow($hWndSec, $true)
            [DockWindowsRecovery.Win32AppBarHelper]::ShowWindow($hWndSec, 5)
        }

        $abd = New-Object DockWindowsRecovery.Win32AppBarHelper+APPBARDATA
        $abd.cbSize = [System.Runtime.InteropServices.Marshal]::SizeOf($abd)
        $abd.hWnd = $hWnd
        # ABS_ALWAYSONTOP = 2
        $abd.lParam = [IntPtr]2

        # ABM_SETSTATE = 10 (0x0A)
        [DockWindowsRecovery.Win32AppBarHelper]::SHAppBarMessage(10, [ref]$abd)

        Write-Host "Sucesso: A barra de tarefas nativa do Windows foi restaurada e reabilitada." -ForegroundColor Green
    } else {
        Write-Host "Aviso: Janela Shell_TrayWnd não localizada. A barra de tarefas já deve estar visível." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Erro ao restaurar barra de tarefas: $_" -ForegroundColor Red
}
