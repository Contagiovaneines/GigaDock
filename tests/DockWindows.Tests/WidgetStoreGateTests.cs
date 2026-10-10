using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DockWindows.App.Views;
using DockWindows.Core.Models;
using Xunit;

namespace DockWindows.Tests;

public class WidgetStoreGateTests
{
    [Theory]
    [InlineData(TipoWidget.DiscordVoz)]
    [InlineData(TipoWidget.WhatsAppNotificacoes)]
    [InlineData(TipoWidget.WorkspacesLinux)]
    public void InstallHandlerRefusesIncompleteOrUnsupportedWidgetEvenWhenCalledDirectly(TipoWidget kind)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            LojaWidgetsWindow? window = null;
            try
            {
                window = new LojaWidgetsWindow();
                var item = new ItemLoja { Tipo = kind, JaAdicionado = true, InstaladoAtivo = false };
                Assert.False(item.PodeInstalar);
                Assert.False(string.IsNullOrWhiteSpace(item.ComoFunciona));
                var button = new Button { Tag = item };
                typeof(LojaWidgetsWindow).GetMethod("BtnAdicionar_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(window, [button, new RoutedEventArgs()]);
                Assert.Null(window.WidgetSelecionado);
                Assert.Null(window.DialogResult);
            }
            catch (Exception error) { failure = error; }
            finally { window?.Close(); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "O bloqueio não deve abrir outro diálogo.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
