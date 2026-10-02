using System;
using System.Diagnostics;
using System.IO;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public class AutostartService : IAutostartService
{
    private static string GetShortcutPath()
    {
        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return Path.Combine(startupFolder, "GigaDock.lnk");
    }

    public bool EstaHabilitado()
    {
        return File.Exists(GetShortcutPath());
    }

    public bool Configurar(bool habilitar)
    {
        try
        {
            var shortcutPath = GetShortcutPath();

            // Migração: remove chave de registro antiga se existir
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                key?.DeleteValue("DockWindows", throwOnMissingValue: false);
            }
            catch { }

            if (habilitar)
            {
                var caminhoExe = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(caminhoExe))
                    caminhoExe = Process.GetCurrentProcess().MainModule?.FileName;

                if (!string.IsNullOrWhiteSpace(caminhoExe))
                {
                    var diretorioTrabalho = Path.GetDirectoryName(caminhoExe) ?? string.Empty;
                    var shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic shell = Activator.CreateInstance(shellType)!;
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = caminhoExe;
                        shortcut.WorkingDirectory = diretorioTrabalho;
                        shortcut.Description = "GigaDock";
                        shortcut.IconLocation = caminhoExe + ",0";
                        shortcut.Save();
                    }
                    return true;
                }
                return false;
            }
            else
            {
                if (File.Exists(shortcutPath))
                    File.Delete(shortcutPath);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
