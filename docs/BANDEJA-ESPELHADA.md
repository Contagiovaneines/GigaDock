# Bandeja da dock — protótipo Windows

O botão tenta mostrar os controles acessíveis dos ícones ocultos em um painel da dock. Não usa uma lista de processos como substituto.

## Funcionamento

- Mostra a barra nativa caso a dock a tenha ocultado, abre os ícones ocultos por UI Automation e lê os botões disponíveis.
- Captura somente o centro de cada botão visível, até 32 × 32 pixels. As imagens ficam na memória, sem gravação, servidor ou telemetria.
- Fecha o painel nativo e mostra a grade na dock. Ao clicar, reabre a bandeja, procura o controle pelo nome e tenta InvokePattern.
- Um botão permite abrir diretamente a bandeja do Windows para usar menus e ações nativas. Nenhum processo é reiniciado para simular o clique.

## Limites

Experimental: não é um host independente de bandeja. O painel nativo aparece brevemente durante leitura e cliques. Atualiza ao abrir, sem monitoramento contínuo. Inclui somente controles dos ícones ocultos, não todos os ícones já visíveis na barra.

As imagens contêm o fundo do controle; não são recursos originais transparentes. A leitura depende de visibilidade, acessibilidade e versão do Windows; sobreposição de outra janela pode interferir na captura. Clique simples depende de InvokePattern; menu de botão direito e duplo clique não são reproduzidos. Nomes podem mudar entre leitura e clique.

Sem acesso à memória privada do Explorer, injeção ou protocolo privado. Linux não recebe este protótipo: precisa de outro provedor e validação por desktop.

## Verificação

Build WPF Release: zero avisos/erros. Dez testes existentes do controlador passaram; não validam a captura nem o clique do espelho.

O ensaio real desta sessão falhou antes da captura: a árvore acessível mostrou três painéis sem o botão de ícones ocultos. Três tentativas não permitiram confirmar imagens ou cliques do novo painel. O protótipo permanece sem confirmação funcional nesta sessão; nenhum aplicativo pessoal foi acionado.

Próxima validação: na instalação atualizada, comparar quantidade/imagens com o painel nativo, testar clique simples e atualização após adicionar/remover ícones, e repetir com outra escala de tela.

Referência: [Microsoft: enumeração dos ícones ocultos](https://devblogs.microsoft.com/oldnewthing/20250929-00/?p=111637).

A interface foi renderizada em teste WPF isolado: painel de 360 × 275 pixels com oito itens ilustrativos. Isso valida a apresentação, não a leitura da bandeja real. Instalador de desenvolvimento atualizado em `release/GigaDock-Setup.exe`.

## Correção: dois painéis abertos

O pedido de fechamento pela seta retornava sucesso antes de o Explorer realmente fechar o painel. Agora a dock recupera o foco pelas APIs WPF, aguarda a atualização e confirma que a janela nativa desapareceu. O pedido de fechamento só é enviado se ela continuar aberta, para evitar reabrir uma bandeja já fechada. A verificação espera até 800 ms; se o painel continuar visível, o espelho não abre.

Quatro testes novos verificam bandeja já fechada, fechamento demorado, pedido aceito sem fechamento e falha no pedido. Os quatro mais dez testes existentes passaram (14 no total). Essa validação usa provedores simulados; ainda falta confirmar a correção na sessão gráfica do usuário com o instalador atualizado.

## Correção: barra nativa permanecendo visível

A leitura registra se a barra estava oculta pela dock e volta a ocultá-la antes de mostrar o espelho. O fechamento e a restauração também são tentados quando não há ícones, ocorre erro ou termina o clique em um ícone. Uma barra deixada visível explicitamente pelo usuário é preservada. Não executa a restauração após encerrar a dock ou desativar o modo de barra principal.

Se não for possível voltar a ocultar a barra, o espelho permanece fechado e aparece uma mensagem. O botão explícito de bandeja do Windows mantém seu comportamento nativo. Ainda pode haver uma aparição breve durante leitura e clique: este espelho depende da janela acessível do Explorer.

Verificação: 19 testes passaram, incluindo cinco novos casos de restauração, preservação da barra visível e recuperação após falha. Build Release executado com sucesso. Testes usam provedores simulados; a ocultação visual no desktop do usuário precisa ser confirmada com o instalador atualizado.

## Fechamento do painel

Fecha pelo X, ao clicar fora, ao mudar para outra janela e após 15 segundos sem interação. Movimento, clique e teclado reiniciam o prazo. Antes de abrir, aguarda 250 ms e reforça a ocultação da barra anteriormente ocultada pela dock; ainda depende da leitura nativa e precisa de validação visual no desktop do usuário.
