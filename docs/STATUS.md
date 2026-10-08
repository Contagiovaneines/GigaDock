# Status técnico

Última atualização: 7 de outubro de 2026.

## Estado atual

O GigaDock 3.1.0 está em beta. A solução usa C#, .NET 10 e WPF e contém cinco projetos:

- `DockWindows.App`: interface e composição da dock;
- `DockWindows.Core`: modelos, contratos e regras;
- `DockWindows.Infrastructure`: persistência e integrações com Windows;
- `DockWindows.Installer`: instalação, atualização e desinstalação;
- `DockWindows.Tests`: testes automatizados.

Configurações e dados permanecem no computador. O projeto não possui backend, conta própria ou telemetria.

## Recursos implementados

- Ambientes independentes com aplicativos, coleções, widgets e aparência.
- Detecção de janelas abertas, indicadores de execução e miniaturas pelo DWM.
- Fixação de aplicativos tradicionais e atalhos estáveis para aplicativos MSIX.
- Temas, dimensões, transparência, divisores, contorno RGB e ocultação automática.
- Mídia, clima, calendário, Pomodoro, notas, lembretes e widgets de sistema.
- Wi-Fi e Bluetooth nos controles rápidos, respeitando as interfaces seguras do Windows.
- Integrações locais com notificações, Discord e OBS WebSocket.
- Instalador por usuário com preservação das configurações durante atualizações.
- Workflow do GitHub Actions para compilar, testar e gerar o instalador.

## Limites conhecidos

- Aplicativo e instalador ainda não possuem assinatura Authenticode confiável. O Smart App Control pode bloquear binários locais até a aprovação do fluxo de assinatura.
- O painel de aplicativos em segundo plano usa processos e pacotes reconhecidos; ele não acessa internamente a bandeja do Explorer.
- Pareamento Bluetooth, redes que exigem nova senha e seleção de saída de áudio usam as páginas oficiais do Windows.
- Alertas dependem das notificações que os aplicativos publicam no Windows.
- OBS exige WebSocket v5 local. O estado do Discord é limitado sem autorização oficial adicional.
- Múltiplos monitores, leitores de tela, escalas variadas e uso prolongado ainda precisam de validação mais ampla.

## Validação mais recente

Em 7 de outubro de 2026:

- `dotnet build DockWindows.slnx -c Release`: aprovado;
- resultado: **0 erros e 1 aviso preexistente** de evento não usado em um fake de teste;
- os testes não foram executados nesta etapa de organização do repositório;
- a suíte permanece configurada no GitHub Actions para execução em Windows.

### Mixer de volume

O widget de áudio passou a consultar e controlar também o volume principal pelo Core Audio, além das sessões por aplicativo e do microfone. O popup recebeu modos normal e compacto, entrada numérica exata, ajuste pela roda do mouse, mudo por clique central, paginação configurável, fixação no topo, ocultação temporária, cópia do nome do processo e acesso ao painel de Som clássico.

A seleção de saída abre a página oficial do Windows. A troca programática do dispositivo padrão não foi implementada porque as interfaces normalmente usadas para isso não constituem uma API desktop pública e documentada compatível com as regras do projeto.

## Organização do repositório

A pasta `docs` foi reduzida aos documentos ativos. Logs de compilação, inventários, capturas, vídeos, protótipos e relatórios temporários foram removidos. Projetos auxiliares de diagnóstico fora da solução, uma cópia antiga de XAML e o antigo roteiro de prompts também foram excluídos.

Arquivos mantidos em `docs`:

- `ASSINATURA-DIGITAL.md`;
- `GITHUB-ACTIONS.md`;
- `PLANO-IMPLEMENTACAO-IDEIAS-FUTURAS.md`;
- `SITE-VERCEL.md`;
- `STATUS.md`;
- `installer-validation.json`.

O `.gitignore` impede que resultados de build, publicação, testes, inventários, prévias e análises locais voltem a ser enviados ao GitHub.

## Próximos passos

1. Obter uma assinatura Authenticode confiável e validar o instalador assinado.
2. Executar o workflow completo após o próximo push.
3. Validar o aplicativo em múltiplos monitores, escalas e versões suportadas do Windows.
4. Priorizar os itens restantes do [plano de evolução](PLANO-IMPLEMENTACAO-IDEIAS-FUTURAS.md).

## Aplicativos abertos em todos os ambientes

Aplicativos com uma janela principal válida agora aparecem automaticamente na dock em qualquer ambiente, mesmo quando nunca foram fixados. A fixação controla somente se o item permanece na dock depois que todas as suas janelas são fechadas. A preferência antiga que ocultava aplicativos abertos não fixados foi mantida apenas para compatibilidade de leitura das configurações e deixou de filtrar a interface.

Validação: o cenário automatizado cobre configurações antigas com a opção habilitada e desabilitada; em ambos os casos, aplicativos abertos do ambiente atual, de outros ambientes e ainda não cadastrados devem entrar na lista. A solução Release compilou sem erros e com um aviso preexistente no fake de rastreamento. A tentativa de executar esse teste local foi interrompida antes da descoberta pela proteção já configurada contra alertas do Smart App Control; ele deverá ser executado pelo GitHub Actions.

## Área útil próxima da dock

No modo de barra principal, a AppBar agora reserva somente a altura visível da dock, a margem inferior e 4 pixels de separação. O espaço transparente usado acima da janela para sombras e magnificação deixou de reduzir a área útil dos programas. A dock continua visualmente separada das janelas sem desperdiçar a faixa superior reservada para seus efeitos.

## Ativação confiável pelo clique

O clique principal nos ícones deixou de ser consumido pela abertura de prévias e sempre tenta ativar ou restaurar uma janela real do aplicativo. Quando existem várias janelas, a dock escolhe primeiro a ativa, depois uma não minimizada. O foco visual é limpo somente depois da execução do comando, evitando cancelar o clique no intervalo entre pressionar e soltar o mouse. Identificadores de janela inválidos são descartados; para um item fixado, a execução normal é usada como recuperação.

## Cobertura de ícones

Aplicativos em execução agora priorizam o ícone associado à janela e ao AUMID antes do ícone obtido pelo caminho. Isso permite substituir resultados genéricos do Shell em aplicativos MSIX, Electron, processos hospedados e executáveis protegidos. Atalhos `.lnk` também usam a imagem declarada pelo próprio atalho por meio da API pública do Shell.

## Painel de aplicativos em segundo plano

O painel deixou de recarregar a coleção em cada mudança de foco de janela, o que eliminou a barra de carregamento e a piscada dos ícones durante o hover. A lista é consultada quando o painel abre. O clique tenta ativar a janela existente, usa o item correspondente da dock ou inicia a referência estável do aplicativo instalado, inclusive `shell:AppsFolder` para aplicativos empacotados.

## Reaproveitamento de APIs públicas do Windows

O rastreamento de janelas passou a observar também `EVENT_OBJECT_LOCATIONCHANGE`. Mudanças de tamanho e posição da janela ativa reavaliam tela cheia imediatamente; o polling foi reduzido de 600 para 2500 ms e permanece apenas como recuperação. A mudança evita reconstruir listas a cada pixel e reduz consultas contínuas.

A tela **Sobre** recebeu um diagnóstico local, consultado uma única vez ao abrir os ajustes, com versão/build do Windows, arquitetura, memória física e tempo ligado. A implementação usa `GetTickCount64`, `GlobalMemoryStatusEx` e informações do runtime, sem PowerShell, conta, backend ou telemetria.

## Limite horizontal da dock

A largura máxima acompanha a área útil do monitor com 12 pixels livres em cada lateral. Quando aplicativos, seções ou widgets excedem esse limite, a janela não passa da tela: o conteúdo navega horizontalmente pela roda do mouse, gesto de toque ou teclado, sem acrescentar uma barra de rolagem ao visual da dock. Mudanças de resolução recalculam o limite automaticamente.

## Refinamento dos controles rápidos

O popup recebeu largura e espaçamento mais confortáveis, cabeçalho alinhado, botão de fechamento maior, contador contextual e cartões de estilo com áreas de clique uniformes. Estados selecionado, hover e foco usam contraste consistente e preservam navegação por teclado.

## README atualizado

O documento principal agora registra o rastreamento global de aplicativos abertos, ativação confiável por clique, cobertura ampliada de ícones, limite horizontal, mixer, controles rápidos, painel de segundo plano e diagnóstico local. O estado beta foi dividido por área com limites verificáveis, e as ideias futuras foram reduzidas a uma lista de produto sem promessas de prazo.

## Demonstração interativa do site

A demonstração hospedada pela pasta `vercel` foi refeita para reproduzir a composição atual da dock: fundo escuro, contorno RGB, ampliação no hover, indicadores de execução, mídia, clima, Pomodoro, monitor do sistema, relógio em cartões, bateria e controles laterais.

Trabalho, Estudos e Pessoal mantêm listas e preferências independentes. O visitante pode adicionar aplicativos ou widgets pela biblioteca, entrar no modo de remoção, alternar o ambiente e ajustar contorno, tamanho e itens do sistema. As escolhas são persistidas apenas no armazenamento local do navegador e podem ser restauradas por ambiente. A dock usa rolagem horizontal quando o conteúdo ultrapassa a largura disponível.

Limite real: a página simula as interações e usa dados ilustrativos; por segurança do navegador, não abre programas, lê processos nem controla recursos do Windows.

## Proteção do instalador e Smart App Control

O instalador local foi confirmado como `NotSigned`, causa direta do bloqueio de publicador exibido pelo Smart App Control. O fluxo local de Authenticode foi corrigido para repassar SignTool e timestamp aos componentes internos, validar cada assinatura e interromper o pacote se o resultado final não for válido. O instalador também recebeu manifesto explícito de execução por usuário, compatibilidade com Windows 10/11, DPI e caminhos longos.

O GitHub Actions agora possui um fluxo SignPath em duas etapas: primeiro assina `DockWindows.App.exe`, monta o instalador usando somente esse aplicativo validado e depois assina `GigaDock-Setup.exe`. O artefato público recebe o nome `ASSINADO` apenas depois de ambas as validações. Builds sem a configuração do serviço continuam claramente identificados como `NAO-ASSINADO` e podem ser bloqueados.

Limite real: não há certificado confiável disponível localmente e a solicitação da SignPath ainda depende de aprovação e configuração no repositório. Portanto, nenhum binário foi declarado assinado nesta etapa e nenhuma proteção do Windows foi desativada.

## Avisos do GitHub Actions

As Actions de checkout e upload de artefatos foram atualizadas para versões baseadas em Node.js 24, removendo os avisos de descontinuação do Node.js 20 nos runners hospedados. O fake de rastreamento usado nos testes passou a implementar o evento opcional de tela cheia sem armazenar um delegado nunca acionado, eliminando o aviso `CS0067` sem alterar o comportamento do teste.

## Prévia visual fiel no site

A demonstração da pasta `vercel` foi revisada a partir dos controles XAML e da ordem padrão das seções do aplicativo. A composição agora segue Iniciar/Pesquisa, clima, mídia, aplicativos, contribuições, monitor, relógio e controles finais, usando altura, raio, fundo translúcido, divisores, indicadores e contorno RGB próximos da interface WPF.

Os símbolos genéricos foram substituídos por representações visuais reconhecíveis de Explorador, Chrome, Teams, Discord, Visual Studio Code, Edge e WhatsApp, com indicadores de execução e notificações. A demonstração passou a ser uma prévia estática; ela não sugere executar aplicativos ou controlar o Windows pelo navegador.

## Monitor compacto com CPU e RAM em anéis

A personalização do Monitor do Sistema recebeu o estilo **CPU e RAM em anéis**. Ele apresenta as duas porcentagens em indicadores circulares independentes, usa as mesmas leituras locais do monitor e ocupa 132 pixels na dock. A galeria de estilos mostra uma prévia ilustrativa e o nome acessível do controle informa os dois valores.

Também foi incluído o estilo **Medidores CPU e RAM**, com dois mostradores circulares metálicos, escala azul, zona de atenção em laranja e vermelho, ponteiros e percentuais. O desenho é vetorial, acompanha a escala de tela e reutiliza as mesmas leituras do monitor sem criar consultas adicionais.

## Gerenciamento de widgets e modo econômico

A tela de Widgets passou a mostrar um resumo de instalados, ativos e inativos, filtros por estado e cartões com estado textual, indicação visual, controle “Exibir na dock”, personalização e desinstalação. A Loja de Widgets agora diferencia itens disponíveis, instalados e ativos; sua ação principal muda entre instalar, ativar e configurar, e a remoção foi renomeada para “Desinstalar deste ambiente”.

A desinstalação exige confirmação e preserva dados locais por padrão. O usuário pode escolher apagar os dados da instância; para Notas, a pasta validada da instância é removida, e configurações locais do widget são limpas. A sincronização do ambiente desabilita imediatamente o runtime removido, interrompendo timers, consultas, painéis e listeners administrados pelo `GerenciadorAtividade`. Runtimes de instâncias adicionais continuam sendo descartados ao trocar de ambiente ou removê-los.

Ajustes > Geral recebeu o **Modo econômico**. Ele reduz o Monitor do Sistema de 2 para 10 segundos, a Bateria de 30 para 60 segundos e o fallback do rastreamento de janelas de 2,5 para 8 segundos. Animações contínuas e RGB ficam suspensos, vidro e blur deixam de ser aplicados, a sombra principal é reduzida, o cache de ícones cai de 128/8 MB para 48/3 MB e o cache de capas de mídia cai de 8 para 3 imagens. As preferências visuais originais são preservadas e voltam a valer quando o modo é desligado. O relógio já agenda a próxima mudança de minuto quando o estilo não mostra segundos, e widgets não essenciais já param seus serviços quando ficam ocultos.

Limite real: os ViewModels principais ainda são objetos estáveis por causa dos bindings WPF atuais. Seus timers, consultas e integrações são ativados sob demanda, mas a substituição integral por hosts/fábricas descartáveis exige uma migração estrutural posterior para permitir destruir e recriar o próprio ViewModel sem quebrar bindings. O trabalho pesado está pausado; permanece apenas a pequena alocação do objeto inativo.

Validação: a solução Release compilou com 0 erros e 0 avisos. A execução local dos testes foi impedida pela proteção do projeto contra o alerta conhecido do Smart App Control para `DockWindows.Tests.dll`; os testes deverão rodar no GitHub Actions ou com assinatura de código confiável.

## Novos layouts do player de mídia

O widget Mídia recebeu três layouts originais inspirados na interação analisada: **Compacto**, com capa e controles essenciais; **Ultralargo**, com capa, título, artista e navegação; e **Expansível no hover**, que ocupa pouco espaço na dock e abre um painel completo acima dela após um atraso curto. O painel expandido apresenta capa, metadados, posição, duração, progresso e controles de reprodução.

Os estilos podem ser escolhidos na personalização do widget ou diretamente pelo submenu **Layout** no botão direito. O mesmo menu permite **Ocultar deste ambiente**. A abertura e o fechamento usam atrasos separados para evitar piscadas ao mover o ponteiro entre a dock e o painel, também funcionam por foco do teclado e encerram timers e popup quando o controle é descarregado. Os layouts reutilizam as sessões públicas de mídia do Windows e não iniciam outra consulta ou serviço.

Validação: solução Release compilada com 0 erros e 0 avisos. A aparência usa componentes e desenho próprios do GigaDock; nenhum recurso gráfico do vídeo foi incorporado.

## Editor visual e interações reaproveitadas do showcase

A galeria de estilos agora apresenta cada opção dentro de uma miniatura própria da dock, com superfície escura, contorno discreto e o controle real usado na prévia. Os cartões foram ampliados e receberam os rótulos padronizados **Compacto**, **Largo** e **Expansível**, sem alterar a composição visual da dock principal.

O clima compacto passou a abrir a previsão detalhada ao clicar; no modo de abertura por mouse, o mesmo painel pode aparecer no hover. O calendário compacto agora abre uma agenda com até oito compromissos futuros, data, horário, título e local, também acessível por Enter ou Espaço.

Aplicativos fixados podem ser reordenados diretamente na dock por arrastar e soltar. O gesto só começa depois do limite de movimento do Windows, aceita apenas itens fixados e persiste a nova ordem no escopo global ou no ambiente ativo. Arquivos externos continuam usando o fluxo já existente de adição à dock.

Coleções visuais, biblioteca de aplicativos e ampliação suave já existiam e foram preservadas. Nenhuma cor, tamanho, ordem de seções ou aparência dos itens da dock foi redesenhada nesta etapa.

Validação: solução Release compilada com 0 erros e 0 avisos.

## Diagnóstico do bloqueio pelo Smart App Control

O erro após a instalação foi reproduzido e identificado como bloqueio do `DockWindows.App.exe` sem assinatura, não como falha de caminho ou extração. O executável instalado não possui fluxo `Zone.Identifier`, portanto `Unblock-File` não altera o resultado. A máquina não possui certificado válido de assinatura de código nem SignTool x64 disponível, então não é possível produzir localmente um pacote confiável para essa política.

O instalador agora reconhece os códigos e textos usuais de bloqueio por política e mostra uma explicação curta em português, orientando o uso de uma versão assinada. O script de empacotamento também identifica builds sem assinatura como artefatos de desenvolvimento. A solução definitiva para distribuição permanece assinar o aplicativo, suas bibliotecas próprias e o instalador com Authenticode e timestamp confiável.

## Instalador 3.1.0

Aplicativo, instalador e manifesto foram alinhados na versão **3.1.0**. O aplicativo win-x64 foi publicado, compactado no recurso `app.zip` e incorporado ao instalador independente. O artefato final está em `release/GigaDock-Setup.exe`; o relatório em `docs/installer-validation.json` registra tamanho, SHA-256 e situação da assinatura.

Validação: o pacote contém `DockWindows.App.exe`, não contém testhost, xUnit, cobertura ou assemblies de testes, e o SHA-256 do instalador corresponde ao relatório. A solução Release compilou com 0 erros e 0 avisos. O executável permanece sem Authenticode porque nenhum certificado de assinatura de código foi fornecido; o Smart App Control pode exibir aviso até que um instalador assinado e com reputação seja distribuído.

## Clima minimalista e largo

O widget Clima recebeu os layouts **Minimalista**, com ícone e temperatura, e **Largo**, com cidade, condição visual, temperatura e mínima/máxima disponíveis. Os dois aparecem na galeria de personalização e podem ser alternados diretamente pelo submenu **Layout** no botão direito do widget.

O clique continua abrindo a previsão completa por hora e por dia. Os novos layouts reutilizam o mesmo ViewModel, cache e ciclo de consulta, sem criar novas requisições ou timers.

Validação: solução Release compilada com 0 erros e 0 avisos.

## Mascotes Pokémon (Beta)

Ajustes recebeu a seção própria **Mascotes (Beta)**. Nela é possível escolher um dos 151 Pokémon originais, ativar ou desativar o mascote e manter uma seleção diferente em cada ambiente. O mascote aparece como um cartão compacto na dock, mostra nome e estado e anima somente quando a dock está visível e as animações estão permitidas.

O runtime consulta os endpoints públicos de Pokémon, espécie e cadeia evolutiva da PokéAPI somente quando o recurso está habilitado. Sprites são armazenados no cache local e reutilizados quando a rede fica indisponível. Após uma hora de atividade da sessão pode ser usada a segunda forma; após três horas, a forma final. O reinício do Windows restaura a forma escolhida. Pokémon sem evolução permanecem na mesma forma.

Eevee escolhe temporariamente Vaporeon em chuva, Jolteon em tempestade e Flareon em sol ou calor, reaproveitando a condição já mantida pelo Clima. A escolha fica estável durante a sessão. Sem condição disponível, usa Vaporeon como alternativa previsível. O widget observa apenas tempo ligado e inatividade do Windows; não registra teclas, textos ou títulos de janelas.

Limites atuais do beta: os sprites estáticos recebem movimento leve por WPF; ações específicas como sentar, correr e dormir ainda dependem de spritesheets próprios. A evolução usa o tempo ligado informado pela sessão do Windows, incluindo períodos anteriores à ativação do widget.

Validação: solução Release compilada com 0 erros e 0 avisos. Os endpoints reais de Bulbasaur, espécie, cadeia evolutiva e sprite foram consultados com sucesso; retornaram `bulbasaur`, evolução para `ivysaur` e sprite no host permitido `raw.githubusercontent.com`.

## Novos estilos de atividade do sistema

O Monitor do Sistema recebeu quatro opções adicionais: **Ventoinha da CPU**, **Rede compacta**, **Atividade compacta** e **Atividade larga**. Rede mostra download e upload reais; os dois formatos de atividade combinam CPU e RAM em anéis. Todos reutilizam o ciclo de leitura existente e não criam timers adicionais.

Como o Windows não oferece uma leitura pública e universal de RPM, a ventoinha é um indicador visual da carga da CPU e informa essa limitação na dica acessível. Ela não apresenta um valor de rotação inventado. As quatro opções possuem prévias reais na galeria e são salvas por ambiente.

Validação: solução Release compilada com 0 erros e 0 avisos.

## Estilos de contribuições do GitHub

O widget GitHub recebeu os estilos **Grade compacta** e **Resumo anual**. O primeiro mostra apenas uma grade quadrada de atividade; o segundo apresenta o total de contribuições no ano ao lado de uma faixa horizontal mais longa. Ambos usam os dados públicos já carregados pelo widget, preservam as animações existentes e são desenhados vetorialmente para acompanhar a escala da tela sem imagens externas.

Os estilos aparecem na galeria de personalização e também podem ser abertos pelo menu de contexto do próprio widget. A escolha é salva por ambiente.

Validação: solução Release compilada com 0 erros e 0 avisos.

## Ciclo e contorno dos alertas visuais

O alerta visual agora é encerrado ao abrir ou selecionar WhatsApp, Teams ou Discord pela dock, inclusive quando a janela do aplicativo já estava ativa. A heurística que interpretava toda segunda janela do WhatsApp como chamada foi removida; chamadas continuam sendo identificadas pelas notificações públicas do Windows e também terminam quando a notificação correspondente é removida.

O alerta deixou de colorir e ampliar a sombra externa da dock. A animação agora permanece no contorno nítido e no preenchimento colorido discreto, seguindo a composição do modo gamer sem espalhar o neon pela área ao redor.

Validação: solução Release compilada com 0 erros e 0 avisos.

## Refinamento visual de Ajustes e Loja de Widgets

As janelas de Ajustes e Loja de Widgets receberam uma linguagem visual unificada e própria do GigaDock. Ajustes agora usa uma navegação lateral mais ampla, seleção em cartões, superfícies neutras em camadas, bordas discretas, espaçamento maior e detalhes em violeta. O comportamento responsivo foi atualizado para preservar essas proporções em telas amplas e continuar recolhendo a navegação em larguras menores.

A Loja de Widgets ganhou navegação lateral por categoria, busca integrada, identificação do ambiente, contador de instalados e cartões com prévias grandes. Cada cartão informa categoria, disponibilidade, estado instalado/ativo, formato e ações de instalar, ativar, configurar ou desinstalar. As prévias usam gradientes e componentes do próprio projeto; nenhuma marca, imagem ou recurso gráfico da referência foi incorporado. A dock principal não foi alterada nesta etapa.

Validação: solução Release compilada com 0 erros e 0 avisos.


## Mascote na linha da dock e GIFs da Pok?API ? 2026-10-08

Implementado percurso horizontal na borda superior da dock, com invers?o nas extremidades, mantendo os itens livres. O mascote respeita largura dispon?vel, visibilidade, anima??es desativadas e modo econ?mico; descansa quando inativo. O GIF tem prioridade pelo campo `sprites.versions.generation-v.black-white.animated.front_default`, com PNG quando o campo animado n?o existe. Quadros s?o compostos com offsets, transpar?ncia, descarte e dura??o, com recorte comum para alinhar o sprite ? linha. Cache local separado por extens?o; URLs limitadas ao diret?rio oficial PokeAPI/sprites via HTTPS. Sem nova depend?ncia.

Valida??o: solu??o Release compilada com 0 erros e 0 avisos. Endpoint real de Bulbasaur e GIF consultados: HTTP 200, 19.484 bytes. Um programa tempor?rio de verifica??o dos quadros compilou, mas sua execu??o foi bloqueada pelo Controle de Aplicativo do Windows (0x800711C7); n?o houve valida??o visual nem confirma??o da decodifica??o em execu??o. Os GIFs s?o anima??es de batalha, n?o sequ?ncias espec?ficas de caminhada.

Pr?ximos passos: verificar visualmente percurso, alinhamento, DPI e quadros em uma execu??o permitida do aplicativo. Detalhes em `docs/mascote-gif.md`.

## Pokédex visual e busca de mascotes — 2026-10-08

A seção Mascotes recebeu uma Pokédex com 151 cartões ilustrados, seleção destacada, painel do companheiro escolhido e visual escuro com acentos verdes e coral. A busca instantânea aceita nome parcial, número e formatos como #025, ignora caixa, acentos e pontuação e distingue Nidoran fêmea e macho. Contador, botão Limpar e mensagem sem resultados completam a navegação. O filtro preserva a escolha salva por ambiente. O painel distingue o Pokémon escolhido da forma evoluída na dock ativa.

As 151 miniaturas do repositório oficial PokeAPI/sprites estão incorporadas ao aplicativo (125.426 bytes), permitindo galeria e busca offline. Licença original e manifesto de URLs, tamanhos e hashes foram preservados. O runtime animado da dock continua usando os GIFs e o cache existentes.

Validação: `dotnet build DockWindows.slnx -c Release --nologo` concluído com 0 erros e 0 avisos. Integridade dos 151 PNGs conferida por CRC, SHA-256 e sequência de IDs. Foram adicionados 14 casos de teste de busca; a tentativa de executá-los foi interrompida pelo bloqueio preventivo já existente no projeto para Smart App Control. Os testes não foram executados. A interface não foi validada visualmente nesta etapa.

Próximos passos: conferir galeria, seleção após filtrar, navegação por teclado e escalas de tela em execução permitida. Detalhes em `docs/pokedex.md`.

## GitHub compacto conforme referência e correção das animações — 2026-10-08

O compacto agora é quadrado (68 × 68), com superfície grafite arredondada e grade verde 7 × 7 centralizada. O estilo largo mantém total e grade em uma faixa proporcional à referência. As prévias da galeria usam as mesmas proporções.

Corrigido o desenho dos efeitos: o controle ignorava estados de cobrinha, Pac-Man e marcas arcade. Agora representa as sete animações do menu e seus símbolos. O motor usa os limites reais de cada estilo (7 × 7 ou 36 × 7), preserva os dados originais e restaura a grade ao desativar. A troca de estilo recalcula a largura e reaproveita o histórico carregado.

Validação: compilação Release com 0 erros e 0 avisos. Adicionados 14 casos cobrindo todos os efeitos nos dois formatos. A execução dos testes foi bloqueada preventivamente pelo projeto devido ao Smart App Control; não houve validação visual em execução. Próximos passos: conferir os sete efeitos, troca de formato, pausa ao ocultar e escalas de tela em execução permitida. Relatório em `docs/github-compacto-animacoes.md`.

## Diagnóstico do alerta Check.dll — 2026-10-08

Confirmado no log Microsoft-Windows-CodeIntegrity/Operational, evento 3077: o bloqueio refere-se ao Check.exe tentando carregar Check.dll em `%TEMP%/DockPokemonCheck-4f15a6a9213f48f9ba2b89c13407b81a/bin/Release/net10.0-windows/`. Esse é o verificador temporário criado durante a validação dos GIFs Pokémon, não uma biblioteca distribuída no GigaDock. A DLL não possui assinatura Authenticode; a política de integridade impediu seu carregamento.

Resultado: origem e motivo identificados por caminho, evento e inspeção da assinatura. Nenhuma configuração de segurança foi alterada e nenhuma nova execução desse verificador foi feita. Próximos passos: manter a verificação em execução pendente até haver ambiente permitido ou assinatura confiável. Etapa de diagnóstico, sem alteração de código; não exigiu nova compilação.

## Visual do indicador compacto CPU/RAM — 2026-10-08

O estilo Atividade compacta recebeu fundo azul-grafite, borda discreta, divisória central e anéis maiores com trilha visível. Os números e rótulos agora são centralizados pela largura real do texto, com mais contraste e indicação de porcentagem. A largura passou de 82 para 102 unidades WPF para acomodar leituras de três dígitos. O formato largo usa a mesma linguagem visual, e a prévia foi alinhada às dimensões do compacto.

O desenho trata 100% como círculo completo e valores indisponíveis como travessão. As leituras continuam usando o serviço existente, sem novos timers ou consultas.

Validação: `dotnet build DockWindows.slnx -c Release --nologo` concluído com 0 erros e 0 avisos. Não foi realizada validação visual em execução. Nenhum verificador temporário foi executado, devido ao bloqueio de integridade identificado anteriormente. Próximos passos: conferir legibilidade em diferentes escalas e leituras 0, 100 e indisponível em execução permitida.

## Clima: espaçamento e sobreposições nas prévias — 2026-10-08

Os layouts Minimalista, Temperatura e Largo receberam áreas separadas para ícone e texto, larguras revistas e fundo azul-grafite com borda discreta. O largo organiza mínima e máxima em duas linhas próprias, separadas da temperatura atual. Ícones vetoriais WPF substituem os símbolos de fonte para manter limites previsíveis. Textos têm uma linha, reticências e recorte por área, evitando que graus e números invadam outras regiões. Os formatos Local, Condição, Previsão e Horas também usam os ícones com dimensões fixas.

Na galeria, foi removido o Viewbox intermediário que ampliava as prévias menores. A área de apresentação tem largura fixa e só reduz os widgets quando necessário. A referência ilustrativa agora inclui mínimas explícitas.

Validação: solução Release compilada com 0 erros e 0 avisos. Não houve execução visual nem lançamento de verificadores temporários. Próximos passos: conferir as prévias e a dock em execução permitida, especialmente temperatura negativa, cidade longa e escalas de 125%, 150% e 200%.

## Instalador 3.2.0 — 2026-10-08

Versão compartilhada, constante do instalador, manifestos e README atualizados para 3.2.0. Aplicativo win-x64 publicado com as alterações recentes de mascotes/Pokédex, GitHub, CPU/RAM e clima; conteúdo compactado em app.zip e incorporado ao instalador independente.

Artefato: `release/GigaDock-Setup.exe` (173.715.506 bytes; 165,67 MiB). Cópia preservada em `dist/release-20261008-020254/GigaDock-Setup.exe`. Metadados de aplicativo e instalador confirmados em 3.2.0.0; hash SHA-256 conferido contra `docs/installer-validation.json`. O pacote contém o executável principal e não contém runners de testes, xUnit, cobertura ou Check.dll.

Validação: solução Release compilada com 0 erros e 0 avisos. Publicação e empacotamento concluídos. O instalador não foi executado nem instalado nesta etapa. Assinatura Authenticode: NotSigned; o certificado autoassinado local não foi tratado como assinatura confiável. Nenhuma configuração global ou repositório de certificados foi alterado. Próximo passo: validar instalação/atualização em ambiente permitido e providenciar assinatura confiável para distribuição sem o bloqueio já identificado do Smart App Control.

## README da versão 3.2 — 2026-10-08

README atualizado com a versão pública 3.2, correspondente ao aplicativo e instalador 3.2.0. Documentadas Pokédex, busca local, mascotes com GIFs e evolução, estilos e sete animações do GitHub, monitor CPU/RAM, novos layouts do clima e correções de espaçamento. Incluídas instruções de uso, cache e serviços externos, atribuição dos sprites e limites reais de validação.

Validação: alteração somente documental; revisão das funções contra o código e os registros desta sessão, com verificação de whitespace. Não houve nova compilação nem execução de testes. Próximos passos: atualizar os resultados documentados após a validação visual e de instalação em ambiente permitido.

## Bloqueio do instalador 3.2 e correção do fluxo de assinatura — 2026-10-08

Evento 3077 do CodeIntegrity confirmou que o Windows bloqueou `release/GigaDock-Setup.exe`; assinatura atual NotSigned. Disponível somente certificado autoassinado GigaDock OpenSource, inadequado para confiança pública do Smart App Control.

O empacotamento passou a rejeitar certificados autoassinados, verificar o resultado das assinaturas e recusar UnknownError. O arquivo em release só é substituído após a validação final do fluxo assinado. O auxiliar build_e_assinar.ps1 agora exige um certificado existente e não cria/importa certificados nem altera raízes de confiança.

Validação: sintaxe PowerShell dos dois scripts aprovada; tentativa com o certificado local recusada antes de publicar; solução Release com 0 erros e 0 avisos. O instalador existente permanece sem assinatura e bloqueado. Não foi realizada nova instalação nem alteração da proteção do Windows. Próximo passo necessário: disponibilizar certificado de provedor confiável ou configurar serviço de assinatura para assinar o aplicativo, componentes e instalador.

## Reconstrução do instalador após bloqueio Smart App Control — 2026-10-08

Aplicativo e instalador 3.2.0 reconstruídos e publicados pelo fluxo atualizado. Artefato em `release/GigaDock-Setup.exe`, cópia em `dist/release-20261008-021855/GigaDock-Setup.exe`. SHA-256 conferido contra o relatório de empacotamento. Publicação compilada com sucesso; instalador não executado.

Bloqueio permanece sem solução: assinatura NotSigned; somente certificado autoassinado disponível em CurrentUser e nenhum certificado de código disponível em LocalMachine. Fluxo SignPath existe no GitHub Actions, mas a configuração remota não pôde ser consultada porque gh está sem autenticação. Solicitada ao usuário a informação sobre certificado ou serviço de assinatura disponível. Próximo passo obrigatório para liberar instalação com Smart App Control ativo: assinatura confiável de aplicativo, componentes e setup. Reconstrução local não equivale a correção da confiança.

## Alerta de instalação no README — 2026-10-08

Incluído, a pedido do usuário, alerta CAUTION com destaque vermelho no GitHub e observação sobre o bloqueio do Smart App Control. Documentados os passos opcionais para desativar o recurso, impacto sobre todos os aplicativos, limitações de reativação e alternativa recomendada de assinatura confiável, com referência oficial da Microsoft. Nenhuma configuração do Windows foi modificada.

Validação: alteração somente documental e verificação de whitespace. Não houve compilação ou testes nesta etapa. Próximo passo: substituir a orientação de build sem assinatura quando houver instalador com assinatura confiável disponível.

## Passos reais para Pikachu e demais mascotes — 2026-10-08

Substituído o deslocamento visual de GIFs de batalha por sequências Walk locais para os 151 Pokémon originais. Cada espécie usa quadros laterais próprios para esquerda e direita, proporção estável, recorte comum e ciclo sincronizado à distância. Movimento a 18 unidades WPF/s, pausa de 350 ms nas pontas e interrupção da passada quando o mascote descansa. GIFs continuam como alternativa se o recurso local não puder ser carregado. Sem novas consultas ou timers.

Assets obtidos do PMDCollab/SpriteCollab, com créditos e política de uso preservados e documentados separadamente da licença MIT. README atualizado e relatório em `docs/pokemon-caminhada.md`; manifesto em `docs/pokemon-walk-assets.json`.

Validação: 151 spritesheets conferidos quanto a dimensões, oito direções e durações. Solução Release compilada com 0 erros e 0 avisos. Teste PokemonWalkAnimationTests executado e aprovado, incluindo decodificação WPF das 151 espécies, estabilidade de dimensões e ciclo por distância. Falha inicial de URI corrigida e teste repetido com sucesso. Não houve validação visual da dock em execução nem reconstrução do instalador. Próximos passos: conferir passada, alinhamento e pausas em diferentes escalas; integrar descanso/dormir específicos e regenerar instalador quando solicitado.

## Auditoria dos 151 Pokémon e voo por espécie — 2026-10-08

Conferidos os 151 recursos locais de movimento, com SHA-256 e manifesto por espécie. Adicionado comportamento aéreo para 18 voadores e flutuação para 9 espécies; demais 124 continuam no chão. Doduo/Dodrio permanecem terrestres. Selecionadas sequências Hover, Float, FlapAround, Idle e Special0 onde adequadas, com resolução de CopyOf e créditos preservados.

Aéreos recebem altura e oscilação suaves, animação por tempo mesmo ao parar horizontalmente e área vertical de 60 unidades para evitar cortes. Terrestres mantêm passos por distância. Sono pousa/congela e preferência de animações desativadas mantém imagem estática. Relatório e limites de poses em `docs/pokemon-voo.md`, inventário em `docs/pokemon-movement-audit.json`.

Validação: 13 testes executados e aprovados, incluindo decodificação WPF das 151 espécies, ciclo por tempo/distância e altura conforme espécie, sono e preferências. Solução Release compilada com 0 erros e 0 avisos. Não houve validação visual na dock em execução nem regeneração do instalador. Próximos passos: conferir voo/planagem e oscilação em diferentes escalas e incluir a alteração no próximo pacote solicitado.

## Tempo de evolução desde a escolha — 2026-10-08

Removida a evolução baseada no tempo ligado do Windows. A contagem começa ao escolher a espécie, com etapas após uma e três horas. Trocar de espécie reinicia o prazo; reaplicar a mesma seleção preserva tempo e forma. README e ajuda da Pokédex atualizados. Relatório: docs/pokemon-tempo-evolucao.md.

Validação: compilação Debug concluída e um teste aprovado cobrindo limites, tempo prévio do sistema, seleção repetida e troca de espécie. Sem validação visual nem reconstrução do instalador. Próximos passos: conferir na dock e incluir no próximo pacote.

## Virada dos Pokémon nas bordas — 2026-10-08

Mantidas as sequências laterais próprias de esquerda/direita. A pausa de 350 ms agora mostra uma pose frontal nos primeiros 175 ms e o novo lado antes de retomar o deslocamento, tanto no chão quanto no ar. Nenhuma direção de costas é usada. Recorte comum inclui a pose frontal para preservar tamanho e alinhamento.

Validação: compilação e teste das sequências dos 151 Pokémon, incluindo os quadros frontais; resultado registrado em docs/pokemon-virada.md. Próximos passos: conferir a virada visualmente na dock e incluir no próximo instalador.

## Poses de descanso e sono — 2026-10-08

Auditados os 151 Pokémon: 27 possuem Sit, 124 usam Idle e todos possuem Sleep. Recursos incorporados com créditos existentes preservados e manifesto docs/pokemon-rest-assets.json. Descansando mostra pose frontal sentada (último quadro de Sit) ou animação Idle; Dormindo usa Sleep. Ambos pousam na linha da dock. A escala considera a altura da caminhada para evitar ampliar a pose sentada.

Validação: compilação Debug concluída e 13 testes aprovados, incluindo decodificação das poses de descanso/sono dos 151 Pokémon e ciclos frontais. Relatório docs/pokemon-descanso.md. Sem inspeção visual na dock nem reconstrução do instalador. Próximos passos: conferir transição, escala e poses em diferentes telas e incluir no próximo pacote.

## Personagens Pac-Man no GitHub — 2026-10-08

Desenho WPF em pixels inspirado na referência enviada, com boca alternada orientada ao deslocamento, quatro fantasmas coloridos com olhos e pontos dourados no percurso. Funciona nas grades compacta e anual. Relatório: docs/github-pacman-sprites.md.

Validação: compilação Debug e 28 testes aprovados, incluindo direção, boca e quatro fantasmas nos dois estilos. Sem inspeção visual na dock nem atualização do instalador. Próximos passos: conferir legibilidade em escalas de tela e incluir no próximo pacote.

## Instalador 3.2 atualizado — 2026-10-08 às 03:18

Regenerado release/GigaDock-Setup.exe com todas as alterações atuais: evolução desde a escolha, movimento lateral e voo, virada frontal, descanso/sono dos 151 Pokémon e personagens Pac-Man no GitHub. Publicação Release win-x64 concluída para aplicativo e instalador. Artefato preservado em dist/release-20261008-031745/GigaDock-Setup.exe.

Verificados: executável do aplicativo dentro de app.zip idêntico à publicação recém-gerada, créditos presentes, ausência de artefatos de teste e cópia release idêntica ao instalador gerado. Versão 3.2.0.0, tamanho 176873522 bytes, SHA-256 ABB5CD89521613EE581A821CBCE8242EF5D18A4C8AD18907775750E7FA67E32C. Detalhes em docs/installer-validation.json.

Limites: instalador continua sem assinatura Authenticode e pode ser bloqueado pelo Smart App Control. Não foi executada instalação manual nem validação visual na dock. Próximos passos: validar instalação e interface em Windows e assinar com certificado confiável para distribuição.

## Eevee com evolução imediata por clima — 2026-10-08

Eevee passa a ser a única exceção aos prazos de horas: espera 2 segundos visível, faz brilho pulsante de 1,2 segundo parado de frente e assume Vaporeon/Jolteon/Flareon conforme o clima em cache. Mantém a forma na sessão. Troca de espécie, ocultação e descarte cancelam a transição. Preferência de animações respeitada. README, ajuda e relatório docs/eevee-clima.md atualizados.

Validação: compilação Debug e 10 testes aprovados para clima e contagem de evolução normal. Publicação Release win-x64 do aplicativo e instalador concluída; release/GigaDock-Setup.exe atualizado a partir de dist/release-20261008-032124. Metadados e hash em docs/installer-validation.json. Sem inspeção visual da transição ou instalação manual. Instalador permanece sem assinatura. Próximos passos: conferir efeito e cancelamento visualmente na dock e assinar o pacote com certificado confiável para distribuição.

## Transformação de evolução com esfera de energia — 2026-10-08

Inspecionados 102 quadros do GIF fornecido. Substituído o pulso simples por efeito WPF de 3 segundos: silhueta branca, esfera luminosa com arcos de energia azul/branca e partículas, revelação da espécie nova. Aplicado ao Eevee por clima e aos demais ao alcançar seus prazos de evolução. Sem fundo, temporizador extra ou dependência de download. Relatório docs/pokemon-evolution-effect.md.

Validação: compilação Debug/Release concluída; 23 testes de Pokémon aprovados e um teste adicional de renderização WPF aprovado após ajuste do UpdateLayout no teste. Prévia de seis quadros renderizados inspecionada em docs/pokemon-evolution-preview.png. Instalador 3.2 regenerado em release/GigaDock-Setup.exe, cópia preservada em dist/release-20261008-032453. Metadados em docs/installer-validation.json. Sem inspeção em tempo real na dock nem instalação manual; pacote continua sem assinatura digital. Próximos passos: verificar animação na dock e distribuição assinada.

## Instalador entregue novamente — 2026-10-08

A pedido do usuário, regenerado instalador 3.2 com o estado atual, incluindo o efeito de evolução com esfera de energia. Publicação Release win-x64 concluída para aplicativo e instalador. Conferidos hash do aplicativo no ZIP em relação à publicação, ausência de artefatos de teste e igualdade da cópia final release. Artefato preservado em dist/release-20261008-032552/GigaDock-Setup.exe; entrega em release/GigaDock-Setup.exe. Metadados e SHA-256 em docs/installer-validation.json.

Limites: instalação manual não executada; pacote permanece sem assinatura digital. Próximos passos: verificar instalação e animação na dock e obter assinatura confiável para distribuição.

## Inversão das direções dos Pokémon — 2026-10-08

Atendendo ao relato visual de deslocamento de costas, invertida a associação das linhas laterais dos sprites: linha 6 para esquerda e linha 2 para direita. A alteração vale para caminhada e voo; a pose frontal continua usando linha 0.

Validação: compilação Debug concluída e 13 testes de sprites/movimento aprovados. A validação verifica carregamento e ciclos, não identifica visualmente o lado para o qual cada espécie olha. Sem inspeção da dock em execução. Próximos passos: confirmar o sentido na dock após instalar a versão corrigida.
Instalador 3.2 também regenerado com a inversão: publicação Release concluída, entrega em release/GigaDock-Setup.exe e cópia preservada em dist/release-20261008-032809. Metadados em docs/installer-validation.json. Instalação manual não executada; permanece sem assinatura digital.

## Sono exclusivo do Snorlax — 2026-10-08

Somente Snorlax dorme automaticamente, com cochilos de 30 segundos a cada 2 minutos desde a escolha, independentemente de interação no Windows. As outras 150 espécies apenas descansam por inatividade. Consulta de estado a cada segundo, sem novas chamadas de rede. README atualizado; regras e limites em docs/snorlax-sono.md.

Validação: compilação Debug concluída e 10 testes aprovados, incluindo limites de entrada/saída, repetição, exclusividade entre as 151 espécies e relógio desde a escolha. Sem observação de ciclo real na dock. Próximos passos: conferir soneca e retomada na dock.
Instalador 3.2 regenerado com o sono exclusivo: publicação Release concluída, entrega em release/GigaDock-Setup.exe e cópia preservada em dist/release-20261008-033007. Metadados em docs/installer-validation.json. Instalação manual não executada; permanece sem assinatura digital.

## Transformação aleatória exclusiva do Ditto — 2026-10-08

Ditto (#132) sorteia outra espécie após 2 segundos e muda novamente a cada 2 minutos, entre as outras 150 do catálogo, sem repetir a forma atual. Usa a esfera de transformação e movimento da forma copiada. Permanece selecionado como Ditto, sem herdar evolução do Eevee ou sono do Snorlax. Trocar de espécie reinicia o ciclo. README atualizado e relatório docs/ditto-transformacao.md.

Validação: compilação Debug e 11 testes aprovados para exclusividade, intervalos, reinício, sorteio e alcance das espécies, além do sono exclusivo do Snorlax. Sem observação visual na dock. Próximos passos: confirmar transformação e movimento das formas copiadas na dock.
Instalador 3.2 regenerado com a habilidade do Ditto: publicação Release concluída, entrega em release/GigaDock-Setup.exe e cópia preservada em dist/release-20261008-033135. Metadados em docs/installer-validation.json. Instalação manual não executada; permanece sem assinatura digital.

## Pausas frontais durante o passeio — 2026-10-08

Pokémon param horizontalmente e olham para frente por 2 a 4 segundos após intervalos aleatórios de 6 a 12 segundos de movimento, retomando o mesmo sentido. Terrestres ficam parados; voadores continuam o ciclo frontal no ar. Descanso, sono e transformação têm prioridade. Sem novos temporizadores. README atualizado e relatório docs/pokemon-pausas-frontais.md.

Validação: publicação Release do aplicativo e instalador concluída. Instalador 3.2 atualizado em release/GigaDock-Setup.exe; cópia preservada em dist/release-20261008-033222. Metadados em docs/installer-validation.json. Sem inspeção visual na dock, instalação manual ou novos testes automatizados nesta alteração visual localizada. Pacote permanece sem assinatura digital. Próximos passos: conferir frequência das pausas e retomada na dock.

## Ditto alternando formas a cada meia hora — 2026-10-08

Ajustado o ciclo conforme solicitado: 30 minutos como Ditto, transformação de frente, 30 minutos como espécie copiada, retorno a Ditto e mais 30 minutos antes de copiar outra. Não repete a última espécie copiada. Mantidas exclusividade, animação e seleção original na Pokédex. README e docs/ditto-transformacao.md atualizados.

Validação: compilação Debug e 2 testes aprovados, cobrindo limites de 30/60/90 minutos, alternância, exclusividade, reinício e alcance das 150 formas. Não foi observado um ciclo real na dock. Próximos passos: conferir a alternância em execução.
Instalador 3.2 regenerado: publicação Release concluída, entrega em release/GigaDock-Setup.exe e cópia preservada em dist/release-20261008-033348. Metadados em docs/installer-validation.json. Instalação manual não executada; permanece sem assinatura digital.

## Pausas raras somente no centro da dock — 2026-10-08

Ajustado conforme refinamento do usuário: pausas frontais somente ao cruzar o meio da dock, após acumular de 90 a 180 segundos de movimento efetivo. Para no centro exato do percurso por 2 a 3 segundos e retoma o mesmo sentido. Sono, descanso e pausas não contam como caminhada. README e docs/pokemon-pausas-frontais.md atualizados.

Validação: publicação Release do aplicativo concluída. Sem inspeção visual na dock ou novos testes automatizados nesta alteração visual localizada. Próximos passos: conferir a frequência e alinhamento no centro em execução.
Instalador 3.2 também regenerado: publicação Release concluída, entrega em release/GigaDock-Setup.exe e cópia preservada em dist/release-20261008-033653. Metadados em docs/installer-validation.json. Instalação manual não executada; pacote permanece sem assinatura digital.
