using DockWindows.App.Common;

namespace DockWindows.Tests;

public class DittoTransformationTests
{
    [Fact]
    public void SomenteDittoTransformaComEsperaInicialEIntervaloSemRepetirForma()
    {
        var ability = new DittoTransformation(new Random(123));
        Assert.Equal(132, ability.TargetFor(132, 132, TimeSpan.Zero));
        Assert.Equal(132, ability.TargetFor(132, 132, TimeSpan.FromMinutes(29.99)));
        var first = ability.TargetFor(132, 132, TimeSpan.FromMinutes(30));
        Assert.InRange(first, 1, 151);
        Assert.NotEqual(132, first);
        Assert.Equal(first, ability.TargetFor(132, first, TimeSpan.FromMinutes(59.99)));
        Assert.Equal(132, ability.TargetFor(132, first, TimeSpan.FromMinutes(60)));
        Assert.Equal(132, ability.TargetFor(132, 132, TimeSpan.FromMinutes(89.99)));
        var second = ability.TargetFor(132, 132, TimeSpan.FromMinutes(90));
        Assert.NotEqual(first, second);
        Assert.NotEqual(132, second);
        for (var id = 1; id <= 151; id++)
            if (id != 132) Assert.Equal(id, ability.TargetFor(id, id, TimeSpan.FromDays(1)));
        ability.Reset();
        Assert.Equal(132, ability.TargetFor(132, second, TimeSpan.Zero));
    }

    [Fact]
    public void SorteioPodeAtingirTodasAsOutrasEspeciesDoCatalogo()
    {
        var ability = new DittoTransformation(new Random(456));
        var seen = new HashSet<int>();
        var displayed = 132;
        for (var i = 0; i < 10000; i++)
        {
            var target = ability.TargetFor(132, displayed, TimeSpan.FromMinutes((i + 1) * 30));
            Assert.NotEqual(displayed, target);
            if (i % 2 == 1) Assert.Equal(132, target);
            else { Assert.NotEqual(132, target); seen.Add(target); }
            displayed = target;
        }
        Assert.Equal(150, seen.Count);
        Assert.DoesNotContain(132, seen);
    }
}
