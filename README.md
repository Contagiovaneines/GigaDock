# Dock Windows

Aplicativo desktop nativo e moderno para Windows 10 e Windows 11 com uma barra flutuante (dock) e múltiplos ambientes de produtividade (**Trabalho**, **Estudos** e **Pessoal**). Desenvolvido originalmente em C# com .NET 10 e WPF, com interface 100% em português do Brasil e funcionamento estritamente local (sem telemetria, contas ou nuvem).

---

## 📋 Pré-requisitos do Sistema

- **Sistema Operacional:** Windows 10 ou Windows 11 (64 bits, arquitetura x64).
- **Runtime do .NET:** [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) instalado no computador.
- **Permissões:** Roda como usuário comum (`asInvoker`). Não exige privilégios de Administrador nem janelas de UAC.

---

## 🚀 Instalação e Execução

### Opção 1: Instalador Oficial e Atualizador (Recomendado)
1. Execute o instalador oficial `dist/DockWindows-Setup.exe` (~0,49 MB).
2. O assistente de instalação/atualização detecta automaticamente versões anteriores:
   - Realiza **atualização *in-place*** sem duplicar atalhos ou pastas;
   - Realiza **backup preventivo automático** das configurações do usuário antes de substituir arquivos;
   - Se o modo "Usar como barra principal" estiver ativo, **restaura preventivamente a barra nativa do Windows** antes de atualizar binários;
   - Permite escolher criação de atalho no Menu Iniciar e na Área de Trabalho e autostart;
   - Instalação segura no perfil local do usuário (`%LOCALAPPDATA%\Programs\DockWindows`) sem exigir privilégios de administrador.
3. *Nota sobre Assinatura Digital:* O executável não possui certificado comercial pago. Ao abrir no Windows SmartScreen, clique em **"Mais informações"** e em seguida **"Executar assim mesmo"**.

### Opção 2: Executável Portátil Pré-Compilado
1. Baixe ou extraia a pasta `dist/app/` (ou `dist/DockWindows-v1.0.0/`).
2. Dê um duplo clique em `DockWindows.App.exe`.
3. A barra flutuante aparecerá centralizada na borda inferior da tela.

### Opção 3: Compilação a partir do Código-Fonte
Certifique-se de possuir o .NET SDK 10 instalado (`dotnet --info`).

1. **Restaurar e Compilar a Solução:**
   ```powershell
   dotnet build DockWindows.slnx
   ```

2. **Executar a Bateria de Testes (60 testes automatizados):**
   ```powershell
   dotnet test DockWindows.slnx
   ```

3. **Gerar o Instalador Oficial Standalone:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File tools/build-installer.ps1
   ```

---

## 🪟 Área de Aplicativos e Janelas Abertas (V1.2)

O Dock Windows traz controle completo de aplicativos e janelas abertas:

1. **Área Permanente de Aplicativos (`AppsPermanentes`):**
   - Separada dos itens específicos de Trabalho, Estudos e Pessoal.
   - Atalhos para **Menu Iniciar** (⊞), **Pesquisa** (🔍 <kbd>Win+S</kbd>) e **Explorador de Arquivos** (📁 <kbd>Win+E</kbd>).
   - Extração do ícone real de executáveis (`.exe`), atalhos (`.lnk`) e apps do sistema.
   - Fixar, desafixar e reordenar por menu de contexto, arrastar e soltar (drag & drop) ou na central de personalização.
   - Preservados ao alternar entre ambientes.

2. **Rastreamento de Janelas Abertas:**
   - Detecta janelas abertas em tempo real via APIs Win32 oficiais (`EnumWindows`, `SetWinEventHook`, `GetForegroundWindow`).
   - Não duplica o ícone se um aplicativo fixado estiver aberto.
   - **Indicadores Visuais do Windows 11:**
     - Ponto cinza sutil para apps abertos em segundo plano.
     - Barra azul iluminada e realce translúcido para o app ativo em primeiro plano.
     - Badge numérico no canto superior direito para aplicativos com múltiplas janelas (ex: `2`, `3`).
   - **Interação:**
     - Clique em app com 1 janela: alterna entre focar e minimizar.
     - Clique em app com múltiplas janelas: abre flyout suspenso listando cada janela para alternar ou fechar individualmente.
     - Menu de contexto: Nova Janela, Fixar/Desafixar da Dock, Mover Esquerda/Direita e Fechar Todas as Janelas.

3. **Organização e Seções Configuráveis:**
   - 6 seções modulares: `[Iniciar e Pesquisa] [Apps permanentes e abertos] [Coleções] [Itens do ambiente atual] [Widgets] [Relógio e controles]`.
   - Na janela unificada de **Ajustes e Personalização**, você pode reordenar as seções (⬆️ Subir / ⬇️ Descer), ocultar qualquer uma delas e alternar se os apps fixados são globais ou exclusivos do ambiente ativo.
   - Documentação detalhada em `docs/09-area-aplicativos-janelas-abertas.md` e `docs/10-ajustes-colecoes-widgets-temas.md`.

---

## 🗂️ Coleções de Aplicativos e Widgets Embutidos (V1.3)

O Dock Windows V1.3 expande a organização da barra com:

1. **Coleções de Aplicativos (Pastas / Grupos):**
   - Agrupe vários atalhos ou aplicativos sob um único ícone temático na barra.
   - Suporte a **Coleções Globais** (visíveis em qualquer ambiente) e **Coleções por Ambiente** (específicas de Trabalho, Estudos ou Pessoal).
   - Badge numérico sutil indicando a quantidade de apps contidos na pasta.
   - **Flyout Ancorado:** Ao clicar na coleção, um painel suspenso abre imediatamente com os aplicativos em grade.
   - Fecha automaticamente ao pressionar <kbd>Esc</kbd>, clicar fora ou ao iniciar um aplicativo.

2. **Widgets no Dock em Modo Compacto e Expandido:**
   - **Relógio Digital:** Exibição da hora atual ou formato expandido com data.
   - **Pomodoro de Foco:** Cronômetro integrado para produtividade com controle de foco e pausas curtas/longas.
   - **Calendário e Próximos Compromissos:** Exibe a data e o próximo compromisso local agendado. Sem eventos cadastrados, mostra um estado vazio útil e elegante ("Sem eventos pendentes"). Gerencie seus compromissos localmente sem nuvem ou telemetria.

3. **Magnificação Suave de Ícones (Hover):**
   - Aumento fluido dos ícones ao passar o mouse (escala 1.18x com aceleração cúbica natural), respeitando opções de redução de movimento do Windows.
   - Transição inteligente ao mudar de ambiente: **apenas os itens específicos do ambiente são animados**, mantendo fixos os aplicativos globais e as janelas abertas.

4. **Divisores e Espaçadores Configuráveis:**
   - Escolha entre estilo **Linha vertical**, **Espaço vazio** ou **Ponto sutil** entre blocos da dock, com controle de largura deslizante.

5. **5 Temas Visuais Originais:**
   - **Vidro Líquido (Apple Liquid Glass):** Transparência fluida profunda, dupla borda com reflexo especular de alta refração (`inset 1px 1px 1px var(--highlight)`) e microinterações táteis de clique e hover.
   - **Discreto (Minimal Slate):** Visual minimalista em tons de ardósia e transparência equilibrada.
   - **Escuro (Dark Obsidian):** Contraste clássico profundo com acento azul Windows 11 Fluent.
   - **Colorido (Vibrant Aura):** Estilo moderno em degradê violeta escuro com realces magenta e ciano.
   - **Com Brilho (Glow Glass):** Efeito vidro translúcido com bordas iluminadas e sombras projetadas.

---

## 🖥️ Modo de Substituição ("Usar como barra principal")

Nas Configurações da Dock (Aba ⚙️ Geral), você pode ativar a opção **"Usar como barra principal"**:
- O dock é posicionado rente à borda inferior do monitor primário.
- A barra de tarefas nativa do Windows é recolhida de forma limpa e oficial via API `SHAppBarMessage` do Windows.
- O processo `explorer.exe` **nunca** é encerrado ou modificado.
- A opção vem **desativada por padrão** e pode ser revertida a qualquer momento:
  - Clicando com o botão direito na dock > **"🔄 Restaurar Barra do Windows"**;
  - Clicando com o botão direito no ícone da bandeja > **"🔄 Restaurar Barra do Windows"**;
  - Ao sair do aplicativo ou ao desinstalar.

### Recuperação de Emergência da Barra de Tarefas
Se o aplicativo fechar de forma inesperada ou não iniciar, você pode restaurar a barra instantaneamente:
- **Scripts inclusos no projeto:** Dê um duplo clique em `tools\restaurar-barra-windows.bat` (ou execute `tools\restaurar-barra-windows.ps1` no PowerShell).
- **Consulte a documentação completa:** Para atalhos de substituição de recursos da barra nativa (ex: Central de Ações <kbd>Win+N</kbd>, Configurações Rápidas <kbd>Win+A</kbd>, Bandeja <kbd>Win+B</kbd>), leia `docs/08-substituicao-personalizacao-instalador.md`.

---

## ⚙️ Janela de Configurações em Três Colunas (V1.4)

Acesse com o botão direito na dock ou no ícone da bandeja para abrir a janela redimensionável de configurações:
- **Coluna 1 — Navegação Lateral:**
  - Identificação de nome, versão (`1.4.0`) e ambiente ativo;
  - Grupo **Principal**: *Ambientes*, *Widgets* e *Espaçadores*;
  - Grupo **Ajustes**: *Aparência*, *Geral*, *Utilitários* e *Sobre*.
- **Coluna 2 — Itens da Seção:**
  - Lista de ambientes com cores, crachá ativo, reordenação e botões `+`/`−` (com bloqueio defensivo contra exclusão do último ambiente);
  - Lista de widgets disponíveis/instalados com ativação rápida e reordenação;
  - Lista de espaçadores com estilo e dimensões;
  - Cartões interativos dos 4 temas visuais predefinidos.
- **Coluna 3 — Editor de Detalhes:**
  - **Ambientes:** Edição de nome e cor; personalização exclusiva do indicador de aplicativos abertos (**Barra**, **Ponto**, **Pílula**) com cor hex dedicada; lista de apps com reordenação, alternância de escopo (*"Somente neste ambiente"* vs *"Global"*), adição de apps (`.exe`/`.lnk`), arquivos, pastas, URLs (com validação defensiva) e coleções; checklist de widgets ativos por ambiente.
  - **Widgets:** Formato (*Compacto* vs *Expandido*), tempos de Pomodoro e gerenciador de compromissos locais (criação e exclusão de eventos sem telemetria).
  - **Espaçadores:** Estilo (*Linha*, *Espaço*, *Ponto*), slider de largura (4px a 32px) e visibilidade.
  - **Aparência:** Sliders de opacidade, raio de curvatura, espaçamento, alternância de desfoque e velocidade de animações.
  - **Geral e Utilitários:** Modo barra principal, auto-hide, autostart, atalhos, exportação/importação JSON de configurações e recuperação de emergência da barra de tarefas.

---

## 🗑️ Como Desinstalar

- **Via Windows:** Abra *Configurações > Aplicativos > Aplicativos Instalados* (ou o Painel de Controle), localize **Dock Windows** e clique em **Desinstalar**.
- **Via Desinstalador Direto:** Execute `Uninstall.exe --uninstall` na pasta `%LOCALAPPDATA%\Programs\DockWindows`.
- O desinstalador restaura a barra de tarefas do Windows imediatamente e pergunta se deseja manter ou apagar suas preferências pessoais (`%LOCALAPPDATA%\DockWindows`).

---

## 📁 Onde Ficam as Configurações e Como Funciona a Persistência

- **Diretório:** `%LOCALAPPDATA%\DockWindows\`
- **Arquivo Principal:** `settings.json` (Esquema v4 com migração atômica automática a partir de v1, v2 e v3)
- **Arquivo de Backup Automático:** `settings.json.bak`
- **Arquivo de Escrita Temporária:** `settings.json.tmp`

---

## ⌨️ Atalhos de Teclado e Acessibilidade

| Atalho | Ação |
| :--- | :--- |
| `Ctrl + Alt + D` | Exibir ou ocultar a barra flutuante da dock |
| `Ctrl + Alt + 1` | Alternar imediatamente para o ambiente **Trabalho** |
| `Ctrl + Alt + 2` | Alternar imediatamente para o ambiente **Estudos** |
| `Ctrl + Alt + 3` | Alternar imediatamente para o ambiente **Pessoal** |
| `Esc` | Fechar popovers abertos (Coleções, Calendário, Pomodoro e janelas de apps) |
| `Tab` / `Shift + Tab` | Navegar sequencialmente entre os elementos da interface |
| `Enter` / `Espaço` | Acionar o item ou botão atualmente focalizado |
| `Shift + F10` / `Menu` | Abrir menu de contexto da dock ou do item selecionado |

---

## 📦 Detalhes Técnicos da Publicação

- **Tipo de Publicação:** Framework-Dependent Deployment (FDD), arquitetura `win-x64`.
- **Dependências:** `Microsoft.WindowsDesktop.App 10.0` (WPF/.NET 10).
- **Segurança de Binários:** O binário não utiliza assinaturas digitais forjadas ou autofirmadas sem credenciais legítimas, atendendo às diretrizes de conformidade do projeto.

