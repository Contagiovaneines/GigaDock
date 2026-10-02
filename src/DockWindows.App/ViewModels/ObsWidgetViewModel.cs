using DockWindows.App.Common;
using System.Diagnostics;
using System.Windows.Input;

namespace DockWindows.App.ViewModels
{
    public class ObsWidgetViewModel : ObservableObject
    {
        private bool _habilitado;
        public bool Habilitado
        {
            get => _habilitado;
            set => SetProperty(ref _habilitado, value);
        }

        private bool _estaGravando;
        public bool EstaGravando
        {
            get => _estaGravando;
            set
            {
                if (SetProperty(ref _estaGravando, value))
                {
                    OnPropertyChanged(nameof(TextoBotaoGravar));
                    OnPropertyChanged(nameof(CorBotaoGravar));
                    OnPropertyChanged(nameof(CorGlowGravar));
                }
            }
        }

        public string TextoBotaoGravar => EstaGravando ? "Parar" : "Gravar";
        public string CorBotaoGravar => EstaGravando ? "#FF3B30" : "#8E8E93"; 
        public string CorGlowGravar => EstaGravando ? "#FF3B30" : "Transparent";

        public ICommand AbrirAppCommand { get; }
        public ICommand AlternarGravacaoCommand { get; }

        public ObsWidgetViewModel()
        {
            Habilitado = true; // Habilitado por padrão para testes

            AbrirAppCommand = new RelayCommand(AbrirObs);
            AlternarGravacaoCommand = new RelayCommand(AlternarGravacao);
        }

        private void AbrirObs()
        {
            try
            {
                Process.Start(new ProcessStartInfo("obs64.exe") { UseShellExecute = true });
            }
            catch
            {
                try
                {
                    // Fallback
                    Process.Start(new ProcessStartInfo("obs-studio://") { UseShellExecute = true });
                }
                catch
                {
                    // Ignora erro
                }
            }
        }

        private void AlternarGravacao()
        {
            EstaGravando = !EstaGravando;
            // TODO: Integrar OBS WebSocket no futuro para acionar gravao real
        }
    }
}
