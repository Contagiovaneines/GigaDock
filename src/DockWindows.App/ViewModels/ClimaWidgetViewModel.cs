using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class ClimaWidgetViewModel : ObservableObject
{
    private string _condicao = "Mostly Cloudy";
    private string _local = "Istanbul";
    private string _temperatura = "21°";
    private string _iconeEmoji = "⛅";
    
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
    }
}
