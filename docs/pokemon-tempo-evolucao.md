# Tempo de evolução

A evolução usa um relógio monotônico iniciado na escolha da espécie, em vez do tempo ligado do Windows. A segunda forma fica disponível após uma hora; a forma final, após três horas desde a escolha, quando existentes. Eevee é a exceção: espera 2 segundos visível, faz uma animação de 3 segundos e evolui conforme o clima disponível. As evoluções por horas usam o mesmo efeito de transformação.

Trocar de espécie reinicia a contagem e a forma. Reaplicar configurações com a mesma espécie preserva o relógio e a evolução. O tempo pertence à sessão do aplicativo e continua passando quando o mascote fica oculto. Fechar o aplicativo descarta a contagem.

Validação: compilação Debug concluída durante o teste. Um teste aprovado verifica escolha com cinco dias de tempo prévio, limites de uma e três horas, preservação ao repetir a mesma espécie e reinício ao trocar. Sem validação visual na dock ou reconstrução do instalador.
