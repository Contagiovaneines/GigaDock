using System.Runtime.InteropServices;

namespace DockWindows.Infrastructure.Windows;

public sealed record DiagnosticoSistemaInfo(
    string Windows,
    string Arquitetura,
    string MemoriaInstalada,
    string TempoLigado);

public static class DiagnosticoSistemaService
{
    public static DiagnosticoSistemaInfo Obter()
    {
        var versao = Environment.OSVersion.Version;
        var windows = $"Windows {versao.Major}.{versao.Minor} (build {versao.Build})";
        var arquitetura = RuntimeInformation.OSArchitecture.ToString();
        var memoria = "Não disponível";

        try
        {
            var estado = new EstadoMemoria { Tamanho = (uint)Marshal.SizeOf<EstadoMemoria>() };
            if (GlobalMemoryStatusEx(ref estado))
                memoria = $"{estado.TotalFisico / (1024d * 1024 * 1024):F1} GB";
        }
        catch { }

        var ligado = TimeSpan.FromMilliseconds(GetTickCount64());
        var tempoLigado = ligado.TotalDays >= 1
            ? $"{(int)ligado.TotalDays} d {ligado.Hours} h"
            : $"{ligado.Hours} h {ligado.Minutes} min";

        return new(windows, arquitetura, memoria, tempoLigado);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EstadoMemoria
    {
        public uint Tamanho, Carga;
        public ulong TotalFisico, DisponivelFisico, TotalPagina, DisponivelPagina;
        public ulong TotalVirtual, DisponivelVirtual, DisponivelVirtualEstendida;
    }

    [DllImport("kernel32.dll")]
    private static extern ulong GetTickCount64();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref EstadoMemoria estado);
}
