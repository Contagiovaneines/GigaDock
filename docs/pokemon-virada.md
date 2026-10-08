# Virada nas bordas

Os 151 Pokémon usam sprites laterais próprios para cada direção. Ao alcançar uma borda, param por 350 ms, mostram a pose frontal durante 175 ms e depois assumem o novo lado antes de retornar. A mesma regra vale para caminhada, voo e flutuação. Não são usados quadros de costas.

O recorte comum das três direções mantém dimensões estáveis durante a troca. A preferência de animações desativadas continua interrompendo o movimento.

Validação: compilação Debug concluída e 13 testes aprovados, incluindo carregamento dos sprites laterais e frontais dos 151 Pokémon, dimensões, ciclos e regras de altura. Não houve inspeção visual da dock em execução nem atualização do instalador.
