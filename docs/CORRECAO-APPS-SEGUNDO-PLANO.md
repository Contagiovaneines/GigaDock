# Clique em aplicativos em segundo plano — 8 de outubro de 2026

O painel executava o atalho novamente quando não encontrava uma janela principal. Isso podia iniciar outra instância de aplicativos como o OBS.

Agora o clique enumera novamente as janelas pelo caminho completo do executável, incluindo janelas ocultas com título e sem proprietário ou estilo de ferramenta. Janelas ocultadas pelo DWM são ignoradas. A ativação só informa sucesso quando a janela fica em primeiro plano. O painel não executa mais programas; se a ativação falhar, orienta a usar a bandeja do Windows ou o atalho fixado se o processo terminou.

São usadas APIs públicas Win32: EnumWindows, QueryFullProcessImageName, ShowWindow e SetForegroundWindow. Não há acesso à estrutura interna da bandeja do Explorer.

Validação: `dotnet build DockWindows.slnx --no-restore` passou com zero avisos e erros. `dotnet test tests/DockWindows.Tests/DockWindows.Tests.csproj --no-restore --filter FullyQualifiedName~AppAreaAndWindowTrackingTests` recompilou os projetos e executou 26 testes: 25 passaram e 1 falhou em `PersonalizarClima_AplicaEstilosNoAmbienteCorretoEPreservaVisibilidade`, linha 285 (esperado 9, obtido 11). A falha está em personalização de clima; sua preexistência não foi verificada. `git diff --check` passou.

Limite: não foi executado teste manual com OBS. Aplicativos que não mantêm janela principal restaurável precisam ser abertos pela própria bandeja. O Windows também pode recusar a transferência de foco.

Próximo passo: testar OBS aberto, minimizado e oculto na bandeja; confirmar que o clique mantém a mesma instância e restaura a janela quando disponível.
