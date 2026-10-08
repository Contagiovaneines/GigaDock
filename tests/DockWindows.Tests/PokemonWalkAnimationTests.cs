using DockWindows.App.Common;

namespace DockWindows.Tests;

public class PokemonWalkAnimationTests
{
    [Fact]
    public void CatalogoTemPassosLateraisValidosComCicloPorDistancia()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                for (var id = 1; id <= 151; id++)
                {
                    var walk = PokemonWalkAnimation.Load(id);
                    Assert.True(walk != null, $"Walk ausente ou inválido para #{id:000}");
                    Assert.NotEmpty(walk.Left);
                    Assert.Equal(walk.Left.Count, walk.Right.Count);
                    Assert.Equal(walk.Left.Count, walk.Front.Count);
                    var first = walk.Left[0].Image;
                    foreach (var frame in walk.Left.Concat(walk.Right).Concat(walk.Front))
                    {
                        Assert.True(frame.Image.IsFrozen);
                        Assert.True(frame.DelayMilliseconds > 0);
                        Assert.Equal(first.PixelWidth, frame.Image.PixelWidth);
                        Assert.Equal(first.PixelHeight, frame.Image.PixelHeight);
                    }
                    Assert.Same(walk.Left[0].Image, walk.FrameAt(0, false));
                    Assert.Same(walk.Right[0].Image, walk.FrameAt(0, true));
                    Assert.Same(walk.FrameAt(3, true), walk.FrameAt(3 + PokemonWalkAnimation.DistancePerCycle, true));
                    Assert.Equal(PokemonLocomotion.KindFor(id), walk.MovementKind);
                    Assert.Same(walk.Right[0].Image, walk.FrameAtTime(walk.Right.Sum(f => f.DelayMilliseconds), true));
                    foreach (var state in new[] { "Rest", "Sleep" })
                    {
                        var pose = PokemonWalkAnimation.Load(id, state);
                        Assert.True(pose != null, $"{state} ausente para #{id:000}");
                        Assert.NotEmpty(pose.Front);
                        Assert.All(pose.Front, f => Assert.True(f.Image.IsFrozen && f.DelayMilliseconds > 0));
                        Assert.Same(pose.Front[0].Image, pose.FrontAtTime(0));
                        Assert.Same(pose.Front[0].Image, pose.FrontAtTime(pose.Front.Sum(f => f.DelayMilliseconds)));
                        Assert.Equal(state == "Sleep" ? "Sleep" : id == 25 ? "Sit" : pose.AnimationName, pose.AnimationName);
                    }
                }
                Assert.Null(PokemonWalkAnimation.Load(0));
                Assert.Null(PokemonWalkAnimation.Load(152));
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    [Theory]
    [InlineData(6, PokemonMovementKind.Flying)]
    [InlineData(12, PokemonMovementKind.Flying)]
    [InlineData(16, PokemonMovementKind.Flying)]
    [InlineData(142, PokemonMovementKind.Flying)]
    [InlineData(149, PokemonMovementKind.Flying)]
    [InlineData(81, PokemonMovementKind.Floating)]
    [InlineData(92, PokemonMovementKind.Floating)]
    [InlineData(151, PokemonMovementKind.Floating)]
    [InlineData(25, PokemonMovementKind.Ground)]
    [InlineData(84, PokemonMovementKind.Ground)]
    [InlineData(85, PokemonMovementKind.Ground)]
    [InlineData(94, PokemonMovementKind.Ground)]
    public void AlturaRespeitaEspecieSonoEPreferenciaDeAnimacao(int id, PokemonMovementKind expected)
    {
        var kind = PokemonLocomotion.KindFor(id);
        Assert.Equal(expected, kind);
        Assert.Equal(0, PokemonLocomotion.Altitude(kind, 600, sleeping: true, animate: true));
        if (kind == PokemonMovementKind.Ground)
            Assert.Equal(0, PokemonLocomotion.Altitude(kind, 600, sleeping: false, animate: true));
        else
        {
            var stationary = PokemonLocomotion.Altitude(kind, 0, sleeping: false, animate: false);
            Assert.True(stationary > 0);
            Assert.Equal(stationary, PokemonLocomotion.Altitude(kind, 600, sleeping: false, animate: false));
            Assert.InRange(PokemonLocomotion.Altitude(kind, 600, sleeping: false, animate: true), stationary - 2, stationary + 2);
            Assert.NotEqual(stationary, PokemonLocomotion.Altitude(kind, 600, sleeping: false, animate: true));
        }
    }
}
