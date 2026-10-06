# Inicialização com instância única


## 06/10/2026 — Instância única da dock

StartupUri removido: a janela principal é criada apenas após adquirir mutex nomeado Local por SID do usuário/sessão. Segundo lançamento sinaliza evento AutoReset e encerra antes de criar janela, widgets ou hooks. Processo principal recebe sinal sem polling e revela a dock existente, restaurando janela minimizada/ocultação automática. Encerramento secundário não restaura a barra do Windows indevidamente; mutex/evento/registro de espera liberados no encerramento. Mutex abandonado após falha permite nova inicialização.

Build Release aprovado: zero erros e aviso CS0067 existente no fake de teste. Log instancia-unica-build.txt. Não executado teste manual de múltiplos processos/UI nesta etapa. As instâncias antigas já abertas precisam ser fechadas para iniciar a versão corrigida. Instalador anterior não inclui esta mudança. Próximo passo: validar cliques rápidos, auto-hide, encerramento/reabertura e sessões distintas no executável atualizado.
