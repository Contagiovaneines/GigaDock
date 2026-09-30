namespace DockWindows.Core.Models;

public class AtalhoConfig
{
    public bool Control { get; set; } = true;
    public bool Alt { get; set; } = true;
    public bool Shift { get; set; } = false;
    public bool Windows { get; set; } = false;
    public string Tecla { get; set; } = "D";

    public override string ToString()
    {
        var partes = new List<string>();
        if (Control) partes.Add("Ctrl");
        if (Alt) partes.Add("Alt");
        if (Shift) partes.Add("Shift");
        if (Windows) partes.Add("Win");
        partes.Add(Tecla);
        return string.Join(" + ", partes);
    }
}
