# Alertas visuais de WhatsApp


## 06/10/2026 — Prioridade e duração dos alertas WhatsApp

RGB de música/gamer suprimido durante alertas, retomado após o término. Mensagens usam fila com pulso único de 700 ms e intervalo de 150 ms por evento recebido. Chamadas reconhecidas usam cor fixa e prioridade sobre mensagens; removido encerramento genérico após 15 segundos e por clique na dock. Remoção da última notificação de chamada encerra o estado, mensagens aguardam na fila. Animação de sombra limitada a um ciclo; preferências de animação reduzida mantidas. IDs de toast deduplicados; reconhecimento textual de chamada mais específico e exclui chamada perdida/encerrada.

Limite real: UserNotificationListener observa notificações, não o protocolo de chamada do WhatsApp. Atender/rejeitar/encerrar só encerra o alerta automaticamente quando o WhatsApp remove a notificação correspondente. Se o toast não existir, a permissão for negada ou não houver evento de remoção, não há confirmação confiável do estado da chamada. Não prometer detecção garantida de atendimento. Alertas visuais nos ajustes podem ser desligados para limpar o estado. Visual e chamadas reais ainda não validados; instalador anterior não inclui esta mudança. Build Release aprovado, zero erros e aviso CS0067 existente. Teste de prioridade adicionado, mas bloqueado ao carregar DockWindows.Infrastructure.dll pelo Controle de Aplicativo do Windows (0x800711C7): zero testes aprovados nesta etapa. Resultado em TestResults/whatsapp-alerta.trx. Build final incremental: zero erros e zero avisos.
