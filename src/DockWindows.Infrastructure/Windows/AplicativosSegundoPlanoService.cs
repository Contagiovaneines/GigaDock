using System.Diagnostics;
using System.IO;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public sealed class AplicativosSegundoPlanoService : IAplicativosSegundoPlanoService
{
    private static readonly HashSet<string> ProcessosDoSistema = new(StringComparer.OrdinalIgnoreCase)
    {
        "applicationframehost", "conhost", "csrss", "ctfmon", "dllhost", "dwm", "explorer", "fontdrvhost",
        "lsass", "memory compression", "registry", "runtimebroker", "searchhost", "searchindexer",
        "chrome_crashpad_handler", "crashpad_handler", "msedgewebview2", "securityhealthservice", "services", "sihost", "smartscreen", "smss", "spoolsv", "startmenuexperiencehost",
        "svchost", "system", "systemsettings", "taskhostw", "textinputhost", "userinit", "widgetservice",
        "update", "widgets", "wininit", "winlogon", "wmiprvse", "wudfhost", "testhost", "dotnet"
    };

    // Aplicativos conhecidos por continuarem ativos na bandeja mesmo quando possuem
    // uma janela aberta. Para os demais, uma janela visível pertence à área principal do dock.
    private static readonly HashSet<string> AplicativosComBandeja = new(StringComparer.OrdinalIgnoreCase)
    {
        "discord", "ms-teams", "teams", "spotify", "steam",
        "whatsapp", "telegram", "signal", "onedrive", "dropbox", "googledrivefs",
        "lghub", "armourycrate", "translucenttb", "powertoys"
    };

    public IReadOnlyList<AplicativoSegundoPlanoInfo> ObterAplicativos()
    {
        var resultado = new Dictionary<string, AplicativoSegundoPlanoInfo>(StringComparer.OrdinalIgnoreCase);
        var instalados = InstalledAppsScanner.CacheAtual ?? Array.Empty<AppInstalado>();
        int sessaoAtual = Process.GetCurrentProcess().SessionId;

        foreach (var processo in Process.GetProcesses())
        {
            using (processo)
            {
                try
                {
                    if (processo.Id == Environment.ProcessId || processo.SessionId != sessaoAtual ||
                        ProcessosDoSistema.Contains(processo.ProcessName)) continue;

                    var caminho = NormalizarCaminho(processo.MainModule?.FileName);
                    if (caminho == null || !EhAplicativoDoUsuario(caminho)) continue;

                    var versao = processo.MainModule?.FileVersionInfo;
                    var aumid = caminho.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase)
                        ? InstalledAppsScanner.ObterAumidPorCaminhoPacote(caminho)
                        : null;
                    var instalado = EncontrarInstalado(instalados, caminho, aumid);
                    var possuiBandejaConhecida = AplicativosComBandeja.Contains(processo.ProcessName);
                    if (instalado == null && string.IsNullOrWhiteSpace(aumid) && !possuiBandejaConhecida) continue;

                    var janela = processo.MainWindowHandle;
                    if (janela != IntPtr.Zero && !possuiBandejaConhecida) continue;

                    var produto = LimparNome(versao?.ProductName);
                    var descricao = LimparNome(versao?.FileDescription);
                    var nome = instalado?.Nome ?? produto ?? descricao ?? processo.ProcessName;
                    var referenciaIcone = instalado?.CaminhoExecucao ??
                        (!string.IsNullOrWhiteSpace(aumid) ? InstalledAppsScanner.PrefixoAppsFolder + aumid : caminho);
                    var chave = CriarChave(instalado, aumid, versao?.CompanyName, produto, caminho);
                    var atual = new AplicativoSegundoPlanoInfo(nome, caminho, janela != IntPtr.Zero, janela, referenciaIcone);

                    if (!resultado.TryGetValue(chave, out var existente)) resultado[chave] = atual;
                    else if (!existente.PossuiJanela && atual.PossuiJanela) resultado[chave] = atual;
                    else if (existente.ReferenciaIcone == existente.CaminhoExecutavel && referenciaIcone != caminho)
                        resultado[chave] = existente with { Nome = nome, ReferenciaIcone = referenciaIcone };
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    // Processos protegidos ou encerrados durante a leitura são ignorados.
                }
            }
        }

        return resultado.Values
            .OrderByDescending(a => a.PossuiJanela)
            .ThenBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Take(40)
            .ToArray();
    }

    private static AppInstalado? EncontrarInstalado(IReadOnlyList<AppInstalado> instalados, string caminho, string? aumid)
    {
        if (!string.IsNullOrWhiteSpace(aumid))
            return instalados.FirstOrDefault(a => string.Equals(a.ParsingName, aumid, StringComparison.OrdinalIgnoreCase));
        return instalados.FirstOrDefault(a => string.Equals(NormalizarCaminho(a.CaminhoExecucao), caminho, StringComparison.OrdinalIgnoreCase));
    }

    private static string CriarChave(AppInstalado? instalado, string? aumid, string? empresa, string? produto, string caminho)
    {
        if (instalado != null) return "app:" + instalado.ParsingName;
        if (!string.IsNullOrWhiteSpace(aumid)) return "aumid:" + aumid.Split('!')[0];
        var produtoLimpo = LimparNome(produto);
        if (!string.IsNullOrWhiteSpace(produtoLimpo)) return $"produto:{LimparNome(empresa)}|{produtoLimpo}";
        return "exe:" + caminho;
    }

    private static string? LimparNome(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? NormalizarCaminho(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return null;
        try { return Path.GetFullPath(caminho).TrimEnd(Path.DirectorySeparatorChar); }
        catch { return null; }
    }

    private static bool EhAplicativoDoUsuario(string caminho)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrEmpty(windows) && caminho.StartsWith(windows + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return false;
        return File.Exists(caminho) && caminho.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    }
}
