using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Core.Validation;

namespace DockWindows.Infrastructure.Windows;

public class LauncherService : ILauncherService
{
    public LaunchResult Executar(ItemFixado item)
    {
        var validacao = ItemValidator.ValidarItem(item);
        if (!validacao.Valido)
        {
            return LaunchResult.Falha(validacao.MensagemErro ?? "Item com parâmetros inválidos.");
        }

        return item.Tipo switch
        {
            TipoItem.WebUrl => AbrirUrl(item.CaminhoOuUrl),
            TipoItem.Pasta => AbrirPasta(item.CaminhoOuUrl),
            TipoItem.Aplicativo or TipoItem.Arquivo => ExecutarCaminho(item.CaminhoOuUrl, item.Argumentos),
            _ => LaunchResult.Falha("Tipo de item não suportado.")
        };
    }

    public LaunchResult AbrirLocal(ItemFixado item)
    {
        if (item.Tipo == TipoItem.WebUrl)
        {
            return LaunchResult.Falha("Links da web não possuem diretório local.");
        }

        var caminho = Environment.ExpandEnvironmentVariables(item.CaminhoOuUrl);
        if (item.Tipo == TipoItem.Pasta)
        {
            return AbrirPasta(caminho);
        }

        if (File.Exists(caminho))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{caminho}\"",
                    UseShellExecute = true
                });
                return LaunchResult.Ok();
            }
            catch (Exception ex)
            {
                return LaunchResult.Falha($"Não foi possível abrir o local do arquivo: {ex.Message}");
            }
        }

        return LaunchResult.Falha($"Arquivo '{item.CaminhoOuUrl}' não foi localizado para abrir a pasta correspondente.");
    }

    public LaunchResult ExecutarCaminho(string caminho, string? argumentos = null)
    {
        try
        {
            var expandido = Environment.ExpandEnvironmentVariables(caminho);

            var startInfo = new ProcessStartInfo
            {
                FileName = expandido,
                Arguments = argumentos ?? string.Empty,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Maximized
            };

            // Se for arquivo em pasta específica, definir WorkingDirectory
            if (File.Exists(expandido))
            {
                var dir = Path.GetDirectoryName(expandido);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    startInfo.WorkingDirectory = dir;
                }
            }

            Process.Start(startInfo);
            return LaunchResult.Ok();
        }
        catch (Win32Exception w32Ex)
        {
            return LaunchResult.Falha($"Erro do Windows ao executar '{caminho}': {w32Ex.Message}");
        }
        catch (Exception ex)
        {
            return LaunchResult.Falha($"Falha ao iniciar '{caminho}': {ex.Message}");
        }
    }

    private static LaunchResult AbrirPasta(string caminhoPasta)
    {
        try
        {
            var expandido = Environment.ExpandEnvironmentVariables(caminhoPasta);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{expandido}\"",
                UseShellExecute = true
            });
            return LaunchResult.Ok();
        }
        catch (Exception ex)
        {
            return LaunchResult.Falha($"Não foi possível abrir a pasta '{caminhoPasta}': {ex.Message}");
        }
    }

    private static LaunchResult AbrirUrl(string url)
    {
        try
        {
            var urlNormalizada = url;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                urlNormalizada = "https://" + url;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = urlNormalizada,
                UseShellExecute = true
            });
            return LaunchResult.Ok();
        }
        catch (Exception ex)
        {
            return LaunchResult.Falha($"Não foi possível abrir o link no navegador padrão: {ex.Message}");
        }
    }
}
