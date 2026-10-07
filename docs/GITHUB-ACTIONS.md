# Ativar o build automático no GitHub

O arquivo `.github/workflows/build-windows.yml` está preparado localmente. Ele instala .NET 10 em Windows 2025, restaura dependências, compila, executa os testes sem a categoria `SystemIntegration` e chama `build_release.ps1`. A categoria excluída contém testes que alteram autostart/barra do Windows. Falha em build/testes impede a geração do artefato.

Sem a SignPath configurada, o instalador fica no artefato `GigaDock-Windows-x64-NAO-ASSINADO-NUMERO`, junto com o manifesto de hash/assinatura. Esse arquivo serve para desenvolvimento e pode ser bloqueado pelo Smart App Control. Resultados dos testes ficam em um artefato separado, inclusive quando o teste falha. O workflow não publica Releases nem envia código automaticamente.

Depois da aprovação da SignPath, o mesmo workflow envia o artefato produzido no GitHub Actions para o serviço oficial e publica `GigaDock-Windows-x64-ASSINADO-NUMERO` somente quando o Windows confirma uma assinatura Authenticode válida.

## O que você precisa fazer

1. Envie o código atual e o arquivo do workflow ao repositório correto pelo Git/VS Code. Não envie apenas o workflow se o restante do código ainda estiver desatualizado. Confira as alterações antes do commit; há trabalho acumulado no projeto.
2. Abra o repositório no GitHub e clique na aba **Actions**.
3. Abra **GigaDock - Build Windows**. O envio já dispara o build; também é possível usar **Run workflow** após o arquivo chegar à branch padrão.
4. Aguarde a execução terminar. Se houver falha, abra a etapa vermelha e consulte o log; o instalador só é disponibilizado após sucesso.
5. Na página da execução concluída, em **Artifacts**, baixe `GigaDock-Windows-x64-ASSINADO-NUMERO` quando ele estiver disponível. O artefato marcado `NAO-ASSINADO` não é adequado para distribuição pública.
6. Depois de confirmar uma execução no GitHub, informe **GitHub Actions** no formulário da SignPath. A integração de assinatura depende da aprovação e configuração da SignPath; não use os parâmetros do certificado local para representar esse serviço.

## Configuração após a aprovação da SignPath

Cadastre `SIGNPATH_API_TOKEN` como segredo do repositório e estas informações como variáveis do GitHub Actions: `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG`, `SIGNPATH_APP_ARTIFACT_CONFIGURATION_SLUG` e `SIGNPATH_SETUP_ARTIFACT_CONFIGURATION_SLUG`. Os valores são fornecidos ou definidos no painel da SignPath. As duas configurações precisam tratar o arquivo enviado pelo `upload-artifact` como ZIP: a primeira aplica Authenticode ao `DockWindows.App.exe`; a segunda, ao `GigaDock-Setup.exe`.

O aplicativo é assinado antes de ser embutido no instalador. Em seguida, o instalador completo também é assinado. Essa ordem evita instalar um executável interno sem publicador verificável.

Enquanto qualquer valor estiver ausente, a etapa de assinatura é ignorada e nenhum artefato é apresentado como assinado.

## Repositório: conferir o endereço

O remote local atual é `https://github.com/Contagiovaneines/WinDock-.git`. No formulário mostrado foi usado `https://github.com/Contagiovaneines/GigaDock`. Esses endereços devem representar o mesmo projeto ou ser corrigidos. O remote não foi alterado nesta etapa.

## Validação

O instalador local atual permanece sem assinatura e pode ser bloqueado pelo Smart App Control. A integração foi preparada, mas somente uma execução no GitHub após a aprovação e configuração da SignPath poderá produzir o artefato assinado. Não foram alteradas políticas de proteção do Windows.

Referências: [build .NET no GitHub Actions](https://docs.github.com/en/actions/tutorials/build-and-test-code/net), [setup-dotnet](https://github.com/actions/setup-dotnet), [artefatos](https://github.com/actions/upload-artifact), [integração oficial da SignPath](https://docs.signpath.io/trusted-build-systems/github).
