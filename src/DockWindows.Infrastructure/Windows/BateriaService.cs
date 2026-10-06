using System.Runtime.InteropServices;

namespace DockWindows.Infrastructure.Windows;

public enum EstadoCargaBateria { Carregando, Descarregando, Completa, SemBateria, Desconhecida, Indisponivel }
public record StatusBateria(bool? PossuiBateria, int? Porcentagem, bool Carregando, bool? NaTomada, bool LeituraDisponivel = true)
{
    public EstadoCargaBateria Estado => !LeituraDisponivel ? EstadoCargaBateria.Indisponivel
        : PossuiBateria == false ? EstadoCargaBateria.SemBateria
        : PossuiBateria == null ? EstadoCargaBateria.Desconhecida
        : Porcentagem == null ? EstadoCargaBateria.Desconhecida
        : Carregando ? EstadoCargaBateria.Carregando
        : Porcentagem == 100 ? EstadoCargaBateria.Completa
        : NaTomada == false ? EstadoCargaBateria.Descarregando : EstadoCargaBateria.Desconhecida;
}

public interface IBateriaService { StatusBateria ObterStatus(); }

public sealed class BateriaService : IBateriaService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag;
        public uint BatteryLifeTime, BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    public StatusBateria ObterStatus() => GetSystemPowerStatus(out var status)
        ? Interpretar(status.BatteryFlag, status.BatteryLifePercent, status.ACLineStatus)
        : new(null, null, false, null, false);

    public static StatusBateria Interpretar(byte flags, byte porcentagem, byte tomada)
    {
        bool? possui = flags == 255 ? null : (flags & 128) == 0;
        int? carga = possui != false && porcentagem <= 100 ? porcentagem : null;
        bool carregando = possui == true && (flags & 8) != 0;
        bool? naTomada = tomada == 255 ? null : tomada == 1;
        return new(possui, carga, carregando, naTomada);
    }
}
