using DockWindows.Core.Models;

namespace DockWindows.Core.Widgets;

public enum PomodoroEstado
{
    Foco = 0,
    PausaCurta = 1,
    PausaLonga = 2
}

public class PomodoroEngine
{
    private WidgetConfig _config;
    private int _segundosTotais;

    public event EventHandler? CicloConcluido;
    public event EventHandler? EstadoMudou;

    public PomodoroEngine(WidgetConfig? config = null)
    {
        _config = config ?? new WidgetConfig();
        ConfigurarEstado(PomodoroEstado.Foco);
    }

    public PomodoroEstado Estado { get; private set; } = PomodoroEstado.Foco;
    public int SegundosRestantes { get; private set; }
    public int CiclosConcluidos { get; private set; }
    public bool EstaExecutando { get; private set; }

    public string EstadoTexto => Estado switch
    {
        PomodoroEstado.Foco => "Foco",
        PomodoroEstado.PausaCurta => "Pausa Curta",
        PomodoroEstado.PausaLonga => "Pausa Longa",
        _ => "Pomodoro"
    };

    public string CorEstado => Estado switch
    {
        PomodoroEstado.Foco => "#FF9F0A",
        PomodoroEstado.PausaCurta => "#30D158",
        PomodoroEstado.PausaLonga => "#0A84FF",
        _ => "#FF9F0A"
    };

    public string TempoFormatado
    {
        get
        {
            int minutos = SegundosRestantes / 60;
            int segundos = SegundosRestantes % 60;
            return $"{minutos:D2}:{segundos:D2}";
        }
    }

    public double Progresso
    {
        get
        {
            if (_segundosTotais <= 0) return 0;
            return 1.0 - ((double)SegundosRestantes / _segundosTotais);
        }
    }

    public void Iniciar() => EstaExecutando = true;
    public void Pausar() => EstaExecutando = false;

    public void Alternar()
    {
        if (EstaExecutando) Pausar();
        else Iniciar();
    }

    public void Reiniciar()
    {
        Pausar();
        ConfigurarEstado(Estado);
    }

    public void AvancarFase()
    {
        Pausar();

        if (Estado == PomodoroEstado.Foco)
        {
            CiclosConcluidos++;
            if (CiclosConcluidos % _config.CiclosAtePausaLonga == 0)
            {
                Estado = PomodoroEstado.PausaLonga;
            }
            else
            {
                Estado = PomodoroEstado.PausaCurta;
            }
        }
        else
        {
            Estado = PomodoroEstado.Foco;
        }

        ConfigurarEstado(Estado);
    }

    public void Tick()
    {
        if (!EstaExecutando) return;

        if (SegundosRestantes > 0)
        {
            SegundosRestantes--;
        }
        else
        {
            CicloConcluido?.Invoke(this, EventArgs.Empty);
            AvancarFase();
        }
    }

    public void AtualizarConfiguracao(WidgetConfig config)
    {
        _config = config;
        if (!EstaExecutando)
        {
            ConfigurarEstado(Estado);
        }
    }

    private void ConfigurarEstado(PomodoroEstado novoEstado)
    {
        Estado = novoEstado;
        int minutos = novoEstado switch
        {
            PomodoroEstado.Foco => _config.DuracaoFocoMinutos,
            PomodoroEstado.PausaCurta => _config.DuracaoPausaCurtaMinutos,
            PomodoroEstado.PausaLonga => _config.DuracaoPausaLongaMinutos,
            _ => 25
        };

        if (minutos <= 0) minutos = 1;

        _segundosTotais = minutos * 60;
        SegundosRestantes = _segundosTotais;
        EstadoMudou?.Invoke(this, EventArgs.Empty);
    }
}
