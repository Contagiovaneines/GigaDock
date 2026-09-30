# Dock Windows — regras do projeto

Objetivo: aplicativo desktop original para Windows 10/11 com uma barra flutuante e ambientes Trabalho, Estudos e Pessoal. Cada ambiente possui seus próprios itens fixados, aparência e widgets.

- Use C#, .NET 10 e WPF. Se uma dependência ou API não for compatível, registre o motivo e proponha solução documentada.
- Interface em português do Brasil. Dados e configurações locais; nenhum backend, conta ou telemetria.
- Mantenha a barra de tarefas do Windows funcional. Não esconda ou mova janelas de outros programas na primeira versão.
- Integrações com Windows devem usar APIs públicas e documentadas. Não altere configurações globais sem ação explícita do usuário.
- Valide caminhos e URLs antes de executar. Exiba erros compreensíveis. Não armazene credenciais.
- Preserve acessibilidade, navegação por teclado e escalas de tela. Não copie marca, nome nem recursos gráficos do PrimoDock.
- Antes de encerrar uma etapa de implementação, compile e relate resultado e limites reais. Não declare como testado o que não foi executado.
- Consulte somente o arquivo de prompt da etapa solicitada. Salve saídas em `docs/` e atualize `docs/STATUS.md` com etapa, resultado e próximos passos.
