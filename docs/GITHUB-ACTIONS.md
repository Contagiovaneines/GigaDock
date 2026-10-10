# Ativar o build automático no GitHub

O arquivo `.github/workflows/build-windows.yml` está preparado localmente. Ele instala .NET 10 em Windows 2025, restaura dependências, compila e chama `build_release.ps1` antes da suíte de testes. Depois executa os testes sem a categoria `SystemIntegration`. A categoria excluída contém testes que alteram autostart/barra do Windows. Falha de compilação/empacotamento impede o artefato. O instalador de desenvolvimento não assinado é disponibilizado antes dos testes; se a suíte falhar, a execução continua vermelha e esse arquivo não deve ser tratado como validado. Assinatura permanece depois dos testes, condicionada ao sucesso.

Sem a SignPath configurada, o instalador fica no artefato `GigaDock-Windows-x64-NAO-ASSINADO-NUMERO`, junto com o manifesto de hash/assinatura. Esse arquivo serve para desenvolvimento e pode ser bloqueado pelo Smart App Control. Resultados dos testes ficam em um artefato separado, inclusive quando o teste falha. O workflow não publica Releases nem envia código automaticamente.

Depois da aprovação da SignPath, o mesmo workflow envia o artefato produzido no GitHub Actions para o serviço oficial e publica `GigaDock-Windows-x64-ASSINADO-NUMERO` somente quando o Windows confirma uma assinatura Authenticode válida.

## O que você precisa fazer

1. Envie o código atual e o arquivo do workflow ao repositório correto pelo Git/VS Code. Não envie apenas o workflow se o restante do código ainda estiver desatualizado. Confira as alterações antes do commit; há trabalho acumulado no projeto.
2. Abra o repositório no GitHub e clique na aba **Actions**.
3. Abra **GigaDock - Build Windows**. O envio já dispara o build; também é possível usar **Run workflow** após o arquivo chegar à branch padrão.
4. Aguarde a execução terminar. Se houver falha, abra a etapa vermelha e consulte o log; o instalador de desenvolvimento pode já estar disponível mesmo que os testes falhem. Confira o resultado dos testes antes de usá-lo.
5. Na página da execução concluída, em **Artifacts**, baixe `GigaDock-Windows-x64-ASSINADO-NUMERO` quando ele estiver disponível. O artefato marcado `NAO-ASSINADO` não é adequado para distribuição pública.
6. Se usar SignPath, siga a configuração descrita abaixo e a documentação do serviço. A integração de assinatura depende da aprovação e configuração da SignPath; não use os parâmetros do certificado local para representar esse serviço.

## Configuração após a aprovação da SignPath

Cadastre `SIGNPATH_API_TOKEN` como segredo do repositório e estas informações como variáveis do GitHub Actions: `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG`, `SIGNPATH_APP_ARTIFACT_CONFIGURATION_SLUG` e `SIGNPATH_SETUP_ARTIFACT_CONFIGURATION_SLUG`. Os valores são fornecidos ou definidos no painel da SignPath. As duas configurações precisam tratar o arquivo enviado pelo `upload-artifact` como ZIP: a primeira aplica Authenticode ao `DockWindows.App.exe`; a segunda, ao `GigaDock-Setup.exe`.

O aplicativo é assinado antes de ser embutido no instalador. Em seguida, o instalador completo também é assinado. Essa ordem evita instalar um executável interno sem publicador verificável.

Enquanto qualquer valor estiver ausente, a etapa de assinatura é ignorada e nenhum artefato é apresentado como assinado.

## Repositório

Confira se os links do README e do site apontam para o repositório que receberá o código.

## Diagnóstico de testes travados

A execução [37799689870](https://github.com/Contagiovaneines/GigaDock/actions/runs/37799689870) compilou com sucesso, mas ficou aproximadamente 44 minutos na suíte Windows e excedeu o limite de 45 minutos do job. A etapa de gerar o EXE foi ignorada. O teste exato não foi identificado pelos metadados públicos.

Agora a etapa de testes Windows tem limite de dez minutos e o VSTest interrompe inatividade de três minutos, salvando a sequência de testes no artefato de resultados. Logs incluem os nomes dos testes. Isso mantém a falha visível e permite identificar o travamento sem bloquear a geração inicial do instalador de desenvolvimento.

Envie todos os arquivos novos referenciados pela solução. `src/GigaDock.*`, projetos de testes e o diagnóstico em `prototypes/` também fazem parte do build; publicar apenas arquivos já rastreados ou a pasta `vercel/` não inclui esses projetos.

## Validação

Na validação local de 10 de outubro de 2026, a solução compilou em Release com zero erros/avisos e os 243 testes Windows elegíveis passaram em 3 minutos e 10 segundos com o diagnóstico de travamento ativado. O travamento do runner não se reproduziu. A sintaxe YAML e a ordem dos passos foram verificadas; o workflow atualizado ainda precisa ser executado no GitHub após o envio dos arquivos.

O instalador local atual permanece sem assinatura e pode ser bloqueado pelo Smart App Control. A integração foi preparada, mas somente uma execução no GitHub após a aprovação e configuração da SignPath poderá produzir o artefato assinado. Não foram alteradas políticas de proteção do Windows.

Referências: [build .NET no GitHub Actions](https://docs.github.com/en/actions/tutorials/build-and-test-code/net), [setup-dotnet](https://github.com/actions/setup-dotnet), [artefatos](https://github.com/actions/upload-artifact), [integração oficial da SignPath](https://docs.signpath.io/trusted-build-systems/github).
