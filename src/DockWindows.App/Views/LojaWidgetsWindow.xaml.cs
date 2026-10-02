using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DockWindows.Core.Models;

namespace DockWindows.App.Views;

public partial class LojaWidgetsWindow : Window
{
    public WidgetInstanceConfig? WidgetSelecionado { get; private set; }
    public TipoWidget? WidgetParaRemover { get; private set; }
    public bool Removeu { get; private set; }

    private readonly List<WidgetInstanceConfig> _widgetsInstalados;

    public LojaWidgetsWindow(IEnumerable<WidgetInstanceConfig>? widgetsInstalados = null)
    {
        InitializeComponent();
        _widgetsInstalados = widgetsInstalados?.ToList() ?? new List<WidgetInstanceConfig>();
        CarregarLoja();
    }

    private void CarregarLoja()
    {
        var catalogo = new List<ItemLoja>
        {
            new ItemLoja { Tipo = TipoWidget.Relogio, Formato = FormatoWidget.Compacto, Nome = "Relógio", DescricaoFormato = "Apenas Ícone", Icone = "🕒" },
            new ItemLoja { Tipo = TipoWidget.Relogio, Formato = FormatoWidget.Expandido, Nome = "Relógio", DescricaoFormato = "Hora e Data", Icone = "🕒" },
            
            new ItemLoja { Tipo = TipoWidget.Clima, Formato = FormatoWidget.Compacto, Nome = "Clima", DescricaoFormato = "Ícone e Temp", Icone = "⛅" },
            new ItemLoja { Tipo = TipoWidget.Clima, Formato = FormatoWidget.Expandido, Nome = "Clima", DescricaoFormato = "Detalhado com Local", Icone = "⛅" },

            new ItemLoja { Tipo = TipoWidget.WhatsAppNotificacoes, Formato = FormatoWidget.Compacto, Nome = "WhatsApp", DescricaoFormato = "Apenas Ícone", Icone = "💬" },
            new ItemLoja { Tipo = TipoWidget.WhatsAppNotificacoes, Formato = FormatoWidget.Expandido, Nome = "WhatsApp", DescricaoFormato = "Última Mensagem", Icone = "💬" },

            new ItemLoja { Tipo = TipoWidget.TeamsStatus, Formato = FormatoWidget.Compacto, Nome = "Teams", DescricaoFormato = "Apenas Ícone", Icone = "👨‍💻" },
            new ItemLoja { Tipo = TipoWidget.TeamsStatus, Formato = FormatoWidget.Expandido, Nome = "Teams", DescricaoFormato = "Status Detalhado", Icone = "👨‍💻" },

            new ItemLoja { Tipo = TipoWidget.DiscordVoz, Formato = FormatoWidget.Compacto, Nome = "Discord", DescricaoFormato = "Apenas Ícone", Icone = "🎮" },
            new ItemLoja { Tipo = TipoWidget.DiscordVoz, Formato = FormatoWidget.Expandido, Nome = "Discord", DescricaoFormato = "Sala de Voz", Icone = "🎮" },

            new ItemLoja { Tipo = TipoWidget.Pomodoro, Formato = FormatoWidget.Expandido, Nome = "Pomodoro", DescricaoFormato = "Cronômetro Visual", Icone = "🍅" },
            new ItemLoja { Tipo = TipoWidget.CalendarioCompromissos, Formato = FormatoWidget.Expandido, Nome = "Calendário", DescricaoFormato = "Próximo Evento", Icone = "📅" },
            new ItemLoja { Tipo = TipoWidget.GitHubContribuicoes, Formato = FormatoWidget.Expandido, Nome = "GitHub", DescricaoFormato = "Gráfico de Commits", Icone = "🐙" },
            new ItemLoja { Tipo = TipoWidget.MonitorSistema, Formato = FormatoWidget.Expandido, Nome = "Monitor", DescricaoFormato = "CPU e RAM", Icone = "🖥️" },
            new ItemLoja { Tipo = TipoWidget.Notas, Formato = FormatoWidget.Compacto, Nome = "Notas", DescricaoFormato = "Acesso Rápido", Icone = "📝" }
        };

        foreach (var item in catalogo)
        {
            var jaTem = _widgetsInstalados.FirstOrDefault(w => w.Tipo == item.Tipo);
            if (jaTem != null)
            {
                item.JaAdicionado = true;
                item.FormatoAtivo = jaTem.Formato == item.Formato;
            }
        }

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(catalogo);
        view.GroupDescriptions.Add(new PropertyGroupDescription("Nome"));
        ListaLoja.ItemsSource = view;
    }

    private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ItemLoja item)
        {
            WidgetSelecionado = new WidgetInstanceConfig
            {
                Id = "wgt-" + Guid.NewGuid().ToString().Substring(0, 8),
                Tipo = item.Tipo,
                Nome = item.Nome,
                Formato = item.Formato,
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
    public FormatoWidget Formato { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string DescricaoFormato { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public bool JaAdicionado { get; set; }
    public bool FormatoAtivo { get; set; }
    
    // UI Helpers
    public string TextoBotao => JaAdicionado && FormatoAtivo ? "Instalado" : (JaAdicionado ? "Trocar Formato" : "Adicionar");
    public string CorBotao => JaAdicionado && FormatoAtivo ? "#1DB954" : "#0A84FF";
    public double LarguraCard => Formato == FormatoWidget.Compacto ? 120 : 260;
}

