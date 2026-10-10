# Auditoria de arquivos, recursos e memória

Data: 10 de outubro de 2026. Escopo: fontes Windows/Linux, projetos, empacotamento, testes, workflows, documentação e site. Auditoria; nenhum arquivo foi apagado e nenhuma otimização do aplicativo foi aplicada.

## Resultado

O principal excesso de armazenamento está nos builds e pacotes locais, não nos fontes. A RAM exige mudanças no ciclo de vida dos objetos; apagar documentação e imagens que não são carregadas pelo aplicativo não reduz seu consumo de memória.

| Inventário | Resultado aproximado |
| --- | --- |
| Arquivos locais acessíveis, excluindo `.git` | 23.473 arquivos; 19,27 GiB |
| Arquivos candidatos à publicação, respeitando `.gitignore` | 1.307 arquivos; 8,18 MiB |
| `dist/`, incluindo builds e cópias dentro dessa pasta | 13,70 GiB |
| Laboratório Linux em `prototypes/GigaDock.Linux.DesktopProbe/bin/` | 3,19 GiB |
| `release/` | 213,14 MiB |
| `bin/obj` de `src/tests`, fora de `dist` e do laboratório | 1,49 GiB |
| Mídias em fontes, docs e site, antes dos filtros de publicação | 629 arquivos: 627 PNG e dois ICO; nenhum GIF ou vídeo |
| Oito prévias sem referências em `docs/guide-previews/` | 408,24 KiB |

Valores são do momento da coleta, não tamanho de download ou RAM. O inventário percorreu arquivos acessíveis, sem seguir links simbólicos. A conferência de pastas registrou 23 falhas de leitura de metadados em arquivos do laboratório; os totais não incluem esses arquivos. A pasta `.git` foi deliberadamente excluída. Caches pessoais fora do projeto não foram inventariados. Os números de fontes respeitam o Git; a primeira seleção por diretório incluía `app.zip` e prévias ignoradas, portanto não deve ser usada como tamanho do repositório.

## Limpeza possível

| Caminho ou grupo | Decisão | Consequência e condição |
| --- | --- | --- |
| Builds antigos em `dist/release-*`, pacotes e cópias de revisão em `dist/` | Removíveis depois de escolher os artefatos a guardar | Não são entrada do código do aplicativo; perde-se o pacote antigo e a possibilidade de comparar/instalar aquela versão |
| `src/**/bin`, `src/**/obj`, `tests/**/bin`, `tests/**/obj` | Regeneráveis | Exige restore/build; testes e scripts com `--no-build/--no-restore` precisam de nova compilação antes de funcionar |
| `release/` e `src/DockWindows.Installer/Resources/app.zip` | Gerados; manter o último instalador se necessário | Não apagar `app.zip` antes de um build direto do instalador; regenerar pelo script de empacotamento |
| `docs/TestResults/`, logs e relatórios de build | Arquiváveis ou removíveis | Não entram no aplicativo; apagam evidências de testes e de falhas |
| `docs/local/capturas/` e relatórios antigos de trabalho | Opcionais para o produto | Não carregados pelo aplicativo; conservar o que for útil para rastreabilidade |
| `docs/local/` inteiro | Não recomendar remoção indiscriminada | Também contém fontes de ferramentas de prévia, scripts e evidências; não é apenas cache |
| Laboratório Linux em `prototypes/.../bin/linux-lab` | Não tratar como cache comum | Inclui SDK, bibliotecas nativas e ambiente de validação; apagar exige reconstruir o laboratório para testar Linux |
| `docs/guide-previews/` | Pode sair dos fontes públicos; preferir mover para arquivo local | Oito PNG sem referências encontradas nos fontes, projetos, scripts, documentação pública ou site; não incluídos nos recursos dos aplicativos |
| `docs/referencia/assets/*.json` | Preservar | Manifestos de procedência/integridade dos sprites, referenciados nos guias de licenças |
| Avisos, `LICENCE.txt`, `credits.txt`, `spritebot_credits.txt`, `NOTICE.md` e `SOURCE-README.md` | Preservar | Atribuição e condições dos recursos de terceiros; não são código morto |
| `assets/gigadock.*`, `vercel/assets/*` e screenshots usadas | Preservar | Identidade dos executáveis, recursos Avalonia, site e README |
| `prototypes/GigaDock.Linux.DesktopProbe` sem `bin/obj` | Preservar na estrutura atual | Projeto referenciado pela solução; apagar quebra o build e a ferramenta de diagnóstico |
| Testes, workflows e scripts de instalação/assinatura | Preservar | Não compõem o runtime principal, mas sustentam validação, distribuição e manutenção |

Prévia removível: `ajustes-1180.png`, `ajustes-400.png`, `ajustes-600.png`, `ajustes-820.png`, `guia-aplicativo.png`, `guia-compacto.png`, `guia-instalador.png` e `instalador-guia.png`, todas em `docs/guide-previews/`. As cópias usadas pelo site ficam em `vercel/assets/screenshots/`.

Não há exclusão de arquivo fonte C# declarada segura nesta auditoria. Recursos dinâmicos e classes carregadas pela interface impedem concluir desuso apenas pela ausência de referência textual. Não há garantia de que uma limpeza de artefatos preserve todos os comandos de desenvolvimento sem recompilação.

## Duplicação e imagens

- Dois pares de capturas são idênticos por SHA-256: prévia de ajustes e guia em docs têm cópias utilizadas pelo site. As cópias de docs podem sair da publicação.
- `assets/gigadock.ico` e `vercel/assets/gigadock.ico` também são idênticos. Ambos possuem consumidores em locais diferentes; conservar a cópia do site enquanto ele é publicado a partir de `vercel/`.
- Sete espécies têm `Idle-Anim.png` idêntico a `Walk-Anim.png`: 0003, 0006, 0054, 0055, 0083, 0125 e 0145. Não apagar: `PokemonWalkAnimation.Load` calcula o nome do arquivo dinamicamente. Deduplicar exigiria mudar o carregador, manifestos e testes de descanso.
- PNG de espécies estáticas são utilizados pela seleção da Pokédex, por URI construída em `AjustesViewModel.PokemonOpcao.SpriteLocal`. As animações entram no WPF por glob do projeto; Walk/AnimData entram no Avalonia. Não são sobra.
- PNG dos sprites totalizam aproximadamente 3,12 MiB comprimidos. Somar `largura × altura × 4` de todas as imagens equivale a cerca de 90,50 MiB de pixels RGBA. Isso é uma estimativa para todas decodificadas ao mesmo tempo, não consumo medido: o aplicativo não necessariamente carrega todas, e objetos, texturas e buffers podem acrescentar memória.
- Não existem GIFs/vídeos nessa seleção de mídias do projeto. O Windows baixa GIF/PNG de mascotes para cache pessoal em tempo de execução; `PokemonFrame.Carregar` decodifica quadros GIF. Esse custo existe mesmo sem arquivos `.gif` nos fontes.
- A documentação pública atual cobre instalação, arquitetura, widgets, distribuição e licenças. Consolidar `WIDGETS-LINUX.md` em `WIDGETS.md` é possível, mas requer preservar instruções e atualizar links. Ganho de disco pequeno; nenhum ganho de RAM do aplicativo.

## Memória observada e limites

Foi amostrado somente o processo Windows já aberto, sem abrir outra instância, mudar widgets, fechar janelas ou forçar coleta de lixo. Foram 12 amostras, espaçadas em três segundos:

| Métrica | Mínimo | Média | Máximo |
| --- | --- | --- | --- |
| Working set, memória residente | 218,62 MiB | 224,55 MiB | 236,86 MiB |
| Private bytes, memória privada comprometida | 284,95 MiB | 290,86 MiB | 303,28 MiB |

Não somar essas métricas: elas descrevem aspectos diferentes. Uma leitura inicial mostrou 1.304 handles e 41 threads; isso não prova vazamento. Não foi confirmada a versão instalada, nem controlado o uso do computador durante a amostragem. Não foi executado perfil de heap, medição de GPU ou benchmark Linux nesta auditoria. Não há evidência para prometer um valor mínimo de RAM ou redução percentual.

## Achados de código para reduzir consumo

| Prioridade | Evidência | Ação proposta | Limite da conclusão |
| --- | --- | --- | --- |
| Alta | `GigaDock.App.Linux/PokemonSprite.cs`: stream do PNG passado a `new Bitmap` sem `using`; `_bitmap` sem descarte explícito; detach só interrompe timer | Definir dono e descarte do stream/bitmap; liberar no fim do ciclo de vida ou compartilhar sprite por cache com orçamento e contagem de usuários | Risco de pressão nativa; vazamento permanente não demonstrado. Não descartar no detach sem suportar reattach |
| Alta | `GigaDock.App.Linux/MainWindow.cs`: `Refresh` substitui toda a árvore e cria novo `PokemonSprite`; popup também cria sprite | Reutilizar controles por ID e liberar recursos substituídos; medir troca de ambientes repetida | Recriação e alocação confirmadas, economia ainda não medida |
| Alta | `MascotePokemonViewModel`: Walk, Rest, Sleep e Frames podem permanecer retidos quando widget perde atividade; `PokemonFrame.Carregar` compõe todos os quadros GIF | Carregar animações sob demanda; evitar GIF adicional quando a sequência local serve; liberar dados de widgets desinstalados ou inativos com política clara | Preservar caminhada, sono, evolução e retomada; não apagar imagens para resolver retenção |
| Média | `MainViewModel` constrói os VMs principais de widgets mesmo quando não instalados | Criar runtime somente ao instalar/ativar; destruir ao desinstalar; serviços compartilhados com dono explícito | Timers já são controlados por atividade; construir um VM não significa automaticamente executar polling |
| Média | Linux: timer principal de 1 s, leituras completas de sistema a cada 3 s e `X11WindowService` executando `wmctrl` a cada 3 s | Agendar conforme widgets ativos; relógio da barra por minuto; manter Pomodoro/alertas necessários; modo econômico também no Linux | Prioriza CPU, alocações e processos transitórios; não representa redução garantida da RAM residente |
| Média | `Visuals.IconCache`: até 256 bitmaps de largura 64; cache estático sem limpeza explícita ou orçamento em bytes | Orçamento por pixels, tamanho adequado à escala, invalidação e descarte seguro na saída | Existe limite de quantidade; não é cache sem limite. Altura proporcional e overhead variam |
| Média | `PokemonSprite.Render`: soma durações a cada render; `PokemonEvolutionEffect.OnRender`: cria pincéis, geometrias e canetas | Pré-calcular valores e reutilizar recursos congeláveis quando possível | Reduz trabalho e pressão de GC; provavelmente menor impacto em memória permanente |
| Média | Downloads de sprite usam `GetByteArrayAsync`; Frames GIF são decodificados integralmente sem orçamento explícito no carregador | Limites de resposta, dimensões e quantidade de quadros; streaming limitado e cancelamento; limite de cache em disco | GIF de origem válida também pode crescer; preservar formatos e mensagens de erro |
| Baixa | `LojaWidgetsWindow`: `ItemsControl` com `WrapPanel` dentro de `ScrollViewer` | Considerar virtualização se catálogo crescer ou cards passarem a conter mídias pesadas | Só 26 entradas hoje; não priorizar antes do perfil |
| Baixa | `MainViewModel` recria instâncias adicionais de relógio/notas ao sincronizar | Atualizar por ID e reutilizar instâncias compatíveis | O código atual já chama Dispose antes de limpar; é churn, não vazamento comprovado |

Já existem proteções úteis: cache Windows de ícones limitado a 128 entradas/8 MiB estimados, ou 48/3 MiB no modo econômico; capas de mídia limitadas a oito/três; previews decodificados em 256/560/600 px; histórico de clipboard limitado a 20 textos de até 4.000 caracteres; gerenciador de atividade, desassinatura de eventos e Dispose dos runtimes adicionais. Modo econômico Windows muda monitor de 2 para 10 s, bateria de 30 para 60 s e rastreamento de janelas de 2,5 para 8 s. Esses limites não incluem todo overhead nativo/GPU.

Ical.Net é utilizado pelo importador de calendário; Avalonia.Desktop e Fluent sustentam o frontend Linux. Não há dependência de aplicativo identificada como seguramente removível. Limpeza de código desativado da loja deve considerar preferências antigas e migração, antes de excluir enum, classe ou serviço.

## Etapas recomendadas

Atualização posterior desta auditoria: a etapa [Pokémon Linux](POKEMON-LINUX.md) aplicou fechamento de streams e descarte explícito dos bitmaps do mascote, com testes gráficos de troca de tema e Dispose. Reutilização da árvore da dock e comparação controlada de RAM continuam pendentes; os valores de memória anteriores não foram medidos novamente nessa etapa.

1. **Medir uma base controlada.** Builds Release isolados, mesma configuração/DPI e widgets; coleta por 15 minutos, sem widgets, configuração típica, várias instâncias, dock oculta e modo econômico. Repetir em Windows e Linux. Registrar working set/RSS, private bytes, heap gerenciado, alocação, CPU, handles, threads e recursos nativos.
2. **Enxugar armazenamento.** Guardar último instalador/pacote/ZIP; arquivar prévias e evidências úteis; limpar builds antigos. Conferir Git e links. Fazer restore/build e gerar novamente ambos os pacotes para validar regeneração.
3. **Corrigir dono dos recursos Linux.** Streams, bitmaps, caches e controles substituídos. Testar abrir/fechar Pokédex, popup e ajustes 100 vezes e alternar ambientes 100 vezes; observar platô após aquecimento e coletas normais, sem exigir retorno exato à memória inicial.
4. **Carregar widgets e animações sob demanda.** Preservar configurações e alertas essenciais; limitar GIF e sprites; liberar ao desinstalar e cancelar consultas. Testes de reativação, sono, evolução, notas e Pomodoro.
5. **Reduzir atualizações e reavaliar.** Modo econômico Linux, leitura por demanda e atualização por ID. Repetir a base do passo 1; apresentar resultados antes/depois e diferenças de funcionalidades. Otimizar efeitos/virtualização apenas se os perfis justificarem.

Não usar chamadas periódicas a `GC.Collect`, esvaziamento forçado de working set ou exclusão de DLLs do runtime como solução de consumo. Não ativar trimming em WPF/Avalonia indiscriminadamente; tamanho de pacote e memória em uso são problemas diferentes e a compatibilidade exige validação específica.

## Evidências e validação

Inventário, hashes de mídia, referências textuais, tamanho das pastas e amostras reais estão em `docs/local/auditoria-recursos/`; script reproduzível em `docs/local/auditar-recursos.py`. Não são prometidos como arquivos públicos necessários ao produto. Recursos gerados/ignorados foram separados dos candidatos ao Git. Nenhum código runtime foi modificado; não foi executada nova suíte ou compilação por causa deste relatório. O build e 243 testes registrados na etapa anterior continuam sendo evidências daquela etapa, não um benchmark de memória.

Fundamentação: [decodificação WPF em tamanho de miniatura](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-load-an-image-as-a-thumbnail), [ciclo de vida de recursos não gerenciados](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/unmanaged) e [coleta de métricas com dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters). Essas referências orientam as propostas; a magnitude dos ganhos depende de execução e medição no projeto.
