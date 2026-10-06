using DockWindows.Core.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;

namespace DockWindows.Core.Widgets;

/// <summary>Importação limitada em tamanho/horizonte. Não faz rede ou persistência.</summary>
public static class ImportadorCalendario
{
    public static IReadOnlyList<CompromissoLocal> Importar(string texto, DateTime inicio, DateTime fim,
        TimeZoneInfo? destino = null, CancellationToken cancelamento = default)
    {
        if (texto.Length > 2 * 1024 * 1024) throw new FormatException("Calendário excede 2 MB.");
        if (fim <= inicio || fim - inicio > TimeSpan.FromDays(366)) throw new ArgumentOutOfRangeException(nameof(fim));
        destino ??= TimeZoneInfo.Local;
        var calendario = Calendar.Load(texto) ?? throw new FormatException("Calendário inválido.");
        if (calendario.Events.Count > 2000) throw new FormatException("Calendário excede 2.000 eventos.");
        var resultado = new List<CompromissoLocal>();
        // A margem cobre eventos em outros fusos e datas flutuantes. Filtrar depois da conversão.
        var desde = new CalDateTime(DateTime.SpecifyKind(inicio.AddDays(-2), DateTimeKind.Unspecified));
        var ate = fim.AddDays(2);
        var opcoes = new EvaluationOptions { MaxUnmatchedIncrementsLimit = 10000 };
        int avaliados = 0;
        foreach (var ocorrencia in calendario.GetOccurrences<CalendarEvent>(desde, opcoes))
        {
            cancelamento.ThrowIfCancellationRequested();
            if (++avaliados > 10000) throw new FormatException("Calendário excede o limite de ocorrências.");
            var data = ocorrencia.Period.StartTime;
            if (data.Value > ate) break;
            var evento = (CalendarEvent)ocorrencia.Source;
            if (string.Equals(evento.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)) continue;
            var local = !data.HasTime || data.IsFloating ? data.Value
                : TimeZoneInfo.ConvertTimeFromUtc(data.AsUtc, destino);
            if (local < inicio || local >= fim) continue;
            resultado.Add(new CompromissoLocal
            {
                Id = $"{evento.Uid}:{data.Value:O}:{data.TzId}",
                Titulo = evento.Summary ?? "Evento importado", Local = evento.Location,
                Descricao = evento.Description, DataHora = DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
                DiaInteiro = !data.HasTime
            });
        }
        return resultado.OrderBy(c => c.DataHora).ToArray();
    }
}
