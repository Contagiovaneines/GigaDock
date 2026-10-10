using DockWindows.Core.Widgets;

namespace GigaDock.Tests.Core;

public sealed class CalendarPortabilityTests
{
    [Fact]
    public void CalendarioUtc_ConverteFusoSemDependenciaDaUi()
    {
        const string ical = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//GigaDock//Tests//PT
            BEGIN:VEVENT
            UID:portabilidade
            DTSTAMP:20261009T000000Z
            DTSTART:20261009T150000Z
            DTEND:20261009T160000Z
            SUMMARY:Reunião de estudos
            END:VEVENT
            END:VCALENDAR
            """;
        var zone = TimeZoneInfo.CreateCustomTimeZone("TesteUTC-3", TimeSpan.FromHours(-3), "TesteUTC-3", "TesteUTC-3");
        var result = ImportadorCalendario.Importar(ical, new DateTime(2026, 10, 9), new DateTime(2026, 10, 10), zone);
        var entry = Assert.Single(result);
        Assert.Equal("Reunião de estudos", entry.Titulo);
        Assert.Equal(new DateTime(2026, 10, 9, 12, 0, 0), entry.DataHora);
    }
}
