# 🚀 GigaDock 3.0

O **GigaDock** é um dock moderno, nativo e ultra-leve para Windows 10/11 que traz produtividade, fluidez e organização para sua área de trabalho. Criado para quem deseja uma interface limpa, inspirada no macOS, sem perder a performance.

![GigaDock Preview](docs/dock-apps-preview.jpg)

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

## 🧩 Galeria de Widgets (Status Oficial)

O GigaDock possui uma loja interna onde você pode ativar e desativar "módulos". Aqui está o status real de cada um deles:

### ✅ Widgets 100% Funcionais
- **Relógio Digital:** Mostra as horas e expande para exibir a data completa.
- **Clima e Tempo:** Previsão e temperatura atual baseada na sua localização.
- **Contribuições do GitHub:** Acompanhe seus commits. **(Easter Egg: Clique com o botão direito para alternar entre as animações da Cobrinha e Pac-Man devorando a grade!)**
- **Monitor de Sistema:** Acompanhe o uso real de CPU e Memória RAM sem abrir o gerenciador de tarefas.
- **WhatsApp:** Mostra a última mensagem recebida. (Clique com o botão direito para testar o **Alerta Neon Verde**).
- **Microsoft Teams:** Exibe o status atual e reuniões. (Clique com botão direito para testar o **Alerta Neon Roxo**).
- **Discord Voz:** Mostra a sala atual. (Clique com o botão direito para testar o **Alerta Neon Azul**).
- **Pomodoro de Foco:** Cronômetro funcional de 25 minutos para técnica Pomodoro.
- **Bloco de Notas:** Um espaço rápido para rascunhos.
- **Calendário de Compromissos:** Leitor de agenda para próximos eventos.

### 🚧 Widgets em Desenvolvimento (Ainda não funcionais na V3.0)
- **Cotação de Moedas:** Visualizador de Dólar, Euro e Criptomoedas em tempo real. *(Atualmente desabilitado na Loja).*
- **Controle de Mídia Avançado (Spotify/Music):** Atualmente em fase de Mock (dados falsos), aguardando implementação da API global do Windows Media.
- **Assistente Virtual Flutuante (Mascote IA):** Mascote inteligente alimentado por Inteligência Artificial. Planejado para o futuro.

---

## 📥 Como Baixar e Instalar

Você pode configurar do seu jeito! Baixe a última versão na aba de [Releases](https://github.com/Contagiovaneines/WinDock-/releases) do GitHub.

1. Baixe o instalador seguro **GigaDock-Setup.exe**.
2. Execute o instalador (ele vai extrair os arquivos e lidar com o Windows Defender Smart App Control).
3. Abra o GigaDock e personalize seus ícones, coleções e widgets!

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

