using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DockWindows.Core.Models;

namespace DockWindows.App.Views;

public partial class LojaWidgetsWindow : Window
{
    public WidgetInstanceConfig? WidgetSelecionado { get; private set; }
    public TipoWidget? WidgetParaRemover { get; private set; }
    public bool Removeu { get; private set; }

    private readonly List<TipoWidget> _tiposJaAdicionados;

    public LojaWidgetsWindow(IEnumerable<TipoWidget>? tiposJaAdicionados = null)
    {
        InitializeComponent();
        _tiposJaAdicionados = tiposJaAdicionados?.ToList() ?? new List<TipoWidget>();
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
                Nome = "Bloco de Notas", 
                Icone = "📝", 
                Descricao = "Escreva lembretes rápidos ou anotações diretamente na barra." 
            },
                        new ItemLoja { 
                Tipo = TipoWidget.MonitorSistema, 
                Nome = "Monitor de Sistema", 
                Icone = "🖥️", 
                Descricao = "Acompanhe o uso de CPU e RAM em tempo real sem abrir o gerenciador de tarefas." 
            },
            new ItemLoja { 
                Tipo = TipoWidget.GitHubContribuicoes, 
                Nome = "Contribuições do GitHub", 
                Icone = "🐙", 
                Descricao = "Acompanhe seu gráfico de contribuições diárias do GitHub direto da sua barra." 
            },
                        new ItemLoja { Tipo = TipoWidget.Clima, Nome = "Clima e Tempo", Icone = "⛅", Descricao = "Previsão do tempo atual e temperatura." },
            new ItemLoja { Tipo = TipoWidget.WhatsAppNotificacoes, Nome = "WhatsApp", Icone = "💬", Descricao = "Notificações do WhatsApp não lidas." },
            new ItemLoja { Tipo = TipoWidget.TeamsStatus, Nome = "Microsoft Teams", Icone = "👨‍💻", Descricao = "Status de reunião do Teams." },
            new ItemLoja { Tipo = TipoWidget.DiscordVoz, Nome = "Discord", Icone = "🎮", Descricao = "Status de voz do Discord." },
            new ItemLoja { 
                Tipo = TipoWidget.CotacaoMoedas, 
                Nome = "Cotação Financeira (Em Breve)", 
                Icone = "📈", 
                Descricao = "Veja o preço do Dólar, Euro ou Bitcoin piscando em tempo real na sua Dock." 
            }
        };

        // Marca quais já estão adicionados
        foreach (var item in catalogo)
        {
            item.JaAdicionado = _tiposJaAdicionados.Contains(item.Tipo);
        }

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

            if (item.JaAdicionado)
            {
                // Remover
                WidgetParaRemover = item.Tipo;
                Removeu = true;
                DialogResult = true;
                Close();
                return;
            }

            // Adicionar
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
    public bool JaAdicionado { get; set; }
    public string TextoBotao => JaAdicionado ? "Remover" : "Adicionar";
    public string CorBotao => JaAdicionado ? "#EF4444" : "#0A84FF";
}




