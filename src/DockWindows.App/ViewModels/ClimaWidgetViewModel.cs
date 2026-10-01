using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class ClimaWidgetViewModel : ObservableObject
{
    private string _condicao = "Mostly Cloudy";
    private string _local = "Istanbul";
    private string _temperatura = "21Â°";
    private string _iconeEmoji = "â›…";
    
    public string Condicao
    {
        get => _condicao;
        set => SetProperty(ref _condicao, value);
    }
    
    public string Local
    {
        get => _local;
        set => SetProperty(ref _local, value);
    }
    
    public string Temperatura
    {
        get => _temperatura;
        set => SetProperty(ref _temperatura, value);
    }

    public string IconeEmoji
    {
        get => _iconeEmoji;
        set => SetProperty(ref _iconeEmoji, value);
    }

    public ClimaWidgetViewModel()
    {
        _ = AtualizarClimaAsync();
        
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = System.TimeSpan.FromHours(1)
        };
        timer.Tick += (s, e) => _ = AtualizarClimaAsync();
        timer.Start();
    }

        private string _localizacaoPreferida = string.Empty;

    public void SincronizarLocalizacao(string localizacao)
    {
        _localizacaoPreferida = localizacao ?? string.Empty;
        _ = AtualizarClimaAsync();
    }

    private async System.Threading.Tasks.Task AtualizarClimaAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = System.TimeSpan.FromSeconds(10);
            
            string url = "https://wttr.in/";
            if (!string.IsNullOrWhiteSpace(_localizacaoPreferida))
            {
                url += System.Uri.EscapeDataString(_localizacaoPreferida);
            }
            url += "?format=%C|%t|%l";

            var response = await client.GetStringAsync(url);
            if (!string.IsNullOrWhiteSpace(response))
            {
                var parts = response.Split('|');
                if (parts.Length >= 3)
                {
                    System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
                    {
                        Condicao = parts[0].Trim();
                        Temperatura = parts[1].Trim();
                        Local = parts[2].Trim();
                        
                        var condLower = Condicao.ToLowerInvariant();
                        if (condLower.Contains("rain") || condLower.Contains("chuva") || condLower.Contains("drizzle") || condLower.Contains("shower")) IconeEmoji = "🌧️";
                        else if (condLower.Contains("cloud") || condLower.Contains("nublado") || condLower.Contains("overcast")) IconeEmoji = "☁️";
                        else if (condLower.Contains("clear") || condLower.Contains("limpo") || condLower.Contains("sunny") || condLower.Contains("sol")) IconeEmoji = "☀️";
                        else if (condLower.Contains("snow") || condLower.Contains("neve")) IconeEmoji = "❄️";
                        else if (condLower.Contains("storm") || condLower.Contains("tempestade") || condLower.Contains("thunder")) IconeEmoji = "⛈️";
                        else IconeEmoji = "🌤️";
                    });
                }
            }
        }
        catch
        {
            // Fallback
        }
    }
}

