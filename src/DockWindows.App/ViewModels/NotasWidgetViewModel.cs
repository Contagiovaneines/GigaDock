using System.Windows.Threading;
using System.Windows.Input;
using System.IO;
using System.Linq;
using DockWindows.App.Common;

namespace DockWindows.App.ViewModels;

public class NotasWidgetViewModel : ObservableObject
{
    private string _textoNotas = string.Empty;
    private bool _painelAberto;
    private bool _habilitado;
    private DockWindows.Core.Models.FormatoWidget _formato = DockWindows.Core.Models.FormatoWidget.Expandido;
    private readonly DispatcherTimer _timerSalvar;
    private bool _pendenteSalvar;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public DockWindows.Core.Models.FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }

    public string TextoNotas
    {
        get => _textoNotas;
        set
        {
            if (SetProperty(ref _textoNotas, value))
            {
                _pendenteSalvar = true;
                _timerSalvar.Stop();
                _timerSalvar.Start(); // Salva após 2s sem digitar
                OnPropertyChanged(nameof(ResumoNotas));
            }
        }
    }

    public string ResumoNotas
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_textoNotas)) return "📝 Sem anotações";
            var primeira = _textoNotas.Split('\n', '\r').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
            return primeira.Length > 25 ? "📝 " + primeira[..22] + "..." : "📝 " + primeira;
        }
    }

    public bool PainelAberto
    {
        get => _painelAberto;
        set => SetProperty(ref _painelAberto, value);
    }

    public ICommand AlternarPainelCommand { get; }

    public NotasWidgetViewModel()
    {
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);

        _timerSalvar = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timerSalvar.Tick += (_, _) =>
        {
            _timerSalvar.Stop();
            if (_pendenteSalvar)
            {
                _pendenteSalvar = false;
                SalvarNotas();
            }
        };

        CarregarNotas();
    }

    private void CarregarNotas()
    {
        try
        {
            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DockWindows");
            var arquivo = Path.Combine(pasta, "notas.txt");
            if (File.Exists(arquivo))
            {
                _textoNotas = File.ReadAllText(arquivo);
                OnPropertyChanged(nameof(TextoNotas));
                OnPropertyChanged(nameof(ResumoNotas));
            }
        }
        catch { /* Silencioso se falhar ao carregar */ }
    }

    private void SalvarNotas()
    {
        try
        {
            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DockWindows");
            Directory.CreateDirectory(pasta);
            var arquivo = Path.Combine(pasta, "notas.txt");
            File.WriteAllText(arquivo, _textoNotas);
        }
        catch { /* Silencioso se falhar ao salvar */ }
    }
}



