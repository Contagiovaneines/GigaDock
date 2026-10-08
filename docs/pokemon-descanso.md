# Descanso e sono

Auditoria local do catálogo SpriteCollab dos 151 Pokémon: 27 possuem Sit, incluindo Pikachu; 124 usam Idle como repouso. Todos os 151 possuem Sleep. Não existe uma pose sentada específica para todas as espécies.

Recursos de origem: [PMDCollab/SpriteCollab](https://github.com/PMDCollab/SpriteCollab). Créditos e condições de uso existentes em Assets/PokemonWalk continuam aplicáveis. Origens e hashes em pokemon-rest-assets.json.

No estado Descansando, Sit mantém seu último quadro frontal sentado; Idle reproduz sua sequência frontal. Dormindo reproduz Sleep. As poses ficam na linha da dock, inclusive para voadores em repouso, e respeitam a preferência de animações. A pausa breve para virar nas bordas conserva sua própria pose de virada.

Validação: compilação Debug concluída; 13 testes aprovados, com carregamento e ciclos de descanso/sono de todas as espécies. Não houve validação visual na dock nem reconstrução do instalador. Espécies sem Sit permanecem em sua pose Idle original.
