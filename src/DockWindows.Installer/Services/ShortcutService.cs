using System;
using System.IO;

namespace DockWindows.Installer.Services;

public static class ShortcutService
{
    public static void CriarAtalho(string caminhoAtalho, string caminhoAlvo, string diretorioTrabalho, string descricao)
    {
        try
        {
            var pastaPai = Path.GetDirectoryName(caminhoAtalho);
            if (!string.IsNullOrEmpty(pastaPai) && !Directory.Exists(pastaPai))
            {
                Directory.CreateDirectory(pastaPai);
            }

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(caminhoAtalho);
                shortcut.TargetPath = caminhoAlvo;
                shortcut.WorkingDirectory = diretorioTrabalho;
                shortcut.Description = descricao;
                shortcut.IconLocation = caminhoAlvo + ",0";
                shortcut.Save();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Falha ao criar atalho '{caminhoAtalho}': {ex.Message}");
        }
    }

    public static void RemoverAtalho(string caminhoAtalho)
    {
        try
        {
            if (File.Exists(caminhoAtalho))
            {
                File.Delete(caminhoAtalho);
            }
        }
        catch { }
    }
}
