using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.App.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;
    private readonly IAutostartService _autostartService;
    private readonly ISettingsRepository _settingsRepo;

    private bool _iniciarComWindows;
    private Ambiente? _ambienteSelecionado;

    public SettingsViewModel(MainViewModel mainVm, IAutostartService autostartService, ISettingsRepository settingsRepo)
    {
        _mainVm = mainVm;
        _autostartService = autostartService;
        _settingsRepo = settingsRepo;

        _iniciarComWindows = _autostartService.EstaHabilitado();

        Ambientes = new ObservableCollection<Ambiente>(_mainVm.Preferencias.Ambientes);
        AmbienteSelecionado = Ambientes.FirstOrDefault(a => a.Id == _mainVm.AmbienteAtivo?.Id) ?? Ambientes.FirstOrDefault();

        AbrirPastaConfiguracoesCommand = new RelayCommand(AbrirPastaConfiguracoes);
        SalvarEFecharCommand = new RelayCommand(SalvarEFechar);
        AbrirPersonalizarCommand = new RelayCommand(() =>
        {
            FecharJanela?.Invoke();
            _mainVm.AbrirPersonalizarCommand.Execute(null);
        });
        RestaurarBarraWindowsCommand = new RelayCommand(() =>
        {
            _mainVm.RestaurarBarraWindows();
            OnPropertyChanged(nameof(UsarComoBarraPrincipal));
        });
    }

    public ObservableCollection<Ambiente> Ambientes { get; }

    public Ambiente? AmbienteSelecionado
    {
        get => _ambienteSelecionado;
        set => SetProperty(ref _ambienteSelecionado, value);
    }

    public bool IniciarComWindows
    {
        get => _iniciarComWindows;
        set
        {
            if (SetProperty(ref _iniciarComWindows, value))
            {
                _autostartService.Configurar(value);
                _mainVm.Preferencias.IniciarComWindows = value;
            }
        }
    }

    public bool UsarComoBarraPrincipal
    {
        get => _mainVm.UsarComoBarraPrincipal;
        set
        {
            _mainVm.UsarComoBarraPrincipal = value;
            OnPropertyChanged();
        }
    }

    public bool ExibirSeletorAmbientes
    {
        get => _mainVm.ExibirSeletorAmbientes;
        set
        {
            _mainVm.ExibirSeletorAmbientes = value;
            OnPropertyChanged();
        }
    }

    public bool DesativarAnimacoes
    {
        get => _mainVm.DesativarAnimacoes;
        set
        {
            _mainVm.DesativarAnimacoes = value;
            OnPropertyChanged();
        }
    }

    public TemaModo Tema
    {
        get => _mainVm.Tema;
        set
        {
            _mainVm.Tema = value;
            OnPropertyChanged();
        }
    }

    public TamanhoIcone TamanhoIcones
    {
        get => _mainVm.TamanhoIcones;
        set
        {
            _mainVm.TamanhoIcones = value;
            OnPropertyChanged();
        }
    }

    public double OpacidadeDock
    {
        get => _mainVm.OpacidadeDock;
        set
        {
            _mainVm.OpacidadeDock = value;
            OnPropertyChanged();
        }
    }

    public bool SempreNoTopo
    {
        get => _mainVm.SempreNoTopo;
        set
        {
            _mainVm.SempreNoTopo = value;
            OnPropertyChanged();
        }
    }

    public bool OcultarAutomaticamente
    {
        get => _mainVm.OcultarAutomaticamente;
        set
        {
            _mainVm.OcultarAutomaticamente = value;
            OnPropertyChanged();
        }
    }

    public string CaminhoArquivoConfig => _settingsRepo.ObterCaminhoConfiguracoes();

    public ICommand AbrirPastaConfiguracoesCommand { get; }
    public ICommand SalvarEFecharCommand { get; }
    public ICommand AbrirPersonalizarCommand { get; }
    public ICommand RestaurarBarraWindowsCommand { get; }

    public Action? FecharJanela { get; set; }

    private void AbrirPastaConfiguracoes()
    {
        try
        {
            var dir = Path.GetDirectoryName(CaminhoArquivoConfig);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{dir}\"",
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    private void SalvarEFechar()
    {
        _mainVm.SalvarPreferencias();
        FecharJanela?.Invoke();
    }
}
