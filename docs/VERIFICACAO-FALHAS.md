# Verificação de falhas


## Verificação e correções de falhas — 2026-10-06

- Revisadas as 12 falhas históricas da seleção ampla. Corrigida deduplicação de aplicativos fixados por caminho resolvido: nome curto e caminho absoluto do mesmo executável passam a produzir um item. Escopo global já corrigido na etapa de ajustes permanece explícito e preservado.
- Atualizados testes de schema para migração sequencial até v6, seções inline, botões de ação desativados por padrão e restauração conforme os padrões atuais. Testes de apps não fixados habilitam a preferência explicitamente; teste de troca de ambiente verifica isolamento. Tamanho do ícone é validado independentemente da altura configurada da barra.
- Teste de filtro do launchpad usa nome sintético para não colidir com aplicativos instalados no PC. Essa última alteração foi compilada após a execução; não reexecutada devido ao bloqueio de política observado.
- Executada suíte com filtro Category!=SystemIntegration: **120 resultados, 60 aprovados e 60 falhas por bloqueio de carregamento 0x800711C7**, zero falhas de assertion observadas entre testes que puderam executar. Não interpretar bloqueados como aprovados. Registro: docs/TestResults/verificacao-final.trx; classificação em docs/test-verification-summary.json.
- Não executados testes que alteram autostart/barra nativa; proteção de Controle de Aplicativo preservada. Não repetidos binários/auxiliares em caminhos alternativos.
- Build Release final da solução: zero erros, um aviso CS0067 existente em fake de tracking. Não foram feitos testes visuais, instalação ou validação de integrações reais.
- Próximos passos: executar os casos bloqueados em ambiente permitido e validar ponta a ponta ajustes, migrações, deduplicação, escopo, prévias e escalas. Não afirmar que não há falhas restantes, pois metade da suíte não pôde executar.
