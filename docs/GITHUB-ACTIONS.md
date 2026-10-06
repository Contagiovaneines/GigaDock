# Ativar o build automático no GitHub

O arquivo `.github/workflows/build-windows.yml` está preparado localmente. Ele instala .NET 10 em Windows 2025, restaura dependências, compila, executa os testes sem a categoria `SystemIntegration` e chama `build_release.ps1`. A categoria excluída contém testes que alteram autostart/barra do Windows. Falha em build/testes impede a geração do artefato.

O instalador fica no artefato `GigaDock-Windows-x64-NUMERO`, junto com o manifesto de hash/assinatura. Resultados dos testes ficam em um artefato separado, inclusive quando o teste falha. Retenção: 14 dias. O workflow não publica Releases, não envia código automaticamente e ainda não assina arquivos. Nenhum segredo é necessário para esse build.

## O que você precisa fazer

1. Envie o código atual e o arquivo do workflow ao repositório correto pelo Git/VS Code. Não envie apenas o workflow se o restante do código ainda estiver desatualizado. Confira as alterações antes do commit; há trabalho acumulado no projeto.
2. Abra o repositório no GitHub e clique na aba **Actions**.
3. Abra **GigaDock - Build Windows**. O envio já dispara o build; também é possível usar **Run workflow** após o arquivo chegar à branch padrão.
4. Aguarde a execução terminar. Se houver falha, abra a etapa vermelha e consulte o log; o instalador só é disponibilizado após sucesso.
5. Na página da execução concluída, em **Artifacts**, baixe `GigaDock-Windows-x64-NUMERO` e extraia o ZIP.
6. Depois de confirmar uma execução no GitHub, informe **GitHub Actions** no formulário da SignPath. A integração de assinatura depende da aprovação e configuração da SignPath; não use os parâmetros do certificado local para representar esse serviço.

## Repositório: conferir o endereço

O remote local atual é `https://github.com/Contagiovaneines/WinDock-.git`. No formulário mostrado foi usado `https://github.com/Contagiovaneines/GigaDock`. Esses endereços devem representar o mesmo projeto ou ser corrigidos. O remote não foi alterado nesta etapa.

## Validação

Build local Release executado. Workflow ainda não enviado nem executado no GitHub nesta etapa. O instalador produzido por este fluxo continua sem assinatura e pode ser bloqueado pelo Smart App Control. Não foram alteradas políticas de proteção do Windows.

Referências: [build .NET no GitHub Actions](https://docs.github.com/en/actions/tutorials/build-and-test-code/net), [setup-dotnet](https://github.com/actions/setup-dotnet), [artefatos](https://github.com/actions/upload-artifact).
