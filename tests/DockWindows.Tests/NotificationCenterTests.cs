using System.Windows.Media;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public sealed class NotificationCenterTests
{
    [Fact]
    public async Task AbrirOuAtualizar_NaoSolicitaPermissaoImplicitamente()
    {
        var source = new FakeSource();
        using var vm = new NotificationCenterViewModel(source, new FakeIcons(), () => false);
        await vm.RefreshAsync();
        Assert.Equal(0, source.Requests);
        Assert.Equal(2, vm.Count);
        Assert.Equal("Nova", vm.Items[0].Title);
    }
    [Fact]
    public async Task Ativar_SolicitaAcessoSomentePorAcaoExplicita()
    {
        var source = new FakeSource();
        using var vm = new NotificationCenterViewModel(source, new FakeIcons(), () => false);
        await vm.ActivateAsync(); Assert.Equal(1, source.Requests);
    }
    [Fact]
    public async Task InstalacaoSemIdentidade_NaoPodeSolicitarAcesso()
    {
        var source = new FakeSource { HasPackageIdentity = false };
        using var vm = new NotificationCenterViewModel(source, new FakeIcons(), () => false);
        Assert.False(vm.ActivateCommand.CanExecute(null));
        await vm.ActivateAsync(); Assert.Equal(0, source.Requests);
    }
    [Fact]
    public async Task RemocaoIndividual_AtualizaListaEContador()
    {
        var source = new FakeSource();
        using var vm = new NotificationCenterViewModel(source, new FakeIcons(), () => false);
        await vm.RefreshAsync(); await vm.RemoveAsync(2);
        Assert.Single(vm.Items); Assert.Equal(1u, vm.Items[0].Id); Assert.Equal("1", vm.Badge);
    }
    [Theory]
    [InlineData(false, 2, 0)]
    [InlineData(true, 0, 1)]
    public async Task LimparTodas_ExigeConfirmacao(bool confirm, int expectedCount, int expectedClears)
    {
        var source = new FakeSource();
        using var vm = new NotificationCenterViewModel(source, new FakeIcons(), () => confirm);
        await vm.RefreshAsync(); await vm.ClearAsync();
        Assert.Equal(expectedCount, vm.Count); Assert.Equal(expectedClears, source.Clears);
    }
    [Fact]
    public async Task PermissaoRevogada_DescartaConteudoDaMemoria()
    {
        var source = new FakeSource();
        using var vm = new NotificationCenterViewModel(source, new FakeIcons(), () => false);
        await vm.RefreshAsync(); source.Entries.Clear(); source.EstadoPermissao = "Acesso negado";
        await vm.RefreshAsync(); Assert.Empty(vm.Items); Assert.False(vm.HasItems);
        Assert.Equal("Acesso negado", vm.Status);
    }
    [Fact]
    public async Task ErroDeLeitura_NaoMantemConteudoAntigo()
    {
        var source = new FakeSource();
        using var vm = new NotificationCenterViewModel(source, new FakeIcons(), () => false);
        await vm.RefreshAsync(); source.Fail = true; await vm.RefreshAsync();
        Assert.Empty(vm.Items); Assert.Contains("Não foi possível", vm.Status);
    }
    [Fact]
    public async Task ConteudoInalterado_NaoRecriaCartoesOuIcones()
    {
        var icons = new FakeIcons(); var source = new FakeSource();
        using var vm = new NotificationCenterViewModel(source, icons, () => false);
        await vm.RefreshAsync(); var card = vm.Items[0]; await vm.RefreshAsync();
        Assert.Same(card, vm.Items[0]); Assert.Equal(2, icons.Reads);
    }
    private sealed class FakeSource : INotificationCenterSource
    {
        public bool HasPackageIdentity { get; set; } = true;
        public string EstadoPermissao { get; set; } = "Permitida";
        public int Requests, Clears; public bool Fail;
        public List<DockNotification> Entries = [new(1, "Exemplo", "sample", DateTimeOffset.Now.AddMinutes(-1), "Anterior", "Texto"), new(2, "Exemplo", "sample", DateTimeOffset.Now, "Nova", "Texto")];
        public Task RequestAccessAsync() { Requests++; return Task.CompletedTask; }
        public Task<IReadOnlyList<DockNotification>> ReadAsync() => Fail ? throw new InvalidOperationException() : Task.FromResult<IReadOnlyList<DockNotification>>(Entries.ToArray());
        public bool Remove(uint id) { Entries.RemoveAll(n => n.Id == id); return true; }
        public bool ClearAll() { Clears++; Entries.Clear(); return true; }
    }
    private sealed class FakeIcons : IIconExtractionService
    {
        public int Reads;
        public ImageSource? ObterIcone(ItemFixado item) => null;
        public ImageSource? ObterIcone(string value, TipoItem type = TipoItem.Aplicativo) { Reads++; return null; }
        public ImageSource? ObterIconeJanela(IntPtr window) => null;
        public ImageSource? ObterIconeAppModernoJanela(IntPtr window) => null;
    }
}
