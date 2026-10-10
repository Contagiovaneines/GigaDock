using DockWindows.Core.Widgets;

namespace GigaDock.Tests.Core;

public sealed class PokemonDockMotionTests
{
    [Fact]
    public void CaminhaDezoitoPixelsPorSegundo()
    {
        var motion = new PokemonDockMotion();
        for (var i = 0; i < 10; i++) motion.Advance(400, 44, 100, true);
        Assert.Equal(34, motion.Position, 6);
        Assert.Equal(18, motion.Distance, 6);
    }
    [Fact]
    public void BordaViraDeFrenteAntesDeVoltar()
    {
        var motion = new PokemonDockMotion();
        for (var i = 0; i < 8; i++) motion.Advance(90, 44, 100, true);
        Assert.False(motion.FacingRight);
        Assert.True(motion.FacingFront);
        var edge = motion.Position;
        motion.Advance(90, 44, 100, true);
        Assert.Equal(edge, motion.Position);
        for (var i = 0; i < 4; i++) motion.Advance(90, 44, 100, true);
        Assert.True(motion.Position < edge);
    }
    [Fact]
    public void ReducaoDeAnimacoesENovaLarguraNaoSaemDaDock()
    {
        var motion = new PokemonDockMotion();
        motion.Advance(500, 44, 100, false);
        Assert.Equal(16, motion.Position);
        motion.Advance(30, 44, 100, true);
        Assert.Equal(0, motion.Position);
        Assert.Equal(0, motion.Distance);
    }
    [Fact]
    public void PausaFrontalNoMeioDoPercurso()
    {
        var motion = new PokemonDockMotion(() => 0);
        for (var i = 0; i < 4; i++) motion.Advance(90, 44, 100, true);
        Assert.True(motion.FacingFront);
        var middle = motion.Position;
        motion.Advance(90, 44, 100, true);
        Assert.Equal(middle, motion.Position);
    }
}
