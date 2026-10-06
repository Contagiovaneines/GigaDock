namespace DockWindows.Core.Models;

public record EstiloWidget(string Id, string Nome, string Referencia, FormatoWidget Formato = FormatoWidget.Compacto);

public static class EstilosWidget
{
    public static IReadOnlyList<EstiloWidget> Para(TipoWidget tipo) => tipo switch
    {
        TipoWidget.Relogio => new EstiloWidget[]
        {
            new("hora", "Hora", "12:34"), new("segundos", "Hora com segundos", "12:34:56"),
            new("hora-data", "Hora e data", "12:34\nSeg, 22 Jun"), new("flip", "Dígitos em cartões", "1 2 : 3 4"),
            new("data", "Data", "Segunda-feira\n22 de junho"), new("dia", "Dia do mês", "22\nJun"),
            new("hoje", "Hoje", "22 Jun, segunda\n12:34"),
            new("mundial", "Relógios mundiais", "São Paulo 12:34\nLondres 16:34\nTóquio 00:34"),
            new("cronometro", "Cronômetro", "0:00 ▶"), new("temporizador", "Temporizador de 5 minutos", "5:00 ▶"),
            new("analogico-minimal", "Analógico minimalista", ""),
            new("analogico-claro", "Analógico clássico claro", ""),
            new("analogico-escuro", "Analógico clássico escuro", ""),
            new("analogico-digital", "Analógico e digital", "12:34")
        },
        TipoWidget.Clima => new EstiloWidget[]
        {
            new("temperatura", "Temperatura", "☁ 21°"), new("local", "Local e detalhes", "São Paulo · ☁ 21°\n18 km/h · 0 mm", FormatoWidget.Expandido),
            new("previsao", "Previsão diária", "Seg ☁ 24° · Ter ☂ 22° · Qua ☁ 21°", FormatoWidget.Expandido),
            new("compacto", "Hoje e previsão", "21° · Seg 24° · Ter 22° · Qua 21°"),
            new("condicao", "Condição do tempo", "☂\nChuva, 21°"), new("horas", "Próximas horas", "12h 21° · 15h 20° · 18h 19°", FormatoWidget.Expandido),
            new("vento", "Vento", "18 km/h · SO"), new("sol", "Nascer e pôr do sol", "06:12 ☀ 20:18", FormatoWidget.Expandido),
            new("detalhado", "Cartão azul com detalhes", "21°\nSensação 23° · próximas horas", FormatoWidget.Expandido)
        },
        TipoWidget.CalendarioCompromissos => new EstiloWidget[] { new("agenda", "Agenda de hoje", "Hoje\nReunião 13:30 · Revisão 15:30", FormatoWidget.Expandido), new("proximo", "Próximo evento", "Hoje · 13:30\nReunião", FormatoWidget.Compacto), new("reuniao", "Próxima reunião", "Revisão do projeto\n14:00 · Entrar ▶"), new("central-reuniao", "Central da reunião", "Revisão do projeto\nComeça em 8 min · Entrar ▶", FormatoWidget.Expandido) },
        TipoWidget.Midia => new EstiloWidget[] { new("capa", "Capa e controles", "Nome da música\nArtista · ◀ Ⅱ ▶", FormatoWidget.Expandido), new("tocando", "Tocando agora", "♫ Nome da música   ◀ Ⅱ ▶"), new("mini", "Controle mini", "Ⅱ"), new("barra", "Barra de reprodução", "Nome da música   Ⅱ\n1:24 ━━━━━ 3:42", FormatoWidget.Expandido) },
        TipoWidget.Bateria => new EstiloWidget[] { new("compacto", "Porcentagem e bateria", "78% ▰"), new("anel", "Anel de carga", "78%") },
        TipoWidget.MonitorSistema => new EstiloWidget[]
        {
            new("compacto", "CPU e RAM compactos", "CPU 34% · RAM 61%"), new("expandido", "CPU e RAM", "CPU 34% · RAM 61%", FormatoWidget.Expandido),
            new("cpu", "CPU em anel", "34%"), new("ram", "RAM em anel", "61%"),
            new("cpu-grafico", "Gráfico de CPU", "CPU 34%", FormatoWidget.Expandido), new("ram-grafico", "Gráfico de RAM", "RAM 61%", FormatoWidget.Expandido),
            new("rede", "Rede", "↓ 4,2 MB/s\n↑ 312 KB/s"), new("download", "Download", "↓ 4,2 MB/s"), new("upload", "Upload", "↑ 312 KB/s"),
            new("rede-grafico", "Gráfico de rede", "↓ 4,2 MB/s · ↑ 312 KB/s", FormatoWidget.Expandido), new("armazenamento", "Armazenamento", "128 GB livres")
        },
        _ => new EstiloWidget[] { new("compacto", "Compacto", "Resumo", FormatoWidget.Compacto), new("expandido", "Expandido", "Resumo com detalhes", FormatoWidget.Expandido) }
    };

    public static string Resolver(WidgetInstanceConfig widget, bool analogicoLegado = false, bool climaLegado = false)
    {
        if (Para(widget.Tipo).Any(e => e.Id == widget.Estilo)) return widget.Estilo;
        return widget.Tipo switch
        {
            TipoWidget.Relogio => analogicoLegado ? "analogico-digital" : "hora-data",
            TipoWidget.Clima => climaLegado ? "detalhado" : "compacto",
            TipoWidget.CalendarioCompromissos => widget.Formato == FormatoWidget.Expandido ? "agenda" : "proximo",
            TipoWidget.Midia => "capa",
            _ => widget.Formato == FormatoWidget.Expandido ? "expandido" : "compacto"
        };
    }

    public static bool Aplicar(WidgetInstanceConfig widget, string id)
    {
        var estilo = Para(widget.Tipo).FirstOrDefault(e => e.Id == id);
        if (estilo == null) return false;
        widget.Estilo = estilo.Id;
        widget.Formato = estilo.Formato;
        return true;
    }
}
