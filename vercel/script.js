"use strict";

const widgets = [
  ["Relógio", "rotina", "16 52", "Implementado", "Hora, segundos, data, cartões, fusos e estilos analógicos.", "Configuração independente por ambiente."],
  ["Pomodoro", "rotina", "25:00", "Implementado", "Foco, pausas, ciclos e som de transição.", "O prazo é preservado quando a interface fica oculta."],
  ["Calendário e reuniões", "rotina", "05 · Reunião", "Implementado", "Agenda local, próximo evento e leitura de calendário ICS.", "Sem login obrigatório em Google ou Outlook."],
  ["Mídia", "rotina", "♫ Tocando agora", "Implementado", "Capa, faixa, artista, progresso e controles de reprodução.", "Depende da sessão de mídia publicada pelo Windows."],
  ["Clima", "rotina", "☂ 23°", "Implementado", "Temperatura, chuva, vento, próximas horas e previsão diária.", "A consulta depende do serviço externo configurado."],
  ["Monitor do sistema", "sistema", "CPU 25% · RAM 89%", "Implementado", "CPU, memória, rede e armazenamento em tempo real.", "Coleta local, compartilhada e sem telemetria."],
  ["Bateria", "sistema", "100% ▰", "Implementado", "Indicador compacto ou anel e animação durante a carga.", "Dispositivos sem bateria mostram um traço."],
  ["Mixer de volume", "sistema", "🔊 70%", "Beta", "Volume principal e por aplicativo, mudo e microfone.", "A seleção de saída usa as opções seguras do Windows."],
  ["WhatsApp e Teams", "conexoes", "3 notificações", "Beta", "Indicadores locais a partir das notificações do Windows.", "Não acessa mensagens nem conversas."],
  ["GitHub", "conexoes", "▦ ▦ ▦ ▦ ▦", "Beta", "Contribuições públicas com estilos de jogos e cache.", "Depende dos dados públicos disponibilizados pelo GitHub."],
  ["Discord e OBS", "conexoes", "● Conectado", "Beta", "Integrações locais opcionais e atalhos rápidos.", "OBS exige WebSocket local; Discord possui limites oficiais."]
];

const features = [
  ["▦", "Um ambiente para cada contexto.", "Aplicativos, widgets e aparência independentes para Trabalho, Estudos e Pessoal."],
  ["✦", "Uma dock com sua identidade.", "Transparência, dimensões, divisores, ordem das seções, contorno RGB e estilos de relógio."],
  ["↗", "Aplicativos sempre ao alcance.", "Janelas abertas aparecem nos ambientes com indicador de execução, ícone e ativação por clique."],
  ["◌", "Atualiza quando faz sentido.", "Widgets visuais pausam quando ocultos e serviços necessários usam cache ou eventos do Windows."],
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
