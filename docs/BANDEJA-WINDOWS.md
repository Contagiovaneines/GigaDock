> Atualização: o botão agora tenta o [protótipo da bandeja espelhada](BANDEJA-ESPELHADA.md). O acesso nativo continua disponível no painel. A documentação abaixo descreve o provedor nativo.

# Ícones ocultos do Windows

A seta da dock solicita a abertura da bandeja nativa do Windows por UI Automation. O próprio Windows mantém os ícones, a ordem, os menus de contexto e as ações dos aplicativos. O painel permanece na posição definida pelo Windows; não é incorporado nem reposicionado dentro da dock.

No modo de barra principal, a dock havia ocultado e desativado a barra nativa com um watchdog. Antes de abrir a bandeja, agora restaura a barra pela mesma infraestrutura, interrompendo esse watchdog. A barra fica temporariamente disponível; o botão Mostrar/esconder barra permite ocultá-la novamente. A preferência de modo principal não é alterada nem salva por essa ação. No modo normal, a seta não modifica a configuração da barra.

A implementação anterior mostrava processos em execução, filtrados por instalação e nomes conhecidos. Isso produz uma lista diferente da bandeja: alguns processos não registram ícones, enquanto ícones de status podem pertencer a programas não reconhecidos pelo filtro. A lista anterior não deve ser descrita como uma cópia da bandeja.

`WindowsTrayService` encontra o HWND da barra com FindWindow e usa AutomationElement.FromHandle, evitando depender de uma barra oculta aparecer entre os controles do desktop. Busca somente botões dentro dessa barra e usa InvokePattern, sem leitura da memória do Explorer, mensagens privadas ou clique por coordenadas.

O Windows desta máquina expôs o nome **Mostrar Ícones Ocultos Mostrar ícones ocultos**, com texto repetido, ID genérico `SystemTrayIcon` e classe `SystemTray.AccentButton`. A correspondência agora aceita o prefixo acessível sem diferenciar maiúsculas, ou os identificadores específicos conhecidos. O ID genérico não basta: volume, relógio e outros ícones podem compartilhar esse mesmo ID. Esses valores do shell não constituem contrato de compatibilidade entre todas as versões do Windows.

Se o controle estiver ausente, desabilitado, fora da tela ou indisponível, aparece orientação em um pequeno painel dentro da dock, acompanhando o tema. Não exibe mais uma MessageBox bloqueante para essa falha. A coordenação limita a espera a cinco segundos, com cancelamento verificado antes de invocar o controle. Quando todos os ícones estiverem visíveis, pode não existir painel oculto.

Validação da correção: solução Release compilada sem erros/avisos; 15 testes de controles, ordem de restauração e reconhecimento do botão aprovados. A barra oculta foi encontrada por HWND. Um teste pelo botão existente da própria dock a restaurou, executou o serviço C# atualizado e confirmou a janela nativa de ícones ocultos visível. O teste fechou o painel e devolveu a barra ao estado anterior. A sequência de coordenação foi testada com serviços controlados, sem mudar preferências pessoais.

Prévias WPF reais em paletas clara/escura, renderização 96/144/192 DPI, foco e comandos conferidos. O contorno isolado foi removido; botões usam a mesma superfície de 32 px, e a seta usa um chevron simples. Escalas renderizadas não equivalem a testes em três monitores físicos. Windows 10 e outras versões do Explorer ainda precisam de validação.

O código anterior de listagem por processos foi preservado nesta mudança para evitar uma remoção extensa junto da integração; a seta deixou de acionar essa listagem. Linux não recebe a integração do Explorer.

Evidências atuais: `docs/TestResults/controles-bandeja/`, `docs/local/capturas/controles-novos/`, `docs/local/inspecionar-bandeja.ps1` e `docs/local/native-tray-probe/`. A inspeção privada de nomes da barra fica ignorada pelo Git. Evidências da etapa anterior de ativação permanecem em `docs/TestResults/bandeja-windows/`; sua rodada combinada interrompida não conta como aprovação.

No Linux foram padronizados os botões de adicionar/ajustes e executado o smoke em Ubuntu/WSLg com dados isolados: 81 testes Linux, 58 compartilhados, caminhada nos seis temas e widgets locais aprovados. Não há implementação de bandeja StatusNotifier/AppIndicator ou enumeração equivalente dos processos nesse frontend; ícones de segundo plano continuam no painel do próprio desktop. Nenhuma chamada ao serviço Windows é feita pelo Linux. Outras distribuições/desktops ainda precisam de validação.

Instalador Windows local sem assinatura e pacote Linux x64 foram regenerados. Instalar o novo build e reiniciar o aplicativo é necessário para atualizar a dock que já está aberta; não houve instalação automática no aplicativo pessoal.

Referências: [invocação por UI Automation](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/invoke-a-control-using-ui-automation), [registro e atualização de ícones de notificação](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw).

