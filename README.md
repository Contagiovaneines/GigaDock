# GigaDock

Dock moderna e personalizável para Windows 10 e 11, criada com C#, .NET 10 e WPF.

O GigaDock organiza aplicativos, widgets e controles do Windows em ambientes independentes, como **Trabalho**, **Estudos** e **Pessoal**. As configurações ficam no computador, sem conta obrigatória, backend próprio ou telemetria.

> **Status:** versão 3.0.0 em beta. O aplicativo está em desenvolvimento ativo e ainda requer validação em diferentes versões, escalas e configurações do Windows.

## Principais recursos

- Ambientes com aplicativos, coleções, widgets e aparência próprios.
- Aplicativos fixados e aplicativos abertos, inclusive quando pertencem a outro ambiente.
- Magnificação suave dos ícones e indicadores de execução, atividade e notificações.
- Miniaturas de janelas pelo DWM, com atrasos de abertura e fechamento configuráveis.
- Temas, cores, transparência, altura, cantos, divisores e ocultação automática.
- Reprodução de mídia com capa, faixa, artista, progresso e controles.
- Mixer com volume principal e por aplicativo, mudo, roda do mouse, valor exato, fixação, ocultação temporária e modo compacto.
- Contorno RGB para mídia, modo gamer e alertas de WhatsApp e Teams.
- Controles rápidos com redes Wi-Fi, dispositivos Bluetooth e ações do Windows.
- Painel filtrado de aplicativos em segundo plano.
- Compatibilidade com teclado, foco visível e escalas de tela.

## Widgets

| Categoria | Widgets |
| --- | --- |
| Produtividade | Relógio, Pomodoro, calendário, reuniões, notas e lembrete de água |
| Sistema | CPU, RAM, rede, armazenamento, bateria, áudio e conectividade |
| Arquivos | Downloads, capturas, área de transferência e estante de arquivos |
| Internet | Clima, cotação de moedas e contribuições do GitHub |
| Integrações | Mídia do Windows, WhatsApp, Teams, Discord e OBS WebSocket |

Relógio e Notas aceitam várias instâncias por ambiente. Outros widgets ainda usam uma instância de cada tipo por ambiente.

## Instalação

O instalador mais recente gerado no repositório está em:

```text
release/GigaDock-Setup.exe
```

Requisitos:

- Windows 10 ou Windows 11 x64;
- .NET Desktop Runtime 10 x64 para o aplicativo instalado.

O instalador preserva as configurações durante atualizações e instala o aplicativo em `%LOCALAPPDATA%\Programs\DockWindows`.

### Assinatura digital

Os executáveis de desenvolvimento ainda não possuem assinatura Authenticode confiável. O Smart App Control pode impedir a instalação ou execução. O fluxo de assinatura está preparado e depende da aprovação do projeto em um serviço de assinatura para código aberto.

Não é recomendado desativar recursos de segurança do Windows. Consulte [Assinatura digital](docs/ASSINATURA-DIGITAL.md).

## Uso básico

1. Abra **Ajustes → Ambientes** para criar ou personalizar um ambiente.
2. Fixe aplicativos diretamente na dock ou arraste arquivos e atalhos para ela.
3. Abra **Widgets → Loja de Widgets** para escolher widgets e estilos.
4. Use **Aparência** para ajustar dimensões, cores, transparência e efeitos.
5. Use **Visualizações** para configurar miniaturas, prévias e abertura por clique ou mouse.

Os botões de Wi-Fi e Bluetooth abrem listas dentro dos Controles rápidos. Redes com perfil salvo podem ser reconectadas pela dock; novas senhas e pareamentos continuam na interface segura do Windows.

## Privacidade

O GigaDock não possui backend, conta ou telemetria própria. Configurações e dados pessoais permanecem locais:

| Conteúdo | Local |
| --- | --- |
| Configurações | `%LOCALAPPDATA%\DockWindows\settings.json` |
| Notas | `%LOCALAPPDATA%\DockWindows\widgets\<ambiente>\<instância>\notas.txt` |
| Logs de diagnóstico | `%LOCALAPPDATA%\DockWindows\logs` |
| Credencial opcional do OBS | Gerenciador de Credenciais do Windows |

Alguns widgets consultam serviços externos somente quando habilitados: wttr.in para clima, GitHub para atividade pública e Banco Central Europeu para cotações. URLs iCalendar são definidas pelo usuário.

## Desenvolvimento

Requisitos: Windows e o SDK definido em [global.json](global.json).

```powershell
dotnet restore DockWindows.slnx
dotnet build DockWindows.slnx -c Release --no-restore
dotnet run --project src/DockWindows.App/DockWindows.App.csproj
```

Para gerar o instalador:

```powershell
.\build_release.ps1
```

O script publica executáveis únicos, rejeita artefatos de teste dentro do pacote e registra tamanho e SHA-256 em [installer-validation.json](docs/installer-validation.json).

Os testes são executados no GitHub Actions. Em computadores com Smart App Control em modo estrito, o projeto interrompe testes locais antes que o Windows tente carregar `DockWindows.Tests.dll` sem assinatura.

## Estrutura

| Diretório | Conteúdo |
| --- | --- |
| `src/DockWindows.App` | Interface WPF e ViewModels |
| `src/DockWindows.Core` | Modelos, contratos e regras de domínio |
| `src/DockWindows.Infrastructure` | Persistência e integrações com Windows |
| `src/DockWindows.Installer` | Instalador, atualização e desinstalação |
| `tests/DockWindows.Tests` | Testes automatizados |
| `docs` | Guias, decisões, status e validações |
| `tools` | Scripts de build e manutenção |

## Limitações atuais

- O instalador ainda não possui assinatura confiável.
- Integrações dependem das permissões e APIs disponíveis no Windows e nos aplicativos.
- O painel de segundo plano é uma visão própria de processos reconhecidos; não replica internamente a bandeja do Explorer.
- Alertas de WhatsApp e Teams dependem das notificações publicadas no Windows.
- Troca de saída de áudio e pareamento Bluetooth são encaminhados às páginas oficiais do Windows.
- OBS exige WebSocket v5 local; Discord mostra apenas o estado do processo sem autorização oficial adicional.
- Consumo prolongado, múltiplos monitores, leitores de tela e escalas entre 100% e 200% ainda precisam de validação ampla.

Veja o [status técnico](docs/STATUS.md) e o [plano de evolução](docs/PLANO-IMPLEMENTACAO-IDEIAS-FUTURAS.md) para detalhes.

## Contribuição

Leia [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) e [AGENTS.md](AGENTS.md) antes de enviar alterações.

## Licença

Distribuído sob a [Licença MIT](LICENSE).
