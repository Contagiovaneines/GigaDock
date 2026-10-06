using DockWindows.App.Common;
using System.Diagnostics;
using System.Windows.Input;

namespace DockWindows.App.ViewModels
{
    public class ObsWidgetViewModel : ObservableObject, IAtividadeWidget
    {
    public bool? EmExecucao => false;

    public DockWindows.Core.Widgets.SaudeWidget Saude => DockWindows.Core.Widgets.SaudeWidget.Indisponivel;
    public string? MotivoEstado => EstadoIntegracao;

        private bool _habilitado;
        public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado) { }
        public string EstadoIntegracao => "Indisponível: controle de gravação requer integração OBS WebSocket.";
        public string ErroIntegracao { get; private set; } = "";
    public void Dispose() { }
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

        public string TextoBotaoGravar => EstaGravando ? "Parar" : "Sem conexão";
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
                    ErroIntegracao = "Não foi possível abrir o OBS. Verifique se está instalado.";
                    OnPropertyChanged(nameof(ErroIntegracao));
                }
            }
        }

        private void AlternarGravacao()
        {
            ErroIntegracao = EstadoIntegracao;
            OnPropertyChanged(nameof(ErroIntegracao));
            // Não confirmar gravação sem resposta do OBS.
        }
    }
}
