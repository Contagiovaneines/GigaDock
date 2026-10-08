# Transformação aleatória do Ditto

Somente quando a espécie selecionada é Ditto (#132), a primeira transformação ocorre após 30 minutos desde a escolha. Olha para frente durante o efeito local de esfera de energia de 3 segundos e assume uma das outras 150 espécies do catálogo. Mantém a forma por 30 minutos, volta a Ditto e aguarda outros 30 minutos antes de copiar uma espécie diferente da última. A nova forma anda, voa ou flutua segundo seus recursos existentes.

A seleção na Pokédex continua sendo Ditto. Não herda evolução por clima do Eevee nem o ciclo de sono do Snorlax, mesmo copiando essas formas. Trocar de espécie reinicia a habilidade; repetir a mesma seleção preserva o ciclo. O tempo continua enquanto oculto e, ao voltar, avança uma etapa se o prazo passou, sem executar todas as mudanças perdidas.

Validação: compilação Debug e 2 testes aprovados, cobrindo limites de 30/60/90 minutos, alternância entre Ditto e forma copiada, exclusividade, ausência de repetição da última cópia, reinício e alcance das 150 espécies em sequência determinística. Sem observação visual de um ciclo real na dock ou instalação manual.
