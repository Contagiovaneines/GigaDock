using System.Windows.Input;
using System.Windows.Media;
using DockWindows.App.Common;

namespace DockWindows.App.ViewModels;

public sealed class AplicativoSegundoPlanoViewModel
{
    public AplicativoSegundoPlanoViewModel(string nome, string caminho, bool possuiJanela, ImageSource? icone, Action abrir)
    {
        Nome = nome;
        Caminho = caminho;
        PossuiJanela = possuiJanela;
        Icone = icone;
        AbrirCommand = new RelayCommand(abrir);
    }

    public string Nome { get; }
    public string Caminho { get; }
    public bool PossuiJanela { get; }
    public ImageSource? Icone { get; }
    public ICommand AbrirCommand { get; }
}
