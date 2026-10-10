# Preparar o projeto para o GitHub

## Conteúdo público

Envie código em `src/`, testes, ferramentas, workflows, projeto/solução, ícones, site estático, guias atuais e manifestos de origem. Preserve `LICENSE`, créditos e avisos de terceiros.

`docs/local/` guarda planos antigos, relatórios de trabalho e capturas de laboratório. Resultados de testes, `bin/`, `obj/`, `release/`, `dist/`, arquivos de sessão, certificados privados e configurações do usuário são ignorados. Não faça upload da pasta inteira por arrastar arquivos no navegador: use o Git para respeitar `.gitignore`.

Arquivos binários de distribuição devem acompanhar uma release, separados do código-fonte. Relatórios antigos não precisam virar documentação pública; guias de instalação, desenvolvimento, dependências e licenças precisam.

## Auditoria executável

Da raiz, no Windows ou Linux com Python 3 e Git:

```bash
python tools/audit-publication.py --history
git status --short
git diff --check
git diff --stat
```

A auditoria verifica os candidatos atuais e, com `--history`, os blobs alcançáveis pelas refs locais. Busca padrões de chaves/tokens conhecidos, credenciais em URLs, e-mails e caminhos de usuários. Não imprime valores encontrados; salva apenas arquivo/linha/categoria em `docs/local/auditoria/publicacao.json`.

O processo retorna erro para achados no conteúdo atual, arquivos privados candidatos ou arquivos rastreados que passaram a ser ignorados. Achados históricos são registrados separadamente; um resultado atual limpo não significa histórico limpo.

Limites: busca textual por padrões em arquivos de até 2 MB; não examina visualmente todas as imagens, metadados de autoria dos commits, objetos inalcançáveis, LFS, arquivos no servidor ou uploads anteriores. Revise também capturas antes de publicar. Nome de autoria na licença e URLs públicas do projeto foram preservados. Contatos públicos em créditos vendorizados não são tratados como vazamento do usuário.

## Histórico Git

E-mail/chave Pix estavam na tela Sobre e foram removidos da cópia atual. A auditoria de 10 de outubro de 2026 encontrou **184 ocorrências de e-mails/caminhos pessoais em 1629 blobs históricos examinados**; não são 184 segredos únicos. A revisão dos metadados identificou três endereços de autoria que não usam `users.noreply.github.com`. Mover documentos para uma pasta ignorada também não os remove do histórico. Nenhuma chave/token reconhecida pelos padrões da auditoria foi encontrada; isso não é uma garantia de ausência de segredos.

Se desejar publicar sem esses dados antigos, revise o relatório local e escolha entre criar um repositório novo a partir de uma exportação limpa dos arquivos públicos ou reescrever o histórico com uma ferramenta específica. Reescrever histórico altera identificadores de commits e pode exigir coordenação com colaboradores; não foi feito automaticamente. Caso apareça uma credencial verdadeira, revogue-a no serviço de origem antes de qualquer envio.

Para obter uma cópia dos arquivos públicos atuais, incluindo trabalho ainda não commitado e sem o histórico antigo:

```bash
python tools/export-public-source.py
```

O ZIP em `dist/GigaDock-codigo-fonte.zip` não inclui `.git`, relatórios locais, builds ou configurações pessoais. Extraia em outra pasta para revisar e iniciar um repositório novo se optar por essa alternativa. O exportador executa a auditoria antes de gerar o arquivo. Os relatórios locais completos permanecem no projeto original. A exportação desta etapa teve integridade e 46 links locais da documentação conferidos, sem links quebrados; as três capturas públicas foram inspecionadas visualmente.

O Git também pode publicar nome/e-mail do autor dos commits. Confira sua identidade em `git config user.name` e `git config user.email`; use o endereço privado fornecido pelo GitHub para novos commits se essa for sua preferência. Alterar a configuração não modifica commits existentes.

## Revisão antes do envio

Confira as alterações e a lista de arquivos. Use `git add -n --all` para pré-visualizar o que será incluído; só depois prepare seu commit. Revise as exclusões de relatórios antigos e a nova pasta de manifestos como parte da organização.

Os arquivos públicos foram organizados localmente. Nenhum commit, push, release ou deploy foi realizado nesta etapa. O histórico original foi preservado.
