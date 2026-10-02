using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

        private List<ItemLoja> _catalogoCompleto = new();

    private void CarregarLoja()
    {
        _catalogoCompleto = new List<ItemLoja>
        {
            new ItemLoja { 
                Tipo = TipoWidget.Relogio, Formato = FormatoWidget.Expandido, Nome = "Relógio", 
                Descricao = "Relógio digital fluido com fusos horários, marcador de segundos milissegundo e estilo customizável.",
                DescricaoFormato = "Hora e Data", Icone = "\uE121", CorIcone = "#253342", Categoria = "Sistema",
                PreviewTitle = "14:48", PreviewSubtitle = "São Paulo", PreviewIcone = "", PreviewCor = "#0A84FF", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.Clima, Formato = FormatoWidget.Expandido, Nome = "Clima", 
                Descricao = "Radar meteorológico compacto com alertas de chuva imediata, índice UV e qualidade do ar em tempo real.",
                DescricaoFormato = "Detalhado com Local", Icone = "\uE9CA", CorIcone = "#21313A", Categoria = "Utilidade",
                PreviewTitle = "24°C", PreviewSubtitle = "Céu Limpo   Umid 60%", PreviewIcone = "\uE9CA", PreviewCor = "#FF9F0A", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.WhatsAppNotificacoes, Formato = FormatoWidget.Expandido, Nome = "WhatsApp", 
                Descricao = "Visualizador de mensagens prioritárias e contador de notificações com suporte a leitura rápida.",
                DescricaoFormato = "Última Mensagem", Icone = "\uE8BD", CorIcone = "#193524", Categoria = "Comunicação", Custo = "v1.2",
                PreviewTitle = "3 Conversas", PreviewSubtitle = "1 Nova", PreviewIcone = "\uE8BD", PreviewCor = "#25D366", ShowPreviewBar = true
            },
            new ItemLoja { 
                Tipo = TipoWidget.TeamsStatus, Formato = FormatoWidget.Expandido, Nome = "Teams", 
                Descricao = "Controle de microfone rápido, status de presença em chamada e detecção de reuniões agendadas.",
                DescricaoFormato = "Status Detalhado", Icone = "\uE716", CorIcone = "#292138", Categoria = "Comunicação",
                PreviewTitle = "Em Reunião", PreviewSubtitle = "Mic Mudo", PreviewIcone = "\uE716", PreviewCor = "#FF453A", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.DiscordVoz, Formato = FormatoWidget.Expandido, Nome = "Discord", 
                Descricao = "Veja quem está falando no seu canal de voz e controle seu microfone e áudio.",
                DescricaoFormato = "Canal de Voz", Icone = "\uE716", CorIcone = "#20242B", Categoria = "Comunicação",
                PreviewTitle = "Gamer Room", PreviewSubtitle = "Você, Alex, Sam", PreviewIcone = "\uE716", PreviewCor = "#5865F2", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.GitHubContribuicoes, Formato = FormatoWidget.Expandido, Nome = "GitHub Actions", 
                Descricao = "Acompanhe PRs abertas, code reviews pendentes e status de pipelines do GitHub Actions.",
                DescricaoFormato = "Gráfico de Commits", Icone = "\uE943", CorIcone = "#20242B", Categoria = "Dev Tools", Custo = "Pro",
                PreviewTitle = "2 PRs", PreviewSubtitle = "#412 pass", PreviewIcone = "\uE943", PreviewCor = "#1DB954", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.CalendarioCompromissos, Formato = FormatoWidget.Expandido, Nome = "Calendário", 
                Descricao = "Agenda instantânea sincronizada com Outlook e Google Calendar com contagem regressiva para eventos.",
                DescricaoFormato = "Próximo Evento", Icone = "\uE163", CorIcone = "#28303D", Categoria = "Produtividade",
                PreviewTitle = "Sync de Design", PreviewSubtitle = "em 12m", PreviewIcone = "\uE163", PreviewCor = "#0A84FF", ShowPreviewBar = true
            },
            new ItemLoja { 
                Tipo = TipoWidget.MonitorSistema, Formato = FormatoWidget.Expandido, Nome = "Monitor de Sistema", 
                Descricao = "Medidor per-core de CPU, RAM e clock de GPU sem consumir recursos de primeiro plano da máquina.",
                DescricaoFormato = "CPU e RAM", Icone = "\uE950", CorIcone = "#1F2342", Categoria = "Sistema", Custo = "Core Lab",
                PreviewTitle = "CPU 28%", PreviewSubtitle = "RAM 14.2 GB", PreviewIcone = "\uE950", PreviewCor = "#5E5CE6", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.Pomodoro, Formato = FormatoWidget.Expandido, Nome = "Pomodoro Timer", 
                Descricao = "Timer de foco com técnica Pomodoro, ciclos de descanso, som de transição discreto e integração de tarefas.",
                DescricaoFormato = "Cronômetro Visual", Icone = "\uE916", CorIcone = "#3A2222", Categoria = "Produtividade",
                PreviewTitle = "18:42", PreviewSubtitle = "Foco #2", PreviewIcone = "\uE916", PreviewCor = "#FF453A", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.OBSStudio, Formato = FormatoWidget.Expandido, Nome = "OBS Studio Control", 
                Descricao = "Inicie e pare gravações diretamente da dock, abra o OBS com um clique. Minimalista.",
                DescricaoFormato = "Rec e Status", Icone = "\uE714", CorIcone = "#1C1C22", Categoria = "Mídia", Custo = "Gratuito",
                PreviewTitle = "OBS Studio", PreviewSubtitle = "Pronto", PreviewIcone = "\uE714", PreviewCor = "#FF3B30", ShowPreviewBar = false
            }
        };

        foreach (var item in _catalogoCompleto)
        {
            var jaTem = _widgetsInstalados.FirstOrDefault(w => w.Tipo == item.Tipo);
            if (jaTem != null)
            {
                item.JaAdicionado = true;
                item.FormatoAtivo = jaTem.Formato == item.Formato;
            }
        }
        
        TxtInstaladosBadge.Text = $"● Instalados: {_catalogoCompleto.Count(x => x.JaAdicionado)}";
        AplicarFiltros();
    }

    private string _filtroCategoria = "Todos";

    private void Filtro_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.IsChecked == true)
        {
            _filtroCategoria = rb.Content.ToString() ?? "Todos";
            AplicarFiltros();
        }
    }

    private void TxtBusca_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        if (_catalogoCompleto == null) return;
        var termo = TxtBusca.Text.ToLowerInvariant();
        var filtrados = _catalogoCompleto.Where(x => 
            (_filtroCategoria == "Todos" || x.Categoria == _filtroCategoria) &&
            (string.IsNullOrWhiteSpace(termo) || x.Nome.IndexOf(termo, StringComparison.OrdinalIgnoreCase) >= 0 || x.Descricao.IndexOf(termo, StringComparison.OrdinalIgnoreCase) >= 0)
        ).ToList();

        CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(filtrados);
        ListaLoja.ItemsSource = view;
    }

    private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ItemLoja item && !item.JaAdicionado)
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
    public string Descricao { get; set; } = string.Empty;
    public string DescricaoFormato { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public string CorIcone { get; set; } = "#333340";
    public string Categoria { get; set; } = "Sistema";
    public string Custo { get; set; } = "Gratuito";
    public bool JaAdicionado { get; set; }
    public bool FormatoAtivo { get; set; }

    public string PreviewTitle { get; set; } = string.Empty;
    public string PreviewSubtitle { get; set; } = string.Empty;
    public string PreviewIcone { get; set; } = string.Empty;
    public string PreviewCor { get; set; } = "#1DB954";
    public bool ShowPreviewBar { get; set; } = false;

    // UI Helpers
    public string TextoBotao => JaAdicionado ? "Instalado" : "+ Adicionar";
    public string CorBotao => JaAdicionado ? "Transparent" : "#0A84FF"; 
    public string CorBordaBotao => JaAdicionado ? "#1DB954" : "#0A84FF";
    public string CorTextoBotao => JaAdicionado ? "#1DB954" : "#FFFFFF";
    public string ForegroundIcone => JaAdicionado ? "#8E8E93" : "#0A84FF"; // just a trick
    
    // Tag background
    public string CustoBackground => Custo == "Pro" ? "#3D2447" : (Custo == "Gratuito" ? "#193524" : "#24324D");
    public string CustoForeground => Custo == "Pro" ? "#D18EE2" : (Custo == "Gratuito" ? "#25D366" : "#4AA1FF");
}



