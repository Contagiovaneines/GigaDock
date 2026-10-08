# Mascote: percurso e GIF

O mascote percorre a borda superior a 24 unidades WPF por segundo, vira nas pontas e usa os GIFs animados de Black/White informados pela Pok?API. A ?rea transparente ? recortada com limites comuns aos quadros. Visibilidade e prefer?ncias de anima??o controlam o timer; descarregar o controle remove assinaturas e interrompe a execu??o. A pr?via nos ajustes mostra o primeiro quadro.

Fontes verificadas em 2026-10-08:
- https://pokeapi.co/docs/v2
- https://github.com/PokeAPI/sprites
- https://pokeapi.co/api/v2/pokemon/1
- https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/generation-v/black-white/animated/1.gif (HTTP 200; 19.484 bytes).

GIFs de batalha s?o usados durante o deslocamento horizontal; n?o representam passos direcionais espec?ficos. Falhas de rede usam o cache quando dispon?vel. Sem cache, ? necess?rio acesso inicial ? rede.

Valida??o: `dotnet build DockWindows.slnx -c Release --nologo` passou com zero avisos e erros. Um execut?vel tempor?rio de teste dos GIFs foi bloqueado pelo Controle de Aplicativo (0x800711C7). A composi??o dos quadros e o alinhamento visual ainda precisam de verifica??o em execu??o permitida. Nenhuma pol?tica do Windows foi alterada.
