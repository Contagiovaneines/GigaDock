using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;
using System.Collections.ObjectModel;
using System.Linq;

namespace DockWindows.App.ViewModels;

// A classe DiscordUsuario já estava definida aqui antes, mantenho:
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
    private string _salaVoz = "Conectando...";
    private bool _estaEmCall = false;
    private readonly DiscordIpcService _service;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public string SalaVoz { get => _salaVoz; set => SetProperty(ref _salaVoz, value); }
    public bool EstaEmCall { get => _estaEmCall; set => SetProperty(ref _estaEmCall, value); }
    
    public ObservableCollection<DiscordUsuario> UsuariosNaCall { get; } = new();

        public System.Windows.Input.ICommand TestarAlertaCommand { get; }
    public System.Windows.Input.ICommand AbrirAppCommand { get; }

    public DiscordWidgetViewModel(System.Action<string>? onAlerta = null)
    {
        TestarAlertaCommand = new RelayCommand(() => onAlerta?.Invoke("#5865F2"));
        AbrirAppCommand = new RelayCommand(() => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "discord:", UseShellExecute = true }); } catch { } });
        _service = new DiscordIpcService();
        _service.OnCanalVozAlterado += (sala) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                SalaVoz = sala;
                EstaEmCall = true;
            });
        };
        _service.OnUsuarioFlando += (nome, inicial, cor, falando) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                var usr = UsuariosNaCall.FirstOrDefault(u => u.Nome == nome);
                if (usr != null)
                {
                    usr.EstaFalando = falando;
                }
                else
                {
                    UsuariosNaCall.Add(new DiscordUsuario { Nome = nome, AvatarInicial = inicial, CorAvatar = cor, EstaFalando = falando });
                }
            });
        };
        _service.Iniciar();
    }
}






