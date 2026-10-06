
"use strict";
const widgets = [
 ["Relógio","rotina","12:26","Implementado","Hora, segundos, data, cartões, horários mundiais e analógicos.","Fusos fixos; cartões sem animação de virada."],
 ["Cronômetro e temporizador","rotina","05:00 ▷","Implementado","Inicie, pause e reinicie os controles de tempo do relógio.","Temporizador de 5 minutos, sem alarme nem persistência após encerrar."],
 ["Pomodoro","rotina","25:00","Implementado","Foco, pausas, ciclos e som de transição; preserva o prazo com a interface oculta.","Configuração por ambiente; não há motores independentes por ambiente."],
 ["Calendário e reuniões","rotina","05 · Reunião às 14h","Implementado","Agenda local, próximo evento, próxima reunião e leitura de calendário ICS.","Sem login Google/Outlook ou controle de câmera e microfone."],
 ["Mídia","rotina","♫ Tocando agora Ⅱ","Implementado","Capa, fundo derivado da música, controles e estilos mini ou com barra.","Depende da sessão publicada pelo player no Windows; sem arrastar o progresso.","media"],
 ["Clima","rotina","☀ 21°","Implementado","Hoje, previsão, próximas horas, vento, nascer/pôr do sol e cartão azul.","Consulta wttr.in; intervalos e dados dependem do serviço."],
 ["Monitor do sistema","sistema","CPU 34% · RAM 61%","Implementado","CPU, RAM, gráficos, rede, download, upload e armazenamento.","Coletor compartilhado; sem GPU. VPN pode duplicar tráfego.","system"],
 ["Bateria","sistema","◔ 96%","Implementado","Indicador compacto ou anel, com carga e estado de carregamento.","Equipamentos sem bateria ou dados conhecidos mostram um traço.","system"],
 ["WhatsApp","conexoes","◉ 3 notificações","Integração parcial","Contagens locais e alertas visuais pelas notificações do Windows.","Não acessa conversas; depende de permissão e notificações publicadas.","connect"],
 ["Teams","conexoes","▣ Próxima atividade","Integração parcial","Notificações e heurísticas de processos, reuniões e chamadas.","Não consulta presença oficial nem garante nomes de participantes.","connect"],
 ["GitHub","conexoes","▦ ▦ ▦ ▦ ▦","Integração parcial","Contribuições públicas e animações do painel, com cache.","Depende do HTML público; não monitora GitHub Actions.","connect"],
 ["Discord","conexoes","◉ Discord","Demonstrativo","Atalho e tentativa de conexão ao pipe local.","Sala/usuários ilustrativos; RPC real não implementado.","connect"],
 ["OBS","conexoes","◉ OBS","Demonstrativo","Atalho para abrir o OBS e controle visual de gravação.","O botão não controla gravação real; WebSocket ausente.","connect"],
 ["Notas","rotina","▤ Uma ideia por perto","Em evolução","Leitura e salvamento de notas em arquivo local.","A interface e o catálogo ainda têm partes incompletas."]
];
const features=[
 ["▣","Um ambiente para cada contexto.","Itens fixados, aparência e widgets por ambiente. Alterne entre Trabalho, Estudos e Pessoal sem misturar seus atalhos."],
 ["✦","Uma dock com sua identidade.","Temas, transparência, cantos, tamanho, ordem das seções e contorno RGB. Modo gamer persistente e RGB musical."],
 ["↗","Prévias ao alcance do mouse.","Expansão dos ícones, prévias de janelas e pastas, abertura por clique ou hover e navegação por teclado."],
 ["◌","Atualiza quando faz sentido.","Widgets visuais pausam quando ocultos. Cache evita consultas repetidas; Pomodoro e notificações necessárias continuam. Consumo real de CPU/RAM ainda precisa de medição."],
 ["⌘","Seus dados ficam com você.","Configurações e notas locais, sem conta ou backend próprio. Clima, GitHub e calendários por URL dependem de internet."]
];
const defaults={
 Trabalho:{rgb:true,media:true,weather:true,clock:true,battery:true,trash:true,size:"normal",clockStyle:"date"},
 Estudos:{rgb:false,media:false,weather:true,clock:true,battery:true,trash:false,size:"normal",clockStyle:"cards"},
 Pessoal:{rgb:true,media:true,weather:false,clock:true,battery:false,trash:true,size:"normal",clockStyle:"minimal"}
};
const states=structuredClone(defaults);
const apps={Trabalho:[["▣","Arquivos","app-folder"],["⌘","Editor de código","app-code"],["▦","Teams","app-meeting"],["◉","WhatsApp","app-chat"]],Estudos:[["▤","Notas","app-notes"],["▣","Materiais","app-folder"],["⌘","Editor de código","app-code"]],Pessoal:[["♫","Música","app-music"],["◉","Conversas","app-chat"],["▣","Fotos","app-folder"]]};
let environment="Trabalho",playing=true,track=0;
const tracks=["Um novo começo","Horizonte aberto","Depois do expediente"];
const $=selector=>document.querySelector(selector);
function element(tag,className,text){const node=document.createElement(tag);node.className=className;node.textContent=text;return node;}
function announce(message){$("#announcement").textContent=message;}
function render(){
 const state=states[environment];$("#desktop").dataset.environment=environment;$("#environment-caption").textContent=`AMBIENTE / ${environment.toUpperCase()}`;
 $("#desktop-subtitle").textContent={Trabalho:"Menos distrações. Mais espaço para criar.",Estudos:"Cada descoberta começa com um pouco de foco.",Pessoal:"Um espaço para o que faz seu dia melhor."}[environment];
 $("#dock").dataset.rgb=String(state.rgb);$("#dock").dataset.size=state.size;
 for(const name of ["media","weather","clock","battery","trash"])$(`#demo-${name}`).hidden=!state[name];
 $("#demo-clock").classList.toggle("cards",state.clockStyle==="cards");$("#demo-clock small").hidden=state.clockStyle!=="date";
 $("#settings-environment").textContent=environment;
 document.querySelectorAll("[data-env]").forEach(button=>button.setAttribute("aria-pressed",String(button.dataset.env===environment)));
 document.querySelectorAll("[data-setting]").forEach(input=>{input.checked=state[input.dataset.setting];});
 $("#dock-size").value=state.size;$("#clock-style").value=state.clockStyle;
 $("#demo-apps").replaceChildren(...apps[environment].map(([symbol,title,className])=>{const button=element("button",`dock-app ${className}`,symbol);button.setAttribute("aria-label",`Prévia de ${title}`);button.dataset.preview=title;return button;}));
}
function catalog(filter="todos"){
 const selection=widgets.filter(widget=>filter==="todos"||widget[1]===filter);
 $("#widget-grid").replaceChildren(...selection.map(([title,category,preview,status,description,limit,className])=>{
 const card=element("article","widget-card","");const visual=element("div",`widget-visual ${className||""}`,preview);visual.setAttribute("aria-hidden","true");
 const heading=element("div","widget-heading","");heading.append(element("h3","",title),element("span",`badge ${status!=="Implementado"?"partial":""}`,status));
 card.append(visual,heading,element("p","",description),element("small","",limit));return card;
 }));$("#catalog-count").textContent=`${selection.length} recursos`;
}
$("#feature-grid").replaceChildren(...features.map(([icon,title,text],index)=>{const card=element("article",`feature ${index===0?"feature-large":""}`,"");card.append(element("span","feature-icon",icon),element("h3","",title),element("p","",text));if(index===0){const mini=element("div","mini-environments","");for(const name of Object.keys(apps))mini.append(element("span","",`${name}　 ▣ ▦ ◉`));card.append(mini);}return card;}));
function openSettings(){render();$("#settings-dialog").showModal();}
$("#settings-btn").addEventListener("click",openSettings);$("#dock-settings").addEventListener("click",openSettings);
document.querySelectorAll("[data-env]").forEach(button=>button.addEventListener("click",()=>{environment=button.dataset.env;$("#dock-popover").hidden=true;render();announce(`Ambiente ${environment} selecionado.`);}));
document.querySelectorAll("[data-setting]").forEach(input=>input.addEventListener("change",()=>{states[environment][input.dataset.setting]=input.checked;render();}));
$("#dock-size").addEventListener("change",event=>{states[environment].size=event.target.value;render();});
$("#clock-style").addEventListener("change",event=>{states[environment].clockStyle=event.target.value;render();});
$("#reset-demo").addEventListener("click",()=>{states[environment]=structuredClone(defaults[environment]);render();announce("Ambiente restaurado.");});
document.querySelectorAll("[data-filter]").forEach(button=>button.addEventListener("click",()=>{document.querySelectorAll("[data-filter]").forEach(item=>item.setAttribute("aria-pressed",String(item===button)));catalog(button.dataset.filter);}));
$("#play-track").addEventListener("click",()=>{playing=!playing;$("#play-track").textContent=playing?"Ⅱ":"▷";$("#play-track").setAttribute("aria-pressed",String(playing));$("#play-track").setAttribute("aria-label",playing?"Pausar reprodução":"Retomar reprodução");announce(playing?"Reprodução simulada retomada.":"Reprodução simulada pausada.");});
function changeTrack(step){track=(track+step+tracks.length)%tracks.length;$("#song-title").textContent=tracks[track];$("#demo-media").style.background=["linear-gradient(120deg,#8d804e55,#164b3e66)","linear-gradient(120deg,#7d428e66,#215b9066)","linear-gradient(120deg,#9b413966,#79693066)"][track];announce(`Faixa ilustrativa: ${tracks[track]}`);}
$("#next-track").addEventListener("click",()=>changeTrack(1));$("#previous-track").addEventListener("click",()=>changeTrack(-1));
$("#dock").addEventListener("click",event=>{const button=event.target.closest("[data-preview]");if(!button)return;$("#popover-title").textContent=button.dataset.preview;$("#popover-text").textContent=button.dataset.preview==="Lixeira"?"Prévia ilustrativa. No aplicativo, o atalho abre a lixeira do Windows.":"No Windows, este item abre ou ativa o programa. Prévias dependem do DWM; esta página não executa aplicativos.";$("#dock-popover").hidden=false;});
$(".popover-close").addEventListener("click",()=>{$("#dock-popover").hidden=true;});
document.addEventListener("keydown",event=>{if(event.key==="Escape")$("#dock-popover").hidden=true;});
render();catalog();
