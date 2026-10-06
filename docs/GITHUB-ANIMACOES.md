# Animações do widget GitHub

Clique com o botão direito na grade da dock. O menu mostra o estilo atual e permite escolher Cobrinha, Pac-Man, Breakout, Galaga, Puzzle Bobble, Bomberman, Minesweeper ou desativar. Seleção salva na preferência local GitHubAnimacao, compartilhada entre ambientes como a configuração do usuário GitHub.

O menu anterior apontava para DefinirAnimacaoCommand inexistente; o comando agora recebe e valida os parâmetros. Estilos inválidos não alteram a escolha. Desativar cancela o reinício e restaura a grade.

São simulações decorativas próprias em WPF, sem assets externos: bola e raquete rebatendo em Breakout; nave, invasores e disparos em Galaga; bolhas e grupos removidos em Puzzle Bobble; bomba com explosão em cruz em Bomberman; revelação de casas, números de vizinhos e bandeiras em Minesweeper. Não são jogos interativos nem reprodução exata dos SVGs cujos arquivos não foram fornecidos.

A grade usa 91 células existentes, um único timer de 150 ms e paleta de brushes congelados compartilhada. Ao ocultar ou desabilitar animações, para o timer e restaura os níveis originais. Ciclos arcade duram até 140 quadros, seguidos de pausa de 3 segundos. Só iniciam com dados carregados, visibilidade e permissão de animação; não acrescentam consultas de rede. Não foi medido consumo de RAM ou CPU.

Compilação Release registrada em github-arcade-build.txt. Foram criados 12 casos de teste cobrindo seleção, entrada inválida, desativação, execução oculta e restauração após animar os cinco novos estilos. Tentativa local em github-arcade-tests.txt bloqueada pelo Controle de Aplicativo do Windows ao carregar DockWindows.Core.dll (0x800711C7): zero testes aprovados. Necessário executar no CI e validar legibilidade e interação no aplicativo. Instalador anterior não contém esta implementação.
