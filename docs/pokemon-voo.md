# Auditoria dos mascotes e movimento aéreo — 2026-10-08

Os 151 Pokémon originais continuam com recursos locais de movimento. A escolha agora considera o comportamento visual da espécie, com 18 voadores, 9 flutuantes e 124 terrestres.

| Comportamento | Espécies |
| --- | --- |
| Voo | Charizard, Butterfree, Beedrill, Pidgey, Pidgeotto, Pidgeot, Spearow, Fearow, Zubat, Golbat, Venomoth, Farfetch’d, Scyther, Aerodactyl, Articuno, Zapdos, Moltres, Dragonite |
| Flutuação | Magnemite, Magneton, Gastly, Haunter, Koffing, Weezing, Porygon, Dragonair, Mew |
| Chão | Demais 124, incluindo Pikachu, Doduo e Dodrio |

A classificação é uma escolha visual do aplicativo por espécie, não uma reprodução automática dos tipos da Pokédex. Assim Doduo/Dodrio não são elevados apenas por possuírem tipo Voador. Evoluções recebem o comportamento da espécie atualmente exibida.

Foram priorizadas sequências disponíveis no PMDCollab: Hover, Float e FlapAround; Dragonite usa Special0, que apresenta voo lateral. A referência CopyOf é resolvida, como Hover de Zubat que utiliza os quadros Shoot. Outras espécies usam Idle ou Walk apropriados, com deslocamento aéreo; nem todos os assets oferecem uma sequência exclusiva chamada Fly. Farfetch’d, por exemplo, usa pose lateral de Idle em deslocamento aéreo.

Voo fica a 9 unidades WPF acima da linha, flutuação a 6, com oscilação suave de 2 unidades. A animação aérea acompanha o tempo e continua quando o deslocamento horizontal pausa, permitindo bater asas/pairar. Terrestres continuam com ciclo por distância. Ao dormir, o mascote pousa e congela os quadros. Ao desativar animações, a posição aérea fica estática. A área vertical aumenta de 44 para 60 unidades para evitar cortes na oscilação.

Manifesto dos recursos aéreos em `docs/pokemon-air-assets.json`. Auditoria dos 151 recursos ativos em `docs/pokemon-movement-audit.json`. Créditos e condições do PMDCollab preservados junto aos assets.

Validação: teste de carregamento/decodificação das 151 espécies aprovado, com dimensões estáveis, quadros congelados, durações e repetição de ciclos. Outros 12 casos verificam classificação, altura, sono e animações desativadas. Total: 13 testes executados e aprovados. Solução Release compilada com 0 erros e 0 avisos. Não houve conferência visual na dock em execução nem atualização do instalador.
