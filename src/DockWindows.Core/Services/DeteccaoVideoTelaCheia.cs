namespace DockWindows.Core.Services;

public static class DeteccaoVideoTelaCheia
{
    public static bool Reconhecer(string processo, string titulo)
    {
        var nome = Path.GetFileNameWithoutExtension(processo).ToLowerInvariant();
        if (nome is "vlc" or "mpv" or "mpc-hc" or "mpc-hc64" or "mpc-be" or "mpc-be64" or "potplayermini" or "potplayermini64" or "netflix") return true;
        if (nome is not ("chrome" or "msedge" or "firefox" or "brave" or "opera" or "vivaldi")) return false;
        return new[] { "youtube", "netflix", "prime video", "disney+", "disney plus", "twitch", "vimeo", "globoplay" }
            .Any(servico => titulo.Contains(servico, StringComparison.OrdinalIgnoreCase));
    }
}
