# Como gerar o instalador assinado

O instalador 3.2.0 continua sem assinatura. Nesta máquina existe somente o certificado autoassinado `GigaDock OpenSource`; ele não resolve a confiança pública exigida pelo Smart App Control. É necessário um certificado de provedor confiável ou um serviço de assinatura configurado.

Em 2026-10-08, o evento 3077 do log CodeIntegrity confirmou o bloqueio de `release/GigaDock-Setup.exe`. O empacotamento agora rejeita certificados autoassinados e resultados de assinatura diferentes de `Valid`, inclusive `UnknownError`, antes de substituir o instalador em `release/`. O auxiliar `build_e_assinar.ps1` exige um certificado existente; não cria certificados nem adiciona raízes de confiança. Sintaxe dos scripts validada, rejeição do certificado local executada e compilação Release concluída com zero erros e avisos. A instalação permanece bloqueada até obter assinatura confiável.

## 1. Obter o certificado

Solicite um certificado **RSA de assinatura de código (Code Signing)** a uma certificadora confiável. Confirme antes da contratação que ela aceita seu país e seu cadastro como pessoa física ou jurídica, que o certificado é compatível com Authenticode/Smart App Control e que o serviço permite usar o SignTool no Windows. O fornecedor deverá validar sua identidade e entregar as instruções de acesso à chave (token, serviço de assinatura ou provedor criptográfico).

Para projeto open source, verifique a elegibilidade da SignPath Foundation nas [opções oficiais de assinatura da Microsoft](https://learn.microsoft.com/windows/apps/package-and-deploy/code-signing-options). Esse serviço tem processo próprio e não é integrado por este script. Azure Artifact Signing também tem requisitos de elegibilidade; não é uma opção garantida para pessoa física no Brasil. Não crie contas ou contrate serviços sem conferir condições e custos.

## 2. Preparar o computador

- Instale .NET SDK 10 e o [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/) com o SignTool.
- Instale/configure o certificado conforme o fornecedor, no repositório **Pessoal do usuário atual** (`Cert:\CurrentUser\My`). O script usa este repositório; provedores que exigem um fluxo próprio precisam de adaptação.
- Token/PIN devem ser usados apenas no software do fornecedor. Não coloque senha, PFX ou chave privada no projeto ou no chat.
- Use um ambiente de desenvolvimento autorizado a executar scripts PowerShell. Nesta máquina a política bloqueou arquivos `.ps1`; o fluxo assinado não foi executado. O procedimento não altera nem desativa essa política ou o Smart App Control.

Liste apenas as informações públicas dos certificados:

```powershell
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Select-Object Subject, Thumbprint, NotAfter, HasPrivateKey
```

Copie o `Thumbprint` do certificado escolhido. Confirme com o fornecedor a URL de **timestamp RFC3161**. Descubra o caminho do SignTool x64 instalado:

```powershell
Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Filter signtool.exe -Recurse |
    Where-Object FullName -Match '\\x64\\' |
    Select-Object FullName
```

## 3. Gerar a versão assinada

Na pasta do projeto, substitua os três valores abaixo pelos dados reais:

```powershell
.\build_release.ps1 -Assinar `
    -CertificadoThumbprint 'THUMBPRINT_REAL_DE_40_CARACTERES' `
    -SignToolPath 'C:\Program Files (x86)\Windows Kits\10\bin\VERSAO_INSTALADA\x64\signtool.exe' `
    -TimestampUrl 'https://SERVIDOR_RFC3161_DA_CERTIFICADORA'
```

O script valida validade, finalidade de assinatura, RSA, chave privada e cadeia do certificado. O fluxo usa SHA-256 para arquivo e timestamp. O identificador `/sha1` do SignTool escolhe o certificado pelo thumbprint; não é o algoritmo usado para assinar o arquivo.

`Directory.Build.targets` copia os componentes próprios e as dependências Ical.Net/NodaTime para `obj/.../signed-bundle`, assina as cópias e atualiza `FilesToBundle`. Não altera o cache NuGet. Verifica também as assinaturas das demais DLLs do bundle; uma nova dependência sem assinatura interrompe o build e exige revisão do fluxo. Depois assina/verifica o executável publicado, monta `app.zip`, publica e assina/verifica o setup. A assinatura garante integridade/identidade, não comprova ausência de bugs nem garante aprovação de qualquer política corporativa.

Somente após sucesso o setup substitui `release/GigaDock-Setup.exe`. Sem `-Assinar`, o script continua gerando build de desenvolvimento e mostra aviso de arquivo sem assinatura.

## 4. Conferir e testar

```powershell
Get-AuthenticodeSignature .\release\GigaDock-Setup.exe |
    Select-Object Status, StatusMessage, SignerCertificate, TimeStamperCertificate
```

O resultado esperado é `Valid`, com fornecedor correto e timestamp. Teste instalar, atualizar e desinstalar em Windows 10/11 com a proteção ativa. Cada alteração nos binários exige novo build e novas assinaturas. A assinatura não é aplicada retroativamente às DLLs de testes (`testhost`); a validação do pacote de distribuição e a política do ambiente de testes são assuntos separados.

Referências: [assinatura para Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control), [SignTool](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool), [assinatura antes do bundle .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).

## Validação nesta etapa

Sintaxe dos scripts analisada pelo parser PowerShell sem erros. Build Release aprovado, zero erros e aviso CS0067 existente no fake de teste. Publicação com assinatura habilitada sem certificado foi recusada corretamente. Não executados assinatura real, timestamp, verificação de binários assinados ou instalação: faltam certificado/provedor e ambiente autorizado a executar scripts. Logs `assinatura-build.txt` e `assinatura-sem-certificado.txt`.
