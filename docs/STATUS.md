# Status técnico

Última atualização: 7 de outubro de 2026.

## Estado atual

O GigaDock 3.0.0 está em beta. A solução usa C#, .NET 10 e WPF e contém cinco projetos:

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
