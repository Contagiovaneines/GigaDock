using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DockWindows.Infrastructure.Windows;

public class AppSearchResult
{
    public string Nome { get; set; } = string.Empty;
    public string Caminho { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
}

public static class AppSearchService
{
    private static List<AppSearchResult>? _cache;

    public static List<AppSearchResult> BuscarAppsInstalados()
    {
        if (_cache != null) return _cache;

        var resultados = new List<AppSearchResult>();
        var caminhosBusca = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs)
        };

        var processados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pastaBase in caminhosBusca)
        {
            if (string.IsNullOrEmpty(pastaBase) || !Directory.Exists(pastaBase)) continue;

            try
            {
                var atalhos = Directory.GetFiles(pastaBase, "*.lnk", SearchOption.AllDirectories);
                foreach (var atalho in atalhos)
                {
                    var nome = Path.GetFileNameWithoutExtension(atalho);
                    
                    // Evita duplicatas (ex: app no user e no common) e atalhos inuteis
                    if (nome.Contains("Desinstalar", StringComparison.OrdinalIgnoreCase) || 
                        nome.Contains("Uninstall", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (processados.Add(nome))
                    {
                        resultados.Add(new AppSearchResult
                        {
                            Nome = nome,
                            Caminho = atalho,
                            Icone = atalho // O IconExtractionService sabe extrair de .lnk
                        });
                    }
                }
            }
            catch { }
        }

        _cache = resultados.OrderBy(r => r.Nome).ToList();
        return _cache;
    }
}
