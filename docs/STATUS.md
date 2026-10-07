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
