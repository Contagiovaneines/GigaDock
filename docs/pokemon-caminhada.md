# Caminhada real dos mascotes — 2026-10-08

A referência enviada mostra Pikachu de perfil com um ciclo de patas. O comportamento anterior deslocava GIFs de batalha, que não contêm essa sequência. A dock agora prioriza spritesheets Walk para os 151 Pokémon originais, com direções laterais próprias.

O deslocamento é de 18 unidades WPF por segundo. Cada ciclo de passos acompanha 16 unidades de distância real, respeitando as proporções de duração entre os quadros da sequência. Quando o mascote para por inatividade, a passada para junto. Nas extremidades ele muda de direção e faz uma pausa de 350 ms. Os quadros laterais têm um recorte transparente comum e a imagem mantém sua proporção com altura de 44 unidades, evitando alterações de tamanho entre passos. Visibilidade, animações desativadas e modo econômico continuam controlando o timer existente.

Os arquivos são incorporados ao aplicativo e não geram consultas novas. Os GIFs da PokéAPI continuam disponíveis como alternativa caso uma sequência local não possa ser carregada. Evoluções temporárias também usam a sequência da espécie exibida.

Fonte: [PMDCollab/SpriteCollab](https://github.com/PMDCollab/SpriteCollab), diretórios `sprite/0001` até `sprite/0151`. Formato consultado na [documentação do PMD](https://wiki.pmdo.pmdcollab.org/PMD_Sprite_Format): quadros horizontais, direções verticais e durações em unidades de 1/60 segundo. Usadas as linhas 2 e 6, correspondentes a esquerda e direita. Créditos e política originais foram preservados em `Assets/PokemonWalk`; os recursos têm condições próprias, separadas da licença MIT do código. Manifesto de origem e hashes em `docs/pokemon-walk-assets.json`.

Validação: conferidas as dimensões, as oito direções e as durações positivas dos 151 spritesheets. Solução Release compilada com 0 erros e 0 avisos. O teste `PokemonWalkAnimationTests` foi executado e aprovado: carregou e decodificou as 151 espécies, verificou quadros congelados com dimensões estáveis nas duas direções, durações positivas e repetição do ciclo por distância. Um erro inicial de URI de recursos foi identificado pelo teste e corrigido usando streams WPF de recursos relativos.

Limites: não houve inspeção visual do movimento na dock em execução. Descansar e dormir congelam a passada; animações próprias desses estados ainda não foram integradas. O instalador existente não foi regenerado nesta etapa.
