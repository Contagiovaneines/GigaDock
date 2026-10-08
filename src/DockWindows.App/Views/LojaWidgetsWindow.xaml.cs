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
    public string? WidgetIdParaRemover { get; private set; }
    public bool Removeu { get; private set; }

    private readonly List<WidgetInstanceConfig> _widgetsInstalados;
    private readonly string _ambiente;

    public LojaWidgetsWindow(IEnumerable<WidgetInstanceConfig>? widgetsInstalados = null, string ambiente = "Ambiente atual")
    {
        _ambiente = ambiente;
        InitializeComponent();
        Title = $"Loja de widgets · {ambiente}";
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;
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
            new ItemLoja { Tipo = TipoWidget.Midia, Nome = "Mídia", Categoria = "Mídia", Descricao = "Capa, tocando agora, mini ou barra de reprodução da sessão de mídia do Windows.", PreviewTitle = "Nome da música", DescricaoFormato = "Quatro estilos com controles" },
            new ItemLoja { Tipo = TipoWidget.Bateria, Nome = "Bateria", Categoria = "Sistema", Descricao = "Porcentagem compacta ou anel com a carga real da bateria.", PreviewTitle = "78%", DescricaoFormato = "Compacto ou anel" },
            new ItemLoja { Tipo = TipoWidget.Notas, Nome = "Notas", Categoria = "Produtividade", Descricao = "Editor de texto local com salvamento automático. Não exige internet ou conta.", PreviewTitle = "Minhas notas", PreviewSubtitle = "Dados ilustrativos", DescricaoFormato = "Texto local" },
            new ItemLoja { Tipo = TipoWidget.LembreteAgua, Nome = "Lembrete de água", Categoria = "Produtividade", Descricao = "Lembretes locais com intervalo e período ativo configuráveis.", PreviewTitle = "Água · 3 hoje", PreviewSubtitle = "Próximo às 15:30", DescricaoFormato = "Intervalo local" },
            new ItemLoja { Tipo = TipoWidget.CotacaoMoedas, Nome = "Cotação de moedas", Categoria = "Utilidade", Descricao = "Taxas de referência diárias do Banco Central Europeu, com cache offline.", PreviewTitle = "1 USD = 5,20 BRL", PreviewSubtitle = "Atualização diária", DescricaoFormato = "Par de moedas" },
            new ItemLoja { Tipo = TipoWidget.AreaTransferencia, Nome = "Área de transferência", Categoria = "Produtividade", Descricao = "Histórico de textos copiados, limitado e mantido somente nesta sessão.", PreviewTitle = "3 itens copiados", PreviewSubtitle = "Dados locais", DescricaoFormato = "Até 20 textos" },
            new ItemLoja { Tipo = TipoWidget.ArquivosRecentes, Nome = "Downloads e capturas", Categoria = "Produtividade", Descricao = "Arquivos recentes de Downloads e Imagens, com acesso à captura segura do Windows.", PreviewTitle = "Arquivos recentes", PreviewSubtitle = "Downloads e capturas", DescricaoFormato = "Até 20 arquivos" },
            new ItemLoja { Tipo = TipoWidget.Conectividade, Nome = "Wi-Fi e Bluetooth", Categoria = "Sistema", Descricao = "Rede Wi-Fi atual, intensidade do sinal e dispositivos Bluetooth conectados.", PreviewTitle = "Wi-Fi 82%", PreviewSubtitle = "2 dispositivos Bluetooth", DescricaoFormato = "Estado local" },
            new ItemLoja { Tipo = TipoWidget.AudioSistema, Nome = "Áudio do sistema", Categoria = "Sistema", Descricao = "Volume e mudo por aplicativo, além de mudo explícito do microfone padrão.", PreviewTitle = "Áudio por aplicativo", PreviewSubtitle = "Microfone ativo", DescricaoFormato = "Core Audio" },
            new ItemLoja { Tipo = TipoWidget.EstanteArquivos, Nome = "Estante de arquivos", Categoria = "Produtividade", Descricao = "Referências locais com prévias seguras de texto e imagem, sem executar conteúdo incorporado.", PreviewTitle = "Meus arquivos", PreviewSubtitle = "Até 30 referências", DescricaoFormato = "Texto, imagem e metadados" },
            new ItemLoja { 
                Tipo = TipoWidget.Relogio, Formato = FormatoWidget.Expandido, Nome = "Relógio", 
                Descricao = "Hora, segundos, data e mostradores analógicos com estilos por ambiente.",
                DescricaoFormato = "Hora e Data", Icone = "\uE121", CorIcone = "#253342", Categoria = "Sistema",
                PreviewTitle = "14:48", PreviewSubtitle = "São Paulo", PreviewIcone = "", PreviewCor = "#0A84FF", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.Clima, Formato = FormatoWidget.Expandido, Nome = "Clima", 
                Descricao = "Previsão por wttr.in. Requer internet; cache de uma hora e indicação de dados antigos.",
                DescricaoFormato = "Detalhado com Local", Icone = "\uE9CA", CorIcone = "#21313A", Categoria = "Utilidade",
                PreviewTitle = "24°C", PreviewSubtitle = "Céu Limpo   Umid 60%", PreviewIcone = "\uE9CA", PreviewCor = "#FF9F0A", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.WhatsAppNotificacoes, Formato = FormatoWidget.Expandido, Nome = "WhatsApp", 
                Descricao = "Notificações acessíveis do Windows; exige permissão e não confirma mensagens não lidas.",
                DescricaoFormato = "Última Mensagem", Icone = "\uE8BD", CorIcone = "#193524", Categoria = "Comunicação", Custo = "v1.2",
                PreviewTitle = "3 Conversas", PreviewSubtitle = "1 Nova", PreviewIcone = "\uE8BD", PreviewCor = "#25D366", ShowPreviewBar = true
            },
            new ItemLoja { 
                Tipo = TipoWidget.TeamsStatus, Formato = FormatoWidget.Expandido, Nome = "Teams", 
                Descricao = "Notificações do Windows exigem permissão. Presença estimada por processos e títulos, sem API oficial.",
                DescricaoFormato = "Status Detalhado", Icone = "\uE716", CorIcone = "#292138", Categoria = "Comunicação",
                PreviewTitle = "Em Reunião", PreviewSubtitle = "Mic Mudo", PreviewIcone = "\uE716", PreviewCor = "#FF453A", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.DiscordVoz, Formato = FormatoWidget.Expandido, Nome = "Discord", 
                Descricao = "Abre o Discord. Canal e participantes indisponíveis: RPC autenticado ainda não implementado.", TagStatus = "BETA",
                DescricaoFormato = "Canal de Voz", Icone = "\uE716", CorIcone = "#20242B", Categoria = "Comunicação",
                PreviewTitle = "Gamer Room", PreviewSubtitle = "Você, Alex, Sam", PreviewIcone = "\uE716", PreviewCor = "#5865F2", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.GitHubContribuicoes, Formato = FormatoWidget.Expandido, Nome = "GitHub contribuições",
                Descricao = "Contribuições públicas por leitura HTML do GitHub. Requer internet; não monitora Actions ou PRs.", TagStatus = "BETA",
                DescricaoFormato = "Gráfico de Commits", Icone = "\uE943", CorIcone = "#20242B", Categoria = "Dev Tools", Custo = "Pro",
                PreviewTitle = "Contribuições", PreviewSubtitle = "Dados ilustrativos", PreviewIcone = "\uE943", PreviewCor = "#1DB954", ShowPreviewBar = false
            },
            new ItemLoja { 
                Tipo = TipoWidget.CalendarioCompromissos, Formato = FormatoWidget.Expandido, Nome = "Calendário", 
                Descricao = "Eventos locais ou iCalendar (arquivo/link). Links exigem internet; fusos e recorrências são importados.",
                DescricaoFormato = "Próximo Evento", Icone = "\uE163", CorIcone = "#28303D", Categoria = "Produtividade",
                PreviewTitle = "Sync de Design", PreviewSubtitle = "em 12m", PreviewIcone = "\uE163", PreviewCor = "#0A84FF", ShowPreviewBar = true
            },
            new ItemLoja { 
                Tipo = TipoWidget.MonitorSistema, Formato = FormatoWidget.Expandido, Nome = "Monitor de Sistema", 
                Descricao = "CPU, RAM, rede e disco do Windows em anéis, indicadores ou gráficos com leituras locais.",
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
                Descricao = "Conecta ao OBS WebSocket v5 local, confirma gravação e transmissão e protege a senha no Windows.", TagStatus = "BETA",
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
                item.InstaladoAtivo = jaTem.Visivel;
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
        if (_catalogoCompleto == null || TxtBusca == null || ListaLoja == null) return;
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
        if (sender is not Button btn || btn.Tag is not ItemLoja item) return;
        var instalado = _widgetsInstalados.FirstOrDefault(w => w.Tipo == item.Tipo);
        if (instalado != null && !instalado.Visivel && !WidgetCapabilities.PermiteMultiplasInstancias(item.Tipo))
        {
            instalado.Visivel = true;
            WidgetSelecionado = instalado;
            DialogResult = true;
            return;
        }
        var widget = new WidgetInstanceConfig { Tipo = item.Tipo, Nome = item.Nome, Formato = instalado?.Formato ?? item.Formato, Estilo = instalado?.Estilo ?? "", Visivel = instalado?.Visivel ?? true };
        var estilos = new EstilosWidgetWindow(widget, _ambiente) { Owner = this };
        if (estilos.ShowDialog() != true || estilos.EstiloSelecionado == null) return;
        EstilosWidget.Aplicar(widget, estilos.EstiloSelecionado);
        WidgetSelecionado = widget;
        DialogResult = true;
    }

    private void BtnRemover_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ItemLoja item && item.JaAdicionado)
        {
            var w = _widgetsInstalados.FirstOrDefault(x => x.Tipo == item.Tipo);
            if (w != null)
            {
                // To tell the caller to remove it, we return it with a specific property or clear it
                // Instead of a new property, let's just return a "RemoveMe" dummy configuration
                Removeu = true;
                WidgetParaRemover = item.Tipo;
                WidgetIdParaRemover = w.Id;
                DialogResult = true;
                Close();
            }
        }
    }
}

public class ItemLoja
{
    public TipoWidget Tipo { get; set; }
    public string TagStatus { get; set; } = string.Empty;
    public string TagStatusBackground => string.IsNullOrEmpty(TagStatus) ? "Transparent" : "#4A2016";
    public string TagStatusForeground => string.IsNullOrEmpty(TagStatus) ? "Transparent" : "#FF795A";
    public FormatoWidget Formato { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string DescricaoFormato { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public string CorIcone { get; set; } = "#333340";
    public string Categoria { get; set; } = "Sistema";
    public string Custo { get; set; } = "Gratuito";
    public bool JaAdicionado { get; set; }
    public bool InstaladoAtivo { get; set; }
    public bool FormatoAtivo { get; set; }
    public string EstadoTexto => !JaAdicionado ? "Disponível" : InstaladoAtivo ? "Ativo" : "Instalado";
    public string EstadoDescricao => !JaAdicionado ? "Ainda não instalado neste ambiente" : InstaladoAtivo ? "Exibido na dock deste ambiente" : "Instalado, mas oculto da dock";
    public string EstadoCor => !JaAdicionado ? "#8FB9E8" : InstaladoAtivo ? "#72D99C" : "#A9B8C9";

    public string PreviewTitle { get; set; } = string.Empty;
    public string PreviewSubtitle { get; set; } = string.Empty;
    public string PreviewIcone { get; set; } = string.Empty;
    public string PreviewCor { get; set; } = "#1DB954";
    public bool ShowPreviewBar { get; set; } = false;

    // UI Helpers
    public string TextoBotao => JaAdicionado && WidgetCapabilities.PermiteMultiplasInstancias(Tipo)
        ? "Adicionar outro"
        : JaAdicionado ? InstaladoAtivo ? "Configurar estilo" : "Ativar na dock" : "Instalar e escolher estilo";
    public string CorBotao => JaAdicionado ? "Transparent" : "#0A84FF"; 
    public string CorBordaBotao => JaAdicionado ? "#1DB954" : "#0A84FF";
    public string CorTextoBotao => JaAdicionado ? "#1DB954" : "#FFFFFF";
    public string ForegroundIcone => JaAdicionado ? "#8E8E93" : "#0A84FF"; // just a trick
    
    // Tag background
    public string CustoBackground => Custo == "Pro" ? "#3D2447" : (Custo == "Gratuito" ? "#193524" : "#24324D");
    public string CustoForeground => Custo == "Pro" ? "#D18EE2" : (Custo == "Gratuito" ? "#25D366" : "#4AA1FF");
}



