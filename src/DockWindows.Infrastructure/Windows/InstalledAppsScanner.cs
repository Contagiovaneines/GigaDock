using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DockWindows.Infrastructure.Windows;

/// <summary>
/// Um aplicativo instalado, como aparece na lista "Todos os aplicativos" do Menu Iniciar.
/// </summary>
public sealed class AppInstalado
{
    public string Nome { get; init; } = string.Empty;

    /// <summary>Nome de análise do Shell (AUMID para apps modernos, caminho com GUID de pasta conhecida para clássicos).</summary>
    public string ParsingName { get; init; } = string.Empty;

    /// <summary>
    /// Caminho usado para executar e extrair o ícone. É o .exe real quando ele existe;
    /// caso contrário, "shell:AppsFolder\{ParsingName}" (apps da Loja do Windows).
    /// </summary>
    public string CaminhoExecucao { get; init; } = string.Empty;

    /// <summary>Verdadeiro para apps empacotados (Loja do Windows / UWP / MSIX).</summary>
    public bool EhAppModerno { get; init; }
}

/// <summary>
/// Enumera os aplicativos instalados usando a pasta virtual pública "shell:AppsFolder"
/// (API documentada Shell.Application). Ela já une programas clássicos (Win32) e apps
/// da Loja do Windows, e é a mesma fonte usada pelo Menu Iniciar.
/// </summary>
public static class InstalledAppsScanner
{
    public const string PrefixoAppsFolder = @"shell:AppsFolder\";

    private static readonly object Trava = new();
    private static List<AppInstalado>? _cache;
    private static Task<IReadOnlyList<AppInstalado>>? _tarefaEmAndamento;

    /// <summary>Lista em cache (null se ainda não foi escaneada). Não bloqueia.</summary>
    public static IReadOnlyList<AppInstalado>? CacheAtual => _cache;

    private static readonly string[] ExtensoesIgnoradas =
    {
        ".url", ".htm", ".html", ".chm", ".txt", ".pdf", ".rtf", ".md", ".ini", ".log", ".xml", ".msc"
    };

    private static readonly string[] PalavrasIgnoradas =
    {
        "uninstall", "desinstalar", "unins000", "remove ", "readme", "leia-me", "release notes", "notas de versão"
    };

    public static Task<IReadOnlyList<AppInstalado>> ListarAsync(bool forcarAtualizacao = false)
    {
        lock (Trava)
        {
            if (!forcarAtualizacao && _cache != null)
                return Task.FromResult<IReadOnlyList<AppInstalado>>(_cache);

            if (_tarefaEmAndamento != null && !_tarefaEmAndamento.IsCompleted)
                return _tarefaEmAndamento;

            var tcs = new TaskCompletionSource<IReadOnlyList<AppInstalado>>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Shell.Application é um objeto COM de apartamento STA: roda em thread dedicada.
            var thread = new Thread(() =>
            {
                try
                {
                    var resultado = Escanear();
                    lock (Trava) _cache = resultado;
                    tcs.SetResult(resultado);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            })
            {
                IsBackground = true,
                Name = "GigaDock.InstalledAppsScanner"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            _tarefaEmAndamento = tcs.Task;
            return tcs.Task;
        }
    }

    private static List<AppInstalado> Escanear()
    {
        var lista = new List<AppInstalado>();
        var tipoShell = Type.GetTypeFromProgID("Shell.Application");
        if (tipoShell == null) return lista;

        object? shellObj = null;
        try
        {
            shellObj = Activator.CreateInstance(tipoShell);
            if (shellObj == null) return lista;

            dynamic shell = shellObj;
            dynamic? pasta = shell.NameSpace("shell:AppsFolder");
            if (pasta == null) return lista;

            dynamic itens = pasta.Items();
            int total = itens.Count;
            var nomesVistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < total; i++)
            {
                try
                {
                    dynamic? item = itens.Item(i);
                    if (item == null) continue;

                    string nome = (item.Name as string ?? string.Empty).Trim();
                    string parsing = (item.Path as string ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(parsing)) continue;
                    if (DeveIgnorar(nome, parsing)) continue;
                    if (!nomesVistos.Add(nome)) continue;

                    bool ehModerno = parsing.Contains('!') && !parsing.Contains('\\');
                    string caminho = PrefixoAppsFolder + parsing;

                    if (!ehModerno)
                    {
                        var exe = ResolverPastaConhecida(parsing);
                        if (!string.IsNullOrEmpty(exe) && File.Exists(exe) &&
                            exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            caminho = exe;
                        }
                    }

                    lista.Add(new AppInstalado
                    {
                        Nome = nome,
                        ParsingName = parsing,
                        CaminhoExecucao = caminho,
                        EhAppModerno = ehModerno
                    });
                }
                catch
                {
                    // Item individual inválido não deve interromper a varredura.
                }
            }
        }
        finally
        {
            if (shellObj != null && Marshal.IsComObject(shellObj))
                Marshal.FinalReleaseComObject(shellObj);
        }

        return lista.OrderBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static bool DeveIgnorar(string nome, string parsing)
    {
        if (parsing.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return true;

        var ext = Path.GetExtension(parsing);
        if (!string.IsNullOrEmpty(ext) && ExtensoesIgnoradas.Contains(ext, StringComparer.OrdinalIgnoreCase)) return true;

        var nomeLower = nome.ToLowerInvariant();
        return PalavrasIgnoradas.Any(p => nomeLower.Contains(p));
    }

    private static readonly Regex RegexPastaConhecida = new(@"^\{(?<guid>[0-9A-Fa-f\-]{36})\}\\(?<resto>.+)$", RegexOptions.Compiled);

    /// <summary>Converte "{GUID-de-pasta-conhecida}\sub\app.exe" em caminho real.</summary>
    private static string? ResolverPastaConhecida(string parsing)
    {
        if (Path.IsPathRooted(parsing) && File.Exists(parsing)) return parsing;

        var m = RegexPastaConhecida.Match(parsing);
        if (!m.Success) return null;

        try
        {
            var guid = Guid.Parse(m.Groups["guid"].Value);
            if (SHGetKnownFolderPath(ref guid, 0, IntPtr.Zero, out var ptr) == 0 && ptr != IntPtr.Zero)
            {
                try
                {
                    var basePath = Marshal.PtrToStringUni(ptr);
                    if (!string.IsNullOrEmpty(basePath))
                        return Path.Combine(basePath, m.Groups["resto"].Value);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(ptr);
                }
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Para um executável dentro de "WindowsApps" (ex.: WhatsApp da Loja), deduz o AUMID do pacote.
    /// Usa o cache se já existir; senão tenta o padrão "{Família}!App", comum na maioria dos apps.
    /// </summary>
    public static string? ObterAumidPorCaminhoPacote(string caminho)
    {
        if (string.IsNullOrEmpty(caminho)) return null;
        const string marcador = @"\WindowsApps\";
        var idx = caminho.IndexOf(marcador, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var resto = caminho[(idx + marcador.Length)..];
        var pastaPacote = resto.Split('\\')[0];
        // Formato: Nome_Versão_Arquitetura_IdRecurso_IdEditor
        var partes = pastaPacote.Split('_');
        if (partes.Length < 5) return null;

        var familia = partes[0] + "_" + partes[^1];
        var cache = _cache;
        var encontrado = cache?.FirstOrDefault(a => a.EhAppModerno &&
            a.ParsingName.StartsWith(familia + "!", StringComparison.OrdinalIgnoreCase));

        return encontrado?.ParsingName ?? familia + "!App";
    }

    public static string? ObterFamiliaPacotePorCaminho(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return null;
        const string marcador = @"\WindowsApps\";
        var idx = caminho.IndexOf(marcador, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var pastaPacote = caminho[(idx + marcador.Length)..].Split(Path.DirectorySeparatorChar)[0];
        var partes = pastaPacote.Split('_');
        return partes.Length < 5 ? null : partes[0] + "_" + partes[^1];
    }

    public static string? ResolverReferenciaAppEmpacotado(string caminhoAntigo)
    {
        try
        {
            var aplicativos = ListarAsync().GetAwaiter().GetResult();
            return ResolverReferenciaAppEmpacotado(caminhoAntigo, aplicativos);
        }
        catch { return null; }
    }

    public static string? ResolverReferenciaAppEmpacotado(
        string caminhoAntigo,
        IEnumerable<AppInstalado> aplicativos)
    {
        var familia = ObterFamiliaPacotePorCaminho(caminhoAntigo);
        if (familia == null) return null;

        return aplicativos.FirstOrDefault(a => a.EhAppModerno &&
            a.ParsingName.StartsWith(familia + "!", StringComparison.OrdinalIgnoreCase))?.CaminhoExecucao;
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
}
