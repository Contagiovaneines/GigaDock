# GigaDock

Dock moderna e personalizável para Windows 10 e 11, criada com C#, .NET 10 e WPF.

O GigaDock organiza aplicativos, widgets e controles do Windows em ambientes independentes, como **Trabalho**, **Estudos** e **Pessoal**. As configurações ficam no computador, sem conta obrigatória, backend próprio ou telemetria.

> **Status:** versão **3.2** em beta (aplicativo e instalador: **3.2.0**). Instalação, integrações, acessibilidade e comportamento em diferentes computadores ainda passam por validação.

> **Distribuição no Windows:** builds locais sem assinatura podem ser bloqueados pelo Smart App Control. O instalador público deve ser gerado com certificado Authenticode confiável; “Desbloquear arquivo” não substitui a assinatura nesse caso.

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

## Novidades da versão 3.2

### Pokédex e mascotes Pokémon

- Pokédex de Kanto com os **151 Pokémon originais**, cartões ilustrados e seleção por ambiente em **Ajustes → Mascotes**.
- Busca instantânea por nome ou número, incluindo formatos como `Pikachu`, `25` e `#025`, com contador, botão Limpar e indicação de nenhum resultado.
- Miniaturas incorporadas ao aplicativo: a galeria e a busca funcionam sem internet.
- Mascote opcional que percorre a borda superior da dock com sequências locais de caminhada, passos para esquerda e direita e breve pausa nas extremidades. Os GIFs da PokéAPI permanecem como alternativa quando a sequência local não está disponível.
- Movimento por espécie: 18 voadores ficam acima da dock, 9 flutuantes pairam e os demais 124 usam movimento no chão. Os aéreos mantêm a animação ao parar de avançar; ao dormir, pousam. Doduo e Dodrio permanecem terrestres.
- Evolução temporária contada desde a escolha do Pokémon: segunda forma após uma hora e forma final após três horas, quando disponíveis. Trocar de Pokémon reinicia a contagem; manter a mesma espécie preserva o tempo. Fechar o aplicativo reinicia a evolução temporária.
- Eevee é a exceção ao prazo de horas: após 2 segundos, faz uma animação de evolução e usa o clima disponível para escolher Vaporeon, Jolteon ou Flareon. Mantém a forma durante a sessão; sem clima reconhecido, usa Vaporeon.
- Somente Snorlax dorme automaticamente: cochilos de 30 segundos a cada 2 minutos desde a escolha. Os demais Pokémon apenas descansam quando o Windows fica inativo.
- Somente Ditto se transforma aleatoriamente: fica 30 minutos como Ditto, olha para frente e assume outra das 150 espécies por 30 minutos. Depois volta a Ditto por mais 30 minutos antes de copiar outra forma, sem repetir a anterior.
- Após 90 a 180 segundos de movimento, os Pokémon esperam chegar ao meio da dock para parar e olhar para frente por 2 a 3 segundos. Voadores continuam batendo asas enquanto param no ar.
- Movimento respeita visibilidade, preferências de animação e modo econômico; o mascote descansa com a inatividade.

Os 151 Pokémon têm sequências de caminhada incorporadas, com quadros sincronizados à distância percorrida. O descanso usa pose sentada em 27 espécies (incluindo Pikachu) e animação de repouso nas outras 124; todos têm animação de sono. Caminhada, descanso e sono funcionam sem baixar sprites; consultas de dados e o carregamento inicial de GIFs alternativos requerem internet. O painel distingue a espécie escolhida da forma evoluída na dock ativa.

### GitHub com estilos e animações

- **Grade compacta:** cartão quadrado com grade verde 7 × 7 centralizada.
- **Resumo anual:** formato largo com total disponível e grade 36 × 7.
- Menu de contexto com **Cobrinha, Pac-Man, Breakout, Galaga, Puzzle Bobble, Bomberman e Minesweeper**, além de desativar a animação.
- Efeitos acompanham as dimensões do estilo e restauram os níveis originais ao parar. Pausam quando a dock fica oculta ou as animações estão desativadas.
- Dados ilustrativos ficam restritos à galeria; total não confirmado aparece como travessão na dock.

### Monitor do sistema e clima

- Novos estilos **Ventoinha da CPU**, **Rede compacta**, **Atividade compacta** e **Atividade larga**.
- CPU e RAM com anéis maiores, números centralizados, indicação de porcentagem e cores de maior contraste. A ventoinha representa carga da CPU, não uma leitura de RPM.
- Clima com layouts **Minimalista** e **Largo**, além dos estilos de temperatura, condição, local, vento, previsão, horas e sol.
- Ícones vetoriais com tamanho fixo, áreas separadas para temperatura e mínima/máxima, e textos limitados a uma linha para evitar sobreposições.
- Galeria de estilos com escala das prévias corrigida e dimensões mais consistentes com a dock.

**Validação da versão:** compilação Release concluída com zero erros e zero avisos; pacote e versões do instalador conferidos. A verificação visual das alterações e a execução dos testes locais permanecem pendentes devido ao bloqueio identificado do Smart App Control. Veja os limites e resultados em [STATUS.md](docs/STATUS.md).

## Widgets

| Categoria | Widgets |
| --- | --- |
| Produtividade | Relógio, Pomodoro, calendário, reuniões, notas e lembrete de água |
| Sistema | CPU, RAM, rede, armazenamento, bateria, áudio e conectividade |
| Arquivos | Downloads, capturas, área de transferência e estante de arquivos |
| Internet | Clima, cotação de moedas e contribuições do GitHub |
| Integrações | Mídia do Windows, WhatsApp, Teams, Discord e OBS WebSocket |
| Mascotes | Pokédex de Kanto, Pokémon animado e evolução temporária |

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

Os executáveis de desenvolvimento ainda não possuem assinatura Authenticode confiável. O Smart App Control pode impedir a instalação ou execução. Após a aprovação e configuração da SignPath, o GitHub Actions assina primeiro o aplicativo, monta o pacote com esse binário validado e assina o instalador. Somente o artefato identificado como `ASSINADO` é adequado para distribuição; o arquivo local atual e artefatos `NAO-ASSINADO` continuam sujeitos ao bloqueio.

Não é recomendado desativar recursos de segurança do Windows. Consulte [Assinatura digital](docs/ASSINATURA-DIGITAL.md).

### Se o Smart App Control bloquear a instalação

> [!CAUTION]
> **🔴 OBSERVAÇÃO — alteração da proteção do Windows**
>
> O instalador local ainda não possui assinatura digital confiável. A opção recomendada é usar uma versão com assinatura confiável. **Desativar o Smart App Control afeta todos os aplicativos, não apenas o GigaDock.** Dependendo da versão e das atualizações do Windows, reativá-lo pode exigir redefinir ou reinstalar o sistema. Leia o aviso exibido pelo Windows antes de confirmar.
>
> O Smart App Control não oferece uma exceção individual para liberar somente este aplicativo. Esta orientação se aplica ao bloqueio identificado como **Controle Inteligente de Aplicativos / Smart App Control**.

Se, ciente dessas consequências, você optar por desativar o recurso no seu computador:

1. Abra **Iniciar → Configurações**.
2. Entre em **Privacidade e segurança → Segurança do Windows**.
3. Selecione **Controle de aplicativos e navegador**.
4. Abra **Configurações do Controle Inteligente de Aplicativos (Smart App Control)**.
5. Selecione **Desativado**, leia o aviso e confirme se desejar prosseguir.
6. Execute novamente `release\GigaDock-Setup.exe`.

**Não é necessário desativar o Microsoft Defender.** Em computadores administrados por uma empresa, solicite orientação ao administrador se a opção estiver bloqueada. Consulte as [perguntas frequentes oficiais da Microsoft](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions).

## Uso básico

1. Abra **Ajustes → Ambientes** para criar ou personalizar um ambiente.
2. Fixe aplicativos diretamente na dock ou arraste arquivos e atalhos para ela.
3. Abra **Widgets → Loja de Widgets** para escolher widgets e estilos.
4. Use **Aparência** para ajustar dimensões, cores, transparência e efeitos.
5. Use **Visualizações** para configurar miniaturas, prévias e abertura por clique ou mouse.
6. Em **Mascotes**, busque seu Pokémon, selecione o cartão e ative **Exibir na dock**.

Para trocar o efeito do GitHub, clique com o botão direito no widget e escolha uma animação. Use **Escolher estilo…** para alternar entre grade compacta e resumo anual. No Clima, o menu **Layout** permite acessar Minimalista e Largo.

Os botões de Wi-Fi e Bluetooth abrem listas dentro dos Controles rápidos. Redes com perfil salvo podem ser reconectadas pela dock; novas senhas e pareamentos continuam na interface segura do Windows.

Quando aplicativos e widgets ocupam mais espaço que a tela, a dock permanece dentro do monitor. Use a roda do mouse, gesto horizontal ou teclado para navegar pelo conteúdo excedente.

## Privacidade

O GigaDock não possui backend, conta ou telemetria própria. Configurações e dados pessoais permanecem locais:

| Conteúdo | Local |
| --- | --- |
| Configurações | `%LOCALAPPDATA%\DockWindows\settings.json` |
| Notas | `%LOCALAPPDATA%\DockWindows\widgets\<ambiente>\<instância>\notas.txt` |
| Logs de diagnóstico | `%LOCALAPPDATA%\DockWindows\logs` |
| Cache dos sprites Pokémon | `%LOCALAPPDATA%\DockWindows\cache\pokemon` |
| Credencial opcional do OBS | Gerenciador de Credenciais do Windows |

Alguns widgets consultam serviços externos somente quando habilitados: wttr.in para clima, GitHub para atividade pública, Banco Central Europeu para cotações e PokéAPI para dados de Pokémon. Os GIFs Pokémon são obtidos do repositório público PokeAPI/sprites no GitHub. URLs iCalendar são definidas pelo usuário.

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
| Mascotes | Pokédex, caminhada lateral local dos 151 Pokémon, cache de GIFs e evolução temporária | Validação visual na dock em diferentes escalas; animações próprias de descanso e outras ações |
| Novos estilos | GitHub, monitor do sistema e clima implementados e compilados | Conferência visual dos efeitos, espaçamento e escalas em execução permitida |
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

Os sprites Pokémon mantêm os direitos e condições indicados pelo projeto [PokeAPI/sprites](https://github.com/PokeAPI/sprites). A licença fornecida pelo repositório está preservada em [Assets/Pokemon/LICENCE.txt](src/DockWindows.App/Assets/Pokemon/LICENCE.txt).

As sequências de caminhada vêm do [PMDCollab/SpriteCollab](https://github.com/PMDCollab/SpriteCollab), com créditos preservados por espécie. Esses recursos têm condições próprias, incluindo uso não comercial e atribuição para contribuições sob CC BY-NC 4.0, e não estão cobertos pela licença MIT do código. Consulte o [aviso dos sprites de caminhada](src/DockWindows.App/Assets/PokemonWalk/NOTICE.md).
