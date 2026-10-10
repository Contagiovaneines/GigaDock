using System.Windows;
using System.Windows.Controls;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views.Sections;
public partial class SectionNotificacoes : UserControl
{
    public SectionNotificacoes()
    {
        InitializeComponent();
        Loaded += async (_, _) => { if (DataContext is MainViewModel vm) await vm.Notificacoes.RefreshAsync(); };
    }
    private void NotificationButton_Click(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.BandejaEspelhadaAberta = false;
        vm.PainelAppsSegundoPlanoAberto = false;
        vm.BandejaAvisoAberto = false;
        vm.MenuIniciarAberto = false;
        vm.Clock.CalendarioAberto = false;
        vm.ControlesRapidos.Aberto = false;
    }
}
