using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Media.Control;
using Windows.Storage.Streams;
using DockWindows.App.Common;

namespace DockWindows.App.ViewModels;

public class MidiaWidgetViewModel : ObservableObject
{
    private string? _titulo = string.Empty;
    private string? _artista = string.Empty;
    private string? _capaAlbumUrl = string.Empty;
    private bool _estaTocando;
    
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
    
    public bool TemMidia => !string.IsNullOrEmpty(_titulo);
    
    public string? Artista
    {
        get => _artista;
        set => SetProperty(ref _artista, value);
    }
    
    public string? CapaAlbumUrl
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

    public MidiaWidgetViewModel()
    {
        PlayPauseCommand = new RelayCommand(() => _ = TogglePlayPauseAsync());
        AnteriorCommand = new RelayCommand(() => _ = SkipPreviousAsync());
        ProximoCommand = new RelayCommand(() => _ = SkipNextAsync());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                _sessionManager.SessionsChanged += SessionManager_SessionsChanged;
                UpdateCurrentSession(GetBestSession());
            }
        }
        catch
        {
            // Ignorar erros de inicialização caso a API não esteja disponível ou haja erro de permissão
        }
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
        UpdateCurrentSession(GetBestSession());
    }

    private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession(GetBestSession());
    }

    private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
        }

        _currentSession = session;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += Session_PlaybackInfoChanged;
        }

        _ = UpdateMediaPropertiesAsync();
    }

    private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        _ = UpdateMediaPropertiesAsync();
    }

    private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = UpdateMediaPropertiesAsync();
    }

    private async Task UpdateMediaPropertiesAsync()
    {
        if (_currentSession == null)
        {
            await RunOnUiAsync(() =>
            {
                Titulo = string.Empty;
                Artista = string.Empty;
                CapaAlbumUrl = string.Empty;
                EstaTocando = false;
            });
            return;
        }

        try
        {
            var properties = await _currentSession.TryGetMediaPropertiesAsync();
            var playbackInfo = _currentSession.GetPlaybackInfo();

            string titulo = properties?.Title ?? string.Empty;
            string artista = properties?.Artist ?? string.Empty;
            bool estaTocando = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            string capaPath = string.Empty;

            if (properties?.Thumbnail != null)
            {
                try
                {
                    using var stream = await properties.Thumbnail.OpenReadAsync();
                    if (stream != null)
                    {
                        capaPath = Path.Combine(Path.GetTempPath(), $"dockwindows_media_thumb_{Guid.NewGuid():N}.jpg");
                        using var fileStream = File.Create(capaPath);
                        using var netStream = stream.AsStreamForRead();
                        await netStream.CopyToAsync(fileStream);
                    }
                }
                catch
                {
                    // Falha ao carregar thumbnail, manter em branco
                }
            }

            await RunOnUiAsync(() =>
            {
                Titulo = titulo;
                Artista = artista;
                CapaAlbumUrl = capaPath;
                EstaTocando = estaTocando;
            });
        }
        catch
        {
            await RunOnUiAsync(() =>
            {
                Titulo = string.Empty;
                Artista = string.Empty;
                CapaAlbumUrl = string.Empty;
                EstaTocando = false;
            });
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
