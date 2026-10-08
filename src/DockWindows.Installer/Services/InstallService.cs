using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;
using Microsoft.Win32;

namespace DockWindows.Installer.Services;

public class InstallService
{
    public const string AppName = "GigaDock";
    public const string AppExeName = "DockWindows.App.exe";
    public const string CurrentVersion = "3.2.0";
    private const string RegUninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DockWindows";
    private const string RegRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public string ObterDiretorioInstalacaoPadrao()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Programs", "DockWindows");
    }

    public string ObterDiretorioDadosUsuario()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "DockWindows");
    }

    public bool DetectarInstalacaoExistente(out string versaoInstalada, out string pastaInstalada, out bool iniciaComWindows)
    {
        versaoInstalada = string.Empty;
        pastaInstalada = ObterDiretorioInstalacaoPadrao();
        iniciaComWindows = false;

        try
        {
            // 1. Verificar chave no registro de desinstalação
            using var key = Registry.CurrentUser.OpenSubKey(RegUninstallKey);
            if (key != null)
            {
                var ver = key.GetValue("DisplayVersion") as string;
                var loc = key.GetValue("InstallLocation") as string;

                if (!string.IsNullOrWhiteSpace(ver)) versaoInstalada = ver;
                if (!string.IsNullOrWhiteSpace(loc) && Directory.Exists(loc)) pastaInstalada = loc;
            }

            // 2. Verificar se executável existe na pasta encontrada
            var exePrincipal = Path.Combine(pastaInstalada, AppExeName);
            bool existeArquivo = File.Exists(exePrincipal);

            if (existeArquivo && string.IsNullOrEmpty(versaoInstalada))
            {
                try
                {
                    var fileVer = FileVersionInfo.GetVersionInfo(exePrincipal);
                    versaoInstalada = !string.IsNullOrWhiteSpace(fileVer.ProductVersion) ? fileVer.ProductVersion : "1.0.0";
                }
                catch
                {
                    versaoInstalada = "1.0.0";
                }
            }

            // 3. Verificar inicialização automática (checa pasta Startup + chave de registro legada)
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var startupLnk = Path.Combine(startupFolder, "GigaDock.lnk");
            iniciaComWindows = File.Exists(startupLnk);
            if (!iniciaComWindows)
            {
                // Checa chave de registro antiga (instalações anteriores)
                using var runKey = Registry.CurrentUser.OpenSubKey(RegRunKey);
                if (runKey != null)
                    iniciaComWindows = runKey.GetValue("DockWindows") != null;
            }

            return existeArquivo || !string.IsNullOrEmpty(versaoInstalada);
        }
        catch
        {
            return false;
        }
    }

    public bool ExecutarInstalacao(
        string pastaDestino,
        bool criarAtalhoIniciar,
        bool criarAtalhoDesktop,
        bool iniciarComWindows,
        Action<string> notificarProgresso)
    {
        return ExecutarInstalacao(pastaDestino, criarAtalhoIniciar, criarAtalhoDesktop, iniciarComWindows, notificarProgresso, out _);
    }

    public bool ExecutarInstalacao(
        string pastaDestino,
        bool criarAtalhoIniciar,
        bool criarAtalhoDesktop,
        bool iniciarComWindows,
        Action<string> notificarProgresso,
        out string? mensagemErro)
    {
        mensagemErro = null;
        try
        {
            bool ehAtualizacao = DetectarInstalacaoExistente(out var versaoAntiga, out _, out _);

            if (ehAtualizacao)
            {
                notificarProgresso($"Instalação anterior detectada ({versaoAntiga}). Preparando atualização para {CurrentVersion}...");
            }
            else
            {
                notificarProgresso("Preparando diretório para nova instalação...");
            }

            // 1. Se estiver atualizando e o modo de substituição estiver ativo, restaurar a barra nativa com segurança
            if (ehAtualizacao)
            {
                try
                {
                    notificarProgresso("Garantindo restauração segura da barra de tarefas do Windows...");
                    var dirDados = ObterDiretorioDadosUsuario();
                    var arquivoConfig = Path.Combine(dirDados, "settings.json");
                    if (File.Exists(arquivoConfig))
                    {
                        // Backup preventivo antes da atualização
                        var arquivoBackup = Path.Combine(dirDados, $"settings.json.pre-update-{DateTime.Now:yyyyMMddHHmmss}.bak");
                        File.Copy(arquivoConfig, arquivoBackup, overwrite: true);

                        var repo = new JsonSettingsRepository();
                        var prefs = repo.Carregar();
                        if (prefs.UsarComoBarraPrincipal)
                        {
                            var taskbarService = new Win32TaskbarService();
                            taskbarService.RestaurarBarraNativa(prefs.EstadoAnteriorBarraTarefas);
                        }
                    }
                }
                catch { }
            }

            // 2. Encerrar instâncias ativas do GigaDock de forma controlada
            notificarProgresso("Encerrando instâncias em execução do GigaDock...");
            FecharProcessosDock(pastaDestino);

            // 3. Preparar diretório e extrair arquivos (substituição limpa)
            notificarProgresso(ehAtualizacao ? $"Atualizando arquivos do aplicativo para a versão {CurrentVersion}..." : "Copiando e descompactando arquivos do aplicativo...");
            Directory.CreateDirectory(pastaDestino);
            ExtrairArquivosAplicativo(pastaDestino);

            // 4. Copiar este instalador como Uninstall.exe na pasta de destino
            notificarProgresso("Atualizando utilitário de desinstalação...");
            var exeAtual = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exeAtual) && File.Exists(exeAtual))
            {
                var desinstaladorDestino = Path.Combine(pastaDestino, "Uninstall.exe");
                try
                {
                    CopiarArquivoComRetry(exeAtual, desinstaladorDestino);
                }
                catch { }
            }

            var exePrincipal = Path.Combine(pastaDestino, AppExeName);

            // 5. Criar ou atualizar atalhos (sem duplicatas)
            if (criarAtalhoIniciar)
            {
                notificarProgresso("Configurando atalho no Menu Iniciar...");
                var menuIniciar = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                var atalhoStart = Path.Combine(menuIniciar, $"{AppName}.lnk");
                ShortcutService.CriarAtalho(atalhoStart, exePrincipal, pastaDestino, "GigaDock — Barra de produtividade e ambientes");
            }

            if (criarAtalhoDesktop)
            {
                notificarProgresso("Configurando atalho na Área de Trabalho...");
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var atalhoDesk = Path.Combine(desktop, $"{AppName}.lnk");
                ShortcutService.CriarAtalho(atalhoDesk, exePrincipal, pastaDestino, "GigaDock — Barra de produtividade e ambientes");
            }

            // Migração de nome: remove apenas atalhos antigos que apontem para esta instalação.
            ShortcutService.RemoverLegados(Environment.GetFolderPath(Environment.SpecialFolder.Programs), exePrincipal);
            ShortcutService.RemoverLegados(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), exePrincipal);

            // 6. Atualizar registro de inicialização automática
            notificarProgresso("Configurando inicialização automática...");
            ConfigurarInicializacao(exePrincipal, iniciarComWindows);

            // 7. Atualizar registro do painel de controle (versão 2.0.0)
            notificarProgresso("Atualizando registro do sistema...");
            RegistrarNoPainelControle(pastaDestino, exePrincipal);

            // Instalar/atualizar preserva preferências e a barra nativa.
            // Modo de barra principal exige ação explícita dentro do aplicativo.

            notificarProgresso(ehAtualizacao ? "GigaDock atualizado com sucesso!" : "Instalação concluída com sucesso!");
            return true;
        }
        catch (Exception ex)
        {
            mensagemErro = ex.Message;
            notificarProgresso($"Erro durante a instalação/atualização: {ex.Message}");
            return false;
        }
    }

    public bool ExecutarDesinstalacao(bool removerDadosPessoais, Action<string> notificarProgresso)
    {
        try
        {
            notificarProgresso("Encerrando instâncias ativas do GigaDock...");
            FecharProcessosDock();

            // 1. Restaurar imediatamente a barra de tarefas do Windows se estiver oculta
            notificarProgresso("Restaurando barra de tarefas do Windows...");
            try
            {
                var repo = new JsonSettingsRepository();
                var prefs = repo.Carregar();
                var taskbarService = new Win32TaskbarService();
                taskbarService.RestaurarBarraNativa(prefs.EstadoAnteriorBarraTarefas);
            }
            catch { }

            // 2. Remover atalhos
            notificarProgresso("Removendo atalhos do sistema...");
            var menuIniciar = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            ShortcutService.RemoverAtalho(Path.Combine(menuIniciar, $"{AppName}.lnk"));

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            ShortcutService.RemoverAtalho(Path.Combine(desktop, $"{AppName}.lnk"));

            // Remover atalho da pasta Startup (inicialização automática)
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            ShortcutService.RemoverAtalho(Path.Combine(startupFolder, "GigaDock.lnk"));

            // 3. Remover chave de inicialização
            notificarProgresso("Removendo inicialização automática...");
            ConfigurarInicializacao(string.Empty, habilitar: false);

            // 4. Remover registro no Windows
            notificarProgresso("Removendo registro do aplicativo no sistema...");
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(RegUninstallKey, throwOnMissingSubKey: false);
            }
            catch { }

            // 5. Dados pessoais
            if (removerDadosPessoais)
            {
                notificarProgresso("Removendo preferências e configurações pessoais...");
                var dirDados = ObterDiretorioDadosUsuario();
                if (Directory.Exists(dirDados))
                {
                    try { Directory.Delete(dirDados, recursive: true); } catch { }
                }
            }

            // 6. Agendar exclusão da pasta do programa
            notificarProgresso("Limpando arquivos do programa...");
            var pastaApp = ObterDiretorioInstalacaoPadrao();
            if (Directory.Exists(pastaApp))
            {
                AgendarExclusaoPasta(pastaApp);
            }

            notificarProgresso("GigaDock foi desinstalado com sucesso!");
            return true;
        }
        catch (Exception ex)
        {
            notificarProgresso($"Erro durante a desinstalação: {ex.Message}");
            return false;
        }
    }

    private void ExtrairArquivosAplicativo(string destino)
    {
        Directory.CreateDirectory(destino);

        // Verifica se há recurso embutido app.zip
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("DockWindows.Installer.Resources.app.zip");

        if (stream != null)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    var dir = CaminhoPacoteSeguro.Resolver(destino, entry.FullName);
                    Directory.CreateDirectory(dir);
                    continue;
                }

                var arquivoDestino = CaminhoPacoteSeguro.Resolver(destino, entry.FullName);
                var pastaPai = Path.GetDirectoryName(arquivoDestino);
                if (!string.IsNullOrEmpty(pastaPai))
                {
                    Directory.CreateDirectory(pastaPai);
                }

                ExtrairArquivoComRetry(entry, arquivoDestino);
            }
            return;
        }

        // Se executando em modo de desenvolvimento/compilação local (sem o zip embutido),
        // busca os arquivos publicados ou compilados de DockWindows.App
        var pastaBase = AppDomain.CurrentDomain.BaseDirectory;
        var candidatos = new[]
        {
            Path.Combine(pastaBase, "app"),
            Path.Combine(pastaBase, @"..\..\..\..\DockWindows.App\bin\Release\net10.0-windows"),
            Path.Combine(pastaBase, @"..\..\..\..\DockWindows.App\bin\Debug\net10.0-windows"),
            Path.GetFullPath(Path.Combine(pastaBase, @"..\..\..\..\..\src\DockWindows.App\bin\Release\net10.0-windows")),
            Path.GetFullPath(Path.Combine(pastaBase, @"..\..\..\..\..\src\DockWindows.App\bin\Debug\net10.0-windows"))
        };

        foreach (var pasta in candidatos)
        {
            if (Directory.Exists(pasta) && File.Exists(Path.Combine(pasta, AppExeName)))
            {
                CopiarDiretorioRecursivo(pasta, destino);
                return;
            }
        }

        throw new FileNotFoundException("Arquivos do aplicativo GigaDock não foram encontrados para instalação.");
    }

    private static void ExtrairArquivoComRetry(ZipArchiveEntry entry, string destinoArquivo, int maxTentativas = 6)
    {
        for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            try
            {
                entry.ExtractToFile(destinoArquivo, overwrite: true);
                return;
            }
            catch (IOException)
            {
                if (tentativa < maxTentativas)
                {
                    Thread.Sleep(250 * tentativa);
                }
                else
                {
                    // Fallback para Windows: se o arquivo estiver bloqueado, renomeia para .old temporário
                    try
                    {
                        if (File.Exists(destinoArquivo))
                        {
                            var arquivoOld = destinoArquivo + ".old-" + Guid.NewGuid().ToString("N")[..6];
                            File.Move(destinoArquivo, arquivoOld);
                            entry.ExtractToFile(destinoArquivo, overwrite: true);
                            try { File.Delete(arquivoOld); } catch { }
                            return;
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
        }
    }

    private static void CopiarArquivoComRetry(string origem, string destino, int maxTentativas = 6)
    {
        for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            try
            {
                File.Copy(origem, destino, true);
                return;
            }
            catch (IOException)
            {
                if (tentativa < maxTentativas)
                {
                    Thread.Sleep(250 * tentativa);
                }
                else
                {
                    try
                    {
                        if (File.Exists(destino))
                        {
                            var arquivoOld = destino + ".old-" + Guid.NewGuid().ToString("N")[..6];
                            File.Move(destino, arquivoOld);
                            File.Copy(origem, destino, true);
                            try { File.Delete(arquivoOld); } catch { }
                            return;
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
        }
    }

    private static void CopiarDiretorioRecursivo(string origem, string destino)
    {
        Directory.CreateDirectory(destino);
        foreach (var arquivo in Directory.GetFiles(origem))
        {
            var nomeArquivo = Path.GetFileName(arquivo);
            CopiarArquivoComRetry(arquivo, Path.Combine(destino, nomeArquivo));
        }

        foreach (var sub in Directory.GetDirectories(origem))
        {
            var nomeSub = Path.GetFileName(sub);
            CopiarDiretorioRecursivo(sub, Path.Combine(destino, nomeSub));
        }
    }

    public static void FecharProcessosDock(string? pastaDestino = null)
    {
        var currentPid = Environment.ProcessId;

        // 1. Procurar e encerrar por nomes conhecidos de processo
        var nomes = new[] { "DockWindows.App", "DockWindows" };
        foreach (var nome in nomes)
        {
            try
            {
                var procs = Process.GetProcessesByName(nome);
                foreach (var p in procs)
                {
                    if (p.Id == currentPid) continue;
                    try
                    {
                        p.CloseMainWindow();
                        if (!p.WaitForExit(1500))
                        {
                            p.Kill();
                            p.WaitForExit(3000);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        // 2. Se informada a pasta de destino, verificar qualquer processo aberto nela
        if (!string.IsNullOrEmpty(pastaDestino) && Directory.Exists(pastaDestino))
        {
            try
            {
                var todosProcs = Process.GetProcesses();
                foreach (var p in todosProcs)
                {
                    if (p.Id == currentPid) continue;
                    try
                    {
                        var caminhoProc = p.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(caminhoProc) && caminhoProc.StartsWith(pastaDestino, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Kill();
                            p.WaitForExit(3000);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        // 3. Pausa de estabilização do kernel do Windows para desmapeamento de arquivos executáveis
        Thread.Sleep(300);
    }

    private static void ConfigurarInicializacao(string caminhoExe, bool habilitar)
    {
        try
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var shortcutPath = Path.Combine(startupFolder, "GigaDock.lnk");

            if (habilitar && !string.IsNullOrEmpty(caminhoExe))
            {
                var pastaDestino = Path.GetDirectoryName(caminhoExe) ?? string.Empty;
                ShortcutService.CriarAtalho(shortcutPath, caminhoExe, pastaDestino, "GigaDock");
            }
            else
            {
                if (File.Exists(shortcutPath)) File.Delete(shortcutPath);
            }

            // Limpa chave de registro legada (instalações antigas)
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegRunKey, writable: true);
                key?.DeleteValue("DockWindows", throwOnMissingValue: false);
            }
            catch { }
        }
        catch { }
    }

    private void RegistrarNoPainelControle(string pastaDestino, string exePrincipal)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegUninstallKey);
            if (key != null)
            {
                var desinstalador = Path.Combine(pastaDestino, "Uninstall.exe");
                key.SetValue("DisplayName", AppName);
                key.SetValue("DisplayVersion", CurrentVersion);
                key.SetValue("Publisher", "GigaDock");
                key.SetValue("InstallLocation", pastaDestino);
                key.SetValue("UninstallString", $"\"{desinstalador}\" --uninstall");
                key.SetValue("DisplayIcon", $"\"{exePrincipal}\",0");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

                long tamanhoBytes = 0;
                if (Directory.Exists(pastaDestino))
                {
                    foreach (var file in Directory.GetFiles(pastaDestino, "*.*", SearchOption.AllDirectories))
                    {
                        tamanhoBytes += new FileInfo(file).Length;
                    }
                }
                key.SetValue("EstimatedSize", (int)(tamanhoBytes / 1024), RegistryValueKind.DWord);
            }
        }
        catch { }
    }

    private static void AgendarExclusaoPasta(string pasta)
    {
        try
        {
            var cmd = $"/c timeout /t 2 /nobreak >nul & rmdir /s /q \"{pasta}\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmd,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch { }
    }
}






