using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public sealed class LixeiraDesktopService : ILixeiraDesktopService
{
    private const string Caminho = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
    private const string LixeiraId = "{645FF040-5081-101B-9F08-00AA002F954E}";

    public bool ConfigurarVisibilidade(bool visivel, out string? erro)
    {
        erro = null;
        IntPtr desktop = IntPtr.Zero;
        try
        {
            // Obtemos a pasta virtual, que inclui a Lixeira, antes de alterar o registro.
            Marshal.ThrowExceptionForHR(SHGetSpecialFolderLocation(IntPtr.Zero, 0, out desktop));
            using var chave = Registry.CurrentUser.CreateSubKey(Caminho, writable: true);
            if (chave is null) throw new IOException("Não foi possível acessar a configuração da área de trabalho.");
            chave.SetValue(LixeiraId, visivel ? 0 : 1, RegistryValueKind.DWord);
            // SHCNE_UPDATEDIR + SHCNF_IDLIST | SHCNF_FLUSHNOWAIT: atualiza o desktop sem reiniciar o Explorer.
            SHChangeNotify(0x00001000, 0x00003000, desktop, IntPtr.Zero);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or COMException)
        {
            erro = $"Não foi possível alterar a visibilidade da Lixeira na área de trabalho. Verifique as permissões do Windows. {ex.Message}";
            return false;
        }
        finally
        {
            if (desktop != IntPtr.Zero) Marshal.FreeCoTaskMem(desktop);
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetSpecialFolderLocation(IntPtr hwnd, int csidl, out IntPtr pidl);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int evento, uint flags, IntPtr item1, IntPtr item2);
}
