using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;

namespace DockWindows.App.Views.Sections;

public partial class SectionItensAmbiente : UserControl
{
    public SectionItensAmbiente()
    {
        InitializeComponent();
    }

    private void BtnGerenciarAmbiente_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void Itens_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Itens_Drop(object sender, DragEventArgs e)
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
                    mainVm.AdicionarItemDireto(novo);
                }
            }
            e.Handled = true;
        }
    }
}
