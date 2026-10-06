using DockWindows.App.ViewModels;
using DockWindows.Core.Widgets;

namespace DockWindows.Tests;

public class GitHubAnimationSelectionTests
{
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
