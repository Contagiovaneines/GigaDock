# Evolução especial do Eevee

Somente Eevee ignora os prazos de uma e três horas. Quando selecionado e visível, espera 2 segundos e permanece parado de frente durante uma transformação de 3 segundos: silhueta branca, esfera luminosa, energia azul girando e revelação da nova forma. Ao final, assume a forma do clima em cache: chuva → Vaporeon; tempestade/trovoada → Jolteon; sol/calor/céu limpo → Flareon. Sem condição reconhecida, usa Vaporeon.

A forma fica fixa até trocar de espécie ou encerrar o aplicativo. Reaplicar a mesma seleção não repete a evolução. Trocar de Pokémon, ocultar o mascote ou encerrar cancela a transição pendente; ao reaparecer ainda como Eevee, a espera recomeça. A preferência de animações desativadas suprime o brilho, preservando a mudança de forma. Recursos de caminhada são locais; o efeito independe de resposta da PokéAPI.

Validação: compilação Debug e 10 testes aprovados para a escolha da forma por clima, prioridade de tempestade e contagem normal por escolha. O efeito visual e seu tempo real não foram inspecionados na dock; instalação manual não executada.
