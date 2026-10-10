using System.Net;
using System.Text;
using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;
using GigaDock.Services;
namespace GigaDock.Tests.Linux;

public sealed class PublicIntegrationTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Weather_UsesEncodedCityAndDisplaysForecastWithSource()
    {
        var requests = new List<Uri>();
        using var http = new HttpClient(new Handler(request =>
        {
            requests.Add(request.RequestUri!);
            return request.RequestUri!.Host == "geocoding-api.open-meteo.com"
                ? Json("{\"results\":[{\"name\":\"São Paulo\",\"latitude\":-23.5,\"longitude\":-46.6}]}")
                : Json("{\"current\":{\"temperature_2m\":23},\"daily\":{\"time\":[\"2026-10-09\"],\"temperature_2m_min\":[18],\"temperature_2m_max\":[27]}}");
        }));
        var text = await new PublicDataClient(http).WeatherAsync("São Paulo");
        Assert.Contains("São Paulo", text); Assert.Contains("Open-Meteo", text); Assert.Contains("2026-10-09", text);
        Assert.Equal(2, requests.Count); Assert.Contains("name=S%C3%A3o%20Paulo", requests[0].AbsoluteUri);
    }

    [Fact]
    public async Task PublicQueries_ParseGithubAndExchangeWithoutCredentials()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            Assert.Null(request.Headers.Authorization);
            return request.RequestUri!.Host == "api.github.com" ? Json("{\"login\":\"octocat\",\"public_repos\":8,\"followers\":20}") : Json("{\"rate\":5.5,\"date\":\"2026-10-09\"}");
        }));
        var client = new PublicDataClient(http);
        Assert.Contains("Repositórios públicos: 8", await client.GitHubAsync("octocat"));
        Assert.Contains("Referência: 2026-10-09", await client.ExchangeAsync("usd", "brl"));
    }

    [Fact]
    public async Task InvalidPublicQuery_DoesNotContactProvider()
    {
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("Não deveria consultar")));
        var client = new PublicDataClient(http);
        await Assert.ThrowsAsync<ArgumentException>(() => client.GitHubAsync("user?secret=abc"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.WeatherAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExchangeAsync("USD&x=y", "BRL"));
    }

    [Theory]
    [InlineData(429)]
    [InlineData(503)]
    public async Task ProviderUnavailable_ReportsReadableFailure(int status)
    {
        using var http = new HttpClient(new Handler(_ => new HttpResponseMessage((HttpStatusCode)status)));
        await Assert.ThrowsAsync<IOException>(() => new PublicDataClient(http).GitHubAsync("octocat"));
    }

    [Fact]
    public async Task LargeProviderResponse_IsRejected()
    {
        using var http = new HttpClient(new Handler(_ => Json(new string('a', 1024 * 1024 + 1))));
        await Assert.ThrowsAsync<IOException>(() => new PublicDataClient(http).GitHubAsync("octocat"));
    }

    [Fact]
    public void X11Parser_HandlesTitlesAndRejectsForeignOrMaliciousIds()
    {
        var parsed = X11WindowService.Parse("0x03400001 0 1234 host Meu editor de texto\nmalicious 0 1234 host fake\n0x00012345 0 bad host invalid\n");
        var window = Assert.Single(parsed); Assert.Equal("Meu editor de texto", window.Titulo); Assert.Equal(1234, window.ProcessId);
        using var service = new X11WindowService();
        Assert.False(service.Ativar(new DesktopWindowId("windows", "0x03400001")));
        Assert.False(service.Ativar(new DesktopWindowId("x11-ewmh", "0x123; rm -rf")));
        Assert.False(service.Ativar(new DesktopWindowId("x11-ewmh", null!)));
        Assert.False(service.Minimizar(window.Id));
    }

    [Fact]
    public async Task Obs_RejectsRemoteAndUnknownCommandsBeforeConnection()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => ObsLocalClient.RequestAsync(0, "never-save", "GetRecordStatus"));
        await Assert.ThrowsAsync<ArgumentException>(() => ObsLocalClient.RequestAsync(4455, "never-save", "DeleteScene"));
        Assert.Equal(44, ObsLocalClient.Authentication("pass", "salt", "challenge").Length);
    }
}
