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
