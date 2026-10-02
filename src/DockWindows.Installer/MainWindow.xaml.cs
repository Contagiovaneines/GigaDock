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
    }

    private void ConfigurarInterfaceInicial()
    {
        if (_modoDesinstalacao)
        {
            Title = "Desinstalador do GigaDock";
            TxtTituloCabecalho.Text = "GigaDock — Assistente de Desinstalação";
            TxtSubtituloCabecalho.Text = "Remoção segura e restauração do Windows";

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
                TxtTituloCabecalho.Text = "GigaDock — Assistente de Atualização";
                TxtSubtituloCabecalho.Text = $"Versão {versaoInstalada} detectada -> Atualizar para {InstallService.CurrentVersion}";

                TxtTituloCabecalho.Text = "Atualização do Aplicativo";
                TxtDescricaoAcao.Text = "Uma instalação anterior do GigaDock foi detectada. Seus ambientes, atalhos e preferências serão totalmente preservados.";
                BtnAcaoPrincipal.Content = "Atualizar Agora";
            }
            else
            {
                Title = "Instalador do GigaDock";
                TxtTituloCabecalho.Text = "GigaDock — Assistente de Instalação";
                TxtSubtituloCabecalho.Text = $"Versão {InstallService.CurrentVersion} (Windows 10/11 x64)";

                TxtTituloCabecalho.Text = "Instalação do Aplicativo";
                TxtDescricaoAcao.Text = "O GigaDock será instalado localmente no perfil do seu usuário sem exigir privilégios de administrador.";
                BtnAcaoPrincipal.Content = "Instalar";
            }

            PanelOpcoesInstalacao.Visibility = Visibility.Visible;
            PanelDesinstalacao.Visibility = Visibility.Collapsed;
            PanelProgresso.Visibility = Visibility.Collapsed;
            PanelConcluido.Visibility = Visibility.Collapsed;
        }
    }

    private void DragWindow(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); }
        private void BtnSair_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

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
                    catch { }
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
                TxtTituloConcluido.Text = "Atualização Concluída com Sucesso!";
                TxtSubtituloConcluido.Text = $"O GigaDock foi atualizado para a versão {InstallService.CurrentVersion} com todas as suas preferências preservadas.";
            }

            BtnAcaoPrincipal.Content = "Concluir";
            BtnAcaoPrincipal.IsEnabled = true;
            BtnAcaoPrincipal.Background = new SolidColorBrush(Color.FromRgb(48, 209, 88));
        }
        else
        {
            var msgExibir = !string.IsNullOrWhiteSpace(erroDetalhado)
                ? $"Ocorreu um erro durante a instalação:\n\n{erroDetalhado}\n\nVerifique se o aplicativo não está em execução ou permissões de pasta."
                : "Ocorreu um erro durante a instalação. Verifique se o aplicativo não está em execução ou permissões de pasta.";

            MessageBox.Show(this, msgExibir, "Erro na Instalação", MessageBoxButton.OK, MessageBoxImage.Error);
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
            "Deseja realmente prosseguir com a desinstalação do GigaDock?",
            "Confirmar Desinstalação",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (resultado != MessageBoxResult.Yes)
        {
            return;
        }

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

        if (sucesso)
        {
            MessageBox.Show(this, "GigaDock foi desinstalado com sucesso do seu computador.\nA barra de tarefas nativa do Windows foi restaurada.", "Desinstalação Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        else
        {
            MessageBox.Show(this, "Houve um problema durante a desinstalação de alguns arquivos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}



