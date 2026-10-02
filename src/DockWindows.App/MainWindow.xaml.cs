using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DockWindows.App.ViewModels;
using DockWindows.App.Views;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsRepository _settingsRepo;
    private readonly ILauncherService _launcherService;
    private readonly IIconExtractionService _iconService;
    private readonly IAutostartService _autostartService;
    private Win32Hotkeys? _hotkeyService;
    private Win32TrayService? _trayService;

    private readonly DispatcherTimer _autoHideTimer;
    private bool _estaOcultoPorAutoHide;

    private Views.Sections.SectionIniciarPesquisa? _secIniciarPesquisa;
    private Views.Sections.SectionApps? _secApps;
    private Views.Sections.SectionColecoes? _secColecoes;
    private Views.Sections.SectionItensAmbiente? _secItensAmbiente;
    private Views.Sections.SectionWidgets? _secWidgets;
    private Views.Sections.SectionRelogioControles? _secRelogioControles;
    private Views.Sections.SectionMidiaInline? _secMidiaInline;
    private Views.Sections.SectionClimaInline? _secClimaInline;
    

    protected override void OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        if (_viewModel.EstaEmAlerta) _viewModel.EstaEmAlerta = false;
    }

    public MainWindow()
    {
        InitializeComponent();

        _settingsRepo = new JsonSettingsRepository();
        _launcherService = new LauncherService();
        _iconService = new IconExtractionService();
        _autostartService = new AutostartService();

                _viewModel = new MainViewModel(_settingsRepo, _launcherService, _iconService, _autostartService);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        ConectarCallbacksViewModel();

        _autoHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _autoHideTimer.Tick += (s, e) => ExecutarAutoHide();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        SizeChanged += (s, e) => ReposicionarBarra();

        MouseEnter += MainWindow_MouseEnter;
        MouseLeave += MainWindow_MouseLeave;

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(ReposicionarBarra);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private void ConectarCallbacksViewModel()
    {
        _viewModel.MostrarAlerta = (titulo, msg) =>
        {
            System.Windows.MessageBox.Show(this, msg, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
        };

        _viewModel.AtivarJanelaPrincipal = () =>
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            // Hack para roubar o foco no Windows: simular uma tecla
            keybd_event(0, 0, 0, 0);
            SetForegroundWindow(hwnd);
            this.Activate();
            this.Focus();
        };

        _viewModel.PedirTexto = (titulo, prompt) =>
        {
            var dialog = new InputPromptDialog(titulo, prompt) { Owner = this };
            return dialog.ShowDialog() == true ? dialog.ValorResultante : null;
        };

        _viewModel.AbrirDialogoItem = itemExistente =>
        {
            var dialog = new ItemEditDialog(itemExistente) { Owner = this };
            return dialog.ShowDialog() == true ? dialog.ItemResultante : null;
        };

        _viewModel.AbrirJanelaAjustes = secao =>
        {
            var ajustesVm = new AjustesViewModel(_viewModel, _settingsRepo, _autostartService, secao);
            var ajustesWin = new AjustesWindow(ajustesVm) { Owner = this };
            ajustesWin.ShowDialog();
            AtualizarLayoutSecoes();
            ReposicionarBarra();
        };

        _viewModel.AbrirJanelaConfiguracoes = () =>
        {
            _viewModel.AbrirJanelaAjustes?.Invoke("Geral");
        };

        _viewModel.AbrirJanelaPersonalizacao = () =>
        {
            _viewModel.AbrirJanelaAjustes?.Invoke("Aparencia");
        };

        _viewModel.NotificarReposicionamento = () =>
        {
            Dispatcher.Invoke(ReposicionarBarra);
        };

        _viewModel.TrocarAmbienteComTransicao = amb =>
        {
            if (_viewModel.AmbienteAtivo?.Id == amb.Id) return;

            bool animar = !_viewModel.DesativarAnimacoes && SystemParameters.ClientAreaAnimation;
            if (!animar || _secItensAmbiente == null)
            {
                _viewModel.AmbienteAtivo = amb;
                return;
            }

            var duracao = _viewModel.ObterDuracaoTransicao();
            var halfDur = TimeSpan.FromMilliseconds(duracao.TotalMilliseconds / 2.0);

            var fadeOut = new DoubleAnimation(1.0, 0.0, halfDur);
            var slideOut = new DoubleAnimation(0.0, 5.0, halfDur);

            var tt = _secItensAmbiente.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _secItensAmbiente.RenderTransform = tt;
            }

            fadeOut.Completed += (s, e) =>
            {
                _viewModel.AmbienteAtivo = amb;

                var fadeIn = new DoubleAnimation(0.0, 1.0, halfDur)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var slideIn = new DoubleAnimation(-5.0, 0.0, halfDur)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                _secItensAmbiente.BeginAnimation(OpacityProperty, fadeIn);
                tt.BeginAnimation(TranslateTransform.YProperty, slideIn);
            };

            _secItensAmbiente.BeginAnimation(OpacityProperty, fadeOut);
            tt.BeginAnimation(TranslateTransform.YProperty, slideOut);
        };

        _viewModel.SolicitarFechamento = () =>
        {
            Application.Current.Shutdown();
        };

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.DockVisivel) || e.PropertyName == nameof(_viewModel.OcultoPorTelaCheia))
            {
                if (_viewModel.DockVisivel && !_viewModel.OcultoPorTelaCheia)
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                }
                else
                {
                    Hide();
                }
            }
            else if (e.PropertyName == nameof(_viewModel.OrdemSecoes) ||
                     e.PropertyName == nameof(_viewModel.Espacadores) ||
                     e.PropertyName == nameof(_viewModel.EstiloTema))
            {
                Dispatcher.Invoke(AtualizarLayoutSecoes);
            }
            else if (e.PropertyName == nameof(_viewModel.UsarComoBarraPrincipal))
            {
                Dispatcher.Invoke(ReposicionarBarra);
            }
        };
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AtualizarLayoutSecoes();

        if (_viewModel.UsarComoBarraPrincipal)
        {
            _viewModel.TaskbarService.OcultarBarraNativa(out var estadoAnt);
            _viewModel.Preferencias.EstadoAnteriorBarraTarefas = estadoAnt;
            _viewModel.WinKeyHookService.Iniciar();
        }

        ReposicionarBarra();

        var hwnd = new WindowInteropHelper(this).Handle;
        InicializarHotkeys(hwnd);
        InicializarBandeja(hwnd);
    }

    private void InicializarHotkeys(IntPtr hwnd)
    {
        try
        {
            _hotkeyService = new Win32Hotkeys(hwnd);

            // Atalho principal para exibir/ocultar a dock: Ctrl+Alt+D
            var atalhoDock = _viewModel.Preferencias.AtalhoDock ?? new AtalhoConfig { Control = true, Alt = true, Tecla = "D" };
            var res = _hotkeyService.Registrar(1, atalhoDock, () =>
            {
                Dispatcher.Invoke(() => _viewModel.AlternarVisibilidade());
            });

            // Atalhos rÃ¡pidos para alternar ambientes: Ctrl+Alt+1, Ctrl+Alt+2, Ctrl+Alt+3
            _hotkeyService.Registrar(101, new AtalhoConfig { Control = true, Alt = true, Tecla = "1" }, () =>
            {
                Dispatcher.Invoke(() => AlternarAmbientePorIndice(0));
            });
            _hotkeyService.Registrar(102, new AtalhoConfig { Control = true, Alt = true, Tecla = "2" }, () =>
            {
                Dispatcher.Invoke(() => AlternarAmbientePorIndice(1));
            });
            _hotkeyService.Registrar(103, new AtalhoConfig { Control = true, Alt = true, Tecla = "3" }, () =>
            {
                Dispatcher.Invoke(() => AlternarAmbientePorIndice(2));
            });
        }
        catch { }
    }

    private void AlternarAmbientePorIndice(int indice)
    {
        if (indice >= 0 && indice < _viewModel.Ambientes.Count)
        {
            _viewModel.AmbienteAtivo = _viewModel.Ambientes[indice];
            if (!_viewModel.DockVisivel)
            {
                _viewModel.DockVisivel = true;
            }
        }
    }

    private void InicializarBandeja(IntPtr hwnd)
    {
        try
        {
            _trayService = new Win32TrayService(hwnd, "Dock Windows â€” Produtividade");
            _trayService.DuploClique += () =>
            {
                Dispatcher.Invoke(() => _viewModel.AlternarVisibilidade());
            };
            _trayService.CliqueDireito += () =>
            {
                Dispatcher.Invoke(ExibirMenuBandeja);
            };
        }
        catch { }
    }

    private void ExibirMenuBandeja()
    {
        var menu = new ContextMenu
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 34)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 58))
        };

        var itemVisibilidade = new MenuItem
        {
            Header = _viewModel.DockVisivel ? "ðŸ”½ Ocultar Dock" : "ðŸ”¼ Exibir Dock",
            FontWeight = FontWeights.Bold
        };
        itemVisibilidade.Click += (s, e) => _viewModel.AlternarVisibilidade();
        menu.Items.Add(itemVisibilidade);

        menu.Items.Add(new Separator());

        // Submenu de ambientes
        var menuAmbientes = new MenuItem { Header = "ðŸ’¼ Ambientes" };
        foreach (var amb in _viewModel.Ambientes)
        {
            var itemAmb = new MenuItem
            {
                Header = amb.Nome,
                IsChecked = amb.EstaAtivo
            };
            var a = amb;
            itemAmb.Click += (s, e) => _viewModel.AmbienteAtivo = a;
            menuAmbientes.Items.Add(itemAmb);
        }
        menu.Items.Add(menuAmbientes);

        menu.Items.Add(new Separator());

        var itemAjustes = new MenuItem { Header = "âš™ï¸ Ajustes e PersonalizaÃ§Ã£o...", FontWeight = FontWeights.SemiBold };
        itemAjustes.Click += (s, e) => _viewModel.AbrirAjustesCommand.Execute(null);
        menu.Items.Add(itemAjustes);

        var itemRestaurar = new MenuItem { Header = "ðŸ”„ Restaurar Barra do Windows" };
        itemRestaurar.Click += (s, e) => _viewModel.RestaurarBarraWindowsCommand.Execute(null);
        menu.Items.Add(itemRestaurar);

        menu.Items.Add(new Separator());

        var itemSair = new MenuItem { Header = "ðŸšª Sair do Dock Windows" };
        itemSair.Click += (s, e) => Application.Current.Shutdown();
        menu.Items.Add(itemSair);

        menu.IsOpen = true;
    }

    private void ReposicionarBarra()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - ActualWidth) / 2.0;

        double screenBottom = _viewModel.UsarComoBarraPrincipal
            ? SystemParameters.PrimaryScreenHeight
            : workArea.Bottom;

        double margemInferiorJanela = MainGrid?.Margin.Bottom ?? 4.0;
        double gapBordaInferior = _viewModel.UsarComoBarraPrincipal ? 4.0 : 8.0;

        if (!_estaOcultoPorAutoHide)
        {
            Top = screenBottom - ActualHeight + margemInferiorJanela - gapBordaInferior;
        }
        else
        {
            Top = screenBottom - 4.0;
        }

        if (_viewModel.UsarComoBarraPrincipal && !_estaOcultoPorAutoHide)
        {
            DockWindows.Infrastructure.Windows.AppBarHelper.RegisterBar(this);
            DockWindows.Infrastructure.Windows.AppBarHelper.UpdatePos(this);
        }
        else
        {
            DockWindows.Infrastructure.Windows.AppBarHelper.RemoveBar(this);
        }
    }

    public void AtualizarLayoutSecoes()
    {
        SectionsContainer.Children.Clear();

        _secIniciarPesquisa ??= new Views.Sections.SectionIniciarPesquisa();
        _secApps ??= new Views.Sections.SectionApps();
        _secColecoes ??= new Views.Sections.SectionColecoes();
        _secItensAmbiente ??= new Views.Sections.SectionItensAmbiente();
        _secWidgets ??= new Views.Sections.SectionWidgets();
        _secRelogioControles ??= new Views.Sections.SectionRelogioControles();
        _secMidiaInline ??= new Views.Sections.SectionMidiaInline();
        _secClimaInline ??= new Views.Sections.SectionClimaInline();
        

        var secoesOrdenadas = _viewModel.OrdemSecoes.OrderBy(s => s.Ordem).ToList();
        bool primeiroAdicionado = false;
        int separadorIndex = 0;

        foreach (var cfg in secoesOrdenadas)
        {
            if (!cfg.Visivel) continue;

            FrameworkElement? controle = cfg.Tipo switch
            {
                TipoSecaoDock.IniciarPesquisa => _secIniciarPesquisa,
                TipoSecaoDock.Apps => _secApps,
                TipoSecaoDock.Colecoes => _secColecoes,
                TipoSecaoDock.ItensAmbiente => _secItensAmbiente,
                TipoSecaoDock.Widgets => _secWidgets,
                TipoSecaoDock.RelogioControles => _secRelogioControles,
                TipoSecaoDock.MidiaInline => _secMidiaInline,
                TipoSecaoDock.ClimaInline => _secClimaInline,
                
                _ => null
            };

            if (controle == null) continue;

            if (primeiroAdicionado)
            {
                var sep = CriarSeparador(separadorIndex++);
                SectionsContainer.Children.Add(sep);
            }

            SectionsContainer.Children.Add(controle);
            primeiroAdicionado = true;
        }

        ReposicionarBarra();
    }

    private FrameworkElement CriarSeparador(int indiceSeparador)
    {
        var espacadores = _viewModel.Espacadores;
        var cfg = (indiceSeparador < espacadores.Count) ? espacadores[indiceSeparador] : null;

        if (cfg != null && !cfg.Visivel)
        {
            return new FrameworkElement { Width = 0, Height = 0, Visibility = Visibility.Collapsed };
        }

        var estilo = cfg?.Estilo ?? EstiloEspacador.Linha;
        double largura = cfg?.Largura ?? 8.0;
        double halfMargem = Math.Max(2, largura / 2);

        switch (estilo)
        {
            case EstiloEspacador.Espaco:
                return new Border
                {
                    Width = Math.Max(2, largura),
                    Height = 24,
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center
                };

            case EstiloEspacador.Ponto:
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Margin = new Thickness(halfMargem, 0, halfMargem, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                dot.SetBinding(System.Windows.Shapes.Shape.FillProperty, new System.Windows.Data.Binding(nameof(_viewModel.SeparadorColor))
                {
                    Source = _viewModel,
                    Converter = (System.Windows.Data.IValueConverter)Application.Current.FindResource("ColorToBrushConverter")
                });
                return dot;

            case EstiloEspacador.Linha:
            default:
                var sep = new Border
                {
                    Width = Math.Max(2, largura),
                    Height = 24,
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center
                };
                return sep;
        }
    }

    private void MainWindow_MouseEnter(object sender, MouseEventArgs e)
    {
        _autoHideTimer.Stop();
        if (_estaOcultoPorAutoHide)
        {
            _estaOcultoPorAutoHide = false;
            double screenBottom = _viewModel.UsarComoBarraPrincipal
                ? SystemParameters.PrimaryScreenHeight
                : SystemParameters.WorkArea.Bottom;
            double margemInferiorJanela = MainGrid?.Margin.Bottom ?? 4.0;
            double gapBordaInferior = _viewModel.UsarComoBarraPrincipal ? 4.0 : 8.0;
            AnimarPosicaoVertical(screenBottom - ActualHeight + margemInferiorJanela - gapBordaInferior);
        }
    }

    private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_viewModel.OcultarAutomaticamente)
        {
            _autoHideTimer.Start();
        }
    }

    private void ExecutarAutoHide()
    {
        _autoHideTimer.Stop();
        if (_viewModel.OcultarAutomaticamente && !IsMouseOver && !_estaOcultoPorAutoHide)
        {
            _estaOcultoPorAutoHide = true;
            double screenBottom = _viewModel.UsarComoBarraPrincipal
                ? SystemParameters.PrimaryScreenHeight
                : SystemParameters.WorkArea.Bottom;
            AnimarPosicaoVertical(screenBottom - 4.0);
        }
    }

    private void AnimarPosicaoVertical(double destinoTop)
    {
        var anim = new DoubleAnimation
        {
            To = destinoTop,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(TopProperty, anim);
    }

    private void BtnGerenciarAmbiente_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var arquivos = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (arquivos != null)
            {
                foreach (var caminho in arquivos)
                {
                    var isDir = Directory.Exists(caminho);
                    var novo = new ItemFixado
                    {
                        Titulo = isDir ? Path.GetFileName(caminho) : Path.GetFileNameWithoutExtension(caminho),
                        CaminhoOuUrl = caminho,
                        Tipo = isDir ? TipoItem.Pasta : (Path.GetExtension(caminho).Equals(".exe", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(caminho).Equals(".lnk", StringComparison.OrdinalIgnoreCase) ? TipoItem.Aplicativo : TipoItem.Arquivo)
                    };
                    if (string.IsNullOrWhiteSpace(novo.Titulo))
                    {
                        novo.Titulo = caminho;
                    }
                    _viewModel.AdicionarItemDireto(novo);
                }
            }
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _viewModel.MenuIniciarAberto = false;
            _viewModel.Clock.CalendarioAberto = false;
            _viewModel.Pomodoro.PainelAberto = false;
            _viewModel.Calendario.PainelAberto = false;

            foreach (var col in _viewModel.TodasColecoesAtivas)
            {
                col.PainelAberto = false;
            }

            foreach (var app in _viewModel.Aplicativos)
            {
                app.MenuJanelasAberto = false;
            }

            e.Handled = true;
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        _autoHideTimer.Stop();
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _viewModel.WinKeyHookService.Parar();

        if (_viewModel.UsarComoBarraPrincipal)
        {
            _viewModel.TaskbarService.RestaurarBarraNativa(_viewModel.Preferencias.EstadoAnteriorBarraTarefas);
        }
        DockWindows.Infrastructure.Windows.AppBarHelper.RemoveBar(this);

        _viewModel.SalvarPreferencias();
    }
    private System.Windows.Media.Animation.Storyboard? _alertaStoryboard;

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.OcultoPorTelaCheia) && !_viewModel.OcultoPorTelaCheia)
        {
            if (_viewModel.SempreNoTopo)
            {
                Topmost = false;
                Topmost = true;
            }
        }
        
        if (e.PropertyName == nameof(_viewModel.EstaEmAlerta))
        {
            if (_viewModel.EstaEmAlerta)
            {
                IniciarAnimacaoAlerta();
            }
            else
            {
                PararAnimacaoAlerta();
            }
        }
    }

    private void IniciarAnimacaoAlerta()
    {
        if (_alertaStoryboard != null)
        {
            _alertaStoryboard.Stop();
        }

        try
        {
            var cor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_viewModel.CorAlerta);

            // Anima a sombra pulsando
            var animacaoSombra = new System.Windows.Media.Animation.ColorAnimation
            {
                From = System.Windows.Media.Colors.Black,
                To = cor,
                Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.8)),
                AutoReverse = true,
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };

            var animacaoRaio = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 16.0,
                To = 30.0,
                Duration = new System.Windows.Duration(TimeSpan.FromSeconds(0.8)),
                AutoReverse = true,
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };

            System.Windows.Media.Animation.Storyboard.SetTarget(animacaoSombra, DockShadow);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(animacaoSombra, new System.Windows.PropertyPath(System.Windows.Media.Effects.DropShadowEffect.ColorProperty));
            
            System.Windows.Media.Animation.Storyboard.SetTarget(animacaoRaio, DockShadow);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(animacaoRaio, new System.Windows.PropertyPath(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty));

            _alertaStoryboard = new System.Windows.Media.Animation.Storyboard();
            _alertaStoryboard.Children.Add(animacaoSombra);
            _alertaStoryboard.Children.Add(animacaoRaio);
            _alertaStoryboard.Begin();
        }
        catch { }
    }

    private void PararAnimacaoAlerta()
    {
        if (_alertaStoryboard != null)
        {
            _alertaStoryboard.Stop();
            _alertaStoryboard = null;
        }
        
        // Restaura valores originais
        DockShadow.Color = System.Windows.Media.Colors.Black;
        DockShadow.BlurRadius = 16.0;
    }
}






