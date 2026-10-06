using System.Diagnostics;
using System.IO;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public sealed class AplicativosSegundoPlanoService : IAplicativosSegundoPlanoService
{
    private static readonly HashSet<string> ProcessosDoSistema = new(StringComparer.OrdinalIgnoreCase)
    {
        "conhost", "csrss", "ctfmon", "dllhost", "dwm", "explorer", "fontdrvhost",
        "lsass", "memory compression", "registry", "runtimebroker", "searchhost",
        "securityhealthservice", "services", "sihost", "smss", "spoolsv", "startmenuexperiencehost",
        "svchost", "system", "systemsettings", "taskhostw", "textinputhost", "userinit",
        "wininit", "winlogon", "wmiprvse", "wudfhost", "testhost", "dotnet"
    };

    public IReadOnlyList<AplicativoSegundoPlanoInfo> ObterAplicativos()
    {
        var resultado = new Dictionary<string, AplicativoSegundoPlanoInfo>(StringComparer.OrdinalIgnoreCase);
        int sessaoAtual = Process.GetCurrentProcess().SessionId;

        foreach (var processo in Process.GetProcesses())
        {
            using (processo)
            {
                try
                {
                    if (processo.Id == Environment.ProcessId || processo.SessionId != sessaoAtual ||
                        ProcessosDoSistema.Contains(processo.ProcessName)) continue;

                    string? caminhoLido = processo.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(caminhoLido)) continue;
                    string caminho = caminhoLido;
                    if (!File.Exists(caminho) || !EhAplicativoDoUsuario(caminho)) continue;

                    string nome = processo.MainModule?.FileVersionInfo.FileDescription?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(nome)) nome = processo.ProcessName;
                    bool possuiJanela = processo.MainWindowHandle != IntPtr.Zero;

                    if (resultado.TryGetValue(caminho, out var existente))
                        resultado[caminho] = existente with { PossuiJanela = existente.PossuiJanela || possuiJanela };
                    else
                        resultado[caminho] = new AplicativoSegundoPlanoInfo(nome, caminho, possuiJanela);
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    // Processos protegidos ou encerrados durante a leitura são ignorados.
                }
            }
        }

        return resultado.Values
            .OrderBy(a => a.PossuiJanela)
            .ThenBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Take(40)
            .ToArray();
    }

    private static bool EhAplicativoDoUsuario(string caminho)
    {
        string completo;
        try { completo = Path.GetFullPath(caminho); }
        catch { return false; }

        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrEmpty(windows) && completo.StartsWith(windows + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return false;

        return completo.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    }
}
