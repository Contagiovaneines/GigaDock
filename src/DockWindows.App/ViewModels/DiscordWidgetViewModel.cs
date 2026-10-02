using DockWindows.App.Common;
using DockWindows.Core.Models;
using System.Collections.ObjectModel;

namespace DockWindows.App.ViewModels;

public class DiscordUsuario : ObservableObject
{
    public string Nome { get; set; } = string.Empty;
    public string AvatarInicial { get; set; } = string.Empty;
    public string CorAvatar { get; set; } = "#5865F2";
    
    private bool _estaFalando;
    public bool EstaFalando { get => _estaFalando; set => SetProperty(ref _estaFalando, value); }
}

public class DiscordWidgetViewModel : ObservableObject
{
    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private string _salaVoz = "Desconectado";
    private bool _estaEmCall = false;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public string SalaVoz { get => _salaVoz; set => SetProperty(ref _salaVoz, value); }
    public bool EstaEmCall { get => _estaEmCall; set => SetProperty(ref _estaEmCall, value); }
    
    public ObservableCollection<DiscordUsuario> UsuariosNaCall { get; } = new();

    public DiscordWidgetViewModel()
    {
        // Mock inicial
        EstaEmCall = true;
        SalaVoz = "Jogatina #Geral";
        UsuariosNaCall.Add(new DiscordUsuario { Nome = "Giovane", AvatarInicial = "G", CorAvatar = "#5865F2", EstaFalando = true });
        UsuariosNaCall.Add(new DiscordUsuario { Nome = "Alex", AvatarInicial = "A", CorAvatar = "#ED4245", EstaFalando = false });
    }
}
