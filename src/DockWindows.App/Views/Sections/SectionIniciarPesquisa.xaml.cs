using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;

public partial class SectionIniciarPesquisa : UserControl
{
    public SectionIniciarPesquisa()
    {
        InitializeComponent();
        DataContextChanged += SectionIniciarPesquisa_DataContextChanged;
    }

    private void SectionIniciarPesquisa_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            vm.FocarBuscaLaunchpad = () =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtBuscaLaunchpad?.Focus();
                    TxtBuscaLaunchpad?.SelectAll();
                });
            };
        }
    }

    private void PopupLaunchpad_Opened(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            TxtBuscaLaunchpad?.Focus();
            TxtBuscaLaunchpad?.SelectAll();
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void TxtBuscaLaunchpad_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainViewModel vm)
            {
                // Só executa o Enter se o usuário realmente digitou algo para buscar!
                // Isso evita abrir aplicativos aleatórios por esbarrar no Enter.
                if (!string.IsNullOrWhiteSpace(vm.TextoFiltroLaunchpad))
                {
                    var primeiro = vm.ItensLaunchpadFiltrados.FirstOrDefault();
                    if (primeiro != null)
                    {
                        primeiro.ExecutarCommand.Execute(null);
                        vm.MenuIniciarAberto = false;
                        e.Handled = true;
                    }
                }
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.MenuIniciarAberto = false;
                e.Handled = true;
            }
        }
    }

    private void LimparBusca_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.TextoFiltroLaunchpad = string.Empty;
        }
        TxtBuscaLaunchpad?.Focus();
    }

    private void Energia_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
        menu.IsOpen = true;
    }

    private void DesligarPc_Click(object sender, RoutedEventArgs e) => SolicitarEnergia(false);

    private void ReiniciarPc_Click(object sender, RoutedEventArgs e) => SolicitarEnergia(true);

    private async void SolicitarEnergia(bool reiniciar)
    {
        var acao = reiniciar ? "Reiniciar" : "Desligar";
        if (DataContext is MainViewModel vm) vm.MenuIniciarAberto = false;
        if (MessageBox.Show($"{acao} o computador agora? Salve seus arquivos antes de continuar.",
            $"{acao} PC", MessageBoxButton.YesNo, MessageBoxImage.Question,
            MessageBoxResult.No) != MessageBoxResult.Yes) return;

        try
        {
            var executavel = Path.Combine(Environment.SystemDirectory, "shutdown.exe");
            if (!File.Exists(executavel)) throw new FileNotFoundException("O comando de energia do Windows não foi encontrado.");
            var inicio = new ProcessStartInfo(executavel)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            inicio.ArgumentList.Add(reiniciar ? "/r" : "/s");
            // /t 0 evita o fechamento forçado implícito em prazos maiores que zero.
            inicio.ArgumentList.Add("/t");
            inicio.ArgumentList.Add("0");
            using var processo = Process.Start(inicio);
            if (processo == null) throw new InvalidOperationException("Não foi possível iniciar o comando de energia.");
            var erro = await processo.StandardError.ReadToEndAsync();
            await processo.WaitForExitAsync();
            if (processo.ExitCode != 0)
                MessageBox.Show($"O Windows não conseguiu {acao.ToLowerInvariant()} o computador. {erro.Trim()}",
                    "Energia do computador", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Não foi possível {acao.ToLowerInvariant()} o computador. {ex.Message}",
                "Energia do computador", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
