# Pokémon na dock Linux

Atualização de 10 de outubro de 2026: o mascote agora percorre a borda superior da dock, como no Windows, em vez de animar parado em uma posição de widget. Ative e escolha a espécie em **Ajustes → Pokédex**. A seleção continua sendo por ambiente; mascotes globais também usam a camada superior.

## Comportamento visual

- Caminhada a 18 pixels por segundo; quadros ligados à distância percorrida para espécies terrestres.
- Direções direita/esquerda corretas e pausa de 350 ms nas extremidades, com pose frontal antes da volta. Pausas ocasionais no meio do percurso.
- Espécies voadoras/flutuantes usam as mesmas regras de animação e altitude do Windows, com os mesmos recursos locais. A folha inteira de sprites deixou de ser desenhada: apenas o quadro e a direção selecionados aparecem.
- Movimento independente do tema: Discreto, Escuro, Colorido, Com Brilho, Vidro Líquido e Areia. A placa, os widgets e as cores seguem o tema escolhido.
- **Reduzir animações**, em Aparência, mantém o mascote parado; o campo compartilhado de modo econômico também pausa a animação. A altura reserva espaço para voo. Mascotes não interceptam cliques dos aplicativos.
- Snorlax utiliza a regra temporal compartilhada de cochilo e seu recurso Sleep quando disponível. O ciclo começa com a criação do controle; a duração de dois minutos não foi aguardada no teste gráfico desta etapa.

## Recursos e memória

Os streams XML/PNG são fechados após leitura. Ao retirar o controle da árvore visual, seu timer para e os bitmaps são descartados. Ao recolocar, carrega os recursos novamente. Dispose explícito também libera timer e imagens. Folhas de sono só são carregadas quando necessárias. A duração da sequência é calculada uma vez.

O projeto Avalonia inclui os recursos de animação necessários às regras por espécie e reaproveita os arquivos C# puros `PokemonLocomotion` e `PokemonBehavior` já utilizados no Windows, sem referências WPF, user32 ou outros serviços Windows. O deslocamento está em `DockWindows.Core/Widgets/PokemonDockMotion.cs`, com testes portáveis. O renderizador WPF não foi alterado.

Essa atualização resolve o descarte explícito de stream/bitmap do mascote apontado na auditoria. A árvore inteira da dock ainda é reconstruída em Refresh; sua reutilização e o benchmark de RAM continuam como melhorias futuras. Contagem de folhas descartadas não substitui perfil de heap/GPU nem prova uma redução em MiB.

## Validação executada

- Solução Release: zero erros/avisos.
- 58 testes compartilhados e 81 testes Linux aprovados no Ubuntu; 22 testes Windows de animação/comportamento aprovados.
- Interface Avalonia real no Ubuntu 26.04.1 x64/WSL2/WSLg, um monitor, escala 100%: deslocamento confirmado nos seis temas, 151 espécies carregadas, poses de voo de Charizard/Mew renderizadas, redução de animações e modo econômico conferidos.
- Trocas repetidas de tema/Refresh e Dispose verificaram liberação das folhas anteriores e interrupção do timer. Smoke de notas, tarefas, ajustes e widgets Linux também passou.
- Pacote `.tar.gz` self-contained atualizado, extraído e executado com dados isolados. Nenhuma configuração pessoal foi alterada.
- A primeira rodada gráfica parou na verificação de hit test; a configuração do controle foi explicitada e a rodada final passou. As capturas finais foram inspecionadas visualmente.

Evidências locais: `docs/TestResults/pokemon-motion/` e `docs/local/capturas/pokemon-motion-linux/`. Pacote: `release/linux/GigaDock-3.2.0-linux-x64.tar.gz`. SHA-256 histórico do pacote validado nesta etapa: `c142f4a905815d4d4c7f3cc4922df2171aa2b29f1281c2b51bc37f8349c88919`; etapas seguintes podem regenerar esse arquivo.

## Limites de paridade

A caminhada e as regras visuais por espécie se aproximaram do Windows. Isso não torna toda a interface Avalonia idêntica ao WPF: blur nativo e integrações do desktop continuam distintos. Descanso por inatividade global, evolução temporal e transformação do Ditto não foram portados nesta etapa. A sessão Linux pode não fornecer inatividade global, especialmente no Wayland; o código não inventa esse estado.

Outras distribuições, escalas e múltiplos monitores exigem validação. Esta etapa não certifica funcionamento em qualquer Linux. Para instalação e atualização, consulte [Instalação Linux](INSTALACAO-LINUX.md).
