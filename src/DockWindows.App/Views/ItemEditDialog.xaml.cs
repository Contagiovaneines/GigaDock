using System.IO;
using System.Windows;
using System.Windows.Controls;
using DockWindows.Core.Models;
using DockWindows.Core.Validation;
using DockWindows.Infrastructure.Windows;
using Microsoft.Win32;

namespace DockWindows.App.Views;

public partial class ItemEditDialog : Window
{
    private ItemFixado _item;

    public ItemEditDialog(ItemFixado? itemExistente = null)
    {
        InitializeComponent();

        if (itemExistente != null)
        {
            _item = new ItemFixado
            {
                Id = itemExistente.Id,
                Titulo = itemExistente.Titulo,
                CaminhoOuUrl = itemExistente.CaminhoOuUrl,
                Argumentos = itemExistente.Argumentos,
                Tipo = itemExistente.Tipo,
                Ordem = itemExistente.Ordem
            };

            TxtTitulo.Text = _item.Titulo;
            TxtCaminho.Text = _item.CaminhoOuUrl;
            TxtArgumentos.Text = _item.Argumentos ?? string.Empty;

            foreach (ComboBoxItem cbItem in CmbTipo.Items)
            {
                if (cbItem.Tag is TipoItem t && t == _item.Tipo)
                {
                    CmbTipo.SelectedItem = cbItem;
                    break;
                }
            }
        }
        else
        {
            _item = new ItemFixado();
            CmbTipo.SelectedIndex = 0;
        }

        AtualizarVisibilidadeCampos();
    }

    public ItemFixado ItemResultante => _item;

    private void CmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AtualizarVisibilidadeCampos();
    }

    private void AtualizarVisibilidadeCampos()
    {
        if (CmbTipo?.SelectedItem is ComboBoxItem selected && selected.Tag is TipoItem tipo)
        {
            bool isUrl = tipo == TipoItem.WebUrl;
            bool isPasta = tipo == TipoItem.Pasta;
            bool isApp = tipo == TipoItem.Aplicativo;

            if (BtnProcurar != null)
            {
                BtnProcurar.Visibility = isUrl ? Visibility.Collapsed : Visibility.Visible;
            }

            if (LblArgumentos != null && TxtArgumentos != null)
            {
                LblArgumentos.Visibility = isApp ? Visibility.Visible : Visibility.Collapsed;
                TxtArgumentos.Visibility = isApp ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void BtnProcurar_Click(object sender, RoutedEventArgs e)
    {
        var tipo = ObterTipoSelecionado();
        if (tipo == TipoItem.Pasta)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Selecione uma Pasta"
            };
            if (dialog.ShowDialog() == true)
            {
                TxtCaminho.Text = dialog.FolderName;
                if (string.IsNullOrWhiteSpace(TxtTitulo.Text))
                {
                    TxtTitulo.Text = Path.GetFileName(dialog.FolderName);
                }
            }
        }
        else
        {
            var dialog = new OpenFileDialog
            {
                Title = tipo == TipoItem.Aplicativo ? "Selecione um Aplicativo" : "Selecione um Arquivo",
                Filter = tipo == TipoItem.Aplicativo
                    ? "Aplicativos e Atalhos (*.exe;*.lnk)|*.exe;*.lnk|Todos os Arquivos (*.*)|*.*"
                    : "Todos os Arquivos (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtCaminho.Text = dialog.FileName;
                if (string.IsNullOrWhiteSpace(TxtTitulo.Text))
                {
                    TxtTitulo.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }
    }

    private TipoItem ObterTipoSelecionado()
    {
        if (CmbTipo?.SelectedItem is ComboBoxItem selected && selected.Tag is TipoItem tipo)
        {
            return tipo;
        }
        return TipoItem.Aplicativo;
    }

    private void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        _item.Titulo = TxtTitulo.Text.Trim();
        _item.CaminhoOuUrl = TxtCaminho.Text.Trim();
        _item.Tipo = ObterTipoSelecionado();
        _item.Argumentos = string.IsNullOrWhiteSpace(TxtArgumentos.Text) ? null : TxtArgumentos.Text.Trim();

        if (_item.Tipo == TipoItem.WebUrl &&
            !string.IsNullOrWhiteSpace(_item.CaminhoOuUrl) &&
            !_item.CaminhoOuUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !_item.CaminhoOuUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _item.CaminhoOuUrl = "https://" + _item.CaminhoOuUrl;
            TxtCaminho.Text = _item.CaminhoOuUrl;
        }

        var validacao = ItemValidator.ValidarItem(_item, WindowsItemPathValidator.Instance);
        if (!validacao.Valido)
        {
            TxtErro.Text = validacao.MensagemErro;
            TxtErro.Visibility = Visibility.Visible;
            return;
        }

        TxtErro.Visibility = Visibility.Collapsed;
        DialogResult = true;
        Close();
    }
}
