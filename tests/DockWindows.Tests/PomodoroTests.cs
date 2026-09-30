using DockWindows.Core.Models;
using DockWindows.Core.Widgets;
using Xunit;

namespace DockWindows.Tests;

public class PomodoroTests
{
    [Fact]
    public void Inicializacao_EstadoInicialEhFocoComTempoConfigurado()
    {
        var config = new WidgetConfig
        {
            DuracaoFocoMinutos = 20,
            DuracaoPausaCurtaMinutos = 5
        };

        var pomo = new PomodoroEngine(config);

        Assert.Equal(PomodoroEstado.Foco, pomo.Estado);
        Assert.Equal("20:00", pomo.TempoFormatado);
        Assert.False(pomo.EstaExecutando);
    }

    [Fact]
    public void AvancarFase_DoFoco_TransicionaParaPausaCurta()
    {
        var config = new WidgetConfig
        {
            DuracaoFocoMinutos = 25,
            DuracaoPausaCurtaMinutos = 5,
            CiclosAtePausaLonga = 4
        };

        var pomo = new PomodoroEngine(config);
        pomo.AvancarFase();

        Assert.Equal(PomodoroEstado.PausaCurta, pomo.Estado);
        Assert.Equal("05:00", pomo.TempoFormatado);
        Assert.Equal(1, pomo.CiclosConcluidos);
    }

    [Fact]
    public void AvancarFase_AposQuatroCiclos_TransicionaParaPausaLonga()
    {
        var config = new WidgetConfig
        {
            DuracaoFocoMinutos = 25,
            DuracaoPausaCurtaMinutos = 5,
            DuracaoPausaLongaMinutos = 15,
            CiclosAtePausaLonga = 4
        };

        var pomo = new PomodoroEngine(config);
        
        // 1º Ciclo: Foco -> Pausa Curta
        pomo.AvancarFase();
        // 1ª Pausa: Pausa Curta -> Foco
        pomo.AvancarFase();
        // 2º Ciclo: Foco -> Pausa Curta
        pomo.AvancarFase();
        // 2ª Pausa: Pausa Curta -> Foco
        pomo.AvancarFase();
        // 3º Ciclo: Foco -> Pausa Curta
        pomo.AvancarFase();
        // 3ª Pausa: Pausa Curta -> Foco
        pomo.AvancarFase();
        // 4º Ciclo: Foco -> Pausa Longa!
        pomo.AvancarFase();

        Assert.Equal(PomodoroEstado.PausaLonga, pomo.Estado);
        Assert.Equal("15:00", pomo.TempoFormatado);
        Assert.Equal(4, pomo.CiclosConcluidos);
    }

    [Fact]
    public void Tick_DecrementaSegundosQuandoExecutando()
    {
        var config = new WidgetConfig { DuracaoFocoMinutos = 10 };
        var pomo = new PomodoroEngine(config);
        pomo.Iniciar();

        pomo.Tick();
        Assert.Equal("09:59", pomo.TempoFormatado);
    }
}
