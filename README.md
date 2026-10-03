# 🚀 GigaDock 3.1

O **GigaDock** é um dock moderno, nativo e ultra-leve para Windows 10/11 que traz produtividade, fluidez e organização para sua área de trabalho. Criado para quem deseja uma interface limpa, inspirada no macOS, sem perder a performance.

![GigaDock Preview](docs/dock-apps-preview.jpg)

## 🆕 O que há de novo na V3.1

### 🔔 Notificações de Mensagem nos Ícones de App
Quando um aplicativo (Teams, Discord, WhatsApp, etc.) recebe uma mensagem e começa a piscar na barra de tarefas do Windows, o GigaDock agora intercepta esse sinal nativamente via `HSHELL_FLASH` e exibe um **badge translúcido vermelho piscante** sobre o ícone do app na dock. Assim que você clica no app e ele ganha foco, o badge desaparece automaticamente.

### 📞 Widget do Teams com Status e Sala Ativa
O widget inline do Microsoft Teams agora exibe estados ricos em tempo real:
- **Disponível / Ocupado** — com bolinha colorida de status.
- **Em chamada** — mostra o nome da sala em que você está.
- **Nova mensagem** — exibe quem te mandou mensagem, com destaque neon roxo pulsando na dock.

### 🎨 Painéis de Ajustes com Visual Moderno (Wide Layout)
A janela de Ajustes foi refatorada para eliminar a coluna do meio redundante. Agora as telas de **Geral**, **Aparência**, **Widgets**, **Utilitários** e **Sobre** ocupam toda a largura disponível, igual ao estilo do Widget Store — limpo, espaçoso e sem elementos duplicados.

### 🗑️ Lixeira Funcional na Dock
Novo toggle em **Ajustes → Geral → Elementos da Barra**. Ao ativar *"Mostrar Lixeira"*, um ícone de Lixeira aparece no final da dock ao lado do relógio. Clicar nele abre a Lixeira do Windows diretamente.

### 📐 Sliders de Dimensão e Aparência
Em **Ajustes → Aparência**, novo bloco *"Dimensões da Barra"* com três sliders que se aplicam em tempo real:
- **Altura da Barra** (48–120 px) — controla quão alto o dock é.
- **Opacidade do Dock** (30%–100%) — ajusta a transparência do fundo.
- **Raio dos Cantos** (0–40) — arredonda ou deixa reto as bordas do dock.

---

## 🌟 O que há de novo na V3.0
- **Loja de Widgets Visual (Estilo Mac):** Escolha livremente entre aparências *Compactas* (Apenas Ícone) ou *Expandidas* (Detalhadas) para cada widget.
- **Integração Nativa Pac-Man:** O widget do GitHub agora possui as animações *Cobrinha* e *Pac-Man* rodando nativamente na barra.
- **Notificações em Neon:** O sistema de Alerta Global agora faz a sua Dock brilhar em cores vibrantes quando há eventos em apps de comunicação.
- **Proteção Anti-UAC:** O dock não some mais por engano quando você instala novos programas ou abre telas seguras de administrador.

---

## 🛠️ Funcionalidades Principais

- 🖥️ **Múltiplos Ambientes:** Separe seus atalhos em *Trabalho*, *Estudos* e *Pessoal*. Troque de ambiente instantaneamente com Ctrl + Alt + 1/2/3.
- 🔍 **Launchpad Integrado:** Aperte a tecla de atalho para abrir uma barra de pesquisa flutuante (inspirada no Mac) no meio da tela.
- 🎨 **Estilo Vidro Líquido:** Construído em WPF com transparências, desfoques e suporte automático a Temas Claro/Escuro do Windows.
- ⚙️ **Customização Extrema:** Altere a margem, desfoque, tamanho, arredondamento e ative o *Ocultar Automaticamente*.
- 🔒 **100% Local & Seguro:** Nenhuma telemetria, nenhuma conta necessária. Seus dados de integração ficam apenas no seu PC.

---

## 🧩 Galeria de Widgets (Status Atual)

A GigaDock possui uma loja interna ("Widget Store") onde você pode adicionar extensões à sua barra. Aqui está o status real de cada uma delas:

### ✅ Widgets 100% Funcionais
- **Mídia (Tocando Agora):** Conecta nativamente via Windows Media API com Spotify, Edge, etc. Controles de reprodução reais, exibe a capa do álbum e acende o Alerta Neon RGB giratório automaticamente quando a música toca.
- **WhatsApp:** Monitora a contagem de mensagens através da Central de Notificações. Usa um sistema heurístico inteligente de monitoramento de janelas para detectar ligações recebidas em tempo real e piscar o Alerta Neon Verde.
- **Microsoft Teams:** Intercepta notificações nativas de entrada de mensagens para exibir o remetente e acionar o Alerta Neon Roxo. *(Status por cor como Disponível/Ausente arquivado para o futuro)*.
- **Monitor de Sistema:** Acompanhe o uso real de CPU e Memória RAM diretamente na doca de forma otimizada.
- **Pomodoro Timer:** Cronômetro de foco de 25 minutos funcional, com contagem regressiva ao vivo e som suave.
- **Calendário de Compromissos:** Lê e sincroniza agendas (arquivos `.ics`) e exibe os minutos exatos até a sua próxima reunião.
- **Relógio e Clima:** Mostra as horas e a previsão do tempo.

### 🚧 Em Desenvolvimento (Mock / BETA)
Os widgets abaixo ainda estão na prancheta de desenvolvimento. Por enquanto, se você os adicionar na doca, eles vão exibir apenas dados de mentira (Mocks) para ilustrar o visual.
- **Discord (Canais de Voz):** Exibe avatares e salas ilustrativas. A conexão real requer integração complexa com a RPC do Discord e fluxo de autorização.
- **GitHub Actions:** Gráfico de contribuições de mentira. Integração com o Token API do GitHub está na fila.
- **OBS Studio:** Interface de botões REC/Stop ilustrativa. Integração via WebSockets planejada.

---

## 📥 Como Baixar e Instalar

Você pode configurar do seu jeito! Baixe a última versão na aba de [Releases](https://github.com/Contagiovaneines/WinDock-/releases) do GitHub.

1. Baixe o instalador seguro **GigaDock-Setup.exe**.
2. Execute o instalador (ele vai extrair os arquivos e lidar com o Windows Defender Smart App Control).
3. Abra o GigaDock e personalize seus ícones, coleções e widgets!

## 🔮 Ideias Futuras e Limitações Conhecidas

O GigaDock foi criado com uma premissa estrita de ser **100% offline, local e sem contas**. Por causa dessa regra, algumas integrações mais complexas foram mapeadas, mas arquivadas para o futuro:

- **Status Real do Discord (Salas de Voz):** Atualmente o widget do Discord é apenas ilustrativo (mock). Para ler as salas reais e quem está falando, é necessário usar a API oficial do Discord (Discord RPC). Isso exigiria criar um App no portal de desenvolvedores do Discord, gerenciar um *Client ID*, e forçar o usuário a dar "Autorizar" na janela do Discord.
- **Cores de Status do Microsoft Teams:** O Teams não salva localmente se você está "Disponível", "Ocupado" ou "Ausente". Para espelhar essas cores na doca, seria necessário integrar a *Microsoft Graph API*, o que exigiria um App no Azure AD (Entra ID) e forçar o usuário a fazer login corporativo na própria dock.
- *(Ambas as features foram adiadas para manter a dock invisível, sem telemetria e focada puramente na experiência desktop).*

## 💻 Como Compilar do Zero

Se você é desenvolvedor e quer rodar o código-fonte na sua máquina:

1. Clone o repositório:
   `ash
   git clone https://github.com/Contagiovaneines/WinDock-.git
   `
2. Abra a solução no Visual Studio 2022.
3. Certifique-se de ter o SDK do **.NET 10** instalado.
4. Antes de compilar, feche qualquer instância da dock aberta (Stop-Process -Name "DockWindows.App" no PowerShell).
5. Defina o DockWindows.App como projeto de inicialização e compile!

## 🤝 Open Source & Contribuição

Este projeto é **100% Open Source**. Sinta-se à vontade para clonar, fuçar no código, adicionar novos widgets ou alterar do jeito que quiser! 

Se você fizer algo legal, abra um [Pull Request](CONTRIBUTING.md) para enviar de volta pra cá!

## ☕ Apoie o Desenvolvedor

Criado e mantido por **Giovane Ines**.
Se o GigaDock ajudou a melhorar o visual do seu Windows e sua produtividade, mande um salve ou pague um café pro Dev!

- **LinkedIn:** [Giovane Ines](https://www.linkedin.com/in/giovaneines/)
- **GitHub:** [Contagiovaneines](https://github.com/Contagiovaneines)
- **Email:** giovaneinesdev@gmail.com
- **PIX:** giovaneinesdev@gmail.com

---

*Feito com 🩵 e muito C# / WPF. Licenciado sob MIT.*

