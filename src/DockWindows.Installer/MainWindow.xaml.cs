using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DockWindows.Installer.Services;

namespace DockWindows.Installer;

public partial class MainWindow : Window
{
    private readonly InstallService _installService;
    private readonly bool _modoDesinstalacao;
    private string _pastaInstalacao;

    private enum EstadoWizard
    {
        Opcoes,
        Progresso,
        Concluido
    }

    private EstadoWizard _estadoAtual = EstadoWizard.Opcoes;

    public MainWindow()
    {
        InitializeComponent();

        _installService = new InstallService();
        _pastaInstalacao = _installService.ObterDiretorioInstalacaoPadrao();
        TxtCaminhoDestino.Text = _pastaInstalacao;

        var args = Environment.GetCommandLineArgs();
        _modoDesinstalacao = Array.Exists(args, a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) || a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase));

        ConfigurarInterfaceInicial();
        Closing += (_, e) => { if (_estadoAtual == EstadoWizard.Progresso) e.Cancel = true; };
        Loaded += (_, _) =>
        {
            Width = Math.Max(MinWidth, Math.Min(Width, SystemParameters.WorkArea.Width - 32));
            Height = Math.Max(MinHeight, Math.Min(Height, SystemParameters.WorkArea.Height - 32));
        };
    }

    private void ConfigurarInterfaceInicial()
    {
        if (_modoDesinstalacao)
        {
            Title = "Desinstalador do GigaDock";
            TxtTituloCabecalho.Text = "GigaDock â€” Assistente de DesinstalaÃ§Ã£o";
            TxtSubtituloCabecalho.Text = "RemoÃ§Ã£o segura e restauraÃ§Ã£o do Windows";

            PanelOpcoesInstalacao.Visibility = Visibility.Collapsed;
            PanelDesinstalacao.Visibility = Visibility.Visible;
            PanelProgresso.Visibility = Visibility.Collapsed;
            PanelConcluido.Visibility = Visibility.Collapsed;

            BtnAcaoPrincipal.Content = "Desinstalar";
            BtnAcaoPrincipal.Background = new SolidColorBrush(Color.FromRgb(255, 69, 58));
        }
        else
        {
            bool ehAtualizacao = _installService.DetectarInstalacaoExistente(out var versaoInstalada, out var pastaInstalada, out bool iniciaComWindows);

            if (ehAtualizacao)
            {
                _pastaInstalacao = pastaInstalada;
                TxtCaminhoDestino.Text = _pastaInstalacao;
                ChkIniciarComWindows.IsChecked = iniciaComWindows;

                Title = "Atualizador do GigaDock";
                TxtTituloCabecalho.Text = "GigaDock â€” Assistente de AtualizaÃ§Ã£o";
                TxtSubtituloCabecalho.Text = $"VersÃ£o {versaoInstalada} detectada -> Atualizar para {InstallService.CurrentVersion}";

                TxtTituloCabecalho.Text = "AtualizaÃ§Ã£o do Aplicativo";
                TxtDescricaoAcao.Text = "Uma instalaÃ§Ã£o anterior do GigaDock foi detectada. Seus ambientes, atalhos e preferÃªncias serÃ£o totalmente preservados.";
                BtnAcaoPrincipal.Content = "Atualizar Agora";
            }
            else
            {
                Title = "Instalador do GigaDock";
                TxtTituloCabecalho.Text = "GigaDock â€” Assistente de InstalaÃ§Ã£o";
                TxtSubtituloCabecalho.Text = $"VersÃ£o {InstallService.CurrentVersion} (Windows 10/11 x64)";

                TxtTituloCabecalho.Text = "InstalaÃ§Ã£o do Aplicativo";
                TxtDescricaoAcao.Text = "O GigaDock serÃ¡ instalado localmente no perfil do seu usuÃ¡rio sem exigir privilÃ©gios de administrador.";
                BtnAcaoPrincipal.Content = "Instalar";
            }

            PanelOpcoesInstalacao.Visibility = Visibility.Visible;
            PanelDesinstalacao.Visibility = Visibility.Collapsed;
            PanelProgresso.Visibility = Visibility.Collapsed;
            PanelConcluido.Visibility = Visibility.Collapsed;
        }
    }

    private void DragWindow(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); }
        private void BtnSair_Click(object sender, RoutedEventArgs e) { Close(); }

        private async void BtnAcaoPrincipal_Click(object sender, RoutedEventArgs e)
    {
        if (_modoDesinstalacao)
        {
            await ExecutarDesinstalacaoAsync();
        }
        else
        {
            if (_estadoAtual == EstadoWizard.Opcoes)
            {
                await ExecutarInstalacaoAsync();
            }
            else if (_estadoAtual == EstadoWizard.Concluido)
            {
                if (ChkExecutarAgora.IsChecked == true)
                {
                    try
                    {
                        var exePath = Path.Combine(_pastaInstalacao, "DockWindows.App.exe");
                        if (File.Exists(exePath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = exePath,
                                WorkingDirectory = _pastaInstalacao,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        var bloqueioPolitica = ex is System.ComponentModel.Win32Exception win32 && win32.NativeErrorCode is 1260 or 577
                            || ex.Message.Contains("Controle de Aplicativo", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("Application Control", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("policy", StringComparison.OrdinalIgnoreCase);
                        var mensagem = bloqueioPolitica
                            ? "O GigaDock foi instalado com sucesso.\n\nNo entanto, o Windows Smart App Control impediu que ele fosse iniciado automaticamente por ser uma versão de desenvolvimento com assinatura local.\n\nPara desenvolvedores: inicie-o pelo Menu Iniciar, ou instale o certificado GigaDock OpenSource nos confiáveis do Windows."
                            : $"O GigaDock foi instalado, mas nÃ£o foi possÃ­vel iniciÃ¡-lo.\n\nDetalhes: {ex.Message}";
                        MessageBox.Show(this, mensagem, bloqueioPolitica ? "GigaDock Instalado" : "Não foi possível abrir o GigaDock", MessageBoxButton.OK, bloqueioPolitica ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    }
                }
                Close();
            }
        }
    }

    private async Task ExecutarInstalacaoAsync()
    {
        _estadoAtual = EstadoWizard.Progresso;

        PanelOpcoesInstalacao.Visibility = Visibility.Collapsed;
        PanelProgresso.Visibility = Visibility.Visible;
        BtnCancelar.Visibility = Visibility.Collapsed;
        BtnAcaoPrincipal.IsEnabled = false;

        bool criarIniciar = ChkAtalhoIniciar.IsChecked == true;
        bool criarDesktop = ChkAtalhoDesktop.IsChecked == true;
        bool autostart = ChkIniciarComWindows.IsChecked == true;

        bool sucesso = false;
        string? erroDetalhado = null;

        await Task.Run(() =>
        {
            sucesso = _installService.ExecutarInstalacao(
                _pastaInstalacao,
                criarIniciar,
                criarDesktop,
                autostart,
                notificarProgresso: msg =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtStatusProgresso.Text = msg;
                    });
                },
                out erroDetalhado);
        });

        if (sucesso)
        {
            _estadoAtual = EstadoWizard.Concluido;
            PanelProgresso.Visibility = Visibility.Collapsed;
            PanelConcluido.Visibility = Visibility.Visible;

            bool foiAtualizacao = _installService.DetectarInstalacaoExistente(out _, out _, out _);
            if (foiAtualizacao)
            {
                TxtTituloConcluido.Text = "AtualizaÃ§Ã£o ConcluÃ­da com Sucesso!";
                TxtSubtituloConcluido.Text = $"O GigaDock foi atualizado para a versÃ£o {InstallService.CurrentVersion} com todas as suas preferÃªncias preservadas.";
            }

            BtnAcaoPrincipal.Content = "Concluir";
            BtnAcaoPrincipal.IsEnabled = true;
            BtnAcaoPrincipal.Background = new SolidColorBrush(Color.FromRgb(48, 209, 88));
        }
        else
        {
            var msgExibir = !string.IsNullOrWhiteSpace(erroDetalhado)
                ? $"Ocorreu um erro durante a instalaÃ§Ã£o:\n\n{erroDetalhado}\n\nVerifique se o aplicativo nÃ£o estÃ¡ em execuÃ§Ã£o ou permissÃµes de pasta."
                : "Ocorreu um erro durante a instalaÃ§Ã£o. Verifique se o aplicativo nÃ£o estÃ¡ em execuÃ§Ã£o ou permissÃµes de pasta.";

            MessageBox.Show(this, msgExibir, "Erro na InstalaÃ§Ã£o", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnCancelar.Visibility = Visibility.Visible;
            BtnAcaoPrincipal.IsEnabled = true;
            BtnAcaoPrincipal.Content = "Tentar Novamente";
            _estadoAtual = EstadoWizard.Opcoes;
            PanelProgresso.Visibility = Visibility.Collapsed;
            PanelOpcoesInstalacao.Visibility = Visibility.Visible;
        }
    }

    private async Task ExecutarDesinstalacaoAsync()
    {
        var resultado = MessageBox.Show(this,
            "Deseja realmente prosseguir com a desinstalaÃ§Ã£o do GigaDock?",
            "Confirmar DesinstalaÃ§Ã£o",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (resultado != MessageBoxResult.Yes)
        {
            return;
        }

        _estadoAtual = EstadoWizard.Progresso;
        PanelDesinstalacao.Visibility = Visibility.Collapsed;
        PanelProgresso.Visibility = Visibility.Visible;
        BtnCancelar.Visibility = Visibility.Collapsed;
        BtnAcaoPrincipal.IsEnabled = false;

        bool removerDados = ChkRemoverDadosPessoais.IsChecked == true;
        bool sucesso = false;

        await Task.Run(() =>
        {
            sucesso = _installService.ExecutarDesinstalacao(
                removerDados,
                notificarProgresso: msg =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtStatusProgresso.Text = msg;
                    });
                });
        });

        _estadoAtual = sucesso ? EstadoWizard.Concluido : EstadoWizard.Opcoes;
        if (sucesso)
        {
            MessageBox.Show(this, "GigaDock foi desinstalado com sucesso do seu computador.\nA barra de tarefas nativa do Windows foi restaurada.", "DesinstalaÃ§Ã£o ConcluÃ­da", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        else
        {
            MessageBox.Show(this, "Houve um problema durante a desinstalaÃ§Ã£o de alguns arquivos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}





