# Ajustes — revisão visual e funcional


## Ajustes: visual e layout adaptativo — 2026-10-06

- Paleta azul/grafite, cartões com mais respiro, ícone original GigaDock, rótulos com mais contraste e versão derivada da assembly. Foco visível nos interruptores e botões de navegação.
- Layout largo (>=1050 DIP): navegação + lista + edição. Intermediário (760–1049): navegação compacta por seletor e lista/edição lado a lado. Estreito (600–759): lista acima da edição, com rolagem no conteúdo. Seções sem lista usam toda a área disponível. Visualizações conserva painel próprio e é reposicionado junto com as demais seções.
- Tamanho mínimo reduzido para 600×500; tamanho inicial limitado à área de trabalho. Arredondamento de layout e pixels. Não foi substituído por Viewbox que reduzisse toda a interface.
- Corrigido fechamento por Close sem atribuir DialogResult em janela não modal; callbacks do ViewModel liberados ao encerrar.
- Corrigidos itens explicitamente globais: migração legada única, configurações novas sem globais automáticos, preservação em salvar/recarga e exibição em todos os ambientes apenas quando o usuário escolher escopo global. Alternar escopo conserva seleção; remoção/reordenação separam globais e locais. Migração inicial salva preferências diretamente sem substituir ambientes pela coleção ainda não carregada.
- Inspeção estática: XML válido e 29 comandos diretos com propriedades correspondentes no AjustesViewModel. Isso não equivale à execução de cada comando.
- Build Release: zero erros, um aviso CS0067 existente em fake. Tentativa de testes registrada em docs/TestResults/ajustes.trx: carregamento bloqueado pelo Controle de Aplicativo (0x800711C7). Nenhum resultado aprovado desta seleção foi declarado; bloqueio não contornado e auxiliares alternativos não executados para contorná-lo.
- Relatório estruturado: docs/ajustes-validation.json. Suíte ampla anterior, de outra etapa: 107 aprovados e 12 falhas; permanece histórica e não prova correção integral.
- Limites: falta testar layout WPF em execução, todas as seções, seleção/remoção/reordenação globais, teclado, DPI, múltiplos monitores, confirmações e importação/exportação. Integrações Windows não foram disparadas nesta etapa. Não declarar “tudo funcional” sem essas verificações.
- Próximos passos: executar a seleção em ambiente permitido, conferir 600/760/1050/1280 DIP e 100/125/150/200% de escala e resolver as falhas históricas ainda aplicáveis.
