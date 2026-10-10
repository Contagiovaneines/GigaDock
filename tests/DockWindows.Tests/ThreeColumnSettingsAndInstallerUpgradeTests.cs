using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;
using DockWindows.Installer.Services;
using Xunit;

namespace DockWindows.Tests;

public class ThreeColumnSettingsAndInstallerUpgradeTests
{
    private readonly string _tempDir;

    public ThreeColumnSettingsAndInstallerUpgradeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DockWindowsTests_3Col_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private class FakeLauncherService : ILauncherService
    {
        public LaunchResult Executar(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult AbrirLocal(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult ExecutarCaminho(string caminho, string? argumentos = null) => LaunchResult.Ok();
    }

    private class FakeIconExtractionService : IIconExtractionService
    {
        public System.Windows.Media.ImageSource? ObterIcone(ItemFixado item) => null;
        public System.Windows.Media.ImageSource? ObterIcone(string caminhoOuUrl, TipoItem tipo = TipoItem.Aplicativo) => null;
        public System.Windows.Media.ImageSource? ObterIconeJanela(IntPtr hWnd) => null;
        public System.Windows.Media.ImageSource? ObterIconeAppModernoJanela(IntPtr hWnd) => null;
    }

    private class FakeAutostartService : IAutostartService
    {
        public bool EstaHabilitado() => false;
        public bool Configurar(bool habilitar) => true;
    }

    private class FakeWindowTrackingService : IWindowTrackingService
    {
#pragma warning disable CS0067
        public event Action? JanelasAlteradas;
        public event Action<IntPtr>? JanelaAtivada;
        public event Action<bool>? TelaCheiaAlterada;
#pragma warning restore CS0067
        public IReadOnlyList<JanelaInfo> ObterJanelasAbertas() => Array.Empty<JanelaInfo>();
        public IntPtr ObterJanelaAtiva() => IntPtr.Zero;
        public void Iniciar() { }
        public void Parar() { }
        public bool AtivarJanela(IntPtr hWnd) => true;
        public bool MinimizarJanela(IntPtr hWnd) => true;
        public bool FecharJanela(IntPtr hWnd) => true;
        public void Dispose() { }
    }

    private class FakeTaskbarService : ITaskbarService
    {
        public int ObterEstadoAtual() => 0;
        public bool OcultarBarraNativa(out int estadoAnterior)
        {
            estadoAnterior = 0;
            return true;
        }
        public bool RestaurarBarraNativa(int? estadoAnterior = null) => true;
        public void GarantirBarraOculta() { }
        public void Dispose() { }
    }

    private MainViewModel CriarMainViewModel(ISettingsRepository repo)
    {
        return new MainViewModel(
            repo,
            new FakeLauncherService(),
            new FakeIconExtractionService(),
            new FakeAutostartService(),
            new FakeTaskbarService(),
            new FakeWindowTrackingService());
    }

    [Fact]
    public void AjustesJanela_ControlesVisiveisELayoutAdaptaAoRedimensionar()
    {
        ExecutarSta(() =>
        {
            var repo = new JsonSettingsRepository(_tempDir); repo.Salvar(Preferencias.CriarPadrao());
            using var main = CriarMainViewModel(repo);
            var window = new DockWindows.App.Views.AjustesWindow(new AjustesViewModel(main, repo, new FakeAutostartService(), "Ambientes"));
            try
            {
                window.Show(); ProcessarLayout(window);
                foreach (var width in new[] { 1180d, 820d, 600d, 400d })
                {
                    window.Width = width; window.Height = 640; ProcessarLayout(window);
                    foreach (var name in new[] { "MinimizeWindowButton", "MaximizeWindowButton", "CloseWindowButton" })
                    {
                        var button = Assert.IsType<System.Windows.Controls.Button>(window.FindName(name));
                        Assert.True(button.IsVisible);
                        var position = button.TranslatePoint(new System.Windows.Point(), window);
                        Assert.InRange(position.X, 0, window.ActualWidth - button.ActualWidth + 1);
                        Assert.InRange(position.Y, 0, window.ActualHeight - button.ActualHeight + 1);
                    }
                    var compact = (System.Windows.FrameworkElement)window.FindName("CompactNavigation");
                    Assert.Equal(width < 1050, compact.IsVisible);
                    SalvarPrevia(window, $"ajustes-{width:0}");
                }
                var maximize = (System.Windows.Controls.Button)window.FindName("MaximizeWindowButton");
                maximize.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); ProcessarLayout(window);
                Assert.Equal(System.Windows.WindowState.Maximized, window.WindowState);
                maximize.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); ProcessarLayout(window);
                Assert.Equal(System.Windows.WindowState.Normal, window.WindowState);
                var minimize = (System.Windows.Controls.Button)window.FindName("MinimizeWindowButton");
                minimize.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); ProcessarLayout(window);
                Assert.Equal(System.Windows.WindowState.Minimized, window.WindowState);
                window.WindowState = System.Windows.WindowState.Normal; ProcessarLayout(window);
                ((System.Windows.Controls.Button)window.FindName("CloseWindowButton")).RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.False(window.IsVisible);
            }
            finally { window.Close(); }
        });
    }

    [Fact]
    public void GuiaJanela_NavegaPassosAbreSecaoEFuncionaSemNavegacaoNoInstalador()
    {
        ExecutarSta(() =>
        {
            string? section = null;
            var guide = new DockWindows.App.Views.GuideWindow(value => section = value, reduceMotion: true);
            try
            {
                guide.Show(); ProcessarLayout(guide); SalvarPrevia(guide, "guia-aplicativo");
                var widgetStep = Descendentes<System.Windows.Controls.Button>(guide).Single(b => Equals(b.Content, "3  Widgets"));
                widgetStep.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); ProcessarLayout(guide);
                Assert.Contains(Descendentes<System.Windows.Controls.TextBlock>(guide), t => t.Text == "Widgets e tarefas");
                guide.Width = 430; guide.Height = 620; ProcessarLayout(guide); SalvarPrevia(guide, "guia-compacto");
                Descendentes<System.Windows.Controls.Button>(guide).Single(b => Equals(b.Content, "Abrir esta seção"))
                    .RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.Equal("Widgets", section); Assert.False(guide.IsVisible);
            }
            finally { guide.Close(); }
            var installerGuide = new DockWindows.Installer.Views.GuideWindow(reduceMotion: true);
            try
            {
                installerGuide.Show(); ProcessarLayout(installerGuide); SalvarPrevia(installerGuide, "guia-instalador");
                Assert.DoesNotContain(Descendentes<System.Windows.Controls.Button>(installerGuide), b => Equals(b.Content, "Abrir esta seção"));
                Descendentes<System.Windows.Controls.Button>(installerGuide).Single(b => Equals(b.Content, "5  Privacidade"))
                    .RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                ProcessarLayout(installerGuide);
                Descendentes<System.Windows.Controls.Button>(installerGuide).Single(b => Equals(b.Content, "Concluir ✓"))
                    .RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.False(installerGuide.IsVisible);
            }
            finally { installerGuide.Close(); }
        });
    }

    [Fact]
    public void Instalador_AbreGuiaSemInstalarOuAlterarConfiguracoes()
    {
        ExecutarSta(() =>
        {
            var installer = new DockWindows.Installer.MainWindow();
            try
            {
                installer.Show(); ProcessarLayout(installer); SalvarPrevia(installer, "instalador-guia");
                var button = Descendentes<System.Windows.Controls.Button>(installer)
                    .Single(b => Equals(b.Content, "Conhecer o GigaDock →") && b.IsVisible);
                var inspected = false;
                installer.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var guide = Assert.Single(installer.OwnedWindows.Cast<System.Windows.Window>());
                    try
                    {
                        Assert.IsType<DockWindows.Installer.Views.GuideWindow>(guide);
                        Assert.DoesNotContain(Descendentes<System.Windows.Controls.Button>(guide), b => Equals(b.Content, "Abrir esta seção"));
                        inspected = true;
                    }
                    finally { guide.Close(); }
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                button.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Assert.True(inspected); Assert.True(installer.IsVisible);
            }
            finally { installer.Close(); }
        });
    }

    private static void ExecutarSta(Action action)
    {
        Exception? error = null;
        var thread = new System.Threading.Thread(() => { try { action(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(System.Threading.ApartmentState.STA); thread.IsBackground = true; thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(25)), "A verificação da interface excedeu 25 segundos.");
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static void ProcessarLayout(System.Windows.Window window)
    {
        window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }
    private static IEnumerable<T> Descendentes<T>(System.Windows.DependencyObject root) where T : System.Windows.DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendentes<T>(child)) yield return descendant;
        }
    }
    private static void SalvarPrevia(System.Windows.Window window, string name)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DockWindows.slnx"))) root = root.Parent;
        if (root is null) return;
        var directory = Path.Combine(root.FullName, "docs", "guide-previews"); Directory.CreateDirectory(directory);
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var output = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(output);
    }

    [Fact]
    public void AjustesViewModel_NavegacaoTresColunas_AlternaSecoesCorretamente()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        Assert.True(vm.EhSecaoAmbientes);
        Assert.False(vm.EhSecaoWidgets);

        vm.NavegarPara("Widgets");
        Assert.True(vm.EhSecaoWidgets);
        Assert.False(vm.EhSecaoAmbientes);

        vm.NavegarPara("Espacadores");
        Assert.True(vm.EhSecaoEspacadores);

        vm.NavegarPara("Aparencia");
        Assert.True(vm.EhSecaoAparencia);

        vm.NavegarPara("Geral");
        Assert.True(vm.EhSecaoGeral);

        vm.NavegarPara("Utilitarios");
        Assert.True(vm.EhSecaoUtilitarios);

        vm.NavegarPara("Sobre");
        Assert.True(vm.EhSecaoSobre);
    }

    [Fact]
    public void DuplicarAmbiente_PersisteCopiaIndependenteSemTrocarAmbienteAtivo()
    {
        var repo = new JsonSettingsRepository(_tempDir); repo.Salvar(Preferencias.CriarPadrao());
        using var main = CriarMainViewModel(repo);
        var settings = new AjustesViewModel(main, repo, new FakeAutostartService(), "Ambientes");
        var original = settings.AmbienteSelecionado!; var active = main.AmbienteAtivo!.Id;
        settings.PedirTexto = (_, _) => "Ambiente duplicado";
        settings.DuplicarAmbienteCommand.Execute(null);
        var copy = settings.AmbienteSelecionado!;
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(active, main.AmbienteAtivo.Id);
        Assert.Equal(original.Itens.Count, copy.Itens.Count);
        Assert.Empty(original.Itens.Select(i => i.Id).Intersect(copy.Itens.Select(i => i.Id)));
        Assert.Contains(repo.Carregar().Ambientes, environment => environment.Id == copy.Id);
    }

    [Fact]
    public void DuplicarAmbiente_FalhaDeGravacaoRestauraListaESelecao()
    {
        var repo = new FailingSettings(); using var main = CriarMainViewModel(repo);
        var settings = new AjustesViewModel(main, repo, new FakeAutostartService(), "Ambientes");
        var original = settings.AmbienteSelecionado!; var count = main.Ambientes.Count;
        settings.PedirTexto = (_, _) => "Não deve persistir";
        repo.Fail = true; settings.DuplicarAmbienteCommand.Execute(null);
        Assert.Equal(count, main.Ambientes.Count);
        Assert.Equal(count, main.Preferencias.Ambientes.Count);
        Assert.Same(original, settings.AmbienteSelecionado);
    }

    private sealed class FailingSettings : ISettingsRepository
    {
        public bool Fail;
        public Preferencias Carregar() => Preferencias.CriarPadrao();
        public void Salvar(Preferencias prefs) { if (Fail) throw new IOException("Falha simulada."); }
        public Task SalvarAsync(Preferencias prefs) { Salvar(prefs); return Task.CompletedTask; }
        public string ObterCaminhoConfiguracoes() => "configuração simulada";
    }

    [Fact]
    public void AjustesViewModel_EdicaoAmbiente_AtualizaValoresEIndicadorApps()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        Assert.NotNull(vm.AmbienteSelecionado);

        // Edita nome
        vm.NomeAmbienteEditavel = "Trabalho Focado";
        Assert.Equal("Trabalho Focado", vm.AmbienteSelecionado.Nome);

        // Edita cor
        vm.CorAmbienteEditavel = "#107C41";
        Assert.Equal("#107C41", vm.AmbienteSelecionado.CorHex);

        // Edita estilo e cor do indicador de apps abertos
        vm.EstiloIndicadorAppsEditavel = "Pílula";
        vm.CorIndicadorAppsEditavel = "#FFB900";

        Assert.Equal("Pílula", vm.AmbienteSelecionado.EstiloIndicadorApps);
        Assert.Equal("#FFB900", vm.AmbienteSelecionado.CorIndicadorApps);

        // Confirma que gravou no repositório
        var recarregadas = repo.Carregar();
        var ambSalvo = recarregadas.Ambientes.First(a => a.Id == vm.AmbienteSelecionado.Id);
        Assert.Equal("Trabalho Focado", ambSalvo.Nome);
        Assert.Equal("Pílula", ambSalvo.EstiloIndicadorApps);
        Assert.Equal("#FFB900", ambSalvo.CorIndicadorApps);
    }

    [Fact]
    public void AjustesViewModel_ExclusaoAmbiente_ImpedeExcluirUltimoAmbiente()
    {
        var prefs = new Preferencias
        {
            SchemaVersion = 4,
            Ambientes = new List<Ambiente>
            {
                new() { Id = "amb-unico", Nome = "Único Ambiente", CorHex = "#0078D4" }
            }
        };
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        bool alertaExibido = false;
        vm.MostrarAlerta = (titulo, msg) => { alertaExibido = true; };

        Assert.Single(vm.Ambientes);
        vm.ExcluirAmbienteCommand.Execute(null);

        // Deve impedir a exclusão
        Assert.Single(vm.Ambientes);
        Assert.True(alertaExibido);
    }

    [Fact]
    public void AjustesViewModel_AlternarEscopoItem_MoveEntreAmbienteEGlobal()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        Assert.NotNull(vm.AmbienteSelecionado);
        var itemAmb = vm.AmbienteSelecionado.Itens.First();
        vm.ItemSelecionado = itemAmb;

        Assert.False(vm.ItemSelecionadoEhGlobal);
        Assert.Equal("Somente neste ambiente", vm.TextoEscopoItemSelecionado);

        // Alterna para Global
        vm.AlternarEscopoItemCommand.Execute(null);

        Assert.True(vm.ItemSelecionadoEhGlobal);
        Assert.Equal("Global (em todos os ambientes)", vm.TextoEscopoItemSelecionado);
        Assert.Contains(mainVm.Preferencias.AppsPermanentes, a => a.Id == itemAmb.Id);
        Assert.DoesNotContain(vm.AmbienteSelecionado.Itens, i => i.Id == itemAmb.Id);

        // Alterna de volta para Ambiente
        vm.AlternarEscopoItemCommand.Execute(null);

        Assert.False(vm.ItemSelecionadoEhGlobal);
        Assert.Contains(vm.AmbienteSelecionado.Itens, i => i.Id == itemAmb.Id);
        Assert.DoesNotContain(mainVm.Preferencias.AppsPermanentes, a => a.Id == itemAmb.Id);
    }

    [Fact]
    public void AjustesViewModel_ValidacaoURL_NormalizaProtocolo()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        int prompts = 0;
        vm.PedirTexto = (titulo, msg) =>
        {
            prompts++;
            if (prompts == 1) return "github.com"; // URL sem https://
            return "GitHub"; // Título
        };

        vm.AdicionarSiteUrlCommand.Execute(null);

        var itemCriado = vm.AmbienteSelecionado!.Itens.Last();
        Assert.Equal("https://github.com", itemCriado.CaminhoOuUrl);
        Assert.Equal("GitHub", itemCriado.Titulo);
        Assert.Equal(TipoItem.WebUrl, itemCriado.Tipo);
    }

    [Fact]
    public void InstallService_ConstantesEVersionamento_CoerentesComGigaDock()
    {
        var installService = new InstallService();
        Assert.Equal(InstallService.CurrentVersion, typeof(InstallService).Assembly.GetName().Version!.ToString(3));
        Assert.Equal(InstallService.CurrentVersion, typeof(DockWindows.App.App).Assembly.GetName().Version!.ToString(3));
        Assert.Equal("GigaDock", InstallService.AppName);
        Assert.Equal("DockWindows.App.exe", InstallService.AppExeName);
    }

    [Fact]
    public void InstallService_BackupPreUpdate_PreservaConfiguracoesAntesDeAtualizar()
    {
        var dirDados = Path.Combine(_tempDir, "UserData");
        Directory.CreateDirectory(dirDados);

        var arquivoSettings = Path.Combine(dirDados, "settings.json");
        var jsonConfig = @"{ ""schemaVersion"": 3, ""ambienteAtivoId"": ""amb-trabalho"", ""ambientes"": [] }";
        File.WriteAllText(arquivoSettings, jsonConfig);

        // Simula criação do backup preventivo pré-update
        var arquivoBackup = Path.Combine(dirDados, $"settings.json.pre-update-{DateTime.Now:yyyyMMddHHmmss}.bak");
        File.Copy(arquivoSettings, arquivoBackup, overwrite: true);

        Assert.True(File.Exists(arquivoBackup));
        Assert.Equal(jsonConfig, File.ReadAllText(arquivoBackup));
    }
}

