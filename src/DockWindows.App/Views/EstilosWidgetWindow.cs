using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DockWindows.App.Controls;
using DockWindows.Core.Models;

namespace DockWindows.App.Views;

public class EstilosWidgetWindow : Window
{
    public string? EstiloSelecionado { get; private set; }
    public EstilosWidgetWindow(WidgetInstanceConfig widget, string ambiente)
    {
        Title = $"Estilo de {widget.Nome}"; Width = 1060; Height = 700; MinWidth = 380; MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(16, 18, 22)); Foreground = Brushes.WhiteSmoke;
        var cardTemplate = (ControlTemplate)System.Windows.Markup.XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="Button">
                <Border x:Name="Surface" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="16" Padding="{TemplateBinding Padding}">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Background" Value="#263442"/></Trigger>
                    <Trigger Property="IsPressed" Value="True"><Setter TargetName="Surface" Property="Background" Value="#30485C"/></Trigger>
                    <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#B6E2FF"/></Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        var root = new DockPanel { Margin = new Thickness(24) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
        header.Children.Add(new TextBlock { Text = $"{widget.Nome} · {ambiente}", FontSize = 24, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold });
        header.Children.Add(new TextBlock { Text = "Referências ilustrativas. Escolha para aplicar somente neste ambiente.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Silver, Margin = new Thickness(0, 8, 0, 0) });
        header.Children.Add(new TextBlock { Text = Dependencias(widget.Tipo), TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Silver, Margin = new Thickness(0, 6, 0, 0) });
        DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        var cancel = new Button { Template = cardTemplate, Background = new SolidColorBrush(Color.FromRgb(31, 44, 58)), Foreground = Brushes.WhiteSmoke, BorderBrush = Brushes.SlateGray, BorderThickness = new Thickness(1), Content = "Cancelar", IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(cancel, Dock.Bottom); root.Children.Add(cancel);
        var cards = new WrapPanel();
        var climaReferencia = new DockWindows.App.ViewModels.ClimaWidgetViewModel(iniciarConsulta: false);
        climaReferencia.AplicarDados(new DockWindows.App.ViewModels.DadosClima("21°", "Sua cidade", "Nublado", new[]
        {
            new DockWindows.App.ViewModels.PrevisaoClima("Seg", "24°", "Nublado", "Nuvem", "17°"), new("Ter", "22°", "Chuva", "Chuva", "16°"), new("Qua", "21°", "Nublado", "Nuvem", "15°")
        }, new[] { new DockWindows.App.ViewModels.PrevisaoHora("12h", "21°", "Nuvem"), new("15h", "20°", "Chuva"), new("18h", "19°", "Nuvem"), new("21h", "18°", "Nuvem"), new("00h", "17°", "Nuvem") }, "23°", "18 km/h", "SO", "0 mm", "06:12", "20:18", "Nuvem", new DateTime(2026, 6, 22, 12, 0, 0)));
        foreach (var estilo in EstilosWidget.Para(widget.Tipo))
        {
            var panel = new StackPanel { Width = 232 };
            UIElement preview;
            if (widget.Tipo == TipoWidget.Relogio) preview = new EstiloRelogioControl { Width = 176, Height = 56, Estilo = estilo.Id, Horario = new DateTime(2026, 6, 22, 12, 34, 56) };
            else if (widget.Tipo == TipoWidget.Clima) preview = new ClimaEstiloControl { Width = ClimaEstiloControl.LarguraPara(estilo.Id), Height = 64, Estilo = estilo.Id, Dados = climaReferencia };
            else if (widget.Tipo == TipoWidget.Midia) preview = new ReferenciaMidiaControl { Width = 190, Height = 56, Estilo = estilo.Id };
            else if (widget.Tipo == TipoWidget.GitHubContribuicoes) preview = new GitHubContributionStyleControl { Width = estilo.Formato == FormatoWidget.Expandido ? 300 : 68, Height = 68, Estilo = estilo.Id, Total = 1284, Preview = true };
            else if (widget.Tipo is TipoWidget.MonitorSistema or TipoWidget.Bateria)
                preview = new IndicadorSistemaControl { Width = LarguraIndicador(estilo.Id), Height = 64, Estilo = estilo.Id, Titulo = widget.Tipo == TipoWidget.Bateria ? "Bateria" : estilo.Id.StartsWith("ram") ? "RAM" : estilo.Id.StartsWith("rede") ? "Rede" : estilo.Nome, Texto = estilo.Referencia.Split('\n')[0], Secundario = estilo.Referencia.Split('\n').Skip(1).FirstOrDefault() ?? "", Valor = widget.Tipo == TipoWidget.Bateria ? 78 : estilo.Id.StartsWith("ram") ? 61 : 34, ValorSecundario = 61, Serie = Enumerable.Range(0, 30).Select(i => 35.0 + Math.Sin(i * .7) * 20).ToArray(), SerieSecundaria = Enumerable.Range(0, 30).Select(i => 15.0 + Math.Cos(i * .4) * 10).ToArray() };
            else preview = new TextBlock { Text = estilo.Referencia, FontSize = 14, TextAlignment = TextAlignment.Center, Height = 56, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.WhiteSmoke, VerticalAlignment = VerticalAlignment.Center };

            var dockPreview = new Border
            {
                Width = 232, Height = 86, CornerRadius = new CornerRadius(18), Padding = new Thickness(10),
                Background = new LinearGradientBrush(Color.FromArgb(235, 18, 23, 30), Color.FromArgb(245, 7, 10, 15), 90),
                BorderBrush = new LinearGradientBrush(Color.FromRgb(255, 50, 101), Color.FromRgb(42, 221, 255), 0),
                BorderThickness = new Thickness(1.5), Child = new Viewbox { StretchDirection = StretchDirection.DownOnly, Child = preview }
            };
            panel.Children.Add(dockPreview);
            panel.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(8), Background = new SolidColorBrush(Color.FromRgb(35, 49, 62)),
                Padding = new Thickness(8, 3, 8, 3), HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 9, 0, 0), Child = new TextBlock { Text = CategoriaTamanho(estilo), FontSize = 10, Foreground = Brushes.LightSkyBlue, FontWeight = FontWeights.SemiBold }
            });
            panel.Children.Add(new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, Text = estilo.Nome, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Foreground = Brushes.WhiteSmoke, Margin = new Thickness(0, 12, 0, 0) });
            var selected = estilo.Id == EstilosWidget.Resolver(widget);
            panel.Children.Add(new TextBlock { Text = selected ? "✓ Estilo atual" : "Selecionar estilo", FontSize = 11, Foreground = selected ? Brushes.LightSkyBlue : Brushes.SlateGray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 7, 0, 0) });
            var button = new Button { Template = cardTemplate, Cursor = System.Windows.Input.Cursors.Hand, Content = panel, Width = 270, Height = 205, Margin = new Thickness(0, 0, 12, 12), Padding = new Thickness(12), Background = new SolidColorBrush(Color.FromRgb(23, 31, 40)), BorderBrush = selected ? Brushes.DeepSkyBlue : new SolidColorBrush(Color.FromRgb(49, 64, 79)), BorderThickness = new Thickness(selected ? 2 : 1), ToolTip = $"Aplicar {estilo.Nome}" };
            System.Windows.Automation.AutomationProperties.SetName(button, $"{estilo.Nome}, aplicar no ambiente {ambiente}");
            button.Click += (_, _) => { EstiloSelecionado = estilo.Id; DialogResult = true; };
            cards.Children.Add(button);
        }
        root.Children.Add(new ScrollViewer { Content = cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        Content = root;
        Closed += (_, _) => climaReferencia.Dispose();
        MaxHeight = SystemParameters.WorkArea.Height; MaxWidth = SystemParameters.WorkArea.Width;
    }
    private static string CategoriaTamanho(EstiloWidget estilo) => estilo.Id.Contains("expans", StringComparison.OrdinalIgnoreCase)
        ? "EXPANSÍVEL"
        : estilo.Formato == FormatoWidget.Expandido ? "LARGO" : "COMPACTO";
    private static double LarguraIndicador(string estilo) => estilo switch
    {
        "ventoinha-cpu" => 68,
        "rede-compacta" => 88,
        "atividade-compacta" => 102,
        "atividade-larga" => 210,
        _ => 206
    };
    private static string Dependencias(TipoWidget tipo) => tipo switch
    {
        TipoWidget.Midia => "Usa sessões de mídia do Windows. O player deve oferecer controles de mídia; prévia não acessa o player.",
        TipoWidget.Clima => "Requer internet (wttr.in). Dados ilustrativos nesta prévia; cache compartilhado entre estilos no aplicativo.",
        TipoWidget.CalendarioCompromissos => "Eventos locais ou iCalendar. Link requer internet; arquivo local não requer conexão.",
        TipoWidget.TeamsStatus => "Presença estimada por processos e títulos. Alertas exigem acesso às notificações do Windows.",
        TipoWidget.WhatsAppNotificacoes => "Requer acesso às notificações do Windows. Contagem não confirma mensagens não lidas.",
        TipoWidget.DiscordVoz => "Abre o aplicativo instalado. Canal e participantes indisponíveis sem RPC autenticado.",
        TipoWidget.OBSStudio => "Conecta ao OBS WebSocket v5 local e confirma os estados de gravação e transmissão.",
        TipoWidget.GitHubContribuicoes => "Requer internet e nome público. Lê HTML de contribuições; não é integração com Actions.",
        TipoWidget.MonitorSistema => "Leituras locais do Windows somente quando visível. Valores nesta prévia são ilustrativos.",
        TipoWidget.Bateria => "Depende de bateria e leitura de energia disponíveis no Windows. Valor na prévia é ilustrativo.",
        _ => "Dados locais. Prévia ilustrativa sem iniciar serviços externos."
    };
}
