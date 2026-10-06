using System.IO;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;
using DockWindows.Installer.Services;
using Xunit;

namespace DockWindows.Tests;

public class CustomizationAndInstallerTests
{
    [Fact]
    public void Preferencias_ModoSubstituicao_DesativadoPorPadrao()
    {
        var prefs = new Preferencias();
        Assert.False(prefs.UsarComoBarraPrincipal, "O modo de substituição não deve vir ativado por padrão.");
        Assert.Null(prefs.EstadoAnteriorBarraTarefas);
    }

    [Fact]
    public void Preferencias_SecoesVisiveis_PadraoVerdadeiro()
    {
        var prefs = new Preferencias();
        Assert.True(prefs.ExibirSeletorAmbientes);
        Assert.True(prefs.ExibirItensFixados);
        Assert.False(prefs.ExibirBotoesAcao);
        Assert.False(prefs.DesativarAnimacoes);
        Assert.Equal(6, prefs.EspacamentoItens);
    }

    [Fact]
    public void Preferencias_Clonar_GeraCopiaProfundaSemCompartilharReferencias()
    {
        var original = new Preferencias
        {
            Tema = TemaModo.Claro,
            UsarComoBarraPrincipal = false,
            EspacamentoItens = 8,
            Ambientes = new List<Ambiente>
            {
                new Ambiente
                {
                    Id = "amb-1",
                    Nome = "Original",
                    CorHex = "#0078D4",
                    Itens = new List<ItemFixado>
                    {
                        new ItemFixado { Titulo = "Item 1", CaminhoOuUrl = "calc.exe" }
                    },
                    Widgets = new WidgetConfig { RelogioHabilitado = true }
                }
            }
        };

        var clone = original.Clonar();

        Assert.NotNull(clone);
        Assert.Equal(original.Tema, clone.Tema);
        Assert.Equal(original.EspacamentoItens, clone.EspacamentoItens);
        Assert.Single(clone.Ambientes);

        // Modifica clone
        clone.Ambientes[0].Nome = "Clone Modificado";
        clone.Ambientes[0].Itens.Clear();

        // O original permanece intacto
        Assert.Equal("Original", original.Ambientes[0].Nome);
        Assert.Single(original.Ambientes[0].Itens);
    }

    [Fact]
    public void Preferencias_RestaurarAmbientePadrao_RestauraAmbientePredefinido()
    {
        var prefs = Preferencias.CriarPadrao();
        var trabalho = prefs.Ambientes.First(a => a.Nome == "Trabalho");

        // Altera propriedades do trabalho
        trabalho.CorHex = "#FFFFFF";
        trabalho.Widgets.RelogioHabilitado = false;
        trabalho.Itens.Clear();

        // Restaura
        prefs.RestaurarAmbientePadrao("ambiente-trabalho");

        var trabalhoRestaurado = prefs.Ambientes.First(a => a.Id == "ambiente-trabalho");
        Assert.Equal("#0078D4", trabalhoRestaurado.CorHex);
        Assert.Equal(Preferencias.CriarAmbienteTrabalhoPadrao().Widgets.RelogioHabilitado, trabalhoRestaurado.Widgets.RelogioHabilitado);
        Assert.NotEmpty(trabalhoRestaurado.Itens);
    }

    [Fact]
    [Trait("Category", "SystemIntegration")]
    public void TaskbarService_RestaurarBarraNativa_IdempotenteENaoFalha()
    {
        var service = new Win32TaskbarService();
        // Não deve lançar exceção em qualquer ambiente (mesmo sem monitor real / CI)
        var exception = Record.Exception(() => service.RestaurarBarraNativa(0));
        Assert.Null(exception);
    }

    [Fact]
    public void InstallService_ObterDiretorios_CaminhosSaoValidosEIsolados()
    {
        var service = new InstallService();
        var dirInstalacao = service.ObterDiretorioInstalacaoPadrao();
        var dirDados = service.ObterDiretorioDadosUsuario();

        Assert.False(string.IsNullOrWhiteSpace(dirInstalacao));
        Assert.False(string.IsNullOrWhiteSpace(dirDados));
        Assert.Contains("Programs", dirInstalacao);
        Assert.Contains("DockWindows", dirDados);
    }

    [Fact]
    public void ShortcutService_RemoverAtalho_InexistenteNaoLancaExcecao()
    {
        var caminhoFicticio = Path.Combine(Path.GetTempPath(), "atalho_inexistente_" + Guid.NewGuid().ToString("N") + ".lnk");
        var exception = Record.Exception(() => ShortcutService.RemoverAtalho(caminhoFicticio));
        Assert.Null(exception);
    }
}

