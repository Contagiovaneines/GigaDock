using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public sealed class NotificationCardViewModel(DockNotification item, ImageSource? icon, Action<uint> remove)
{
    public uint Id => item.Id;
    public string Application => item.Application;
    public string Title => item.Title;
    public string Body => item.Body;
    public string Time => item.CreatedAt.ToLocalTime().ToString("HH:mm");
    public DateTimeOffset CreatedAt => item.CreatedAt;
    public string Initial => item.Application.Length == 0 ? "•" : item.Application[..1];
    public ImageSource? Icon => icon;
    public ICommand RemoveCommand { get; } = new RelayCommand(() => remove(item.Id));
}

public sealed class NotificationCenterViewModel : ObservableObject, IDisposable
{
    private readonly INotificationCenterSource _source;
    private readonly IIconExtractionService _icons;
    private readonly Func<bool> _confirmClear;
    private readonly DispatcherTimer _timer;
    private bool _open, _busy, _disposed;
    private string _status = "Ative o acesso para mostrar as notificações do Windows aqui.";
    public ObservableCollection<NotificationCardViewModel> Items { get; } = new();
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool CanActivate => _source.HasPackageIdentity;
    public bool NeedsActivation => _source.EstadoPermissao != "Permitida";
    public bool IsBusy { get => _busy; private set => SetProperty(ref _busy, value); }
    public int Count => Items.Count;
    public bool HasItems => Count != 0;
    public bool IsEmpty => !HasItems;
    public string Badge => Count > 99 ? "99+" : Count.ToString();
    public bool IsOpen { get => _open; set { if (SetProperty(ref _open, value) && value) _ = RefreshAsync(); } }
    public ICommand OpenCommand { get; }
    public ICommand ActivateCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearCommand { get; }

    public NotificationCenterViewModel(INotificationCenterSource source, IIconExtractionService icons, Func<bool> confirmClear)
    {
        _source = source; _icons = icons; _confirmClear = confirmClear;
        OpenCommand = new RelayCommand(() => IsOpen = !IsOpen);
        ActivateCommand = new RelayCommand(() => _ = ActivateAsync(), () => !_busy && CanActivate);
        RefreshCommand = new RelayCommand(() => _ = RefreshAsync());
        ClearCommand = new RelayCommand(() => _ = ClearAsync(), () => !_busy && HasItems);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += TimerTick;
    }

    private void TimerTick(object? sender, EventArgs args) => _ = RefreshAsync();
    public async Task ActivateAsync()
    {
        if (_disposed || IsBusy || !CanActivate) return;
        IsBusy = true;
        try { await _source.RequestAccessAsync(); }
        catch { Status = "O Windows não conseguiu autorizar o acesso. Confira as permissões nas configurações."; }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_disposed || IsBusy) return;
        IsBusy = true;
        try
        {
            var entries = await _source.ReadAsync();
            if (_disposed) return;
            var normalized = entries.OrderByDescending(n => n.CreatedAt).Take(200).ToArray();
            if (!Items.Select(i => (i.Id, i.Title, i.Body, i.Application, i.CreatedAt)).SequenceEqual(
                normalized.Select(i => (i.Id, i.Title, i.Body, i.Application, i.CreatedAt))))
            {
                Items.Clear();
                foreach (var entry in normalized)
                    Items.Add(new(entry, _icons.ObterIcone(InstalledAppsScanner.PrefixoAppsFolder + entry.ApplicationId, TipoItem.Aplicativo),
                        id => _ = RemoveAsync(id)));
            }
            Status = _source.EstadoPermissao == "Permitida"
                ? (HasItems ? "Remover aqui também remove da central do Windows." : "Nenhuma notificação no momento.")
                : _source.EstadoPermissao;
            if (_source.EstadoPermissao == "Permitida") _timer.Start(); else _timer.Stop();
        }
        catch
        {
            Items.Clear(); _timer.Stop();
            Status = "Não foi possível ler as notificações. Confira a identidade da instalação e a permissão do Windows.";
        }
        finally
        {
            IsBusy = false;
            foreach (var name in new[] { nameof(Count), nameof(HasItems), nameof(IsEmpty), nameof(Badge), nameof(CanActivate), nameof(NeedsActivation) }) OnPropertyChanged(name);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public async Task RemoveAsync(uint id)
    {
        if (_disposed || IsBusy) return;
        try { if (!_source.Remove(id)) { await RefreshAsync(); return; } }
        catch { Status = "Não foi possível remover a notificação."; return; }
        await RefreshAsync();
    }

    public async Task ClearAsync()
    {
        if (_disposed || IsBusy || !HasItems || !_confirmClear()) return;
        try { if (!_source.ClearAll()) { await RefreshAsync(); return; } }
        catch { Status = "Não foi possível limpar as notificações."; return; }
        await RefreshAsync();
    }

    public void Dispose() { _disposed = true; _timer.Stop(); _timer.Tick -= TimerTick; Items.Clear(); }
}
