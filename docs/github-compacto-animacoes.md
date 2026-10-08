# GitHub compacto e animações — 2026-10-08

O compacto foi ajustado à referência enviada: superfície quadrada de 68 × 68 unidades WPF, cantos arredondados, gradiente grafite e grade verde 7 × 7 centralizada, sem cortar a última linha. O formato largo usa 300 × 68, total à esquerda e grade 36 × 7. A troca de estilo invalida também a medição do controle, para que a dock recalcule sua largura.

O controle anterior desenhava somente `Nivel`: ignorava as cores de cobrinha, cabeça, Pac-Man, fantasma e marcas arcade. Agora desenha esses estados e os símbolos de Minesweeper/Bomberman, com recorte de boca para Pac-Man. A fonte continua observada por mudanças de coleção e propriedades, sem timer adicional de desenho.

O motor de animação passou a usar as mesmas dimensões da apresentação: 49 células no compacto e 252 no largo. Posições, limites, nave, bola, bolhas, bombas e minas acompanham o número de colunas. A seleção de estilo reutiliza os dias já carregados, restaura os níveis originais e reinicia o efeito respeitando visibilidade e preferências de animação. O histórico recebido é mantido separado das células alteradas temporariamente pelas animações.

Dados ilustrativos aparecem apenas na galeria de estilos (`Preview`). Na dock, ausência de dados deixa a grade vazia; total não confirmado aparece como travessão e zero confirmado permanece zero. A legenda do total é “no período”, pois a fonte pública pode abranger mais de um ano civil.

Validação: solução Release compilada com 0 erros e 0 avisos. Adicionados 14 casos para os sete efeitos nas duas dimensões, verificando produção de quadros e restauração ao desativar. A tentativa de executar `GitHubAnimationSelectionTests` foi interrompida pelo bloqueio preventivo do projeto para Smart App Control. Os testes e a verificação visual em execução permanecem pendentes. Nenhuma política do Windows foi alterada.
