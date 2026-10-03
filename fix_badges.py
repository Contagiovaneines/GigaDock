path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Add a PropertyChanged handler to AppItemViewModel creation
bad_create1 = """        var vm = new AppItemViewModel(
            item,
            _windowTrackingService,
            _iconService,
            onExecutar: ExecutarApp,
            onAlternarFixado: AlternarFixadoApp,
            onMoverEsquerda: MoverAppEsquerda,
            onMoverDireita: MoverAppDireita);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        return vm;"""

good_create1 = """        var vm = new AppItemViewModel(
            item,
            _windowTrackingService,
            _iconService,
            onExecutar: ExecutarApp,
            onAlternarFixado: AlternarFixadoApp,
            onMoverEsquerda: MoverAppEsquerda,
            onMoverDireita: MoverAppDireita);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        vm.PropertyChanged += (s, e) => {
            if (e.PropertyName == "NumeroNotificacoes") {
                SincronizarBadgeWidget(vm);
            }
        };
        return vm;"""

c = c.replace(bad_create1, good_create1)

bad_create2 = """        var vm = new AppItemViewModel(
            janela,
            _windowTrackingService,
            _iconService,
            onExecutar: ExecutarApp,
            onAlternarFixado: AlternarFixadoApp,
            onMoverEsquerda: MoverAppEsquerda,
            onMoverDireita: MoverAppDireita);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        return vm;"""

good_create2 = """        var vm = new AppItemViewModel(
            janela,
            _windowTrackingService,
            _iconService,
            onExecutar: ExecutarApp,
            onAlternarFixado: AlternarFixadoApp,
            onMoverEsquerda: MoverAppEsquerda,
            onMoverDireita: MoverAppDireita);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        vm.PropertyChanged += (s, e) => {
            if (e.PropertyName == "NumeroNotificacoes") {
                SincronizarBadgeWidget(vm);
            }
        };
        return vm;"""

c = c.replace(bad_create2, good_create2)

# Insert SincronizarBadgeWidget
insert_sync = """    private void SincronizarBadgeWidget(AppItemViewModel app)
    {
        string proc = (app.Titulo ?? string.Empty).ToLowerInvariant();
        if (proc.Contains("teams") || proc.Contains("msteams"))
        {
            Teams.MensagensNaoLidas = app.NumeroNotificacoes;
        }
        else if (proc.Contains("whatsapp"))
        {
            WhatsApp.MensagensNaoLidas = app.NumeroNotificacoes;
        }
    }

    public void AtualizarAplicativosAbertos"""

c = c.replace("    public void AtualizarAplicativosAbertos", insert_sync)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
