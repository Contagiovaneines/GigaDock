using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Media.Control;
using Windows.Storage.Streams;
using DockWindows.App.Common;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DockWindows.App.ViewModels;

public class MidiaWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => !_disposed && (_iniciando || _escutar && _sessionManager != null);

    private int _revisaoMidia;
    private bool _visual, _escutar, _iniciando, _disposed;
    private string _chaveCapa = "";
    private readonly Dictionary<string, (DateTimeOffset Criada, System.Windows.Media.Imaging.BitmapSource Imagem)> _capas = new();
    public int CapasEmCache => _capas.Count;
    private void GuardarCapa(string chave, System.Windows.Media.Imaging.BitmapSource? imagem)
    {
        if (imagem == null) return;
        _capas[chave] = (DateTimeOffset.UtcNow, imagem);
        while (_capas.Count > 8) _capas.Remove(_capas.MinBy(p => p.Value.Criada).Key);
    }
    public bool TimerAtivo => _timelineTimer.IsEnabled;
    public int ListenersAtivos => (_sessionManager == null ? 0 : 2) + (_currentSession == null ? 0 : 3);
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        var eraVisual = _visual;
        _visual = estado.Visual;
        _escutar = _visual || estado.SegundoPlano;
        if (!_escutar) Desconectar();
        else if (_sessionManager == null) _ = InitializeAsync();
        else if (_visual && !eraVisual) _ = UpdateMediaPropertiesAsync();
        if (!estado.Habilitado) { CapaAlbumUrl = null; _chaveCapa = ""; _capas.Clear(); }
        AtualizarTimer();
    }
    private void AtualizarTimer()
    {
        _timelineTimer.Stop();
        if (!_visual || _disposed) return;
        AtualizarTimeline();
        if (EstaTocando && TemMidia) _timelineTimer.Start();
    }
    private void Desconectar()
    {
        System.Threading.Interlocked.Increment(ref _revisaoMidia);
        if (_sessionManager != null) { _sessionManager.CurrentSessionChanged -= SessionManager_CurrentSessionChanged; _sessionManager.SessionsChanged -= SessionManager_SessionsChanged; }
        if (_currentSession != null) { _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged; _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged; _currentSession.TimelinePropertiesChanged -= Session_TimelinePropertiesChanged; }
        _currentSession = null; _sessionManager = null; _timelineTimer.Stop();
    }
    public void Dispose() { _disposed = true; _visual = _escutar = false; Desconectar(); CapaAlbumUrl = null; _capas.Clear(); }

    private string _estilo = "capa";
    public string Estilo { get => _estilo; set => SetProperty(ref _estilo, value); }
    private double _progresso;
    public double Progresso { get => _progresso; private set => SetProperty(ref _progresso, value); }
    private string _posicao = "—", _duracao = "—";
    public string Posicao { get => _posicao; private set => SetProperty(ref _posicao, value); }
    public string Duracao { get => _duracao; private set => SetProperty(ref _duracao, value); }
    private readonly System.Windows.Threading.DispatcherTimer _timelineTimer;
    private string? _titulo = string.Empty;
    private string? _artista = string.Empty;
    private System.Windows.Media.Imaging.BitmapSource? _capaAlbumUrl;
    private string _corPredominanteHex = "#000000";
    private bool _estaTocando;
    private bool _habilitado = true;
    
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public string? Titulo
    {
        get => _titulo;
        set
        {
            if (SetProperty(ref _titulo, value))
            {
                OnPropertyChanged(nameof(TemMidia));
            }
        }
    }
    
        public bool Habilitado
    {
        get => _habilitado;
        set
        {
            if (SetProperty(ref _habilitado, value))
            {
                OnPropertyChanged(nameof(TemMidia));
            }
        }
    }

    public bool TemMidia => !string.IsNullOrEmpty(_titulo) && _habilitado;
    
    public string? Artista
    {
        get => _artista;
        set => SetProperty(ref _artista, value);
    }
    
    public string CorPredominanteHex { get => _corPredominanteHex; set => SetProperty(ref _corPredominanteHex, value); }

    public System.Windows.Media.Imaging.BitmapSource? CapaAlbumUrl
    {
        get => _capaAlbumUrl;
        set => SetProperty(ref _capaAlbumUrl, value);
    }
    
    public bool EstaTocando
    {
        get => _estaTocando;
        set => SetProperty(ref _estaTocando, value);
    }
    
    public ICommand PlayPauseCommand { get; }
    public ICommand AnteriorCommand { get; }
    public ICommand ProximoCommand { get; }

        [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public ICommand AbrirPlayerCommand { get; }

    private readonly Func<bool> _canOpenPlayer;
    public MidiaWidgetViewModel(Func<bool>? canOpenPlayer = null)
    {
        _timelineTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timelineTimer.Tick += (_, _) => AtualizarTimeline();

        _canOpenPlayer = canOpenPlayer ?? (() => true);
        PlayPauseCommand = new RelayCommand(() => _ = TogglePlayPauseAsync());
        AnteriorCommand = new RelayCommand(() => _ = SkipPreviousAsync());
                ProximoCommand = new RelayCommand(() => _ = SkipNextAsync());
        AbrirPlayerCommand = new RelayCommand(AbrirPlayer);
    }

    private void AtualizarTimeline()
    {
        if (!TemMidia || _currentSession == null) { Progresso = 0; Posicao = Duracao = "—"; return; }
        try
        {
            var timeline = _currentSession.GetTimelineProperties();
            var total = timeline.EndTime - timeline.StartTime;
            if (total <= TimeSpan.Zero) { Progresso = 0; Posicao = Duracao = "—"; return; }
            var position = timeline.Position - timeline.StartTime;
            if (EstaTocando) position += DateTimeOffset.UtcNow - timeline.LastUpdatedTime;
            var seconds = Math.Clamp(position.TotalSeconds, 0, total.TotalSeconds);
            Progresso = seconds / total.TotalSeconds;
            Posicao = $"{(int)(seconds / 60)}:{(int)seconds % 60:00}";
            Duracao = $"{(int)total.TotalMinutes}:{total.Seconds:00}";
        }
        catch { Progresso = 0; Posicao = Duracao = "—"; }
    }

    private async Task InitializeAsync()
    {
        if (_disposed || !_escutar || _iniciando) return;
        _iniciando = true;
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_disposed || !_escutar) return;
            _sessionManager = manager;
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                _sessionManager.SessionsChanged += SessionManager_SessionsChanged;
                UpdateCurrentSession(GetBestSession());
            }
        }
        catch
        {
            // API indisponível/permissão negada: sem polling de retry.
        }
        finally { _iniciando = false; }
    }

    private GlobalSystemMediaTransportControlsSession? GetBestSession()
    {
        if (_sessionManager == null) return null;

        var sessions = _sessionManager.GetSessions();
        if (sessions == null || sessions.Count == 0) return null;

        // 1. Preferir Spotify sempre (mesmo pausado)
        foreach (var s in sessions)
        {
            if (s.SourceAppUserModelId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
                return s;
        }

        // 2. Preferir algo que esteja tocando agora
        foreach (var s in sessions)
        {
            if (s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                return s;
        }

        // 3. Pegar a sessão atual do Windows, mas IGNORAR navegadores pausados (para a dock esconder)
        var atual = _sessionManager.GetCurrentSession();
        if (atual != null)
        {
            var id = atual.SourceAppUserModelId.ToLower();
            bool isBrowser = id.Contains("chrome") || id.Contains("msedge") || id.Contains("brave") || id.Contains("firefox") || id.Contains("opera");
            var status = atual.GetPlaybackInfo()?.PlaybackStatus;
            
            if (isBrowser && status != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                return null;
            }
        }

        return atual;
    }

    private void SessionManager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        _ = RunOnUiAsync(() => { if (_escutar && !_disposed) UpdateCurrentSession(GetBestSession()); });
    }

    private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        _ = RunOnUiAsync(() => { if (_escutar && !_disposed) UpdateCurrentSession(GetBestSession()); });
    }

    private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged; _currentSession.TimelinePropertiesChanged -= Session_TimelinePropertiesChanged;
        }

        _currentSession = session;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += Session_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;
        }

        _ = UpdateMediaPropertiesAsync();
    }

    private void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    { _ = RunOnUiAsync(() => { if (_visual && !_disposed && ReferenceEquals(sender, _currentSession)) AtualizarTimeline(); }); }

    private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        _ = RunOnUiAsync(() =>
        {
            if (!_escutar || _disposed || !ReferenceEquals(sender, _currentSession)) return;
            EstaTocando = sender.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            AtualizarTimer();
        });
    }

    private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RunOnUiAsync(() => { if (ReferenceEquals(sender, _currentSession)) _ = UpdateMediaPropertiesAsync(); });
    }

    private string? _fonteNome = string.Empty;
    private string? _fonteCor = "#1DB954";
    private string? _fonteIcone = "♫";

    public string? FonteNome
    {
        get => _fonteNome;
        set => SetProperty(ref _fonteNome, value);
    }

    public string? FonteCor
    {
        get => _fonteCor;
        set => SetProperty(ref _fonteCor, value);
    }

    public string? FonteIcone
    {
        get => _fonteIcone;
        set => SetProperty(ref _fonteIcone, value);
    }

    private bool _atualizandoMidia, _atualizarNovamente;
    private async Task UpdateMediaPropertiesAsync()
    {
        if (_disposed || !_escutar) return;
        if (_atualizandoMidia) { _atualizarNovamente = true; return; }
        _atualizandoMidia = true;
        try { await LerMediaPropertiesAsync(); }
        finally
        {
            _atualizandoMidia = false;
            if (_atualizarNovamente) { _atualizarNovamente = false; if (!_disposed && _escutar) _ = UpdateMediaPropertiesAsync(); }
        }
    }
    private async Task LerMediaPropertiesAsync()
    {
        if (_disposed || !_escutar) return;
        var revisao = System.Threading.Interlocked.Increment(ref _revisaoMidia);
        var sessao = _currentSession;
        if (sessao == null)
        {
            await RunOnUiAsync(() =>
            {
                if (revisao != System.Threading.Volatile.Read(ref _revisaoMidia) || _disposed || _currentSession != null) return;
                Titulo = string.Empty;
                Artista = string.Empty;
                CapaAlbumUrl = null;
                EstaTocando = false;
                FonteNome = string.Empty;
                CorPredominanteHex = "#000000";
                AtualizarTimer();
            });
            return;
        }

        try
        {
            var properties = await sessao.TryGetMediaPropertiesAsync();
            var playbackInfo = sessao.GetPlaybackInfo();
            string sourceId = sessao.SourceAppUserModelId?.ToLower() ?? "";

            string titulo = properties?.Title ?? string.Empty;
            string artista = properties?.Artist ?? string.Empty;
            bool estaTocando = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            System.Windows.Media.Imaging.BitmapSource? capa = null;
            var chaveCapa = sourceId + "|" + titulo + "|" + artista;
            
            string cor = "#555555";
            string icone = "♫";
            string nome = sourceId;

            if (sourceId.Contains("spotify"))
            {
                cor = "#1DB954";
                icone = "♫";
                nome = "Spotify";
            }
            else if (sourceId.Contains("chrome") || sourceId.Contains("edge") || sourceId.Contains("brave") || sourceId.Contains("firefox"))
            {
                cor = "#FF0000";
                icone = "▶";
                nome = "Navegador";
                if (titulo.Contains("YouTube", StringComparison.OrdinalIgnoreCase) || artista.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
                {
                    nome = "YouTube";
                }
            }
            else if (sourceId.Contains("vlc"))
            {
                cor = "#FF8800";
                icone = "▶";
                nome = "VLC";
            }
            else if (sourceId.Contains("netflix"))
            {
                cor = "#E50914";
                icone = "N";
                nome = "Netflix";
            }

            if (_visual && _capas.TryGetValue(chaveCapa, out var existente) && DateTimeOffset.UtcNow - existente.Criada < TimeSpan.FromHours(1)) capa = existente.Imagem;
            else if (_visual && properties?.Thumbnail != null)
            {
                try
                {
                    using var stream = await properties.Thumbnail.OpenReadAsync();
                    if (stream != null && stream.Size <= 8 * 1024 * 1024)
                    {
                        using var netStream = stream.AsStreamForRead();
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit(); bitmap.StreamSource = netStream;
                        bitmap.DecodePixelWidth = 256; bitmap.DecodePixelHeight = 256;
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit(); bitmap.Freeze(); capa = bitmap;
                    }
                }
                catch { }
            }

            await RunOnUiAsync(() =>
            {
                if (revisao != System.Threading.Volatile.Read(ref _revisaoMidia) || _disposed || !_escutar || !ReferenceEquals(sessao, _currentSession)) return;
                Titulo = titulo;
                Artista = artista;
                if (_visual) { CapaAlbumUrl = capa; _chaveCapa = chaveCapa; GuardarCapa(chaveCapa, capa); }
                EstaTocando = estaTocando;
                FonteCor = cor;
                FonteIcone = icone;
                FonteNome = nome;
                
                string dominColor = "#000000";
                if (_visual && capa != null)
                {
                    try
                    {
                        var pequeno = new System.Windows.Media.Imaging.TransformedBitmap(capa,
                            new System.Windows.Media.ScaleTransform(32.0 / capa.PixelWidth, 32.0 / capa.PixelHeight));
                        var formatted = new System.Windows.Media.Imaging.FormatConvertedBitmap(pequeno, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                        int bwidth = formatted.PixelWidth;
                        int bheight = formatted.PixelHeight;
                        int bytesPerPixel = 4;
                        byte[] pixels = new byte[bwidth * bheight * bytesPerPixel];
                        formatted.CopyPixels(pixels, bwidth * bytesPerPixel, 0);
                        // Agrupa cores semelhantes para preservar a identidade da capa,
                        // sem deixar fundos pretos/brancos dominarem a m?dia.
                        var grupos = new Dictionary<int, (double Peso, double R, double G, double B)>();
                        for (int i = 0; i < pixels.Length; i += bytesPerPixel)
                        {
                            if (pixels[i + 3] < 128) continue;
                            int r = pixels[i + 2], g = pixels[i + 1], b = pixels[i];
                            int max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
                            if (max < 32 || min > 225) continue;
                            double peso = .25 + (max - min) / 255.0;
                            int chave = ((r / 32) << 6) | ((g / 32) << 3) | (b / 32);
                            grupos.TryGetValue(chave, out var grupo);
                            grupos[chave] = (grupo.Peso + peso, grupo.R + r * peso, grupo.G + g * peso, grupo.B + b * peso);
                        }
                        if (grupos.Count > 0)
                        {
                            var dominante = grupos.Values.MaxBy(v => v.Peso);
                            dominColor = $"#{(byte)(dominante.R/dominante.Peso):X2}{(byte)(dominante.G/dominante.Peso):X2}{(byte)(dominante.B/dominante.Peso):X2}";
                        }
                    }
                    catch { }
                }
                if (_visual) CorPredominanteHex = dominColor;
                AtualizarTimer();
            });
        }
        catch
        {
            await RunOnUiAsync(() =>
            {
                if (revisao != System.Threading.Volatile.Read(ref _revisaoMidia) || _disposed) return;
                Titulo = string.Empty;
                Artista = string.Empty;
                CapaAlbumUrl = null;
                EstaTocando = false;
                FonteNome = string.Empty;
                CorPredominanteHex = "#000000";
            });
        }
    }

            private void AbrirPlayer()
    {
        if (!_canOpenPlayer()) return;

        string sourceId = _currentSession?.SourceAppUserModelId ?? "";
        if (string.IsNullOrEmpty(sourceId)) return;

        try
        {
            // Tenta abrir/restaurar via esquema URI ou AUMID (funciona perfeito para Spotify Store e UWP)
            if (sourceId.ToLower().Contains("spotify"))
            {
                Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true });
                return;
            }
            
            Process.Start(new ProcessStartInfo($"shell:AppsFolder\\{sourceId}") { UseShellExecute = true });
        }
        catch { }

        // Fallback Win32 caso a tentativa UWP/URI falhe
        string lowerId = sourceId.ToLower();
        string procName = "";

        if (lowerId.Contains("spotify")) procName = "Spotify";
        else if (lowerId.Contains("chrome")) procName = "chrome";
        else if (lowerId.Contains("edge")) procName = "msedge";
        else if (lowerId.Contains("brave")) procName = "brave";
        else if (lowerId.Contains("firefox")) procName = "firefox";
        else if (lowerId.Contains("opera")) procName = "opera";
        else if (lowerId.Contains("vlc")) procName = "vlc";
        
        if (string.IsNullOrEmpty(procName)) return;

        var procs = Process.GetProcessesByName(procName);
        foreach (var p in procs)
        {
            if (p.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(p.MainWindowHandle, 9); // SW_RESTORE = 9
                SetForegroundWindow(p.MainWindowHandle);
                return;
            }
        }
    }



    private async Task TogglePlayPauseAsync()
    {
        if (_currentSession != null)
        {
            await _currentSession.TryTogglePlayPauseAsync();
        }
    }

    private async Task SkipPreviousAsync()
    {
        if (_currentSession != null)
        {
            await _currentSession.TrySkipPreviousAsync();
        }
    }

    private async Task SkipNextAsync()
    {
        if (_currentSession != null)
        {
            await _currentSession.TrySkipNextAsync();
        }
    }

    private Task RunOnUiAsync(Action action)
    {
        if (Application.Current?.Dispatcher != null)
        {
            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }
        
        action();
        return Task.CompletedTask;
    }
}






