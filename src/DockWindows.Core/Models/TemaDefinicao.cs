namespace DockWindows.Core.Models;

public class TemaDefinicao
{
    public EstiloTema Estilo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string FundoDockColor { get; set; } = "#E6161618";
    public string BordaDockColor { get; set; } = "#300A84FF";
    public string TextoPrincipalColor { get; set; } = "#FFFFFF";
    public string TextoSecundarioColor { get; set; } = "#8E8E93";
    public string FundoCardColor { get; set; } = "#1E1E22";
    public string HighlightColor { get; set; } = "#0A84FF";
    public double RaioCantos { get; set; } = 20.0;
    public double OpacidadePadrao { get; set; } = 0.92;
    public string CorMiniatura { get; set; } = "#161618";
    public string CorBordaMiniatura { get; set; } = "#0A84FF";

    public static List<TemaDefinicao> ObterTemasPredefinidos() => new()
    {
        new TemaDefinicao
        {
            Estilo = EstiloTema.Discreto,
            Nome = "Discreto",
            Descricao = "Visual neutro e minimalista com tons suaves de ardósia e transparência equilibrada.",
            FundoDockColor = "#D922242B",
            BordaDockColor = "#25FFFFFF",
            TextoPrincipalColor = "#F5F5F7",
            TextoSecundarioColor = "#9A9AA0",
            FundoCardColor = "#22242B",
            HighlightColor = "#7E8B9B",
            RaioCantos = 16.0,
            OpacidadePadrao = 0.88,
            CorMiniatura = "#2A2D37",
            CorBordaMiniatura = "#5A6275"
        },
        new TemaDefinicao
        {
            Estilo = EstiloTema.Escuro,
            Nome = "Escuro",
            Descricao = "Estilo clássico com contraste nítido, fundo grafite profundo e acentos em azul Windows 11.",
            FundoDockColor = "#E6161618",
            BordaDockColor = "#350A84FF",
            TextoPrincipalColor = "#FFFFFF",
            TextoSecundarioColor = "#8E8E93",
            FundoCardColor = "#1E1E22",
            HighlightColor = "#0A84FF",
            RaioCantos = 20.0,
            OpacidadePadrao = 0.92,
            CorMiniatura = "#18181A",
            CorBordaMiniatura = "#0A84FF"
        },
        new TemaDefinicao
        {
            Estilo = EstiloTema.Colorido,
            Nome = "Colorido",
            Descricao = "Atmosfera vívida e moderna com realce em degradê ciano e magenta sobre fundo violeta escuro.",
            FundoDockColor = "#E01C182A",
            BordaDockColor = "#60A855F7",
            TextoPrincipalColor = "#FFFFFF",
            TextoSecundarioColor = "#B8B5CE",
            FundoCardColor = "#26203A",
            HighlightColor = "#A855F7",
            RaioCantos = 22.0,
            OpacidadePadrao = 0.90,
            CorMiniatura = "#261F3B",
            CorBordaMiniatura = "#A855F7"
        },
        new TemaDefinicao
        {
            Estilo = EstiloTema.ComBrilho,
            Nome = "Com Brilho",
            Descricao = "Acabamento vítreo translúcido com borda iluminada por reflexo especular e realce celeste.",
            FundoDockColor = "#C01B2434",
            BordaDockColor = "#90FFFFFF",
            TextoPrincipalColor = "#FFFFFF",
            TextoSecundarioColor = "#D0DCEB",
            FundoCardColor = "#202E42",
            HighlightColor = "#38BDF8",
            RaioCantos = 24.0,
            OpacidadePadrao = 0.82,
            CorMiniatura = "#1B2A3D",
            CorBordaMiniatura = "#38BDF8"
        },
        new TemaDefinicao
        {
            Estilo = EstiloTema.VidroLiquido,
            Nome = "Vidro Líquido",
            Descricao = "Inspirado no Apple Liquid Glass: transparência fluida, bordas especulares de alta refração e brilho vítreo.",
            FundoDockColor = "#59101626",
            BordaDockColor = "#B0FFFFFF",
            TextoPrincipalColor = "#FFFFFF",
            TextoSecundarioColor = "#D1D5DB",
            FundoCardColor = "#40182236",
            HighlightColor = "#38BDF8",
            RaioCantos = 24.0,
            OpacidadePadrao = 0.85,
            CorMiniatura = "#111B2B",
            CorBordaMiniatura = "#7DD3FC"
        },
        new TemaDefinicao
        {
            Estilo = EstiloTema.Areia,
            Nome = "Areia",
            Descricao = "Base clara em areia, facetas suaves, ícones em peças brancas e controles em grafite.",
            FundoDockColor = "#FFF2E9DC",
            BordaDockColor = "#D6C9B7",
            TextoPrincipalColor = "#30343B",
            TextoSecundarioColor = "#62646A",
            FundoCardColor = "#292B32",
            HighlightColor = "#176AC6",
            RaioCantos = 22,
            OpacidadePadrao = 1,
            CorMiniatura = "#F2E9DC",
            CorBordaMiniatura = "#CDBEA9"
        }
    };

    public static TemaDefinicao ObterPorEstilo(EstiloTema estilo)
    {
        var temas = ObterTemasPredefinidos();
        return temas.FirstOrDefault(t => t.Estilo == estilo) ?? temas[1];
    }
}
