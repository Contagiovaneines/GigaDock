# Sono exclusivo do Snorlax

Somente Snorlax (#143) entra automaticamente no estado Dormindo. A cada 2 minutos desde sua escolha, cochila por 30 segundos usando a sequência local Sleep. Em seguida, volta à caminhada ou ao descanso conforme a atividade do usuário. O ciclo independe da inatividade do Windows; a primeira soneca começa no minuto 2, a seguinte no minuto 4.

As outras 150 espécies podem descansar após 2 minutos sem interação com o Windows, mas não dormem. Trocar de espécie reinicia o relógio; reaplicar a mesma seleção preserva o ciclo. A contagem continua enquanto o mascote está oculto. A atualização de estado ocorre a cada segundo, sem novas consultas de rede.

Validação: compilação Debug e 10 testes aprovados, cobrindo limites de entrada/saída das sonecas, repetição do ciclo, exclusividade entre as 151 espécies e relógio desde a escolha. Não houve observação de um ciclo real na dock.
