using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace DockWindows.Infrastructure.Windows;

[Flags]
public enum MetricasSolicitadas { Cpu = 1, Ram = 2, Rede = 4, Disco = 8, Todas = 15 }
public enum EstadoAmostra { NaoSolicitada, PrimeiraAmostra, Disponivel, Indisponivel }
public record MetricasSistema(double? Cpu, double? Ram, double? Download, double? Upload, double? DiscoUsado, string DiscoLivre)
{
    public EstadoAmostra EstadoCpu { get; init; } = Cpu.HasValue ? EstadoAmostra.Disponivel : EstadoAmostra.Indisponivel;
    public EstadoAmostra EstadoRede { get; init; } = Download.HasValue ? EstadoAmostra.Disponivel : EstadoAmostra.Indisponivel;
}
public interface IMetricasSistemaService : IDisposable { MetricasSistema Ler(); MetricasSistema Ler(MetricasSolicitadas solicitadas) => Ler(); void Suspender() { } }

public sealed class MetricasSistemaService : IMetricasSistemaService
{
    private PerformanceCounter? _cpu;
    private readonly Dictionary<string, (long Recebidos, long Enviados)> _rede = new();
    private long _instante;
    private bool _cpuInicializada;
    private bool _disposed;
    private DateTimeOffset _tentarCpuApos;
    public void Suspender() { _cpu?.Dispose(); _cpu = null; _cpuInicializada = false; _rede.Clear(); _instante = 0; _tentarCpuApos = default; }
    public MetricasSistema Ler() => Ler(MetricasSolicitadas.Todas);
    public MetricasSistema Ler(MetricasSolicitadas solicitadas)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        double? cpu = null, ram = null, down = null, up = null, disco = null;
        bool primeiraCpu = false, primeiraRede = false;
        string livre = "—";
        if (solicitadas.HasFlag(MetricasSolicitadas.Cpu) && DateTimeOffset.UtcNow >= _tentarCpuApos) try
        {
            if (!_cpuInicializada) { _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpu.NextValue(); _cpuInicializada = true; primeiraCpu = true; }
            else if (_cpu != null) cpu = Math.Clamp(_cpu.NextValue(), 0, 100);
        }
        catch { _cpu?.Dispose(); _cpu = null; _cpuInicializada = false; _tentarCpuApos = DateTimeOffset.UtcNow.AddSeconds(30); }
        if (solicitadas.HasFlag(MetricasSolicitadas.Ram)) try
        {
            var mem = new Memoria { Tamanho = (uint)Marshal.SizeOf<Memoria>() };
            if (GlobalMemoryStatusEx(ref mem)) ram = mem.Carga;
        }
        catch { }
        if (solicitadas.HasFlag(MetricasSolicitadas.Rede)) try
        {
            var instante = Stopwatch.GetTimestamp();
            var segundos = _instante == 0 ? 0 : Stopwatch.GetElapsedTime(_instante, instante).TotalSeconds;
            var atuais = new Dictionary<string, (long Recebidos, long Enviados)>();
            double recebidos = 0, enviados = 0;
            bool comparavel = false;
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                var stat = nic.GetIPStatistics();
                atuais[nic.Id] = (stat.BytesReceived, stat.BytesSent);
                if (_rede.TryGetValue(nic.Id, out var anterior) && segundos > 0)
                {
                    recebidos += Taxa(anterior.Recebidos, stat.BytesReceived, segundos);
                    enviados += Taxa(anterior.Enviados, stat.BytesSent, segundos);
                    comparavel = true;
                }
            }
            if (comparavel) { down = recebidos; up = enviados; }
            else primeiraRede = atuais.Count > 0;
            _rede.Clear(); foreach (var par in atuais) _rede.Add(par.Key, par.Value);
            _instante = instante;
        }
        catch { _rede.Clear(); _instante = 0; }
        if (solicitadas.HasFlag(MetricasSolicitadas.Disco)) try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
            if (drive.IsReady && drive.TotalSize > 0)
            {
                disco = 100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize;
                livre = $"{drive.AvailableFreeSpace / (1024.0 * 1024 * 1024):F0} GB livres";
            }
        }
        catch { }
        return new(cpu, ram, down, up, disco, livre)
        {
            EstadoCpu = !solicitadas.HasFlag(MetricasSolicitadas.Cpu) ? EstadoAmostra.NaoSolicitada
                : cpu.HasValue ? EstadoAmostra.Disponivel : primeiraCpu ? EstadoAmostra.PrimeiraAmostra : EstadoAmostra.Indisponivel,
            EstadoRede = !solicitadas.HasFlag(MetricasSolicitadas.Rede) ? EstadoAmostra.NaoSolicitada
                : down.HasValue ? EstadoAmostra.Disponivel : primeiraRede ? EstadoAmostra.PrimeiraAmostra : EstadoAmostra.Indisponivel
        };
    }
    public static double Taxa(long anterior, long atual, double segundos) => segundos <= 0 || atual < anterior ? 0 : (atual - anterior) / segundos;
    public void Dispose() { Suspender(); _disposed = true; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Memoria { public uint Tamanho, Carga; public ulong TotalFisico, DisponivelFisico, TotalPagina, DisponivelPagina, TotalVirtual, DisponivelVirtual, DisponivelVirtualEstendida; }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref Memoria memoria);
}
