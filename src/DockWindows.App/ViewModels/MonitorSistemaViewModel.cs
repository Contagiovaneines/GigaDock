using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Input;
using DockWindows.App.Common;

namespace DockWindows.App.ViewModels;

public class MonitorSistemaViewModel : ObservableObject
{
    private double _usoCpu;
    private double _usoRam;
    private string _textoResumo = "CPU 0% | RAM 0%";
    private bool _painelAberto;
    private bool _habilitado;
    private DockWindows.Core.Models.FormatoWidget _formato = DockWindows.Core.Models.FormatoWidget.Expandido;
    private readonly DispatcherTimer _timer;
    private PerformanceCounter? _cpuCounter;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public DockWindows.Core.Models.FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }

    public double UsoCpu
    {
        get => _usoCpu;
        set
        {
            if (SetProperty(ref _usoCpu, value))
                OnPropertyChanged(nameof(TextoResumo));
        }
    }

    public double UsoRam
    {
        get => _usoRam;
        set
        {
            if (SetProperty(ref _usoRam, value))
                OnPropertyChanged(nameof(TextoResumo));
        }
    }

    public string TextoResumo => $"CPU {UsoCpu:F0}% | RAM {UsoRam:F0}%";

    public bool PainelAberto
    {
        get => _painelAberto;
        set => SetProperty(ref _painelAberto, value);
    }

    public ICommand AlternarPainelCommand { get; }

    public MonitorSistemaViewModel()
    {
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // Primeira leitura descartada (sempre retorna 0)
        }
        catch
        {
            _cpuCounter = null;
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => AtualizarMetricas();
        _timer.Start();

        // Leitura inicial de RAM
        AtualizarRam();
    }

    private void AtualizarMetricas()
    {
        try
        {
            if (_cpuCounter != null)
            {
                UsoCpu = Math.Round(_cpuCounter.NextValue(), 0);
            }
        }
        catch { UsoCpu = 0; }

        AtualizarRam();
    }

    private void AtualizarRam()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            long totalMemory = gcInfo.TotalAvailableMemoryBytes;
            
            // Usar Process para pegar uso real do sistema
            var proc = Process.GetCurrentProcess();
            long usedByProcess = proc.WorkingSet64;

            // Usar performance counter para RAM global
            using var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            double ramDisponivelMb = ramCounter.NextValue();
            double ramTotalGb = totalMemory / (1024.0 * 1024.0 * 1024.0);
            double ramTotalMb = totalMemory / (1024.0 * 1024.0);
            double ramUsadaMb = ramTotalMb - ramDisponivelMb;
            
            UsoRam = Math.Round((ramUsadaMb / ramTotalMb) * 100, 0);
        }
        catch
        {
            UsoRam = 0;
        }
    }
}



