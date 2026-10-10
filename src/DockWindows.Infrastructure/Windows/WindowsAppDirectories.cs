using System.IO;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

/// <summary>Preserva o diretório de instalação e dados das versões Windows existentes.</summary>
public sealed class WindowsAppDirectories : IAppDirectories
{
    private readonly string _root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows");
    public string Configuracoes => _root;
    public string Dados => _root;
    public string Cache => Path.Combine(_root, "cache");
    public string Estado => _root;
}
