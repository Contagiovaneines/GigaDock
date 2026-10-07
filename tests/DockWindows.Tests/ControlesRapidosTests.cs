using System.Text.Json;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public class ControlesRapidosTests
{
    [Fact]
    public void EstiloDosControles_PersisteSemExecutarConfiguracaoGlobal()
    {
        var a = new Ambiente(); var b = new Ambiente(); var service = new FakeService();
        int salvos = 0;
        using var vm = new ControlesRapidosViewModel(() => salvos++, _ => { }, () => true, service);
        vm.Carregar(a); vm.Estilo = "Anéis";
        Assert.Equal(1, salvos);
        Assert.Empty(service.Chamadas);
        vm.Carregar(b); Assert.Equal("Compacto", vm.Estilo);
        vm.Carregar(JsonSerializer.Deserialize<Ambiente>(JsonSerializer.Serialize(a))!);
        Assert.Equal("Anéis", vm.Estilo);
        vm.Estilo = "invalido"; Assert.Equal("Anéis", vm.Estilo);
    }
    private sealed class FakeService : IControlesRapidosService
    {
        public bool TecladoBloqueado { get; private set; }
        public List<TipoControleRapido> Chamadas { get; } = new();
        public bool Falhar { get; set; }
        public event Action? BloqueioAlterado;
        public void Executar(TipoControleRapido tipo)
        {
            if (Falhar) throw new InvalidOperationException();
            Chamadas.Add(tipo);
            if (tipo == TipoControleRapido.BloquearTeclado)
            {
                TecladoBloqueado = !TecladoBloqueado;
                BloqueioAlterado?.Invoke();
            }
        }
        public void LiberarTeclado() { TecladoBloqueado = false; BloqueioAlterado?.Invoke(); }
        public void Dispose() => LiberarTeclado();
    }

    [Fact]
    public void Visibilidade_PersistePorAmbienteSemExecutarAcao()
    {
        var primeiro = new Ambiente();
        var segundo = new Ambiente();
        var service = new FakeService();
        int salvamentos = 0;
        using var vm = new ControlesRapidosViewModel(() => salvamentos++, _ => { }, () => true, service);
        vm.Carregar(primeiro);
        Assert.Equal(5, vm.Quantidade);
        vm.Itens.Single(i => i.Tipo == TipoControleRapido.Wifi).MostrarNaDock = false;
        Assert.Equal(4, vm.Quantidade);
        Assert.Equal(1, salvamentos);
        Assert.Empty(service.Chamadas);
        vm.Carregar(segundo);
        Assert.Equal(5, vm.Quantidade);
        vm.Carregar(JsonSerializer.Deserialize<Ambiente>(JsonSerializer.Serialize(primeiro))!);
        Assert.Equal(4, vm.Quantidade);
        Assert.DoesNotContain(vm.Fixados, i => i.Tipo == TipoControleRapido.Wifi);
    }

    [Fact]
    public void Suspender_SoExecutaDepoisDeConfirmar()
    {
        var service = new FakeService();
        bool confirmar = false;
        using var vm = new ControlesRapidosViewModel(() => { }, _ => { }, () => confirmar, service);
        vm.Carregar(new Ambiente());
        var suspender = vm.Itens.Single(i => i.Tipo == TipoControleRapido.Suspender);
        suspender.ExecutarCommand.Execute(null);
        Assert.Empty(service.Chamadas);
        confirmar = true;
        suspender.ExecutarCommand.Execute(null);
        Assert.Equal(TipoControleRapido.Suspender, Assert.Single(service.Chamadas));
    }

    [Fact]
    public void TrocarAmbiente_LiberaTecladoEFechaPainel()
    {
        var service = new FakeService();
        using var vm = new ControlesRapidosViewModel(() => { }, _ => { }, () => true, service);
        vm.Carregar(new Ambiente());
        vm.Itens.Single(i => i.Tipo == TipoControleRapido.BloquearTeclado).ExecutarCommand.Execute(null);
        Assert.True(vm.TecladoBloqueado);
        vm.Aberto = true;
        vm.Carregar(new Ambiente());
        Assert.False(vm.TecladoBloqueado);
        Assert.False(vm.Aberto);
        Assert.False(vm.LiberarTecladoCommand.CanExecute(null));
    }

    [Fact]
    public void FalhaNativa_ExibeMensagemCompreensivel()
    {
        string? erro = null;
        var service = new FakeService { Falhar = true };
        using var vm = new ControlesRapidosViewModel(() => { }, mensagem => erro = mensagem, () => true, service);
        vm.Carregar(new Ambiente());
        vm.Itens.Single(i => i.Tipo == TipoControleRapido.ModoEscuro).ExecutarCommand.Execute(null);
        Assert.Contains("Não foi possível", erro);
    }
}
