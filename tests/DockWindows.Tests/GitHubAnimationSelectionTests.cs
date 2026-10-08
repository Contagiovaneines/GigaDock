using DockWindows.App.ViewModels;
using DockWindows.Core.Widgets;

namespace DockWindows.Tests;

public class GitHubAnimationSelectionTests
{
    [Theory]
    [InlineData("grade-compacta")]
    [InlineData("resumo-anual")]
    public void PacManAlternaBocaOrientaMovimentoEMostraQuatroFantasmas(string estilo)
    {
        using var vm = new GitHubWidgetViewModel { Estilo = estilo };
        foreach (var i in Enumerable.Range(0, vm.ColunasAnimacao * 7))
            vm.Contribuicoes.Add(new ContribuicaoDia { Nivel = 4 });
        vm.DefinirAtividade(new EstadoAtividade(true, true, false, true));
        vm.SelecionarAnimacao("PacMan");
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var tick = typeof(GitHubWidgetViewModel).GetMethod("TickAnimacao", flags)!;
        var mouths = new HashSet<bool>();
        var previous = 0;
        for (var frame = 0; frame < 12; frame++)
        {
            tick.Invoke(vm, null);
            var pac = Assert.Single(vm.Contribuicoes, d => d.EhPacMan);
            var index = vm.Contribuicoes.IndexOf(pac);
            var dx = index % vm.ColunasAnimacao - previous % vm.ColunasAnimacao;
            var dy = index / vm.ColunasAnimacao - previous / vm.ColunasAnimacao;
            Assert.Equal(dx > 0 ? 0 : dy > 0 ? 1 : dx < 0 ? 2 : 3, pac.DirecaoPacMan);
            mouths.Add(pac.BocaPacManAberta);
            Assert.Equal(new[] { 0, 1, 2, 3 }, vm.Contribuicoes.Where(d => d.EhFantasma).Select(d => d.CorFantasma).OrderBy(i => i));
            previous = index;
        }
        Assert.Equal(2, mouths.Count);
    }

    public static IEnumerable<object[]> GradesEAnimacoes =>
        from estilo in new[] { "grade-compacta", "resumo-anual" }
        from animacao in Enum.GetNames<EstiloAnimacaoGitHub>()
        select new object[] { estilo, animacao };

    [Theory]
    [MemberData(nameof(GradesEAnimacoes))]
    public void TodasAnimacoesUsamGradeDoEstiloERestauramDados(string estilo, string animacao)
    {
        using var vm = new GitHubWidgetViewModel { Estilo = estilo };
        var niveis = Enumerable.Range(0, vm.ColunasAnimacao * 7).Select(i => i % 5).ToList();
        foreach (var nivel in niveis) vm.Contribuicoes.Add(new ContribuicaoDia { Nivel = nivel });
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(GitHubWidgetViewModel).GetField("_niveisOriginais", flags)!.SetValue(vm, niveis);
        vm.DefinirAtividade(new EstadoAtividade(true, true, false, true));
        vm.SelecionarAnimacao(animacao);
        Assert.True(vm.AnimacaoAtiva);
        var tick = typeof(GitHubWidgetViewModel).GetMethod("TickAnimacao", flags)!;
        var houveQuadro = false;
        for (var i = 0; i < 80; i++)
        {
            tick.Invoke(vm, null);
            houveQuadro |= vm.Contribuicoes.Any(d => d.EhCobra || d.EhCabecaCobra || d.EhPacMan || d.EhFantasma || d.MarcaArcade != 0);
        }
        Assert.True(houveQuadro);
        vm.SelecionarAnimacao("Parado");
        Assert.False(vm.AnimacaoAtiva);
        Assert.Equal(niveis, vm.Contribuicoes.Select(d => d.Nivel));
        Assert.All(vm.Contribuicoes, d =>
        {
            Assert.False(d.EhCobra || d.EhCabecaCobra || d.EhPacMan || d.EhFantasma);
            Assert.Equal(0, d.MarcaArcade);
        });
    }

    [Theory]
    [InlineData("PacMan")]
    [InlineData("Breakout")]
    [InlineData("Galaga")]
    [InlineData("PuzzleBobble")]
    [InlineData("Bomberman")]
    [InlineData("Minesweeper")]
    public void MenuSelecionaEstiloSemExecutarOculto(string estilo)
    {
        using var vm = new GitHubWidgetViewModel();
        vm.DefinirAnimacaoCommand.Execute(estilo);
        Assert.Equal(estilo, vm.AnimacaoSelecionada);
        Assert.True(vm.AnimacaoAutomatica);
        Assert.False(vm.AnimacaoAtiva);
        Assert.Equal(0, vm.Requisicoes);
    }

    [Fact]
    public void DesativarEEntradaInvalidaPreservamEstado()
    {
        using var vm = new GitHubWidgetViewModel();
        vm.SelecionarAnimacao("Breakout");
        vm.SelecionarAnimacao("999");
        Assert.Equal("Breakout", vm.AnimacaoSelecionada);
        vm.DefinirAnimacaoCommand.Execute("Parado");
        Assert.Equal("Parado", vm.AnimacaoSelecionada);
        Assert.False(vm.AnimacaoAutomatica);
        Assert.False(vm.AnimacaoAtiva);
    }

    [Theory]
    [InlineData("Breakout")]
    [InlineData("Galaga")]
    [InlineData("PuzzleBobble")]
    [InlineData("Bomberman")]
    [InlineData("Minesweeper")]
    public void OcultarRestauraGradeEParaAnimacao(string estilo)
    {
        using var vm = new GitHubWidgetViewModel();
        var niveis = Enumerable.Range(0, 91).Select(i => i % 5).ToList();
        foreach (int nivel in niveis) vm.Contribuicoes.Add(new ContribuicaoDia { Nivel = nivel });
        typeof(GitHubWidgetViewModel).GetField("_niveisOriginais", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(vm, niveis);
        vm.DefinirAtividade(new EstadoAtividade(true, true, false, true));
        vm.SelecionarAnimacao(estilo);
        Assert.True(vm.AnimacaoAtiva);
        var tick = typeof(GitHubWidgetViewModel).GetMethod("TickAnimacao", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        for (int i = 0; i < 50; i++) tick.Invoke(vm, null);
        Assert.Contains(vm.Contribuicoes, d => d.MarcaArcade != 0);
        vm.DefinirAtividade(new EstadoAtividade(true, false, false, false));
        Assert.False(vm.AnimacaoAtiva);
        Assert.Equal(niveis, vm.Contribuicoes.Select(d => d.Nivel));
        Assert.All(vm.Contribuicoes, d => Assert.Equal(0, d.MarcaArcade));
    }
}
