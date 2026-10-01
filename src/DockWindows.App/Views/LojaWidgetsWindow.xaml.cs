using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DockWindows.Core.Models;

namespace DockWindows.App.Views;

public partial class LojaWidgetsWindow : Window
{
    public WidgetInstanceConfig? WidgetSelecionado { get; private set; }

    public LojaWidgetsWindow()
    {
        InitializeComponent();
        CarregarLoja();
    }

    private void CarregarLoja()
    {
        var catalogo = new List<ItemLoja>
        {
            new ItemLoja { 
                Tipo = TipoWidget.Relogio, 
                Nome = "Relógio Digital", 
                Icone = "🕒", 
                Descricao = "Um relógio limpo com horas, minutos e segundos. Mostra a data completa quando expandido." 
            },
            new ItemLoja { 
                Tipo = TipoWidget.Pomodoro, 
                Nome = "Pomodoro de Foco", 
                Icone = "🍅", 
                Descricao = "Cronômetro para técnica Pomodoro. Foque por 25 minutos e descanse por 5." 
            },
            new ItemLoja { 
                Tipo = TipoWidget.CalendarioCompromissos, 
                Nome = "Calendário e Agenda", 
                Icone = "📅", 
                Descricao = "Mostra seus próximos eventos do dia (suporta sincronização via .ics/iCal)." 
            },
            new ItemLoja { 
                Tipo = TipoWidget.Notas, 
                Nome = "Bloco de Notas (Em Breve)", 
                Icone = "📝", 
                Descricao = "Escreva lembretes rápidos ou anotações diretamente na barra." 
            },
            new ItemLoja { 
                Tipo = TipoWidget.MonitorSistema, 
                Nome = "Monitor de Sistema (Em Breve)", 
                Icone = "💻", 
                Descricao = "Acompanhe o uso de CPU e RAM em tempo real sem abrir o gerenciador de tarefas." 
            },
            new ItemLoja { 
                Tipo = TipoWidget.CotacaoMoedas, 
                Nome = "Cotação Financeira (Em Breve)", 
                Icone = "📈", 
                Descricao = "Veja o preço do Dólar, Euro ou Bitcoin piscando em tempo real na sua Dock." 
            }
        };

        ListaLoja.ItemsSource = catalogo;
    }

    private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ItemLoja item)
        {
            if (item.Nome.Contains("Em Breve"))
            {
                MessageBox.Show(this, "Este widget ainda está em desenvolvimento e será adicionado numa atualização futura! Fique de olho.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            WidgetSelecionado = new WidgetInstanceConfig
            {
                Id = "wgt-" + Guid.NewGuid().ToString().Substring(0, 8),
                Tipo = item.Tipo,
                Nome = item.Nome.Replace(" (Em Breve)", ""),
                Formato = FormatoWidget.Compacto,
                Visivel = true,
                Ordem = 99
            };
            
            DialogResult = true;
            Close();
        }
    }
}

public class ItemLoja
{
    public TipoWidget Tipo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}
