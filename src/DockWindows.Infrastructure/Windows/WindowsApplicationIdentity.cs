using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DockWindows.Infrastructure.Windows;

public static class WindowsApplicationIdentity
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetApplicationUserModelId(SafeProcessHandle process, ref uint length, [Out] StringBuilder? id);

    public static string Read(int processId)
    {
        if (!OperatingSystem.IsWindows() || processId <= 0) return "";
        using var process = OpenProcess(0x1000, false, processId); // PROCESS_QUERY_LIMITED_INFORMATION
        if (process.IsInvalid) return "";
        uint length = 0;
        if (GetApplicationUserModelId(process, ref length, null) != 122 || length is 0 or > 1024) return "";
        var id = new StringBuilder((int)length);
        return GetApplicationUserModelId(process, ref length, id) == 0 ? id.ToString() : "";
    }
}
