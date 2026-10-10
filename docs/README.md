# Documentação do GigaDock

Este índice reúne a documentação necessária para usar, desenvolver e distribuir o projeto.

| Documento | Assunto |
| --- | --- |
| [Instalação Linux](INSTALACAO-LINUX.md) | Dependências, instalação, atualização, remoção e diagnóstico |
| [Widgets: uso e auditoria](WIDGETS.md) | Funcionamento mínimo, estados da loja, bloqueios e validação Windows/Linux |
| [Widgets Linux](WIDGETS-LINUX.md) | Sensores, Flatpak, workspaces e scripts locais |
| [Validação Linux](LINUX-VALIDACAO.md) | Testes executados, requisitos e limitações reais |
| [Arquitetura](ARQUITETURA.md) | Projetos Windows/Linux e componentes compartilhados |
| [Recursos e memória](AUDITORIA-RECURSOS-E-MEMORIA.md) | Arquivos removíveis, limites de limpeza e plano de redução de RAM |
| [Bandeja Windows](BANDEJA-WINDOWS.md) | Ícones ocultos nativos, comportamento e limites de compatibilidade |
| [Central de notificações](CENTRAL-NOTIFICACOES.md) | Painel Windows, consentimento, identidade assinada e limites reais |
| [Pokémon Linux](POKEMON-LINUX.md) | Caminhada, temas, animações, validação e limites de paridade |
| [Status](STATUS.md) | Estado atual e próximos passos |
| [GitHub Actions](GITHUB-ACTIONS.md) | Builds, testes, artefatos e assinatura opcional |
| [Assinatura digital](ASSINATURA-DIGITAL.md) | Preparação e geração de instaladores assinados |
| [Site estático](SITE-VERCEL.md) | Execução local e configuração da hospedagem |
| [Licenças de terceiros](LICENCAS-TERCEIROS.md) | Avisos e atribuições que devem ser preservados |
| [Publicação no GitHub](PUBLICACAO-GITHUB.md) | O que enviar, auditoria de privacidade e histórico |

Os manifestos em [referencia/assets](referencia/assets/) registram origem e integridade dos recursos gráficos. Não são configurações pessoais.

## Arquivo local

Nem todo Markdown precisa acompanhar o código. Planos de etapas concluídas, relatos de implementação, hashes antigos, pesquisas de trabalho e capturas completas ficaram em `docs/local/historico/` e `docs/local/capturas/`. O Git ignora essa pasta, mas os arquivos continuam disponíveis no computador.

Logs e TRX ficam em `docs/TestResults/`; relatórios de instalador são gerados pelo build e ignorados. Guias e capturas necessárias ao README/site permanecem públicos. Licenças, avisos e créditos dentro de `src/` devem acompanhar a redistribuição dos recursos correspondentes.
