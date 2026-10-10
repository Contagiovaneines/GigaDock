using System.Diagnostics;
using System.Text.Json;
using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.Tests.Linux;

public sealed class LinuxFactAttribute : FactAttribute
{
    public LinuxFactAttribute() { if (!OperatingSystem.IsLinux()) Skip = "Teste nativo requer Linux."; }
}

/// <summary>Corpos nativos só executam no Linux; o relatório distingue isso dos testes portáveis.</summary>
public sealed class NativeLinuxTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GigaDock-native-" + Guid.NewGuid().ToString("N"));

    [LinuxFact]
    public async Task GioLaunch_ExecutesDesktopEntryWithLiteralArguments()
    {
        if (!OperatingSystem.IsLinux()) return;
        Assert.NotNull(LinuxCommands.Find("gio")); Assert.NotNull(LinuxCommands.Find("python3"));
        Directory.CreateDirectory(_root);
        var script = Path.Combine(_root, "capture.py"); var result = Path.Combine(_root, "arguments.json");
        File.WriteAllText(script, "import sys,json\nwith open(sys.argv[1],'w') as f: json.dump(sys.argv[2:],f)\n");
        var desktop = Path.Combine(_root, "test.desktop");
        File.WriteAllText(desktop, $"[Desktop Entry]\nType=Application\nName=Teste de abertura\nExec={LinuxCommands.Find("python3")} \"{script}\" \"{result}\" \"argumento com acentos e espaços\" %%\nTerminal=false\n");
        await new LinuxLauncher().LaunchAsync(new ItemFixado { Titulo = "Teste", Tipo = TipoItem.Aplicativo, CaminhoOuUrl = desktop });
        for (var attempt = 0; attempt < 30 && !File.Exists(result); attempt++) await Task.Delay(100);
        Assert.True(File.Exists(result));
        Assert.Equal(new[] { "argumento com acentos e espaços", "%" }, JsonSerializer.Deserialize<string[]>(File.ReadAllText(result)));
    }

    [LinuxFact]
    public void ExecutablePermissionsAndBrokenLinks_AreCheckedOnLinux()
    {
        if (!OperatingSystem.IsLinux()) return;
        Directory.CreateDirectory(_root); var path = Path.Combine(_root, "app"); File.WriteAllText(path, "#!/bin/sh\nexit 0\n");
        var item = new ItemFixado { Titulo = "App", CaminhoOuUrl = path, Tipo = TipoItem.Aplicativo };
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); Assert.False(LinuxLauncher.Validate(item).Valido);
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute); Assert.True(LinuxLauncher.Validate(item).Valido);
        var link = Path.Combine(_root, "link"); File.CreateSymbolicLink(link, path); File.Delete(path);
        item.CaminhoOuUrl = link; Assert.False(LinuxLauncher.Validate(item).Valido);
    }

    [LinuxFact]
    public async Task SingleInstance_IsExclusiveAcrossProcessesAndReleasedByExit()
    {
        if (!OperatingSystem.IsLinux()) return;
        Directory.CreateDirectory(_root);
        var python = new ProcessStartInfo(LinuxCommands.Find("python3")!) { UseShellExecute = false, RedirectStandardOutput = true };
        python.ArgumentList.Add("-c"); python.ArgumentList.Add("import fcntl,sys,time\nf=open(sys.argv[1],'a')\nfcntl.flock(f,fcntl.LOCK_EX)\nprint('LOCKED',flush=True)\ntime.sleep(10)");
        python.ArgumentList.Add(Path.Combine(_root, "instance.lock"));
        using var process = Process.Start(python)!;
        try { Assert.Equal("LOCKED", await process.StandardOutput.ReadLineAsync()); Assert.Null(SingleInstance.TryAcquire(_root)); }
        finally { if (!process.HasExited) process.Kill(); await process.WaitForExitAsync(); }
        using var instance = SingleInstance.TryAcquire(_root); Assert.NotNull(instance);
    }

    [LinuxFact]
    public async Task Autostart_QuotesPathsAndDoesNotOverwriteUnmanagedEntries()
    {
        if (!OperatingSystem.IsLinux()) return;
        var folder = Path.Combine(_root, "dir com espaços $ e ' literal"); Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "GigaDock"); var marker = Path.Combine(_root, "started");
        File.WriteAllText(path, "#!/bin/sh\nprintf ready > '" + marker + "'\n"); File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var config = Path.Combine(_root, "config"); LinuxAutostart.Set(true, path, config);
        var desktop = LinuxAutostart.GetPath(config);
        var result = await new LinuxCommands().RunAsync("gio", ["launch", desktop]); Assert.Equal(0, result.ExitCode);
        for (var attempt = 0; attempt < 30 && !File.Exists(marker); attempt++) await Task.Delay(100);
        Assert.True(File.Exists(marker));
        LinuxAutostart.Set(false, path, config); Assert.False(File.Exists(desktop));
        File.WriteAllText(desktop, "[Desktop Entry]\nName=Meu arquivo\n");
        Assert.Throws<IOException>(() => LinuxAutostart.Set(false, path, config)); Assert.Contains("Meu arquivo", File.ReadAllText(desktop));
    }

    [LinuxFact]
    public async Task ObsLocal_AuthenticatesAndCorrelatesResponseWithoutSavingPassword()
    {
        if (!OperatingSystem.IsLinux()) return;
        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0); probe.Start();
        var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using var listener = new System.Net.HttpListener(); listener.Prefixes.Add($"http://127.0.0.1:{port}/"); listener.Start();
        var server = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
            var upgraded = await context.AcceptWebSocketAsync(null); using var socket = upgraded.WebSocket;
            async Task Send(object value) => await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(value).AsMemory(), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            async Task<JsonDocument> Receive()
            {
                var buffer = new byte[8192]; var received = await socket.ReceiveAsync(buffer.AsMemory(), CancellationToken.None);
                return JsonDocument.Parse(buffer.AsMemory(0, received.Count));
            }
            await Send(new { op = 0, d = new { rpcVersion = 1, authentication = new { salt = "salt", challenge = "challenge" } } });
            using var identification = await Receive();
            Assert.Equal(1, identification.RootElement.GetProperty("op").GetInt32());
            Assert.Equal(ObsLocalClient.Authentication("pass", "salt", "challenge"), identification.RootElement.GetProperty("d").GetProperty("authentication").GetString());
            await Send(new { op = 2, d = new { negotiatedRpcVersion = 1 } });
            using var request = await Receive();
            var id = request.RootElement.GetProperty("d").GetProperty("requestId").GetString();
            Assert.Equal("GetRecordStatus", request.RootElement.GetProperty("d").GetProperty("requestType").GetString());
            await Send(new { op = 7, d = new { requestId = "unrelated", requestStatus = new { result = true }, responseData = new { outputActive = false } } });
            await Send(new { op = 7, d = new { requestId = id, requestStatus = new { result = true }, responseData = new { outputActive = true } } });
        });
        Assert.Equal("Ativo no OBS.", await ObsLocalClient.RequestAsync(port, "pass", "GetRecordStatus"));
        await server.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(Directory.Exists(_root));
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
