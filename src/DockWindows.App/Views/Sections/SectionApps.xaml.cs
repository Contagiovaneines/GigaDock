using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;

namespace DockWindows.App.Views.Sections;

public partial class SectionApps : UserControl
{
    public SectionApps()
    {
        InitializeComponent();
    }

    private void Apps_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Apps_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is MainViewModel mainVm)
        {
            var arquivos = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (arquivos != null)
            {
                foreach (var caminho in arquivos)
                {
                    var isDir = Directory.Exists(caminho);
                    var novo = new ItemFixado
                    {
                        Titulo = isDir ? Path.GetFileName(caminho) : Path.GetFileNameWithoutExtension(caminho),
                        CaminhoOuUrl = caminho,
                        Tipo = isDir ? TipoItem.Pasta : (Path.GetExtension(caminho).Equals(".exe", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(caminho).Equals(".lnk", StringComparison.OrdinalIgnoreCase) ? TipoItem.Aplicativo : TipoItem.Arquivo)
                    };
                    if (string.IsNullOrWhiteSpace(novo.Titulo))
                    {
                        novo.Titulo = caminho;
                    }
                    mainVm.AdicionarAppPermanenteDireto(novo);
                }
            }
            e.Handled = true;
        }
    }
}
