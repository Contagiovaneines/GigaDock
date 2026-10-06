using System.Windows.Controls;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views;

public partial class VisualizacoesSettings : UserControl
{
    public VisualizacoesSettings()
    {
        InitializeComponent();
        ClimaReferencia.DataContext = new
        {
            Local = "Sua cidade", Temperatura = "26°", Condicao = "Céu limpo", Sensacao = "27°",
            Horas = new[] { new PrevisaoHora("12h", "26°", "Sol"), new("15h", "28°", "Sol"),
                new("18h", "25°", "Nuvem"), new("21h", "22°", "Nuvem"), new("00h", "20°", "Nuvem") },
            Previsoes = new[] { new PrevisaoClima("Hoje", "28°", "Céu limpo", "Sol", "20°"),
                new("Amanhã", "27°", "Nublado", "Nuvem", "19°"), new("Qua", "25°", "Chuva", "Chuva", "18°") }
        };
    }
}