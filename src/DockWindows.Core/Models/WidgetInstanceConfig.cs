namespace DockWindows.Core.Models;

public class WidgetInstanceConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public TipoWidget Tipo { get; set; } = TipoWidget.Relogio;
    public string Nome { get; set; } = "Relógio";
    public FormatoWidget Formato { get; set; } = FormatoWidget.Compacto;
    public string Estilo { get; set; } = "";
    public bool Visivel { get; set; } = true;
    public string EstadoTexto => Visivel ? "Ativo" : "Instalado";
    public string EstadoDescricao => Visivel ? "Exibido na dock deste ambiente" : "Instalado, mas oculto da dock";
    public string EstadoCor => Visivel ? "#72D99C" : "#A9B8C9";
    public int Ordem { get; set; }
    public int VersaoConfiguracao { get; set; } = 1;
    public Dictionary<string, string> Configuracao { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string ObterConfiguracao(string chave, string valorPadrao = "")
    {
        if (string.IsNullOrWhiteSpace(chave)) return valorPadrao;
        Configuracao ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return Configuracao.TryGetValue(chave, out var valor) ? valor : valorPadrao;
    }

    public void DefinirConfiguracao(string chave, string? valor)
    {
        if (string.IsNullOrWhiteSpace(chave)) return;
        Configuracao ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(valor)) Configuracao.Remove(chave);
        else Configuracao[chave] = valor.Trim();
    }
}
