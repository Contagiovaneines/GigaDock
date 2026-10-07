# GigaDock

Dock moderna e personalizável para Windows 10 e 11, criada com C#, .NET 10 e WPF.

O GigaDock organiza aplicativos, widgets e controles do Windows em ambientes independentes, como **Trabalho**, **Estudos** e **Pessoal**. As configurações ficam no computador, sem conta obrigatória, backend próprio ou telemetria.

> **Status:** versão 3.0.0 em beta. Os recursos principais estão funcionais, mas instalação, integrações, acessibilidade e comportamento em diferentes computadores ainda passam por validação.

## Principais recursos

- Ambientes com aplicativos, coleções, widgets e aparência próprios.
- Aplicativos fixados e todos os aplicativos abertos em qualquer ambiente.
- Magnificação suave dos ícones e indicadores de execução, atividade e notificações.
- Ativação e restauração de janelas pelo clique, inclusive para aplicativos com várias janelas.
- Miniaturas de janelas pelo DWM, com atrasos de abertura e fechamento configuráveis.
- Temas, cores, transparência, altura, cantos, divisores e ocultação automática.
- Largura limitada à tela, com navegação horizontal quando o conteúdo excede o espaço.
- Reprodução de mídia com capa, faixa, artista, progresso e controles.
- Mixer com volume principal e por aplicativo, mudo, roda do mouse, valor exato, fixação, ocultação temporária e modo compacto.
- Contorno RGB para mídia, modo gamer e alertas de WhatsApp e Teams.
- Controles rápidos com redes Wi-Fi, dispositivos Bluetooth, estilos por ambiente e ações do Windows.
- Painel estável e filtrado de aplicativos em segundo plano, com abertura pelo clique.
- Ícones por executável, janela, atalho, Shell e AUMID para aplicativos empacotados.
- Diagnóstico local do Windows, arquitetura, memória instalada e tempo ligado.
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

Quando aplicativos e widgets ocupam mais espaço que a tela, a dock permanece dentro do monitor. Use a roda do mouse, gesto horizontal ou teclado para navegar pelo conteúdo excedente.

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

## O que ainda está em beta

| Área | Estado atual | O que falta validar ou concluir |
| --- | --- | --- |
| Instalador | Atualiza por usuário e preserva configurações | Assinatura Authenticode confiável e validação ampla de atualização/desinstalação |
| Aplicativos e ícones | Win32, atalhos, MSIX/AUMID e múltiplas janelas | Casos específicos de processos protegidos, launchers e aplicativos sem ícone público |
| Ambientes e widgets | Dados, aparência e ordem por ambiente; Relógio e Notas aceitam várias instâncias | Runtime independente para múltiplas instâncias dos demais widgets |
| Controles rápidos | Wi-Fi, Bluetooth e ações locais | Pareamento e novas credenciais continuam nas interfaces seguras do Windows |
| Áudio | Volume principal e por aplicativo, mudo e microfone | Eventos de dispositivos e mais validação com drivers e interfaces diferentes |
| OBS | WebSocket v5 local e comandos básicos | Cenas, transmissão, eventos completos e reconexão mais robusta |
| Discord | Estado do processo e abertura do aplicativo | Dados sociais ou de voz exigiriam autorização por integração oficial |
| Notificações | Alertas locais de WhatsApp e Teams | Dependem do conteúdo disponibilizado pelos próprios aplicativos ao Windows |
| Interface | Teclado, foco, escala e limite horizontal | Testes manuais extensos com leitores de tela, vários monitores e escalas de 100% a 200% |
| Desempenho | Widgets ocultos são suspensos e janelas usam eventos Win32 com fallback econômico | Medições prolongadas de memória, handles e consumo em diferentes computadores |

## Ideias futuras

Sem prazo ou compromisso de entrega:

- concluir o runtime independente e permitir várias instâncias de todos os widgets;
- adicionar eventos de conexão e troca de dispositivos ao mixer de áudio;
- ampliar os estados e controles do OBS;
- criar atualização opcional de aplicativos por WinGet, sempre após confirmação;
- oferecer integração opcional com Outlook, OneDrive e Teams por Microsoft Graph, com login e permissões explícitas;
- ampliar prévias seguras de documentos e recursos da estante de arquivos;
- melhorar o compartilhamento entre dispositivos Windows após uma análise de segurança;
- adicionar ferramentas de diagnóstico exportável sem incluir dados pessoais;
- validar e otimizar o GigaDock em múltiplos monitores, diferentes DPIs e sessões prolongadas;
- publicar instaladores assinados e releases reproduzíveis.

Veja o [status técnico](docs/STATUS.md) e o [plano de evolução](docs/PLANO-IMPLEMENTACAO-IDEIAS-FUTURAS.md) para detalhes técnicos.

## Contribuição

Leia [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) e [AGENTS.md](AGENTS.md) antes de enviar alterações.

## Licença

Distribuído sob a [Licença MIT](LICENSE).
