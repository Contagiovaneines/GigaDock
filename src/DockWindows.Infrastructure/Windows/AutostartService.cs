using System.Diagnostics;
using DockWindows.Core.Services;
using Microsoft.Win32;

namespace DockWindows.Infrastructure.Windows;

public class AutostartService : IAutostartService
{
    private const string ChaveRegistro = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string NomeAplicacao = "DockWindows";

    public bool EstaHabilitado()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ChaveRegistro, false);
            var valor = key?.GetValue(NomeAplicacao) as string;
            return !string.IsNullOrWhiteSpace(valor);
        }
        catch
        {
            return false;
        }
    }

    public bool Configurar(bool habilitar)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ChaveRegistro, true);
            if (key == null) return false;

            if (habilitar)
            {
                var caminhoExe = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(caminhoExe))
                {
                    caminhoExe = Process.GetCurrentProcess().MainModule?.FileName;
                }

                if (!string.IsNullOrWhiteSpace(caminhoExe))
                {
                    key.SetValue(NomeAplicacao, $"\"{caminhoExe}\"");
                    return true;
                }
                return false;
            }
            else
            {
                key.DeleteValue(NomeAplicacao, false);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
