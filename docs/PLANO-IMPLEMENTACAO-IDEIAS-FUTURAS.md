# Plano de implementação das ideias futuras

Data da análise: 06/10/2026  
Estado: MVP implementado em 07/10/2026; validações manuais e evoluções listadas ao final

> Implementação iniciada em 06/10/2026. A Fase 0 foi validada para Relógio e Notas e o esquema local chegou à v8, incluindo clima e calendário por ambiente. Os demais tipos mantêm uma instância por ambiente até receberem runtime próprio.

> As fases 1 a 7 possuem MVP funcional. Na Fase 8, o compartilhamento usa o painel oficial do Windows e o Discord informa apenas o estado real do processo; voz permanece indisponível sem autorização oficial. Consulte a seção 18 para o resultado final e os limites preservados.

## 1. Objetivo

Este plano organiza as ideias futuras da GigaDock em entregas incrementais para Windows 10/11, C#, .NET 10 e WPF. Ele preserva os princípios do projeto: dados locais, ausência de backend e telemetria, uso de APIs públicas e documentadas, alterações globais do Windows somente após ação explícita e mensagens compreensíveis quando um recurso não estiver disponível.

O plano não considera todas as ideias igualmente prontas. Algumas dependem de uma mudança estrutural no sistema de widgets; outras exigem permissões do Windows, serviços externos ou autenticação. Cada entrega deve poder ser desativada, ter cancelamento adequado e evitar trabalho em segundo plano quando o recurso não estiver visível ou ativo.

## 2. Diagnóstico do código atual

O modelo `Ambiente` já contém `WidgetsInstalados`, e cada item possui um `Id`. Essa é uma boa base para várias instâncias. Entretanto, o fluxo atual ainda trata cada tipo como singleton:

- `MainViewModel` cria uma única instância de cada ViewModel (`Clock`, `Pomodoro`, `GitHub`, `Clima` e outros);
- `SincronizarWidgetsAmbiente` usa `FirstOrDefault(w => w.Tipo == ...)`, ignorando uma segunda instância do mesmo tipo;
- o gerenciador de atividade registra widgets por nomes fixos, como `Relogio` e `GitHub`, em vez do `WidgetInstanceConfig.Id`;
- `WidgetInstanceConfig` guarda apresentação e ordem, mas não possui configuração própria do conteúdo;
- usuário do GitHub, animação, localização do clima, compromissos e URL iCalendar ainda ficam em `Preferencias`, portanto são compartilhados;
- a loja impede instalar outro widget do mesmo tipo.

Se as funções novas forem implementadas antes de corrigir essa base, será necessário migrá-las duas vezes. A prioridade técnica deve ser transformar cada item da lista em uma instância executável real e definir claramente o que pertence ao aplicativo, ao ambiente ou à instância.

## 3. Arquitetura-alvo

### 3.1 Instância de widget como unidade de execução

Adicionar os seguintes contratos, com nomes ajustáveis durante a implementação:

```text
IWidgetFactory
  └─ Create(WidgetInstanceConfig, WidgetContext) -> IWidgetRuntime

IWidgetRuntime : IAsyncDisposable
  ├─ InstanceId
  ├─ Tipo
  ├─ ViewModel
  ├─ ActivateAsync()
  ├─ SuspendAsync()
  └─ ApplySettingsAsync()

WidgetRuntimeRegistry
  └─ Dictionary<WidgetInstanceId, IWidgetRuntime>
```

O `ItemsControl` da dock deve receber uma coleção ordenada de runtimes e selecionar a View com `DataTemplate`. O registro de atividade passa a usar o `Id` da instância. Serviços caros, como métricas, áudio ou rede, permanecem compartilhados e entregam dados a várias instâncias, evitando um timer ou watcher por cartão.

### 3.2 Configuração com escopo explícito

Cada propriedade nova deve declarar um dos três escopos:

| Escopo | Exemplos | Persistência |
| --- | --- | --- |
| Aplicativo | pasta padrão de capturas, consentimentos, endereço local do OBS | `Preferencias` |
| Ambiente | tema, cor, ordem, visibilidade e regras daquele ambiente | `Ambiente` |
| Instância | fuso de um relógio, duração de um Pomodoro, moedas escolhidas | `WidgetInstanceConfig` |

Adicionar a `WidgetInstanceConfig` um bloco versionado de configurações. A opção recomendada é um documento JSON por instância, validado por um codec tipado de cada widget. Isso mantém compatibilidade com versões antigas sem transformar o modelo central em dezenas de campos específicos. Valores desconhecidos devem ser preservados ao salvar.

### 3.3 Ciclo de vida e consumo de recursos

- Um runtime só fica `Active` quando instalado, habilitado e necessário no ambiente atual.
- Widgets que precisam alertar em outros ambientes usam um modo de segundo plano declarado e econômico.
- Polling de internet usa cache, `ETag` quando disponível, limite mínimo de atualização e cancelamento.
- `DeviceWatcher`, `FileSystemWatcher`, sessões de áudio e conexões WebSocket são compartilhados, desconectados no encerramento e suspensos quando não há assinantes.
- Atualizações da interface são agrupadas e enviadas ao `Dispatcher` somente quando algum valor visível mudou.
- Cada entrega deve comparar memória em repouso, quantidade de timers, threads, handles e consumo após alternar ambientes repetidamente.

### 3.4 Migração

Criar uma migração de esquema única para:

1. gerar IDs únicos onde houver ID fixo ou duplicado;
2. copiar configurações globais legadas para a primeira instância correspondente em cada ambiente;
3. preservar um backup antes da gravação;
4. tornar a migração idempotente;
5. manter leitura segura da versão anterior durante uma versão de transição.

## 4. Ordem recomendada

| Fase | Entrega | Dependência | Complexidade | Prioridade |
| --- | --- | --- | --- | --- |
| 0 | Runtime por instância e configuração por escopo | nenhuma | alta | bloqueadora |
| 1 | Controles de tempo configuráveis e lembrete de água | fase 0 | média | alta |
| 2 | Área de transferência, Downloads, recentes e capturas | fase 0 | média | alta |
| 3 | Wi-Fi, sinal e Bluetooth conectado | fase 0 | média | média |
| 4 | Áudio por aplicativo e microfone | fase 0 | alta | média |
| 5 | Cotação de moedas e GitHub robusto | fase 0 | média | média |
| 6 | OBS com estados reais | fase 0 | alta | média |
| 7 | Prévias de documentos e estante | fases 0 e 2 | alta | média |
| 8 | Discord oficial e compartilhamento entre dispositivos | pesquisa/decisão | muito alta | baixa |

As fases 1 a 3 podem avançar em paralelo depois da fase 0, mas cada uma deve ser entregue e validada separadamente.

## 5. Fase 0 — base para múltiplas instâncias e ambientes independentes

### Implementação

1. Criar `IWidgetRuntime`, `IWidgetFactory`, `WidgetContext` e `WidgetRuntimeRegistry`.
2. Migrar inicialmente dois widgets simples, Relógio e Notas, para provar o desenho.
3. Trocar propriedades únicas da dock por uma coleção de runtimes renderizada por templates.
4. Chavear atividade, estado e comandos pelo `InstanceId`.
5. Permitir que a loja adicione mais de uma instância quando a definição do widget tiver `PermiteMultiplasInstancias = true`.
6. Adicionar editor de configurações da instância selecionada.
7. Migrar os demais widgets em grupos: locais, sistema, internet e integrações.
8. Remover o uso de `FirstOrDefault` como mecanismo de execução depois que todos forem migrados.

### Critérios de aceite

- Dois relógios com fusos e estilos diferentes funcionam no mesmo ambiente.
- Duas notas mantêm conteúdos diferentes.
- O mesmo tipo pode ter configurações distintas em Trabalho e Pessoal.
- Excluir uma instância encerra seus timers, assinaturas e conexões.
- Trocar de ambiente não duplica eventos nem aumenta continuamente memória ou handles.
- Configurações antigas são migradas sem perder widgets, ordem ou aparência.

## 6. Fase 1 — tempo e lembrete de água

### Controles de tempo

Criar configurações por instância para duração do foco, pausas curta e longa, número de ciclos, volume, som, repetição, fuso e formato. Usar `TimeZoneInfo.GetSystemTimeZones()` para a lista de fusos disponíveis. Alarmes devem guardar o instante de vencimento, e não depender apenas de contagem de ticks; assim continuam corretos após suspensão do computador.

Separar o motor (`ITimerScheduler`) da interface. Um único agendador calcula o próximo evento e acorda somente quando necessário. Persistir o estado ativo de forma explícita e restaurá-lo sem tocar som automaticamente caso já tenha expirado há muito tempo.

### Lembrete de água

Adicionar um widget com intervalo, faixa de horário, dias ativos, quantidade diária opcional e período silencioso. O alerta pode usar a própria dock e, após consentimento, notificação do Windows. Não criar serviço permanente separado.

### Critérios de aceite

- Dois temporizadores funcionam de forma independente.
- Mudança de fuso e horário de verão atualiza a exibição sem alterar o instante do alarme.
- Suspender e retomar o computador não duplica alertas.
- O lembrete respeita período silencioso e pode ser adiado ou marcado como concluído pelo teclado.

## 7. Fase 2 — área de transferência, arquivos e capturas

### Área de transferência

Usar `AddClipboardFormatListener` e tratar `WM_CLIPBOARDUPDATE`, sem polling. O histórico deve ser opcional, limitado por quantidade e tamanho e mantido somente em memória por padrão. Fixar um item ou persistir histórico exige ação explícita. Não tentar classificar ou armazenar senhas; informar claramente que qualquer conteúdo copiado pode ser sensível.

MVP: texto e imagem, copiar novamente, excluir item e limpar tudo. HTML, arquivos virtuais e sincronização ficam para uma etapa posterior.

### Downloads e arquivos recentes

Monitorar apenas pastas conhecidas ou selecionadas pelo usuário, com `FileSystemWatcher`, debounce e reconciliação periódica curta quando o painel abrir. Exibir no máximo os itens mais recentes, validar o caminho antes de abrir e mostrar estado para arquivos removidos. Não indexar o disco inteiro.

### Capturas

Usar `Windows.Graphics.Capture` com o seletor seguro do Windows. A captura só começa por comando do usuário, que escolhe janela ou monitor. Validar `GraphicsCaptureSession.IsSupported`, inicializar o seletor com o HWND da janela WPF e salvar em uma pasta escolhida ou conhecida. Gravação contínua de tela fica fora do MVP.

### Critérios de aceite

- O listener não faz polling e é removido ao desligar o widget.
- O histórico respeita limites e pode ser apagado integralmente.
- Renomear, excluir ou mover arquivo não trava a dock.
- A GigaDock nunca captura uma janela sem o seletor/ação do usuário.

## 8. Fase 3 — Wi-Fi e Bluetooth

Criar serviços de Core independentes da plataforma:

```text
IRedeSemFioService
  └─ rede atual, intensidade, estado, atualização

IDispositivosBluetoothService
  └─ dispositivos associados e conectados, eventos de mudança
```

Para Wi-Fi, fazer uma prova técnica entre a API Native Wi-Fi para desktop e `Windows.Devices.WiFi`. A segunda pode exigir a capability `wiFiControl` e consentimento de localização precisa para certos dados. O MVP deve mostrar conexão atual, intensidade e um botão para abrir as configurações de rede; conectar, esquecer ou alterar rede fica fora do escopo inicial.

Para Bluetooth, usar `DeviceWatcher` com seletor e propriedades documentadas, incluindo estado conectado quando fornecido pelo dispositivo. Ativar o watcher somente enquanto houver widget/painel interessado. O MVP lista dispositivos conhecidos/conectados e abre as configurações do Windows; pareamento e transferência não entram nesta fase.

### Critérios de aceite

- Falta de permissão produz uma mensagem e uma ação para abrir a configuração adequada.
- Adaptador ausente ou desligado não gera exceção nem loop de tentativas.
- Mudanças de conexão aparecem por evento ou atualização controlada.
- Fechar/desabilitar o painel libera watchers e handlers.

## 9. Fase 4 — áudio por aplicativo e microfone

Implementar `IAudioSystemService` sobre Core Audio/WASAPI:

- MMDevice API para enumerar endpoints de reprodução e captura;
- `IAudioSessionManager2` e `IAudioSessionControl2` para sessões por aplicativo;
- `ISimpleAudioVolume` para volume e mudo da sessão;
- `IAudioEndpointVolume.SetMute` para silenciar o endpoint de microfone escolhido;
- notificações de dispositivo e sessão para manter a tela atualizada sem polling agressivo.

O controle deve mostrar claramente o dispositivo e o aplicativo afetados. Silenciar microfone é alteração global e só ocorre por clique/atalho explícito, com indicador persistente.

Definir a saída padrão do Windows não possui um caminho desktop público e documentado equivalente ao painel do sistema em todas as versões suportadas. O MVP deve listar as saídas e abrir a página oficial de Som para a troca. Uma prova técnica separada pode avaliar API pública compatível; não usar `IPolicyConfig` ou COM não documentado.

### Critérios de aceite

- Entrar/sair um dispositivo atualiza a lista sem reiniciar a dock.
- Alterar volume afeta apenas a sessão escolhida.
- Aplicativo encerrado desaparece sem deixar referência COM.
- Mudo do microfone requer ação explícita e reflete o estado real.

## 10. Fase 5 — moedas e GitHub

### Cotação de moedas

Usar uma fonte oficial estável, inicialmente as taxas de referência do Banco Central Europeu. Como a base é EUR e a atualização é diária, calcular pares cruzados localmente, armazenar cache de 24 horas e exibir fonte e horário. Em falha de rede, mostrar o último valor como desatualizado. O recurso é informativo e não deve sugerir preço de negociação em tempo real.

Configuração por instância: moeda base, lista de moedas, quantidade opcional, formato e intervalo mínimo de atualização.

### GitHub

Separar aquisição de dados, cache e animação. O primeiro incremento usa a API REST pública para atividade recente, respeitando `ETag`, cabeçalhos de limite e `X-Poll-Interval`. A API de eventos não representa o calendário anual completo e só mantém uma janela limitada.

Para o calendário anual, manter o leitor público atual como fallback identificado e coberto por testes de contrato, ou oferecer GraphQL somente após uma decisão de produto sobre autenticação. Dados privados exigem autorização; tokens nunca devem ficar em `settings.json`. O widget precisa mostrar estado offline, limite de requisições e data da última atualização.

### Critérios de aceite

- Sem atualização repetida quando a resposta não mudou.
- Cache é usado offline com indicação de idade.
- Nome de usuário e animação podem diferir entre instâncias.
- Falhas de rede, perfil inexistente e limite de API têm mensagens diferentes.

## 11. Fase 6 — OBS com estados reais

Substituir o placeholder por um cliente OBS WebSocket v5. O OBS Studio 28 ou superior já inclui o servidor; a porta padrão é 4455 e a autenticação é recomendada. A conexão deve ser somente com host local no MVP, validar porta, implementar `Hello`/`Identify`, desafio de autenticação, reconexão com backoff e assinatura de eventos.

Estados iniciais: conectado, streaming, gravação, pausa, duração e cena atual. Ações iniciais: iniciar/parar gravação, iniciar/parar streaming e trocar cena, com confirmação nas ações destrutivas. Senhas não ficam na configuração do aplicativo; se a conexão autenticada for oferecida, usar o Gerenciador de Credenciais do Windows e nunca registrar o segredo em logs.

### Critérios de aceite

- O estado exibido vem de resposta/evento do OBS, sem simulação otimista.
- OBS fechado ou versão incompatível gera estado desconectado compreensível.
- A reconexão tem limite e para quando o widget é removido.
- Comandos aguardam confirmação do OBS antes de mudar a interface.

## 12. Fase 7 — prévias e estante de arquivos

Criar `IFilePreviewProvider` com provedores por extensão e uma política central de segurança. Começar com imagens, texto simples, PDF por API pública compatível e metadados/thumbnail do Shell para formatos não interpretados. Nunca executar macros, scripts, conteúdo incorporado ou handlers arbitrários para produzir a prévia.

A estante guarda referências a caminhos escolhidos pelo usuário, não cópias ocultas. Deve suportar arrastar e soltar, itens ausentes, revalidação ao abrir, tamanho máximo de prévia, cancelamento e cache de thumbnails com orçamento. Pastas podem ser adicionadas somente por seletor ou arraste explícito.

### Critérios de aceite

- Arquivo grande ou corrompido não bloqueia a thread da interface.
- Extensão não suportada mostra metadados e ação Abrir.
- Arquivo removido é marcado como ausente.
- Cache respeita orçamento e pode ser limpo nos ajustes.

## 13. Fase 8 — itens que exigem decisão ou pesquisa adicional

### Discord

O placeholder atual não deve ser transformado em leitor de chamadas por engenharia reversa do IPC local. As interfaces oficiais encontradas para recursos sociais/voz exigem fluxo de autorização e elegibilidade do Discord Social SDK; Rich Presence permite que a GigaDock publique a própria atividade, mas não equivale a ler livremente a chamada atual do usuário.

Decisão necessária antes da implementação:

1. reduzir o escopo para abrir Discord, indicar processo em execução e publicar presença própria; ou
2. aceitar login/autorização e verificar elegibilidade do SDK oficial; ou
3. manter “participantes da chamada atual” como indisponível.

A terceira opção é a única que mantém integralmente a regra atual de ausência de conta e credenciais.

### Compartilhamento entre dispositivos Windows

Dividir em dois níveis:

1. **MVP seguro:** invocar a interface de compartilhamento do Windows para o arquivo selecionado, deixando o sistema escolher destinos disponíveis;
2. **compartilhamento próprio:** descoberta local, pareamento, identidade, criptografia, consentimento do receptor, retomada, limites, firewall e compatibilidade de rede.

O segundo nível constitui um subsistema próprio e não deve ser incluído junto a widgets comuns. Antes de aprová-lo, produzir um documento de ameaça e uma prova técnica entre dois computadores sem backend. Não anunciar compatibilidade com Compartilhamento por Proximidade sem uma API pública que a sustente.

## 14. Estratégia de testes por entrega

Cada fase deve incluir:

- testes unitários de validação, migração, cache e regras de ciclo de vida;
- testes de integração isoláveis por adaptadores falsos;
- testes reais marcados `SystemIntegration`, executados apenas em Windows compatível;
- acessibilidade por teclado, foco visível, leitor de tela e escalas de 100%, 150% e 200%;
- alternância repetida de ambientes e instalação/remoção repetida de widgets;
- medição antes/depois de memória privada, working set, handles, threads e atividade em repouso;
- build Release e registro fiel do que foi ou não executado.

Para integrações externas, os testes normais não devem depender de internet, OBS ou dispositivos físicos. Respostas e eventos são simulados nos testes automatizados; a validação real fica registrada separadamente.

## 15. Marcos de entrega

### Marco A — fundação concluída

- runtime por `InstanceId`;
- dois widgets iguais independentes;
- migração de configurações legadas;
- editor por instância;
- nenhuma regressão na troca de ambientes.

### Marco B — produtividade local

- tempo, água, área de transferência, Downloads, recentes e captura;
- limites de memória e privacidade documentados;
- funcionamento offline, exceto onde a internet é inerente.

### Marco C — sistema e integrações

- Wi-Fi/Bluetooth, áudio, moedas, GitHub e OBS;
- permissões e estados indisponíveis tratados;
- sem uso de APIs privadas ou segredos em arquivo.

### Marco D — arquivos e pesquisa avançada

- estante e prévias seguras;
- decisão formal sobre Discord;
- decisão formal sobre compartilhamento entre dispositivos.

## 16. Definição de pronto

Uma ideia só deve sair de “beta” quando:

1. a configuração e a migração estão versionadas;
2. o recurso funciona com múltiplas instâncias quando aplicável;
3. o ciclo de vida não deixa timers, eventos, handles ou conexões órfãs;
4. erros, permissões e modo offline possuem estados visíveis;
5. navegação por teclado e escala de tela foram verificadas;
6. build Release e testes aplicáveis passaram;
7. teste manual real foi executado no Windows 10 e 11 ou a limitação foi declarada;
8. README, `docs/STATUS.md` e instalador foram atualizados somente depois da validação.

## 17. Referências técnicas

- Microsoft — [WiFiAdapter.RequestAccessAsync](https://learn.microsoft.com/en-us/uwp/api/windows.devices.wifi.wifiadapter.requestaccessasync)
- Microsoft — [mudanças de acesso à localização para Wi-Fi](https://learn.microsoft.com/en-us/windows/win32/nativewifi/wi-fi-access-location-changes)
- Microsoft — [enumerar dispositivos e DeviceWatcher](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/enumerate-devices)
- Microsoft — [AddClipboardFormatListener](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener)
- Microsoft — [captura de tela com Windows.Graphics.Capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)
- Microsoft — [controles de volume de sessão](https://learn.microsoft.com/en-us/windows/win32/coreaudio/session-volume-controls)
- Microsoft — [IAudioEndpointVolume](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolume)
- OBS Project — [obs-websocket](https://github.com/obsproject/obs-websocket)
- GitHub — [REST API de eventos](https://docs.github.com/en/rest/activity/events)
- GitHub — [ContributionsCollection em GraphQL](https://docs.github.com/en/graphql/reference/objects#contributionscollection)
- Discord — [Social SDK e vinculação de conta](https://docs.discord.com/developers/discord-social-sdk/development-guides/publisher-level-account-linking)

## 18. Resultado da implementação

| Área | Entrega disponível | Limite mantido |
| --- | --- | --- |
| Instâncias e ambientes | IDs normalizados, configuração versionada, Relógio e Notas duplicáveis, clima, GitHub, calendário e estante isolados | Demais tipos usam uma instância por ambiente |
| Tempo e água | Fusos por relógio, Pomodoro configurável e lembrete com adiar/concluir | Vários Pomodoros ainda exigem runtime próprio |
| Área de transferência e arquivos | Listener por evento, histórico limitado, Downloads/Capturas e captura do Windows | Histórico de imagem e captura embutida ficam para evolução |
| Conectividade e áudio | SSID/sinal, Bluetooth conectado, sessões de áudio e mudo do microfone | Troca da saída abre Som, pois a troca direta não possui API desktop pública compatível |
| Moedas e GitHub | ECB com cache, REST GitHub com ETag e fallback anual identificado | Dados privados exigiriam autenticação, fora do escopo local atual |
| OBS | WebSocket v5 local, autenticação e gravação confirmada por resposta | Eventos, cenas, transmissão e reconexão automática continuam em beta |
| Estante | Texto, imagem, primeira página de PDF, metadados, cache limitado e painel Compartilhar | Não executa handlers, macros ou conteúdo incorporado |
| Discord | Abre o aplicativo e informa processo aberto/fechado | Canal e participantes não são lidos sem autorização do SDK oficial |
| Compartilhamento | Share Sheet oficial do Windows para o arquivo escolhido | Transferência própria entre computadores exige projeto e modelo de ameaças separados |

Validação automatizada em 07/10/2026: build Release aprovado; 160 testes normais e o teste de estabilidade de dois minutos aprovados separadamente. O instalador foi regenerado sem assinatura. Integrações com hardware, OBS, Discord, escala, leitor de tela, instalação e desinstalação continuam exigindo validação manual em Windows 10 e 11 antes de retirar o rótulo beta.

