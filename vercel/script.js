"use strict";

const widgets = [
  ["Relógio", "rotina", "16 52", "Implementado", "Hora, segundos, data, cartões, fusos e estilos analógicos.", "Configuração independente por ambiente."],
  ["Pomodoro", "rotina", "25:00", "Implementado", "Foco, pausas, ciclos e som de transição.", "O prazo é preservado quando a interface fica oculta."],
  ["Calendário e reuniões", "rotina", "05 · Reunião", "Implementado", "Agenda local, próximo evento e leitura de calendário ICS.", "Sem login obrigatório em Google ou Outlook."],
  ["Mídia", "rotina", "♫ Tocando agora", "Implementado", "No Windows: capa, faixa, artista, progresso e controles. No Linux: integração opcional com players MPRIS.", "Linux exige playerctl; os recursos visuais variam entre as versões."],
  ["Clima", "rotina", "☂ 23°", "Implementado", "Temperatura, chuva, vento, próximas horas e previsão diária.", "A consulta depende do serviço externo configurado."],
  ["Monitor do sistema", "sistema", "CPU 25% · RAM 89%", "Implementado", "CPU e memória nos dois sistemas. Rede e armazenamento possuem recursos diferentes conforme a versão.", "Coleta local e sem telemetria; disponibilidade depende do sistema."],
  ["Bateria", "sistema", "100% ▰", "Implementado", "Estado da bateria nos dois sistemas; estilos de anel e animação disponíveis no Windows.", "A leitura Linux depende das informações expostas pelo hardware."],
  ["Mixer de volume", "sistema", "🔊 70%", "Beta", "No Windows: volume principal e por aplicativo, mudo e microfone. No Linux: controles de som com pactl.", "Linux exige PulseAudio ou PipeWire com compatibilidade PulseAudio."],
  ["WhatsApp e Teams", "conexoes", "3 notificações", "Beta", "Indicadores locais a partir das notificações do Windows.", "Não acessa mensagens nem conversas."],
  ["GitHub", "conexoes", "▦ ▦ ▦ ▦ ▦", "Beta", "Contribuições públicas com estilos de jogos e cache.", "Depende dos dados públicos disponibilizados pelo GitHub."],
  ["Discord e OBS", "conexoes", "● Conectado", "Beta", "Integrações locais opcionais no Windows. A beta Linux oferece integração OBS via WebSocket v5.", "Discord não está integrado na beta Linux; OBS exige servidor WebSocket local."],
  ["Notas e tarefas", "rotina", "✓ Meu dia", "Implementado", "Anotações e lista de tarefas locais, separadas por ambiente, no Windows e no Linux.", "Sem sincronização online ou conta."],
  ["Pokédex", "rotina", "✦ 151", "Beta", "Sprites animados de 151 Pokémon para acompanhar sua dock nos dois sistemas.", "Aparência e opções ainda estão em evolução."]
];

const features = [
  ["▦", "Um ambiente para cada contexto.", "Aplicativos, widgets e aparência independentes para Trabalho, Estudos e Pessoal."],
  ["✦", "Uma dock com sua identidade.", "Transparência, dimensões, divisores, ordem das seções, contorno RGB e estilos de relógio."],
  ["↗", "Aplicativos sempre ao alcance.", "Itens fixados nos dois sistemas. No Linux, listar e ativar janelas exige sessão X11 e wmctrl; no Windows, há integração com as janelas do sistema."],
  ["◌", "Agora também no Linux.", "Beta x64 com catálogo .desktop, widgets locais e instalação por usuário. Compatibilidade e paridade visual continuam em validação."],
  ["⌘", "Seus dados ficam com você.", "Configurações e notas locais, sem conta, backend próprio ou telemetria."]
];

function element(tag, className, text) {
  const node = document.createElement(tag);
  node.className = className || "";
  if (text !== undefined) node.textContent = text;
  return node;
}

function renderFeatures() {
  const grid = document.querySelector("#feature-grid");
  grid.replaceChildren(...features.map(([symbol, title, text], index) => {
    const card = element("article", `feature ${index === 0 ? "feature-large" : ""}`);
    card.append(element("span", "feature-icon", symbol), element("h3", "", title), element("p", "", text));
    if (index === 0) {
      const environments = element("div", "mini-environments");
      ["Trabalho", "Estudos", "Pessoal"].forEach(name => environments.append(element("span", "", `${name}　 ▣ ▦ ◉`)));
      card.append(environments);
    }
    return card;
  }));
}

function renderWidgets(filter = "todos") {
  const selection = widgets.filter(item => filter === "todos" || item[1] === filter);
  const cards = selection.map(([title, , preview, status, description, limit]) => {
    const card = element("article", "widget-card");
    const visual = element("div", "widget-visual", preview);
    visual.setAttribute("aria-hidden", "true");
    const heading = element("div", "widget-heading");
    heading.append(element("h3", "", title), element("span", `badge ${status !== "Implementado" ? "partial" : ""}`, status));
    card.append(visual, heading, element("p", "", description), element("small", "", limit));
    return card;
  });
  document.querySelector("#widget-grid").replaceChildren(...cards);
  document.querySelector("#catalog-count").textContent = `${selection.length} recursos`;
}

document.querySelectorAll("[data-filter]").forEach(button => button.addEventListener("click", () => {
  document.querySelectorAll("[data-filter]").forEach(item => item.setAttribute("aria-pressed", String(item === button)));
  renderWidgets(button.dataset.filter);
}));

renderFeatures();
renderWidgets();

const screenshotTabs = [...document.querySelectorAll("[data-screen]")];
function selectScreenshot(tab, focus = false) {
  screenshotTabs.forEach(item => {
    const selected = item === tab;
    item.setAttribute("aria-selected", String(selected));
    item.tabIndex = selected ? 0 : -1;
    document.getElementById(item.getAttribute("aria-controls")).hidden = !selected;
  });
  if (focus) tab.focus();
}
screenshotTabs.forEach((tab, index) => {
  tab.addEventListener("click", () => selectScreenshot(tab));
  tab.addEventListener("keydown", event => {
    let target;
    if (event.key === "ArrowRight") target = (index + 1) % screenshotTabs.length;
    else if (event.key === "ArrowLeft") target = (index - 1 + screenshotTabs.length) % screenshotTabs.length;
    else if (event.key === "Home") target = 0;
    else if (event.key === "End") target = screenshotTabs.length - 1;
    else return;
    event.preventDefault(); selectScreenshot(screenshotTabs[target], true);
  });
});
