using System;
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
                var primeiro = vm.ItensLaunchpadFiltrados.FirstOrDefault();
                if (primeiro != null)
                {
                    primeiro.ExecutarCommand.Execute(null);
                    vm.MenuIniciarAberto = false;
                    e.Handled = true;
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
}
