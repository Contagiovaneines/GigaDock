namespace DockWindows.Core.Models;

public enum DecoracaoDock { Nenhuma, Outubro, Natal }

public static class DecoracoesDock
{
    public static string[] Cores(DecoracaoDock decoracao) => decoracao switch
    {
        DecoracaoDock.Outubro => ["#F29B38", "#B69ADB", "#F29B38", "#D2C4ED"],
        DecoracaoDock.Natal => ["#F16D75", "#7DCF9C", "#EDD788", "#90BCEE"],
        _ => []
    };
}
