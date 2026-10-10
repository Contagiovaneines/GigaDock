# Contribuindo para o GigaDock

Faça um fork, trabalhe em uma branch e abra um pull request com o problema resolvido, o comportamento resultante e a validação executada.

## Desenvolvimento

- Use C# e o SDK .NET 10 definido em `global.json`.
- Windows usa WPF; Linux usa Avalonia. Modelos e serviços compartilhados ficam nos projetos sem dependências de interface.
- Mantenha interface em português do Brasil, navegação por teclado e suporte a escalas.
- Prefira APIs documentadas; não altere configurações globais nem esconda o painel do sistema automaticamente.
- Dados devem permanecer locais. Não adicione conta, telemetria ou backend obrigatório.
- Preserve licenças e créditos de dependências e recursos gráficos.

Comandos de build e testes estão no [README](README.md#compilar-e-testar). Compile a plataforma afetada e execute testes pertinentes. Registre limitações reais; não declare testes que não foram executados.

## Relatar problemas

Informe versão do GigaDock, sistema/arquitetura, passos para reproduzir e comportamento esperado. No Linux, inclua distribuição, desktop, X11/Wayland e escala. Remova dados pessoais de capturas e logs.

Não publique senhas, tokens, notas ou configurações reais. Antes de enviar código, consulte [PUBLICACAO-GITHUB.md](docs/PUBLICACAO-GITHUB.md) e execute `python tools/audit-publication.py --history`.
