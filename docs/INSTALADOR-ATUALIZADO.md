# Instalador GigaDock atualizado


## Instalador atualizado e revisão visual — 2026-10-06

- Assistente 820×580, mínimo 720×540, redimensionável e limitado à área de trabalho inicial. Paleta azul/grafite, lateral em degradê, ícone original, cantos mais suaves, botões com padding real e foco de teclado/estado desabilitado visíveis. Textos antigos de versão e promessas de restauração garantida corrigidos.
- Mantidos controles e handlers do fluxo de instalação, atualização e desinstalação. Fechamento bloqueado durante trabalho em andamento; botão Sair usa Close. Inicialização automática desmarcada em instalação nova e preferência existente preservada na atualização. Falha ao iniciar aplicativo após instalar recebe mensagem.
- Compilação da solução anterior à publicação: zero erros e zero avisos incremental. Publicações finais de aplicativo e instalador concluídas sem erros. Pacote app.zip conferido: executável idêntico ao publicado; release/GigaDock-Setup.exe idêntico ao artefato do publish. Hash/tamanho em installer-validation.json.
- Instalador self-contained win-x64; aplicativo embutido continua exigindo .NET Desktop Runtime 10 x64. Instalador não ativa ocultação da barra nativa automaticamente; modo de barra principal permanece escolha explícita no app.
- Limites: não executados instalação/atualização/desinstalação, renderização WPF do assistente ou testes adicionais bloqueados por política Windows. Resultados da suíte anterior permanecem 60 aprovados/60 bloqueados, não aprovação integral do instalador.
- Próximos passos: conferir visual/DPI, progresso, atualização com dados existentes, encerramento durante operação, permissões e desinstalação em ambiente permitido.


## Novo empacotamento — lixeira e mídia

Instalador atualizado com todas as alterações atuais, incluindo gerenciamento/cache/integrações dos widgets, sincronização da lixeira e controles vetoriais/tamanho proporcional de mídia. Publicações Release win-x64 aprovadas; aplicativo publicado idêntico ao embutido no ZIP e setup de release idêntico ao publicado. Hash/tamanho em installer-validation.json. Setup self-contained; app exige .NET Desktop Runtime 10 x64. Política local bloqueou scripts; comandos diretos executados sem alteração da política. Instalação manual e visual não executados. As limitações de testes descritas na revisão anterior são históricas: posteriormente passaram 140 testes da refatoração e quatro da lixeira.
