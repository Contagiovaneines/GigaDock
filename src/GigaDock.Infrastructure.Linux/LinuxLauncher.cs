using System.Diagnostics;
using DockWindows.Core.Models;
using DockWindows.Core.Validation;

namespace GigaDock.Infrastructure.Linux;

public sealed class LinuxLauncher
{

    public static ValidacaoResultado Validate(ItemFixado item)
    {
        if (string.IsNullOrWhiteSpace(item.Titulo) || item.Titulo.Length > 200)
            return ValidacaoResultado.Erro("Informe um título de até 200 caracteres.");
        if (!Enum.IsDefined(item.Tipo)) return ValidacaoResultado.Erro("Escolha um tipo de item válido.");
        if (!string.IsNullOrWhiteSpace(item.Argumentos))
            return ValidacaoResultado.Erro("Use o aplicativo do catálogo para argumentos definidos pelo desktop; argumentos de texto não são executados.");
        var target = item.CaminhoOuUrl;
        if (string.IsNullOrWhiteSpace(target) || target.Any(char.IsControl)) return ValidacaoResultado.Erro("Informe um caminho ou endereço válido.");
        if (item.Tipo == TipoItem.WebUrl) return ItemValidator.ValidarUrl(target);
        if (!Path.IsPathFullyQualified(target)) return ValidacaoResultado.Erro("Use um caminho absoluto do Linux.");
        if (target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || target.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            return ValidacaoResultado.Erro("Este item pertence ao Windows. Escolha um aplicativo Linux no catálogo.");
        if (item.Tipo == TipoItem.Pasta) return ItemValidator.ValidarPasta(target);
        if (!File.Exists(target)) return ValidacaoResultado.Erro("O arquivo não existe ou o link está quebrado.");
        if (item.Tipo == TipoItem.Aplicativo)
        {
            if (target.EndsWith(".desktop", StringComparison.Ordinal))
                return new DesktopCatalog().Read(target) is null ? ValidacaoResultado.Erro("A entrada de aplicativo não está disponível neste desktop.") : ValidacaoResultado.Sucesso();
            if (!LinuxCommands.IsExecutable(target)) return ValidacaoResultado.Erro("O aplicativo não tem permissão de execução no Linux.");
        }
        return ValidacaoResultado.Sucesso();
    }

    public async Task LaunchAsync(ItemFixado item, CancellationToken cancellation = default)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("A prévia Windows não abre aplicativos ou arquivos Linux.");
        var validation = Validate(item);
        if (!validation.Valido) throw new ArgumentException(validation.MensagemErro);
        var target = item.CaminhoOuUrl;
        if (item.Tipo == TipoItem.WebUrl && !Uri.TryCreate(target, UriKind.Absolute, out _)) target = "https://" + target;
        if (item.Tipo == TipoItem.Aplicativo && !target.EndsWith(".desktop", StringComparison.Ordinal))
        {
            using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(target)! });
            if (process is null) throw new IOException("Não foi possível iniciar o aplicativo.");
            return;
        }
        var launchDesktop = item.Tipo == TipoItem.Aplicativo;
        var gio = LinuxCommands.Find("gio") ?? throw new InvalidOperationException("Instale libglib2.0-bin (gio) para abrir aplicativos e arquivos.");
        var start = new ProcessStartInfo(gio) { UseShellExecute = false };
        start.ArgumentList.Add(launchDesktop ? "launch" : "open"); start.ArgumentList.Add(target);
        // Não redirecionar pipes: o aplicativo aberto pode herdá-los e mantê-los abertos por horas.
        using var association = Process.Start(start) ?? throw new IOException("Não foi possível pedir a abertura ao desktop.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation); deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try { await association.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            try { if (!association.HasExited) association.Kill(); } catch (InvalidOperationException) { }
            throw new IOException("O desktop demorou para responder. Confira se o aplicativo abriu antes de tentar novamente.");
        }
        if (association.ExitCode != 0) throw new IOException("O desktop não conseguiu abrir o item. Confira a associação padrão e se o aplicativo está instalado.");
    }
}
