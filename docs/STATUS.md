## 2026-10-10 — remoção do botão adicionar da dock

- Removido o botão + dos controles visíveis da dock Windows, conforme solicitado. Comando de adicionar permanece disponível para os demais fluxos.
- Validação: compilação Release e geração do instalador executadas; sem teste visual nesta etapa.

## 2026-10-10 — fechamento do painel da bandeja

- Botão X acessível, fechamento ao clicar fora (Popup StaysOpen=False), ao desativar a janela e após 15 segundos sem interação. Movimento, clique e teclado reiniciam o prazo; timer e evento removidos ao fechar/descarregar. Preserva o binding ao fechar para permitir reabrir.
- Aguarda 250 ms após ocultar a barra e reforça a ocultação antes de abrir o espelho, cobrindo reação atrasada do Explorer. A aparição durante leitura ainda é uma limitação do espelhamento.
- Verificação: 19 testes existentes de bandeja/restauração passaram e build Release concluído. Fechamento por interação e comportamento do Explorer ainda precisam de validação visual no desktop do usuário.

## 2026-10-10 — restauração da barra após espelhamento

- O painel da dock só abre depois de fechar o painel nativo e voltar a ocultar a barra que a leitura mostrou. Limpeza também em leitura vazia, erro e clique em ícone.
- Preserva a barra deixada visível explicitamente e não altera a barra após encerramento ou desativação do modo principal. Falha ao ocultar impede abrir o espelho e informa o usuário.
- Verificação: 19 testes de bandeja/restauração passaram; compilação Release concluída. Resta confirmar o comportamento visual no desktop do usuário. Aparição breve do Explorer durante a leitura continua possível.
- Detalhes: [BANDEJA-ESPELHADA.md](BANDEJA-ESPELHADA.md).

## 2026-10-10 — central de notificações Windows

- Sino com contador, painel com cores do tema, aplicativo/horário/texto, remoção pela API oficial e limpeza com confirmação. Conteúdo apenas em memória; até 200 avisos recentes, consulta autorizada a cada dez segundos e reaproveitamento de cartões.
- Consentimento solicitado apenas por Ativar notificações; retirada a solicitação automática do serviço na inicialização. Revogação e erro limpam o conteúdo visível.
- Identidade externa com capacidade userNotificationListener, metadados correspondentes no EXE e MSIX incorporado pelo build. Instalador oferece registro opcional apenas para pacote assinado; desinstalação remove somente a identidade própria. Nenhuma identidade ou confiança foi registrada nesta máquina.
- Verificado: solução Release sem avisos/erros, dez testes passando, prévia clara/escura em 100/150/200%, MakeAppx gerando o pacote. Ensaio real sem identidade retornou estado indisponível sem pedir permissão nem ler conteúdo.
- Pendente para funcionamento real: assinatura confiável do MSIX, registro e validação com consentimento. Instalador local não assinado mantém a leitura bloqueada. Não reproduz ações internas de notificações; sem provedor Linux nesta etapa.
- Guia: [CENTRAL-NOTIFICACOES.md](CENTRAL-NOTIFICACOES.md).

## 2026-10-10 — exclusão entre bandeja nativa e espelho

- Corrigido o fechamento: o sucesso do pedido não é mais tratado como confirmação de janela fechada. Recupera foco WPF, verifica visibilidade e só mostra o espelho após o fechamento; impede abrir os dois quando o Explorer demora ou recusa fechar.
- Quatro testes novos de fechamento e dez existentes do controlador passaram. Compilação Release executada pelos testes. Falta validação visual na sessão do usuário com o instalador novo.
- Detalhes em [BANDEJA-ESPELHADA.md](BANDEJA-ESPELHADA.md).

## 2026-10-10 — rodapé do Launchpad organizado

- Substituídas as duas linhas de ações por quatro botões alinhados: Ajustes, Arquivos, Windows e Energia. Vetores de 18 px, altura de 38 px, mesma tipografia e foco visível.
- Desligar, reiniciar e sair ficam no menu Energia. Comandos existentes e confirmação de desligar/reiniciar preservados.
- Verificação executada: build Release pela prévia WPF; tela renderizada em 600 × 454 px; quatro botões de altura igual, menu com três opções, abertura por clique e foco de teclado passaram. Nenhuma ação de energia executada.
- Escopo Windows WPF. Uso dentro do popup da aplicação atualizada e múltiplos monitores ainda precisam de validação; o teste visual utilizou janela isolada.

## 2026-10-10 — ícones dos controles da dock

- Vetores próprios para mostrar barra nativa, adicionar, ajustes, bandeja, controles rápidos e bloqueio de tela. Tamanho de ícone 20 px, traço arredondado uniforme e botões de 34 px com cantos de 11 px.
- Recursos centralizados em DockControls.xaml, preservando cores dos temas, nomes acessíveis, comandos, hover, foco e estilo Anéis dos controles fixados.
- Verificação executada: compilação Release pela prévia WPF; renderização em 96/144/192 DPI com paletas clara e escura; foco visível e comandos de ajustes/controles rápidos passaram. Prévia isolada, sem instalar ou substituir o aplicativo em execução.
- Escopo desta mudança: controles WPF do Windows. Não altera a integração experimental da bandeja nem implementa novos controles Linux.

## 2026-10-10 — protótipo da bandeja espelhada

- Painel da dock com leitura acessível dos ícones ocultos e capturas em memória; clique tenta InvokePattern no controle real. Acesso à bandeja nativa preservado.
- Atualiza ao abrir; menus de botão direito e duplo clique não reproduzidos. Windows experimental, sem mudança na bandeja Linux.
- Build Release passou sem avisos/erros; dez testes existentes do controlador passaram. Teste real não conseguiu encontrar o botão de ícones ocultos nesta sessão; imagens e cliques do espelho ainda não confirmados.
- Limites e próximos testes em [BANDEJA-ESPELHADA.md](BANDEJA-ESPELHADA.md).

## 2026-10-10 - Launchpad: catalogo e visual

- Lista inicial e pesquisa usam agora InstalledAppsScanner (shell:AppsFolder), incluindo Win32 e Microsoft Store; anteriormente a tela mostrava somente dock/ambiente e a pesquisa enumerava apenas .lnk.
- Catalogo assincrono atualizado ao abrir, mensagens de carregamento/erro e busca sem distinguir acentos. Atalhos do ambiente aparecem primeiro.
- Cartoes maiores, nomes em duas linhas, tooltip completo, placeholder acessivel, foco visivel e botoes de energia arredondados; limites de tamanho pela area de trabalho.
- Executado: build Release sem avisos/erros; scanner real encontrou 148 aplicativos, 39 empacotados; renderizacao WPF isolada da interface passou, 600 x 473 px. Instalador regenerado em release/GigaDock-Setup.exe.
- Limites: ainda falta testar interativamente no app atualizado e em varios monitores. Nao pesquisa documentos/configuracoes como o Windows e nao descobre executaveis sem registro no Shell. Alteracao limitada ao Launchpad Windows nesta etapa.

## Correção da bandeja e revisão dos controles — 10 de outubro de 2026

- Causas confirmadas: barra nativa oculta/desativada pelo modo principal; nome acessível do chevron com texto repetido. A seta agora restaura temporariamente a barra, usa HWND/FromHandle e reconhece o prefixo correto. Falhas mostram aviso dentro da dock, sem MessageBox. A preferência principal é preservada.
- Teste com o serviço C# real confirmou painel nativo visível e devolveu a barra ao estado anterior; 15 testes Windows aprovados. Build Release sem erros/avisos. Controles de 32 px padronizados, sem contorno isolado; prévias clara/escura, foco e comandos conferidos em 96/144/192 DPI renderizados.
- Linux: botões adicionar/ajustes com acabamento correspondente; 81 testes Linux e 58 compartilhados aprovados. Smoke Ubuntu/WSLg validou interface, widgets e Pokémon nos seis temas. Bandeja do desktop Linux não foi implementada; nenhum serviço Windows é chamado nesse frontend.
- Relatório atualizado: [Bandeja Windows](BANDEJA-WINDOWS.md). Evidências em `docs/TestResults/controles-bandeja/` e capturas locais. Outros desktops/versões de Explorer e monitores físicos ainda pendentes; sem instalação pessoal, commit ou publicação automática.
- Instalador Windows local sem assinatura e pacote Linux x64 regenerados; fontes públicos exportados novamente. A dock instalada só recebe a correção depois de instalar o novo build e reiniciar o aplicativo.

## Caminhada dos Pokémon no Linux — 10 de outubro de 2026

- Mascote transferido para a borda superior da dock, com deslocamento, retorno nas extremidades, poses/direções, regras compartilhadas de voo/flutuação e recorte correto dos quadros. Funciona nos seis temas; redução de animações e modo econômico pausam o movimento. Bitmaps/streams liberados no ciclo de vida do controle.
- Solução Release: zero erros/avisos. Aprovados 58 testes compartilhados, 81 Linux e 22 Windows de regressão. Smoke Ubuntu/WSLg confirmou deslocamento nos seis temas, 151 espécies, modos de pausa, liberação de folhas e recursos locais. Capturas ground/voo inspecionadas. Primeira rodada falhou no hit test; a rodada final passou após explicitar a configuração.
- Pacote Linux regenerado e executado com dados isolados. Relatório: [Pokémon Linux](POKEMON-LINUX.md). Evidências locais em `docs/TestResults/pokemon-motion/` e `docs/local/capturas/pokemon-motion-linux/`.
- Paridade desta etapa: caminhada e aparência dos sprites. Inatividade global, evolução temporal e Ditto ainda não portados; não há certificação universal de desktop/escala. Próximo passo: validar outras escalas/monitores e portar comportamentos restantes. Sem instalação pessoal, commit ou publicação automática.

## Bandeja nativa Windows — 10 de outubro de 2026

- A diferença das capturas foi identificada: o painel antigo filtrava processos; a bandeja do Windows contém ícones registrados pelos aplicativos. A seta agora solicita o painel nativo usando UI Automation, com proteção contra acionamentos simultâneos e orientação quando o botão não está disponível.
- Build Release com zero erros/avisos. A barra de tarefas não foi exposta à automação nesta sessão; abertura real não validada. Próximo passo: conferir a seta no desktop interativo Windows 10/11. Posição e aparência do painel são controladas pelo Windows; Linux não alterado.
- Relatório: [Bandeja Windows](BANDEJA-WINDOWS.md). Sem mudança global, instalação pessoal ou publicação automática.
- Doze testes de ativação aprovados; rodada combinada com AppAreaAndWindowTrackingTests ficou pendente e foi interrompida, sem aprovação. Instalador Windows de desenvolvimento regenerado, sem assinatura; fonte exportado novamente. A integração nativa ainda precisa de validação no desktop interativo.

## Auditoria de recursos e memória — 10 de outubro de 2026

- Inventariados fontes, mídias, documentação, projetos e saídas locais: aproximadamente 19,27 GiB acessíveis fora de `.git`, contra 8,18 MiB de candidatos aos fontes públicos. Builds antigos são o principal excesso; oito prévias em `docs/guide-previews/` são candidatas a arquivamento. Licenças, sprites dinâmicos e arquivos usados pelo site preservados.
- Processo Windows já aberto amostrado 12 vezes: working set de 218,62 a 236,86 MiB. Coleta sem controlar versão/configuração ou uso da sessão; não prova vazamento nem consumo mínimo. Linux não medido nesta etapa.
- Plano prioriza descarte de bitmaps/streams Linux, reutilização de controles, widgets e animações sob demanda e atualizações econômicas. Relatório: [Auditoria de recursos e memória](AUDITORIA-RECURSOS-E-MEMORIA.md). Evidências em `docs/local/auditoria-recursos/`.
- Auditoria documental: nenhuma exclusão ou alteração de runtime, nenhuma nova compilação/suíte necessária. Próximo passo: benchmark isolado e aplicação das etapas com comparação antes/depois.

## Diagnóstico do GitHub Actions Windows — 10 de outubro de 2026

- A execução pública 37799689870 compilou com sucesso, mas os testes Windows excederam o limite de 45 minutos do job; a geração do instalador foi ignorada. O teste que travou não está identificado nos metadados públicos.
- Workflow ajustado para gerar e disponibilizar o instalador de desenvolvimento antes dos testes. Falha dos testes continua falhando a execução; assinatura depende do sucesso da suíte. Limite Windows de dez minutos e diagnóstico de inatividade de três minutos, com logs, TRX e sequência de testes quando houver travamento.
- Validação local: build Release com zero erros/avisos; 243 testes Windows aprovados em 3 min 10 s, sem reproduzir o travamento. YAML e ordem dos passos conferidos. A execução atualizada no GitHub ainda precisa ocorrer após enviar todos os arquivos novos referenciados pela solução.
- Orientações em [GitHub Actions](GITHUB-ACTIONS.md); evidências locais em `docs/local/historico/CORRECAO-GITHUB-ACTIONS.md` e `docs/TestResults/ci-review/`. Sem commit ou push automático.

## README atualizado — 10 de outubro de 2026

- Documentados os estados da loja, instruções de acesso no Windows/Linux, requisitos locais, bloqueios e limites da auditoria de 209 testes. A tabela Windows deixou de anunciar notificações como integração disponível.
- Alteração apenas documental; links locais verificados. Nenhuma nova compilação ou rodada de testes de aplicativo foi necessária. Próximo passo: validar integrações com aplicativos e hardware reais conforme a auditoria de widgets.

## Auditoria e loja de widgets — 10 de outubro de 2026

- Os 26 tipos possuem contrato compartilhado de função mínima, modo de uso e requisitos/limites; a loja Windows e a seção Widgets Linux mostram essas informações antes da instalação.
- Bloqueados: WhatsApp/Discord voz no instalador Windows, integrações ainda sem runtime Linux e Água automática no Linux. Widgets exclusivos do Linux não podem ser instalados no Windows. Teams Windows é beta de estado estimado; GitHub Linux descreve perfil público, sem prometer contribuições.
- A ativação Linux também recusa ferramentas/sessão ausentes: playerctl, pactl, flatpak e Sway/Hyprland. Não instala dependências automaticamente. Instalações antigas podem ser ocultadas/desinstaladas, preservando dados.
- Corrigidas três cópias indevidas do botão Tarefas dentro dos widgets Pomodoro/Água. Renderização WPF confirmou um único botão com ambos habilitados.
- Build Release: zero erros/avisos. Testes: 74 Windows, 81 Linux e 54 compartilhados, todos aprovados. Loja WPF real: 26 itens, seis instalações bloqueadas, instruções e recusa de cliques forçados. Smoke Avalonia Ubuntu/WSLg confirmou instruções, bloqueios e funcionamento dos recursos locais elegíveis.
- Consultas públicas, comandos e OBS foram cobertos com serviços/respostas controlados; não foram certificados aplicativos pessoais, notificações reais, áudio/mídia, sensores físicos ou disponibilidade atual dos provedores. Outros desktops Linux ainda exigem validação.
- Instalador Windows local sem assinatura e pacote Linux x64 regenerados; fonte público exportado novamente. Relatório público: [Widgets: uso e auditoria](WIDGETS.md). Evidências locais: `docs/local/historico/AUDITORIA-WIDGETS.md`, `docs/local/capturas/loja-widgets*/` e `docs/TestResults/auditoria-widgets/`. Sem instalação pessoal automática, commit ou publicação.

## Tema opcional Areia — 10 de outubro de 2026

- Disponível em Ajustes → Aparência no Windows e Linux. Base bege com facetas vetoriais originais, cantos suaves, peças claras para aplicativos e controles em grafite. A instalação não muda o tema existente.
- Widgets em cartões escuros e relógio com superfície contrastante; ícones originais dos aplicativos preservados. A referência orienta o acabamento; aplicativos, widgets e mascotes dependem da configuração do usuário.
- Build Release da solução sem erros/avisos. Onze testes Windows de temas/controles aprovados, incluindo seleção, persistência e retorno ao tema anterior; 44 testes compartilhados aprovados no Linux.
- Prévia WPF isolada renderizada em 96/144/192 DPI, com foco e comandos verificados. A composição usa estilos reais com conteúdo ilustrativo; não equivale a teste físico em múltiplos monitores.
- Teste gráfico Avalonia em Ubuntu/WSLg aprovou seleção e aplicação pela interface e capturou a dock real. Outras distribuições e desktops ainda não foram validados nesta etapa.
- Instalador Windows local sem assinatura e pacote Linux x64 regenerados; fontes públicos exportados novamente. Relatório e capturas locais em `docs/local/historico/TEMA-AREIA.md` e `docs/local/capturas/areia*/`. Instalação no aplicativo pessoal não executada.

## Visual dos controles Windows — 10 de outubro de 2026

- Ações agrupadas em cápsula; botões de 32 px com ícones consistentes, hover discreto e foco visível. Cores acompanham o tema; modo Anéis e comandos preservados.
- Controles rápidos com ícone de ajustes deslizantes, diferente da engrenagem dos Ajustes. Implementação WPF; Linux e temas completos não foram alterados.
- Build Release sem erros/avisos; cinco testes existentes aprovados. Prévia WPF isolada verificou oito botões, foco e comandos; capturas 96/144/192 DPI em paletas escura/clara ilustrativa, sem teste físico de escalas.
- Relatório e capturas de trabalho em `docs/local/historico/CONTROLES-VISUAL.md` e `docs/local/capturas/controles-novos/`. Falta conferir o visual na dock instalada e em diferentes monitores.
- Instalador Windows regenerado e ZIP dos fontes públicos atualizado; auditoria dos arquivos atuais sem achados pendentes. Sem instalação automática, commit ou publicação.

## Correção da ativação em segundo plano — 10 de outubro de 2026

- O painel deixou de forçar a exibição de janelas ocultas. Preserva bloqueio de diálogos, ativa o popup modal visível e restaura minimizadas de forma assíncrona. Aplicativos ocultos apenas na bandeja devem ser reabertos pelo próprio ícone do aplicativo.
- Ativação fora da thread WPF, popup fechado antes de transferir foco e proteção contra cliques simultâneos; nenhuma nova instância é iniciada.
- Build Release: zero erros/avisos. Doze testes específicos aprovados, incluindo três testes Windows com janelas reais e fechamento pelo comando do X. Rodada final combinada: 46/46 aprovados. Uma rodada anterior teve 33/34 aprovações; falha de estilos passou nas repetições isoladas atuais e anteriores à correção, sem causa estabelecida.
- Instalador local `release/GigaDock-Setup.exe` regenerado, sem assinatura confiável; instalação manual não executada. ZIP dos fontes públicos atualizado para incluir a correção.
- Evidências locais: `docs/local/historico/CORRECAO-ATIVACAO-SEGUNDO-PLANO.md` e `docs/TestResults/background-activation/`. Falta validar com os aplicativos reais após instalar o build atualizado. Sem instalação automática, commit ou push.

## Estado atual — 10 de outubro de 2026

GigaDock 3.2.0 beta: frontend WPF Windows 10/11 x64 e frontend Avalonia Linux x64 na mesma solução, com modelos e serviços compartilhados.

- Windows: ambientes, widgets, Pokédex, ordem persistida de itens, guia no instalador e ajustes adaptáveis. Instalador local sem assinatura confiável.
- Linux: pacote self-contained por usuário; ambientes, widgets, notas, tarefas, aparência, Pokédex e integrações opcionais. Quatro widgets extras: sensores, Flatpak, workspaces Sway/Hyprland e script local manual.
- Site estático em `vercel/` com capturas reais e descrição da beta Linux; publicação externa não executada nesta etapa.

## Validação executada

- Build Release da solução no Windows: zero erros e zero avisos.
- Ubuntu 26.04.1 x64 via WSL2/WSLg: 76 testes Linux e 44 compartilhados aprovados; sete testes Python de empacotamento aprovados.
- Suíte Linux no Windows: 67 aprovados e nove ignorados por exigirem Linux.
- Smoke WSLg: interface, quatro painéis novos e execução manual de script com resultado na dock.
- Instalação/reinstalação/desinstalação Linux isoladas: checksum validado e configurações preservadas.

Esses resultados não equivalem à execução integral da suíte Windows nem à homologação de todos os desktops. [Limites Linux](LINUX-VALIDACAO.md).

## Organização para código aberto

Guias atuais e manifestos de origem ficam públicos. Relatórios de implementação antigos, hashes de builds locais, capturas de laboratório e resultados detalhados foram preservados em `docs/local/`, ignorado pelo Git. E-mail, chave Pix e contato LinkedIn foram removidos da tela Sobre; autoria na licença e links públicos do projeto permanecem.

Auditoria por padrões: conteúdo atual sem achados de dados pessoais/segredos reconhecidos; 1629 blobs históricos examinados, com 184 ocorrências de e-mails/caminhos pessoais (não são 184 segredos únicos). URLs fictícias dos testes e contatos públicos dos créditos foram reconhecidos separadamente. Resultado detalhado local em `docs/local/auditoria/publicacao.json`. [Procedimento e limites](PUBLICACAO-GITHUB.md). Exportação limpa sem `.git` em `dist/GigaDock-codigo-fonte.zip`; nenhum commit, push ou deploy foi executado.

A exportação foi extraída em outra pasta e a solução completa compilou em Release com zero erros/avisos. Integridade do ZIP aprovada; 46 links locais sem falhas, nenhuma entrada de histórico/build/arquivo local e três capturas públicas inspecionadas. O índice do Git deixou de rastrear `docs/installer-validation.json`, que será gerado e ignorado nos próximos builds. Os binários locais antigos não foram regenerados nesta organização; use os novos fontes para futuras releases.

## Próximos passos

Validar sensores físicos, aplicativos Flatpak e sessões reais Sway/Hyprland; depois GNOME/KDE, múltiplos monitores, escalas e outras distribuições. Ampliar validação de interface Windows e assinatura para distribuição. Revisar o histórico Git antes de tornar o repositório público.
