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
    private int _restantes;
    private DateTimeOffset? _fim;
    private readonly TimeProvider _relogio;

    public event EventHandler? CicloConcluido;
    public event EventHandler? EstadoMudou;

    public PomodoroEngine(WidgetConfig? config = null, TimeProvider? relogio = null)
    {
        _relogio = relogio ?? TimeProvider.System;
        _config = config ?? new WidgetConfig();
        ConfigurarEstado(PomodoroEstado.Foco);
    }

    public PomodoroEstado Estado { get; private set; } = PomodoroEstado.Foco;
    public int SegundosRestantes { get => _fim.HasValue ? Math.Max(0, (int)Math.Ceiling((_fim.Value - _relogio.GetUtcNow()).TotalSeconds)) : _restantes; private set => _restantes = value; }
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

    public void Iniciar() { if (EstaExecutando) return; _fim = _relogio.GetUtcNow().AddSeconds(_restantes); EstaExecutando = true; }
    public void Pausar() { _restantes = SegundosRestantes; _fim = null; EstaExecutando = false; }

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
            if (CiclosConcluidos % Math.Max(1, _config.CiclosAtePausaLonga) == 0)
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

        if (SegundosRestantes == 0)
        {
            // Fase termina uma vez; não simula ciclos que não foram iniciados.
            AvancarFase();
            CicloConcluido?.Invoke(this, EventArgs.Empty);
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
