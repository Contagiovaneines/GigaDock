path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\GitHubWidgetViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """    public ICommand AlternarAnimacaoCommand { get; }

    public GitHubWidgetViewModel()
    {
        Contribuicoes = new ObservableCollection<ContribuicaoGitHub>();
        _random = new Random();

        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        
        AlternarAnimacaoCommand = new RelayCommand(() => {
            if (AnimacaoAutomatica && EstiloAtual == EstiloAnimacaoGitHub.Cobrinha)
            {
                EstiloAtual = EstiloAnimacaoGitHub.PacMan;
            }
            else if (AnimacaoAutomatica && EstiloAtual == EstiloAnimacaoGitHub.PacMan)
            {
                AnimacaoAutomatica = false;
                LimparEstadoAnimacao();
            }
            else
            {
                AnimacaoAutomatica = true;
                EstiloAtual = EstiloAnimacaoGitHub.Cobrinha;
                IniciarAnimacao();
            }
        });"""

good = """    public ICommand AlternarAnimacaoCommand { get; }
    public ICommand DefinirAnimacaoCommand { get; }

    public GitHubWidgetViewModel()
    {
        Contribuicoes = new ObservableCollection<ContribuicaoGitHub>();
        _random = new Random();

        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        
        AlternarAnimacaoCommand = new RelayCommand(() => {
            if (AnimacaoAutomatica && EstiloAtual == EstiloAnimacaoGitHub.Cobrinha)
            {
                EstiloAtual = EstiloAnimacaoGitHub.PacMan;
            }
            else if (AnimacaoAutomatica && EstiloAtual == EstiloAnimacaoGitHub.PacMan)
            {
                AnimacaoAutomatica = false;
                LimparEstadoAnimacao();
            }
            else
            {
                AnimacaoAutomatica = true;
                EstiloAtual = EstiloAnimacaoGitHub.Cobrinha;
                IniciarAnimacao();
            }
        });

        DefinirAnimacaoCommand = new RelayCommand<string>(param => {
            if (param == "Cobrinha")
            {
                AnimacaoAutomatica = true;
                EstiloAtual = EstiloAnimacaoGitHub.Cobrinha;
                IniciarAnimacao();
            }
            else if (param == "PacMan")
            {
                AnimacaoAutomatica = true;
                EstiloAtual = EstiloAnimacaoGitHub.PacMan;
                IniciarAnimacao();
            }
            else if (param == "Parado")
            {
                AnimacaoAutomatica = false;
                LimparEstadoAnimacao();
            }
        });"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
