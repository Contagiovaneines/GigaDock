using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GigaDock.Services;

/// <summary>Consultas públicas opcionais. Sem token, geolocalização automática, conta ou envio de dados da dock.</summary>
public sealed class PublicDataClient(HttpClient client)
{
    private async Task<JsonDocument> ReadAsync(string url, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("GigaDock/3.2 (desktop local)");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (!response.IsSuccessStatusCode) throw new IOException("O provedor não respondeu à consulta. Tente novamente mais tarde.");
        if (response.Content.Headers.ContentLength > 1024 * 1024) throw new IOException("Resposta maior que o limite permitido.");
        await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, deadline.Token)) > 0)
        {
            if (buffer.Length + count > 1024 * 1024) throw new IOException("Resposta maior que o limite permitido.");
            buffer.Write(chunk, 0, count);
        }
        return JsonDocument.Parse(buffer.ToArray());
    }

    public async Task<string> WeatherAsync(string city, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(city) || city.Length > 100 || city.Any(char.IsControl)) throw new ArgumentException("Informe uma cidade de até 100 caracteres.");
        using var location = await ReadAsync("https://geocoding-api.open-meteo.com/v1/search?count=1&language=pt&name=" + Uri.EscapeDataString(city.Trim()), token);
        if (!location.RootElement.TryGetProperty("results", out var places) || places.GetArrayLength() == 0) throw new IOException("Cidade não encontrada. Inclua estado ou país na busca.");
        var place = places[0];
        var latitude = place.GetProperty("latitude").GetDouble().ToString(CultureInfo.InvariantCulture);
        var longitude = place.GetProperty("longitude").GetDouble().ToString(CultureInfo.InvariantCulture);
        using var forecast = await ReadAsync($"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m&daily=temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=7", token);
        var root = forecast.RootElement; var days = root.GetProperty("daily");
        var text = new StringBuilder($"{place.GetProperty("name").GetString()} — {root.GetProperty("current").GetProperty("temperature_2m").GetDouble():0.#} °C\n");
        for (var i = 0; i < days.GetProperty("time").GetArrayLength(); i++)
            text.AppendLine($"{days.GetProperty("time")[i].GetString()}: {days.GetProperty("temperature_2m_min")[i].GetDouble():0}–{days.GetProperty("temperature_2m_max")[i].GetDouble():0} °C");
        text.Append("Fonte: Open-Meteo. Atualizado nesta consulta.");
        return text.ToString();
    }

    public async Task<string> ExchangeAsync(string from, string to, CancellationToken token = default)
    {
        from = from.Trim().ToUpperInvariant(); to = to.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(from, "^[A-Z]{3}$") || !Regex.IsMatch(to, "^[A-Z]{3}$")) throw new ArgumentException("Informe códigos de moeda com três letras, como USD e BRL.");
        if (from == to) return $"1 {from} = 1 {to}";
        using var data = await ReadAsync($"https://api.frankfurter.dev/v2/rate/{from}/{to}", token);
        var rate = data.RootElement.GetProperty("rate").GetDecimal();
        return $"1 {from} = {rate:0.####} {to}\nReferência: {data.RootElement.GetProperty("date").GetString()}\nFonte: Frankfurter. Cotação de referência, sem negociação.";
    }

    public async Task<string> GitHubAsync(string user, CancellationToken token = default)
    {
        if (!Regex.IsMatch(user, "^[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?$")) throw new ArgumentException("Informe um nome de usuário público válido do GitHub.");
        using var data = await ReadAsync("https://api.github.com/users/" + Uri.EscapeDataString(user), token);
        var root = data.RootElement;
        return $"{root.GetProperty("login").GetString()}\nRepositórios públicos: {root.GetProperty("public_repos").GetInt32()}\nSeguidores: {root.GetProperty("followers").GetInt32()}\nFonte: GitHub. Dados públicos, sem acesso à conta.";
    }
}
