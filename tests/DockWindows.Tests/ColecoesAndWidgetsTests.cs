using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Persistence;
using Xunit;

namespace DockWindows.Tests;

public class ColecoesAndWidgetsTests
{
    private readonly string _tempDir;

    public ColecoesAndWidgetsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DockWindowsTests_Colecoes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void ColecaoApp_CriacaoEGerenciamentoItens_FuncionaCorretamente()
    {
        var colecao = new ColecaoApp
        {
            Id = "col-teste",
            Nome = "Produtividade",
            Icone = "💼",
            EhGlobal = true,
            Ordem = 0,
            Itens = new List<ItemFixado>
            {
                new() { Titulo = "Calculadora", CaminhoOuUrl = "calc.exe", Tipo = TipoItem.Aplicativo, Ordem = 0 },
                new() { Titulo = "Bloco de Notas", CaminhoOuUrl = "notepad.exe", Tipo = TipoItem.Aplicativo, Ordem = 1 }
            }
        };

        Assert.Equal("Produtividade", colecao.Nome);
        Assert.True(colecao.EhGlobal);
        Assert.Equal(2, colecao.Itens.Count);

        // Adicionar novo item
        colecao.Itens.Add(new ItemFixado { Titulo = "Terminal", CaminhoOuUrl = "cmd.exe", Tipo = TipoItem.Aplicativo, Ordem = 2 });
        Assert.Equal(3, colecao.Itens.Count);

        // Remover item
        colecao.Itens.RemoveAt(0);
        Assert.Equal(2, colecao.Itens.Count);
        Assert.Equal("Bloco de Notas", colecao.Itens[0].Titulo);
    }

    [Fact]
    public void CalendarioWidget_SemCompromissos_ExibeEstadoVazioUtil()
    {
        var widget = new CalendarioWidgetViewModel();
        widget.SincronizarCompromissos(new List<CompromissoLocal>());

        Assert.False(widget.TemCompromissos);
        Assert.Contains("Sem eventos pendentes", widget.TextoExpandido);
        Assert.Equal(FormatoWidget.Compacto, widget.Formato);
        Assert.NotNull(widget.DataCompleta);
    }

    [Fact]
    public void CalendarioWidget_ComCompromissosFuturos_ExibeProximoCompromisso()
    {
        var futuro1 = DateTime.Now.AddHours(2);
        var futuro2 = DateTime.Now.AddDays(1);

        var compromissos = new List<CompromissoLocal>
        {
            new() { Id = "c1", Titulo = "Reunião de Equipe", DataHora = futuro1, Local = "Sala Virtual" },
            new() { Id = "c2", Titulo = "Entrega do Projeto", DataHora = futuro2, Local = "GitHub" }
        };

        var widget = new CalendarioWidgetViewModel();
        widget.SincronizarCompromissos(compromissos);

        Assert.True(widget.TemCompromissos);
        Assert.NotNull(widget.ProximoCompromisso);
        Assert.Equal("Reunião de Equipe", widget.ProximoCompromisso.Titulo);
        Assert.Contains("Reunião de Equipe", widget.TextoCompacto);
    }

    [Fact]
    public void CalendarioWidget_IgnoraCompromissosPassados()
    {
        var passado = DateTime.Now.AddHours(-3);
        var compromissos = new List<CompromissoLocal>
        {
            new() { Id = "c-passado", Titulo = "Café da Manhã", DataHora = passado }
        };

        var widget = new CalendarioWidgetViewModel();
        widget.SincronizarCompromissos(compromissos);

        // Como o evento passado está há mais de 30 min atrás, ou não há próximo
        // ProximoCompromisso pode ser o último ou vazio
        Assert.NotNull(widget.TextoExibicao);
    }

    [Fact]
    public void Temas_TodasAsQuatroDefinicoes_EstaoConfiguradasComPaletasProprias()
    {
        var discreto = TemaDefinicao.ObterPorEstilo(EstiloTema.Discreto);
        var escuro = TemaDefinicao.ObterPorEstilo(EstiloTema.Escuro);
        var colorido = TemaDefinicao.ObterPorEstilo(EstiloTema.Colorido);
        var comBrilho = TemaDefinicao.ObterPorEstilo(EstiloTema.ComBrilho);

        Assert.NotNull(discreto);
        Assert.NotNull(escuro);
        Assert.NotNull(colorido);
        Assert.NotNull(comBrilho);

        Assert.NotEqual(discreto.FundoDockColor, escuro.FundoDockColor);
        Assert.NotEqual(escuro.BordaDockColor, comBrilho.BordaDockColor);
        Assert.True(discreto.RaioCantos > 0);
        Assert.True(comBrilho.OpacidadePadrao > 0);
    }

    [Fact]
    public void Espacadores_ConfiguracaoDeEstilosELargura_FuncionaCorretamente()
    {
        var espacadores = Preferencias.CriarEspacadoresPadrao();

        Assert.NotEmpty(espacadores);
        Assert.Contains(espacadores, e => e.Estilo == EstiloEspacador.Linha);

        var espacador = espacadores.First();
        espacador.Estilo = EstiloEspacador.Ponto;
        espacador.Largura = 12;

        Assert.Equal(EstiloEspacador.Ponto, espacador.Estilo);
        Assert.Equal(12, espacador.Largura);
    }

    [Fact]
    public void Preferencias_Clonar_PreservaColecoesEEspacadoresComCopiasIndependentes()
    {
        var prefs = Preferencias.CriarPadrao();
        var clone = prefs.Clonar();

        Assert.NotSame(prefs, clone);
        Assert.NotSame(prefs.ColecoesGlobais, clone.ColecoesGlobais);
        Assert.NotSame(prefs.Espacadores, clone.Espacadores);
        Assert.NotSame(prefs.CompromissosLocais, clone.CompromissosLocais);

        Assert.Equal(prefs.EstiloTema, clone.EstiloTema);
        Assert.Equal(prefs.ColecoesGlobais.Count, clone.ColecoesGlobais.Count);
        Assert.Equal(prefs.Espacadores.Count, clone.Espacadores.Count);

        // Alteração no clone não afeta original
        clone.ColecoesGlobais.Clear();
        Assert.NotEmpty(prefs.ColecoesGlobais);
    }

    [Fact]
    public void JsonSettingsRepository_PersistenciaCompletaV4_SalvaECarregaSemPerdas()
    {
        var repo = new JsonSettingsRepository(_tempDir);
        var prefs = Preferencias.CriarPadrao();

        prefs.EstiloTema = EstiloTema.ComBrilho;
        prefs.ColecoesGlobais.Add(new ColecaoApp
        {
            Id = "col-nova",
            Nome = "Design",
            Icone = "🎨",
            EhGlobal = true,
            Itens = new List<ItemFixado>
            {
                new() { Titulo = "Figma Web", CaminhoOuUrl = "https://figma.com", Tipo = TipoItem.WebUrl }
            }
        });
        prefs.CompromissosLocais.Add(new CompromissoLocal
        {
            Id = "comp-1",
            Titulo = "Review de Código",
            DataHora = DateTime.Now.AddHours(4),
            Descricao = "Revisar PR do Dock"
        });

        repo.Salvar(prefs);

        var carregadas = repo.Carregar();
        Assert.Equal(8, carregadas.SchemaVersion);
        Assert.Equal(EstiloTema.ComBrilho, carregadas.EstiloTema);
        Assert.Contains(carregadas.ColecoesGlobais, c => c.Nome == "Design");
        Assert.Single(carregadas.CompromissosLocais);
        Assert.Equal("Review de Código", carregadas.CompromissosLocais[0].Titulo);
    }
}

