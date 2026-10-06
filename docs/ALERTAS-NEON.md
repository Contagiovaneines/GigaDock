# Revisão dos alertas neon


## Alertas neon WhatsApp e Teams — 2026-10-06

- Inspecionado fluxo UserNotificationListener -> ToastNotificationService -> MainViewModel -> contorno/sombra WPF. Reconhecimento por nome da aplicação contendo WhatsApp/Teams; depende de notificações publicadas e permissão Windows, não leitura direta de conversas/presença.
- Corrigido incremento duplicado do Teams quando não há aplicativo fixado correspondente. Widgets WhatsApp/Teams passam a receber um incremento cada por evento; contagens do Windows ainda são sincronizadas pelo serviço compartilhado.
- DispararAlertaGlobal respeita AlertasVisuaisHabilitados, descarte e Dispatcher. Desativar a opção cancela o prazo e encerra alerta atual. Nova notificação reinicia prazo de 15 s. Remoção da animação conserva bindings e valores-base da sombra, sem ClearValue que os apagava.
- Contorno estático de alerta acima da superfície e do RGB gamer: WhatsApp verde #25D366, Teams violeta #8B7CFF. Pulso não vai mais até opacidade zero. Com animações desativadas conserva contorno; dock oculta/fullscreen/auto-hide pausa desenho e mantém apenas prazo do alerta.
- Adicionado menu de contexto Testar alerta neon (verde) no widget WhatsApp; Teams já tem Testar Alerta de Reunião (Roxo). Ambos passam pelo mesmo método global e exigem alertas habilitados.
- Build Release: zero erros, um aviso CS0067 existente em fake. Aplicativo e instalador publicados e release/GigaDock-Setup.exe atualizado.
- Limites: não testadas notificações reais recebidas de WhatsApp/Teams, permissão concedida/negada ou aparência do neon em execução. Testes de integração não repetidos diante do bloqueio de carregamento 0x800711C7 já registrado. Revisão de fluxo e compilação não equivalem a confirmação ponta a ponta.

### Conferência manual pendente

1. Habilitar Alertas visuais nos Ajustes e mostrar os widgets WhatsApp/Teams.
2. Abrir o menu do WhatsApp -> Testar alerta neon (verde); abrir o do Teams -> Testar Alerta de Reunião (Roxo). Conferir contorno, duração aproximada de 15 s e nova chamada reiniciando prazo.
3. Desligar alertas durante o efeito: contorno deve desaparecer. Com animações desativadas e alertas habilitados: contorno estático.
4. Ocultar/reabrir dock durante prazo; confirmar pausa visual e retomada somente enquanto alerta não expirou. Repetir com gamer RGB ativo.
5. Autorizar acesso a notificações no Windows e receber uma notificação real de cada app, sem apenas mensagem interna: verificar cor/contagem. Se acesso negado ou aplicativo não publicar toast, neon automático não pode funcionar.
