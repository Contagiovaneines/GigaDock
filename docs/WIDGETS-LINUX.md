# Widgets extras do Linux

Disponíveis em **Ajustes → Widgets**. Ative os widgets desejados no ambiente e, para mostrar o resumo na dock, marque **Mostrar informações na barra**. Clique no widget para abrir seu painel. Eles vêm desativados em uma instalação nova.

A loja informa o modo de uso e os requisitos antes de ativar. Flatpak, mídia, áudio e workspaces ficam bloqueados quando suas ferramentas/sessão estão ausentes. Água automática, notificações/voz e recursos ainda sem runtime Linux aparecem em desenvolvimento. [Consulte a auditoria de todos os widgets](WIDGETS.md).

## Temperatura e ventoinhas

Lê temperaturas em °C e rotação real em RPM expostas pelo hardware em `/sys/class/hwmon`. O painel identifica o chip e o sensor, e atualiza a cada três segundos. Na barra, mostra a maior temperatura disponível e a primeira leitura de RPM. Nenhuma ventoinha é configurada ou controlada.

Máquinas virtuais e equipamentos sem drivers/sensores disponíveis podem mostrar **Sensores indisponíveis**. Zero RPM é uma leitura válida; ausência de leitura não é apresentada como zero.

## Aplicativos Flatpak

Com `flatpak` instalado, clique em **Atualizar aplicativos Flatpak**. A lista consulta aplicativos do usuário e da instalação padrão do sistema. A busca aceita nome ou identificador; mostra até 100 resultados por busca. **Abrir** usa a arquitetura, branch e instalação do item escolhido.

Não instala, atualiza, remove pacotes ou adiciona repositórios. Instalações de sistema com nomes personalizados não são incluídas nesta primeira integração. O resumo da barra é atualizado após consultar a lista.

## Áreas de trabalho Linux

Disponível em sessões **Sway** (`swaymsg`) ou **Hyprland** (`hyprctl`). Clique em **Atualizar áreas de trabalho** para ver a área ativa e as demais, depois clique numa delas para mudar. O indicador da dock reflete a última consulta; mudanças feitas externamente precisam de nova consulta.

As áreas de trabalho do desktop são diferentes dos ambientes Trabalho/Estudos/Pessoal do GigaDock. GNOME/KDE ainda não têm um provedor para este widget. Áreas especiais do Hyprland são omitidas; nomes Sway com caracteres de comandos são recusados na troca.

A interface do GigaDock continua usando X11/XWayland. A integração com o compositor não habilita Wayland nativo nem controle universal de janelas.

## Script local

Escolha um script/executável local de sua confiança pelo seletor ou pelo caminho absoluto. **Salvar caminho do script** guarda somente o caminho; deixe o campo vazio e salve para removê-lo. **Executar script local** executa uma vez, sem argumentos. Não executa ao iniciar, abrir o painel ou trocar de ambiente.

O programa roda com as permissões do usuário; não é uma sandbox. Scripts precisam de permissão de execução e de um interpretador válido. O JSON retornado na saída padrão deve ter `text` (1–80 caracteres) e, opcionalmente, `tooltip` (até 500). O tempo máximo é três segundos e cada saída é limitada a 4096 caracteres. Não use este widget para iniciar aplicativos de longa duração.

Exemplo de script que informa o tempo ligado, sem acessar a rede:

```sh
#!/bin/sh
read -r seconds remainder < /proc/uptime
minutes=$(( ${seconds%%.*} / 60 ))
printf '{"text":"Ligado: %s min","tooltip":"Tempo ligado neste Linux"}\n' "$minutes"
```

Salve como `meu-widget.sh`, dê permissão com `chmod u+x meu-widget.sh` e selecione o arquivo. O resultado fica somente na memória da sessão; reiniciar o app exige executar novamente. Texto é exibido como texto simples, sem HTML.

Nenhum componente Plasma, GNOME, Conky, Eww ou Waybar foi incorporado: são referências de pesquisa. As interfaces continuam sendo desenhadas pelo GigaDock em Avalonia.
