using System;
using System.IO;

namespace DockWindows.Installer.Services;

public static class ShortcutService
{
    public static void RemoverLegados(string pasta, string executavelEsperado)
    {
        foreach (var nome in new[] { "Dock Windows.lnk", "DockWindows.lnk" })
        {
            var caminho = Path.Combine(pasta, nome);
            if (!File.Exists(caminho)) continue;
            object? shell = null;
            object? link = null;
            try
            {
                var tipo = Type.GetTypeFromProgID("WScript.Shell");
                if (tipo == null) continue;
                shell = Activator.CreateInstance(tipo);
                if (shell == null) continue;
                link = ((dynamic)shell).CreateShortcut(caminho);
                string alvo = ((dynamic)link).TargetPath;
                if (!string.IsNullOrWhiteSpace(alvo) &&
                    Path.GetFullPath(alvo).Equals(Path.GetFullPath(executavelEsperado), StringComparison.OrdinalIgnoreCase))
                    File.Delete(caminho);
            }
            finally
            {
                if (link != null && System.Runtime.InteropServices.Marshal.IsComObject(link)) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);
                if (shell != null && System.Runtime.InteropServices.Marshal.IsComObject(shell)) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            }
        }
    }

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
