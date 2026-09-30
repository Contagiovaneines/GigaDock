using System;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class MidiaWidgetViewModel : ObservableObject
{
    private string _titulo = "Mulberry Street";
    private string _artista = "Twenty One Pilots";
    private string _capaAlbumUrl = "https://picsum.photos/300/100"; // Mock album art
    private bool _estaTocando = true;
    
    public string Titulo
    {
        get => _titulo;
        set => SetProperty(ref _titulo, value);
    }
    
    public string Artista
    {
        get => _artista;
        set => SetProperty(ref _artista, value);
    }
    
    public string CapaAlbumUrl
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
        PlayPauseCommand = new RelayCommand(() => EstaTocando = !EstaTocando);
        AnteriorCommand = new RelayCommand(() => Titulo = "Música Anterior");
        ProximoCommand = new RelayCommand(() => Titulo = "Próxima Música");
    }
}
