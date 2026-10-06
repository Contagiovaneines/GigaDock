# Análise do projeto — 05/10/2026

## Revisão consolidada após mídia, ícone, RGB e magnificação

**Validação atual:** `dotnet build DockWindows.slnx -c Release --no-restore` executado, código 0, zero erros e zero avisos na compilação incremental. Os resultados de 108 testes abaixo são da primeira auditoria, antes das alterações recentes. Tentativa posterior de 11 testes selecionados: 3 aprovados e 8 falhas por bloqueio de carregamento `0x800711C7` (Controle de Aplicativo), inclusive os dois novos testes de abertura de painéis. Não houve nova execução de testes nesta revisão nem contorno das restrições.

**Artefato:** `release/GigaDock-Setup.exe`, 172.246.408 bytes, modificado em 05/10/2026 às 22:29:52, conforme metadados locais na revisão. Contém a publicação anterior com ícone, cores da mídia, gamer RGB e magnificação. Não foi reconstruído nesta tarefa de documentação, nem executado para instalação. Não foi verificada publicação externa.

**Escopo:** inventário de 183 arquivos sob src/tests/tools/vercel, leitura direcionada dos cinco projetos da solução, modelos e persistência, serviços Windows, inicialização/instalação, widgets, estilos/loja, controles WPF, scripts e documentação. Revisão adicional de todos os componentes alterados desde a auditoria. Não equivale à leitura linha a linha de cada arquivo nem a testes completos de comportamento.

### Mudanças confirmadas no código

- `Ambiente.ModoAberturaPaineis`: Clique/Mouse por ambiente; HoverFlyout com atraso de 350 ms e timers parados em repouso; controles fecham painéis ao trocar ambiente/modo ou descarregar.
- `ApplicationIcon` e recursos WPF nos projetos App/Installer, Icon nas janelas, ICO em oito resoluções, bandeja via WM_GETICON com fallback genérico. Atalhos/registro já usam o ícone do executável.
- Mídia: miniatura de 32 × 32 pixels, agrupamento de cores, converter de degradê/borda/destaque, progresso real, revisão de consultas e captura da sessão. Mantém arquivos temporários de thumbnail e timer de timeline.
- `ModoGamerRgb`: global, persistido, botão na dock/ajustes/bandeja, independente da mídia; RGB Musical permanece separado. Storyboard condicionado a IsVisible e DesativarAnimacoes, com remoção ao sair dessas condições.
- SectionApps: magnificação de 1,45×/1,16×, animação finita de 140 ms, foco por teclado, descarregamento e preferência de animações. MargemDock reserva espaço transparente proporcional ao tamanho/escala.

### Achados adicionais e riscos a validar

1. **P1 — Gerenciamento de atividade incompleto.** Monitor/bateria interrompem timers ao desativar, mas mídia/relógio/calendário/clima mantêm atualizações. Serviços de notificações também têm consultas periódicas próprias. Não há controlador comum para widget ativo, dock oculta, ambiente inativo e tarefas de tempo. Priorizar ciclo de vida, cache e medidas reais; consumo baixo ainda não está comprovado.
2. **P2 — Cores e cache da mídia.** A leitura reduzida limita o trabalho de paleta, mas capas são copiadas para arquivos temporários com GUID; a proteção de revisão só remove capas de consultas antigas descartadas. Não existe limpeza geral das capas substituídas. Verificar ausência de capa, eventos repetidos e crescimento em sessão longa.
3. **P2 — RGB e ocultação.** IsVisible=false interrompe o storyboard, mas auto-hide por deslocamento deixa o elemento visível do ponto de vista do WPF. Modo gamer mantém blur/rotação; medir custo e suspender renderização fora da tela, preservando a preferência ligada. Verificar animações reduzidas do Windows além da preferência do app.
4. **P2 — Magnificação e geometria.** Expansão usa os vizinhos por índice, não cálculo contínuo de distância ao ponteiro. RenderTransform não redistribui largura: ícones podem se sobrepor, especialmente com tamanhos grandes. A nova margem altera altura da janela e geometria do auto-hide/AppBar. Não afirmar ausência de cortes sem validar DPI, múltiplos monitores e posição dos popups.
5. **P2 — Sincronização dos ajustes.** AjustesViewModel repassa getters/setters de modo de abertura/RGB ao MainViewModel, sem assinatura geral de mudanças deste. Conferir atualização dos interruptores/ComboBox enquanto a janela está aberta e a escolha muda pela dock/bandeja ou após trocar ambiente. É risco de estado visual desatualizado identificado por leitura, ainda sem reprodução manual.
6. **P2 — Recursos solicitados ainda ausentes.** Wi-Fi/Bluetooth abrem ms-settings e não consultam conexões. Win32TrayService só registra o ícone do próprio app; não enumera/reproduz ícones e menus de terceiros. Acesso à bandeja e lista de processos são funções distintas; manter a barra nativa funcional e usar APIs públicas.
7. **P2 — Codificação e referências.** O README tinha novidades anexadas após a autoria e trechos com caracteres substituídos por `?`; foram consolidados e corrigidos nesta revisão. Comentários/registros históricos e alguns textos do projeto ainda exigem revisão de codificação. Algumas galerias usam texto ilustrativo, não uma renderização fiel do widget completo.

**Prioridades que permanecem:** P0 instalador/barra nativa, migrações v4/v5/v6, contrato de apps globais/duplicação, divergência de versões, testes não isolados e integrações demonstrativas. Nenhuma correção de código foi feita nesta revisão. Os achados e 13 falhas históricos seguem abaixo para rastreabilidade.

---

## Primeira auditoria: resultados e evidências históricos


## Resultado da primeira auditoria

A solução compila em Release. A suíte completa executada tem **108 casos: 95 aprovados, 13 falhas e zero ignorados**. Há funcionalidades novas com testes específicos aprovados, mas a situação global impede declarar a versão estável ou todas as integrações funcionais.

Esta etapa atualizou a documentação. Não corrigiu código, testes, migrações ou comportamento do instalador e não publicou uma nova versão do executável.

## Escopo e método

Foi feita varredura da estrutura de Core, Infrastructure, App, Installer, testes, scripts, documentação e demonstração web, seguida de leitura direcionada dos modelos, persistência, inicialização, coordenação dos ambientes, widgets, loja, prévias, integrações nativas e instalação. Foram comparadas as promessas do README anterior com a implementação atual e o histórico recente.

É uma revisão estática com execução de build/testes; não é uma revisão linha a linha de todos os arquivos, auditoria de segurança certificada ou validação completa da interface instalada. Nenhum arquivo de prompt de outra etapa foi consultado.

Comandos executados nesta revisão, no Windows com SDK .NET `10.0.401`:

```powershell
dotnet test DockWindows.slnx -c Release --no-restore
dotnet build DockWindows.slnx -c Release --no-restore
```

O teste completo retornou código 1 pelas 13 falhas. O build posterior retornou código 0, zero erros e zero avisos naquela execução incremental. A recompilação durante os testes emitiu CS0067 no evento não usado do serviço simulado. Não foram executados instalador, publicação, ações de bloquear/suspender nem nova captura WPF.

A suíte completa inclui efeitos externos: teste de autostart e testes de restauração da barra nativa. O teste de autostart usa `finally` para restaurar o booleano original, mas isso não constitui restauração integral do registro/atalho. Recomenda-se separar essas integrações como execução opt-in antes de usá-las novamente fora de ambiente controlado.

## Achados por prioridade

### P0 — Instalador modifica a barra de tarefas sem escolha específica

**Evidência:** [InstallService.cs](../src/DockWindows.Installer/Services/InstallService.cs), método `Instalar`, grava `UsarComoBarraPrincipal = true` e chama `OcultarBarraNativa`. [Win32TaskbarService.cs](../src/DockWindows.Infrastructure/Windows/Win32TaskbarService.cs) usa `ShowWindow(SW_HIDE)` e `EnableWindow(false)` na barra principal e nas secundárias, com watchdog.

**Impacto:** instalação/atualização pode desabilitar a barra do Windows; contradiz as regras em [AGENTS.md](../AGENTS.md), mesmo que o modelo tenha padrão desativado e existam rotinas de restauração. A regra de manter a barra funcional deve prevalecer sobre os textos antigos do STATUS.

**Próxima ação:** retirar ativação automática e rever o modo de substituição, restauração após falhas e integração em múltiplos monitores. Não foi corrigido nesta tarefa de documentação.

### P1 — Migrações não seguem uma sequência única

**Evidência:** [JsonSettingsRepository.cs](../src/DockWindows.Infrastructure/Persistence/JsonSettingsRepository.cs), a partir da condição `SchemaVersion < 4`: as migrações v5/v6 estão dentro desse bloco. [Preferencias.cs](../src/DockWindows.Core/Models/Preferencias.cs) ainda define padrão/criação em schema 4.

**Impacto:** uma configuração v1/v2 pode chegar a v6, enquanto uma já v4 não entra no mesmo caminho de migração. Os testes observaram schema 6 onde esperavam 4. Há alterações de visibilidade/cantos embutidas nas migrações; é necessário revisar preservação da personalização, não apenas mudar as asserções.

**Próxima ação:** declarar uma versão atual única, migrações independentes em sequência e testes para entrada em cada versão, backup e repetição sem mudanças adicionais.

### P1 — Apps globais, isolamento e expectativas dos testes divergem

**Evidência:** [MainViewModel.cs](../src/DockWindows.App/ViewModels/MainViewModel.cs) move `AppsPermanentes` para o primeiro ambiente e limpa a lista durante a construção. O carregamento atual parte dos itens do ambiente. O teste de alternar escopo para Global falhou; testes de permanentes e mesclagem também falharam, incluindo duas entradas de Bloco de Notas.

**Impacto:** o isolamento novo é testado em casos específicos, mas o comportamento dos comandos antigos de escopo global e a ausência de duplicações não estão garantidos pela suíte inteira. Não se deve classificar todas essas falhas como simples testes desatualizados.

**Próxima ação:** decidir suporte/migração de globais, usar fixtures com padrões explícitos e investigar identidade de itens/processos, sem desativar testes que denunciam duplicação.

### P1 — Integrações experimentais expõem aparência de estado real

- [DiscordIpcService.cs](../src/DockWindows.Infrastructure/Windows/DiscordIpcService.cs): conecta ao pipe, não envia handshake e emite nomes de sala/usuários fixos. É demonstrativo, mesmo que o pipe tenha conectado.
- [ObsWidgetViewModel.cs](../src/DockWindows.App/ViewModels/ObsWidgetViewModel.cs): `AlternarGravacao` só troca o booleano visual; não envia ação ao OBS.
- [GitHubWidgetViewModel.cs](../src/DockWindows.App/ViewModels/GitHubWidgetViewModel.cs): há leitura de HTML público real por regex, mas não monitor de Actions. O fallback de total conta dias ativos e não o número exato de contribuições; erros têm pouco retorno visual. A loja o descreve como mock/Actions, divergindo do código.
- [TeamsIntegrationService.cs](../src/DockWindows.Infrastructure/Windows/TeamsIntegrationService.cs): processos/títulos inferem chamadas. “Aberto” não é presença oficial; leitura de sala não deve ser prometida como garantida.
- WhatsApp/Teams: notificações dependem do listener, permissões e heurísticas. A ausência de permissão pode resultar em silêncio, não em indicação suficiente ao usuário.

**Próxima ação:** explicitar demonstrações/indisponibilidade na interface, alinhar catálogo e implementar integrações reais somente com fluxo documentado e autorizado.

### P1 — Testes de integração alteram estado fora das fixtures

**Evidência:** [IntegrationTests.cs](../tests/DockWindows.Tests/IntegrationTests.cs), teste chamado `AutostartService_ConfigurarLeituraEGravacao_OperaEmHKCU`, chama o serviço real. [AutostartService.cs](../src/DockWindows.Infrastructure/Windows/AutostartService.cs) atualmente cria/remove `GigaDock.lnk` via WScript e apaga a chave Run legada. A asserção de configuração bem-sucedida falhou nesta máquina.

**Impacto:** o nome não acompanha o mecanismo atual; a execução depende de COM/política local e pode escrever atalhos/registro do usuário. Também pode apontar para o executável do host de testes por usar `Environment.ProcessPath`.

**Próxima ação:** injetar diretório, caminho do app e criação de atalhos; testes de unidade com serviço simulado e integrações explicitamente opt-in com snapshot/restauração completos.

### P2 — Consultas, validação e descarte precisam ser consolidados

- [ClimaWidgetViewModel.cs](../src/DockWindows.App/ViewModels/ClimaWidgetViewModel.cs) inicia timer/consulta por padrão no construtor. O modo sem consulta existe para referências; o runtime ainda pode consultar mesmo oculto.
- [CalendarioWidgetViewModel.cs](../src/DockWindows.App/ViewModels/CalendarioWidgetViewModel.cs) aceita caminho/URL ICS e usa detecção por `StartsWith("http")`, com parsing básico. A URL é persistida em texto; não há endurecimento equivalente ao novo link de reunião. Recorrência/fusos/cancelamento ainda precisam de revisão.
- Vários serviços/timers iniciam nos construtores e eventos não têm descarte consistente; operações assíncronas podem continuar após troca de estado. Isso indica risco a medir, não vazamento comprovado.
- [LauncherService.cs](../src/DockWindows.Infrastructure/Windows/LauncherService.cs) valida itens, mas comandos de integrações chamam `Process.Start` diretamente e vários `catch` silenciam erros. A validação não cobre uniformemente todos os caminhos.
- Mensagens de texto com codificação inconsistente ainda aparecem em trechos do código. Revisar arquivos UTF-8 e aparência real em pt-BR.

**Próxima ação:** lifecycle explícito, cancelamento, allowlist de protocolos, política para URL ICS e mensagens de erro observáveis. Medir consumo sob widgets ocultos/visíveis.

### P2 — UI e catálogo ainda não cobrem todos os modelos

**Evidência:** [NotasWidgetViewModel.cs](../src/DockWindows.App/ViewModels/NotasWidgetViewModel.cs) tem persistência, mas [SectionNotasInline.xaml](../src/DockWindows.App/Views/Sections/SectionNotasInline.xaml) contém contador/textos fixos e não conclui o editor. Notas e cotação não estão disponíveis como widgets completos no catálogo. Os seletores genéricos compacto/expandido não significam automaticamente dois layouts distintos em todas as integrações antigas.

**Próxima ação:** conferir cada binding/comando e anunciar só as aparências realmente distintas. Finalizar o fluxo de Notas e alinhar tipos, catálogo e componentes.

### P2 — Versionamento e distribuição inconsistentes

**Evidência:** `.csproj` em `2.0.0`, `InstallService.CurrentVersion` em `3.0.0`, teste ainda exigindo `1.6.0` e textos antigos. `build_release.ps1` contém caminhos absolutos desta máquina; [tools/build-installer.ps1](../tools/build-installer.ps1) usa `dist/` e publicação diferente da usada para `release/`.

**Próxima ação:** uma fonte de versão, script portável com verificações de saída, caminho de distribuição único e verificação da dependência do Desktop Runtime. Assinatura/publicação não foram verificadas. Não há mecanismo que neutralize Smart App Control; a promessa antiga foi removida do README.

## As 13 falhas observadas

Resumo transcrito da saída desta execução; não foi gerado arquivo TRX nem rerodada a suíte para produzir um log adicional.

| Teste | Resultado observado / investigação necessária |
| --- | --- |
| `CustomizationAndInstallerTests.Preferencias_RestaurarAmbientePadrao_RestauraAmbientePredefinido` | Relógio restaurado desabilitado onde o teste esperava habilitado. |
| `CustomizationAndInstallerTests.Preferencias_SecoesVisiveis_PadraoVerdadeiro` | Uma visibilidade padrão foi falsa; expectativas não acompanham todos os padrões atuais. |
| `PersistenceTests.Migracao_SchemaV1ParaV2_PreservaAmbientesEAtualizaPropriedades` | Esperado schema 4; recebido 6. |
| `PersistenceTests.Migracao_SchemaV2ParaV3_AdicionaAppsPermanentesESecoes` | Esperado schema 4; recebido 6. |
| `IntegrationTests.AutostartService_ConfigurarLeituraEGravacao_OperaEmHKCU` | `Configurar(true)` retornou falso; serviço real e ambiente dependente. |
| `ThreeColumnSettingsAndInstallerUpgradeTests.InstallService_ConstantesEVersionamento_SaoVersao1_6_0` | Esperado 1.6.0; recebido 3.0.0. |
| `ThreeColumnSettingsAndInstallerUpgradeTests.AjustesViewModel_AlternarEscopoItem_MoveEntreAmbienteEGlobal` | Item não passou a ser reconhecido como global. |
| `AppAreaAndWindowTrackingTests.MesclagemDeApps_ExibeAppAbertoNaoFixado` | Esperada coleção unitária; observados quatro itens. Rever padrões e opção de não fixados. |
| `AppAreaAndWindowTrackingTests.TrocaDeAmbiente_PreservaAppsPermanentes` | Lista mudou entre ambientes; contrato de globais foi alterado pelo fluxo atual. |
| `AppAreaAndWindowTrackingTests.MainViewModel_AlturasBarra_CompactasEstiloApple` | Esperado 46; recebido 64. |
| `AppAreaAndWindowTrackingTests.FecharJanela_DisparaServicoTracking` | `First` não encontrou a entrada de janela esperada. |
| `AppAreaAndWindowTrackingTests.MesclagemDeApps_NaoDuplicaAppFixadoQuandoAberto` | Duas entradas de Bloco de Notas onde deveria existir uma. |
| `TaskbarSubstitutionAndWinKeyTests.ItensLaunchpadFiltrados_ComFiltro_FiltraPorNome` | Resultado filtrado não era unitário. |

## Cobertura e partes positivas

A separação Core/Infrastructure/App/Installer existe. Persistência tem temporário/backup, widgets têm modelos de estilo por ambiente e a leitura das novas métricas admite serviço simulado. Validação de itens, Pomodoro, parser do clima, controle rápido simulado e testes específicos de estilos contribuem para os 95 aprovados.

As atualizações recentes estão descritas no [README](../README.md): mídia, clima em nove estilos, relógios, reuniões, bateria, gráficos, prévias DWM/pastas, galerias e menus Personalizar. Elas não implicam validação de todas as combinações ou dos demais widgets.

## Outros componentes revisados

- `vercel/`: demonstração HTML/CSS/JS, separada da dock. Comportamentos ilustrativos e textos não comprovam recursos do aplicativo; site/publicação não foram testados.
- `docs/`: histórico e protótipos, com registros antigos de conclusão que não equivalem ao estado atual da suíte. Referências a etapas/artefatos antigos podem exigir limpeza.
- `docs/VisualHarness`: auxiliar de captura; a execução anterior foi bloqueada por Controle de Aplicativo do Windows. Não houve tentativa de contornar a política nesta revisão.
- `test_notif/`: programa auxiliar fora da solução principal; não compilado/executado nesta etapa.
- Scripts `update_*.py`/`patch.py` e arquivo XAML `.test`: auxiliares/resíduos sem integração comprovada no build; revisar e arquivar com intenção clara.
- `tools/`: empacotamento e restauração. Não executados; scripts de distribuição precisam de consolidação e validação dos destinos antes de limpeza.

## Validação que continua faltando

Instalação/atualização/desinstalação, preservação/restauração da barra, widgets reais de terceiros, acesso a notificações autorizado/negado, clima real, ICS complexo, foco/leitor de tela, todos os popups, vários monitores e escalas, bateria física, carga do sistema, consumo e concorrência. A análise não confirma desempenho, segurança total ou compatibilidade prática em todas as versões do Windows.

## Ordem sugerida das próximas etapas

1. Corrigir a modificação automática da barra no instalador e criar teste de instalação com dependências simuladas.
2. Unificar versão/schema; corrigir migrações e definir o contrato de globais, duplicação e escopo.
3. Isolar integrações com efeitos externos e resolver a suíte inteira sem apagar asserções úteis.
4. Alinhar UI/catálogo às funções reais; concluir erros, validação e lifecycle.
5. Validar instalação e interfaces no Windows 10/11, DPI e teclado; medir desempenho.
6. Ampliar utilitários pendentes das referências: áudio, microfone, água, área de transferência, captura/arquivos/Downloads e compartilhamento.


## Atualização: lifecycle implementado (2026-10-05)

As descrições anteriores de timers sempre ativos/cache sem limite representam o estado anterior. A etapa posterior implementou o gerenciamento dos 13 widgets, visibilidade real incluindo auto-hide, frequência por estilo, cache TTL, cancelamento e descarte. Consulte AUDITORIA-ATIVIDADE.md para o inventário antes/depois e cada solução. Validação atual da seleção: 41 testes aprovados, incluindo 10 mil alternâncias e dois minutos de Dispatcher; build zero erros, um aviso existente. CPU/RAM reais e validação visual/manual ainda pendentes. As 13 falhas da suíte completa anterior não foram declaradas resolvidas. P0 do instalador e demais problemas de escopo diferente permanecem.
