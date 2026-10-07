"use strict";

const $ = selector => document.querySelector(selector);
const $$ = selector => [...document.querySelectorAll(selector)];
const storageKey = "gigadock-web-demo-v2";

const library = {
  weather:{name:"Clima",type:"widget",icon:"☂",color:"#303a35",kind:"weather"},
  media:{name:"Mídia",type:"widget",icon:"♫",color:"#70425d",kind:"media"},
  pomodoro:{name:"Pomodoro",type:"widget",icon:"25",color:"#7c3f35",kind:"pomodoro"},
  files:{name:"Explorador",type:"app",icon:"▰",color:"#e1ad25"},
  chrome:{name:"Google Chrome",type:"app",icon:"C",color:"linear-gradient(135deg,#e84b3c 0 34%,#f4d046 34% 58%,#32a95c 58% 76%,#4285e5 76%)"},
  mail:{name:"Email",type:"app",icon:"✉",color:"#2674d9"},
  code:{name:"Visual Studio Code",type:"app",icon:"⌁",color:"#1677bb"},
  teams:{name:"Microsoft Teams",type:"app",icon:"T",color:"#6558c9"},
  discord:{name:"Discord",type:"app",icon:"●",color:"#5865f2"},
  spotify:{name:"Spotify",type:"app",icon:"≋",color:"#1db954"},
  whatsapp:{name:"WhatsApp",type:"app",icon:"◔",color:"#18b765"},
  edge:{name:"Microsoft Edge",type:"app",icon:"e",color:"linear-gradient(135deg,#0ea6d7,#17b87a)"},
  steam:{name:"Steam",type:"app",icon:"●",color:"#244b78"},
  notes:{name:"Notas",type:"app",icon:"▤",color:"#f2cc4d"}
};

const defaults = {
  Trabalho:{items:["media","files","chrome","mail","teams","code","spotify","whatsapp","edge"],rgb:true,monitor:true,clock:true,battery:true,size:"normal"},
  Estudos:{items:["weather","pomodoro","notes","files","chrome","code","teams"],rgb:false,monitor:true,clock:true,battery:true,size:"normal"},
  Pessoal:{items:["weather","media","spotify","whatsapp","discord","steam","chrome"],rgb:true,monitor:false,clock:true,battery:true,size:"normal"}
};

const widgets = [
 ["Relógio","rotina","16 14","Implementado","Hora, segundos, data, cartões, horários mundiais e estilos analógicos.","Configuração independente por ambiente."],
 ["Pomodoro","rotina","25:00","Implementado","Foco, pausas, ciclos e som de transição.","O prazo é preservado quando a interface fica oculta."],
 ["Calendário e reuniões","rotina","05 · Reunião","Implementado","Agenda local, próximo evento e leitura de calendário ICS.","Sem login Google ou Outlook."],
 ["Mídia","rotina","♫ Tocando agora","Implementado","Capa, controles e estilos compacto ou com barra.","Depende da sessão de mídia publicada no Windows."],
 ["Clima","rotina","☂ 25°","Implementado","Temperatura, previsão, próximas horas e vento.","A consulta depende do serviço externo configurado."],
 ["Monitor do sistema","sistema","CPU 34% · RAM 61%","Implementado","CPU, RAM, rede e armazenamento em tempo real.","Coletor local compartilhado; sem telemetria."],
 ["Bateria","sistema","100% ▰","Implementado","Indicador compacto ou anel e animação durante a carga.","Dispositivos sem bateria mostram um traço."],
 ["Mixer de volume","sistema","🔊 70%","Beta","Volume principal e por aplicativo, mudo e microfone.","A seleção de saída abre a página oficial do Windows."],
 ["WhatsApp e Teams","conexoes","3 notificações","Beta","Contagens locais a partir das notificações do Windows.","Não acessa mensagens ou conversas."],
 ["GitHub","conexoes","▦ ▦ ▦ ▦ ▦","Beta","Contribuições públicas e animações com cache.","Depende de dados públicos do GitHub."],
 ["Discord e OBS","conexoes","● Conectado","Beta","Integrações locais opcionais e atalhos rápidos.","OBS exige WebSocket local; Discord possui limites oficiais."]
];

const features = [
 ["▣","Um ambiente para cada contexto.","Aplicativos, widgets e aparência independentes. Troque de contexto sem perder o que está aberto."],
 ["✦","Uma dock com sua identidade.","Transparência, dimensões, divisores, ordem das seções, contorno RGB e estilos de relógio."],
 ["↗","Aplicativos sempre ao alcance.","Janelas abertas aparecem nos ambientes, com indicador de execução, ícone e ativação por clique."],
 ["◌","Atualiza quando faz sentido.","Widgets visuais pausam quando ocultos e serviços necessários usam cache ou eventos do Windows."],
 ["⌘","Seus dados ficam com você.","Configurações e notas locais, sem conta, backend próprio ou telemetria."]
];

let environment = "Trabalho";
let editing = false;
let libraryFilter = "todos";
let playing = true;
let state = loadState();

function cloneDefaults(){return JSON.parse(JSON.stringify(defaults));}
function loadState(){
  try{
    const saved = JSON.parse(localStorage.getItem(storageKey));
    if(!saved || typeof saved !== "object") return cloneDefaults();
    const result = cloneDefaults();
    for(const env of Object.keys(result)){
      if(!saved[env]) continue;
      const validItems = Array.isArray(saved[env].items) ? saved[env].items.filter((id,index,list)=>library[id]&&list.indexOf(id)===index) : result[env].items;
      result[env] = {...result[env],...saved[env],items:validItems};
    }
    return result;
  }catch{return cloneDefaults();}
}
function save(){localStorage.setItem(storageKey,JSON.stringify(state));}
function announce(message){$("#announcement").textContent="";requestAnimationFrame(()=>$("#announcement").textContent=message);}
function element(tag,className,text){const node=document.createElement(tag);node.className=className||"";if(text!==undefined)node.textContent=text;return node;}

function renderItem(id){
  const item=library[id];
  const button=element("button",`dock-item ${item.kind||""}`,"");
  button.dataset.itemId=id;button.dataset.type=item.type;button.dataset.running=String(item.type==="app");button.setAttribute("aria-label",item.name);
  if(item.kind==="media"){
    button.append(icon(item),mediaCopy());
  }else if(item.kind==="weather"){
    button.append(element("b","","☂ 25°"),element("small","","Caçapava"),element("small","","Chuva leve"));
  }else if(item.kind==="pomodoro"){
    button.append(element("strong","","25:00"));
  }else button.append(icon(item));
  if(editing){const remove=element("span","remove-badge","−");remove.setAttribute("aria-hidden","true");button.append(remove);}
  return button;
}
function icon(item){const node=element("span","app-icon",item.icon);node.style.background=item.color;return node;}
function mediaCopy(){const copy=element("span","media-copy","");copy.append(element("b","","More Than A Woman · Bee Gees"),element("small","","Saturday Night Fever"),element("span","media-buttons",playing?"◀ Ⅱ ▶":"◀ ▶ ▶"));return copy;}

function render(){
  const current=state[environment];
  $("#desktop").dataset.environment=environment;$("#environment-caption").textContent=`AMBIENTE / ${environment.toUpperCase()}`;
  const text={Trabalho:["Hora de produzir.","Menos distrações. Mais espaço para criar."],Estudos:["Mantenha o foco.","Ferramentas e tempo organizados para estudar."],Pessoal:["Seu tempo, do seu jeito.","Música, conversas e lazer no mesmo lugar."]}[environment];
  $("#desktop-heading").textContent=text[0];$("#desktop-subtitle").textContent=text[1];
  $("#dock").dataset.rgb=String(current.rgb);$("#dock").dataset.size=current.size;$("#dock").classList.toggle("editing",editing);
  $("#dock-items").replaceChildren(...current.items.map(renderItem));
  $(".system-monitor").hidden=!current.monitor;$(".clock-widget").hidden=!current.clock;$(".battery").hidden=!current.battery;
  $("#edit-dock").setAttribute("aria-pressed",String(editing));$("#edit-dock").textContent=editing?"✓ Concluir":"− Remover";
  $("#dock-hint").textContent=editing?"Clique no sinal − do item que deseja remover.":"Clique nos itens para explorar. Passe o mouse sobre os aplicativos.";
  $$('[data-env]').forEach(button=>button.setAttribute("aria-pressed",String(button.dataset.env===environment)));
  $("#settings-environment").textContent=environment;$("#library-environment").textContent=environment;
  $$('[data-setting]').forEach(input=>input.checked=Boolean(current[input.dataset.setting]));$("#dock-size").value=current.size;
}

function openPanel(id){
  const item=library[id];const panel=$("#widget-panel-content");panel.replaceChildren();
  panel.append(element("h3","",item.name));
  const descriptions={media:"Controle a sessão de mídia atual sem sair da dock.",weather:"Caçapava · 25° · sensação de 27° · chuva leve.",pomodoro:"Ciclo de foco demonstrativo pronto para começar.",monitor:"Uso local do computador, atualizado quando o painel está visível.",clock:"Quarta-feira, 7 de outubro · 16:14"};
  panel.append(element("p","",descriptions[id]||`${item.name} está aberto nesta demonstração. No aplicativo instalado, o clique ativa a janela real.`));
  if(id==="monitor"){const grid=element("div","panel-grid","");["CPU 34%","RAM 61%","REDE 2,4 MB/s"].forEach(x=>grid.append(element("span","",x)));panel.append(grid);}
  if(id==="weather"){const grid=element("div","panel-grid","");["Agora 25°","18h 23°","21h 21°"].forEach(x=>grid.append(element("span","",x)));panel.append(grid);}
  $("#widget-panel").hidden=false;
}

function renderLibrary(){
  const current=state[environment].items;
  const entries=Object.entries(library).filter(([,item])=>libraryFilter==="todos"||item.type===libraryFilter);
  $("#library-grid").replaceChildren(...entries.map(([id,item])=>{
    const added=current.includes(id);const button=element("button","library-item","");button.type="button";button.dataset.libraryId=id;button.disabled=added;
    const labels=element("span","","");labels.append(element("b","",item.name),element("small","",item.type==="app"?"Aplicativo":"Widget"));
    button.append(icon(item),labels,element("em","",added?"Adicionado":"＋"));return button;
  }));
}
function openLibrary(){editing=false;render();renderLibrary();$("#library-dialog").showModal();}
function openSettings(){render();$("#settings-dialog").showModal();}

$$('[data-env]').forEach(button=>button.addEventListener("click",()=>{environment=button.dataset.env;editing=false;$("#widget-panel").hidden=true;render();announce(`Ambiente ${environment} selecionado.`);}));
$("#add-item").addEventListener("click",openLibrary);$("#edit-dock").addEventListener("click",()=>{editing=!editing;render();});$("#settings-btn").addEventListener("click",openSettings);
$("#close-widget-panel").addEventListener("click",()=>$("#widget-panel").hidden=true);
$("#dock-items").addEventListener("click",event=>{
  const button=event.target.closest(".dock-item");if(!button)return;const id=button.dataset.itemId;
  if(editing){state[environment].items=state[environment].items.filter(itemId=>itemId!==id);save();render();announce(`${library[id].name} removido de ${environment}.`);return;}
  if(id==="media"&&event.target.closest(".media-buttons")){playing=!playing;render();announce(playing?"Reprodução retomada.":"Reprodução pausada.");return;}
  openPanel(id);
});
$("#dock").addEventListener("click",event=>{
  const action=event.target.closest("[data-action]")?.dataset.action;if(!action)return;
  if(action==="add")openLibrary();else if(action==="settings"||action==="theme")openSettings();else if(action==="quick")showGeneric("Controles rápidos","Wi-Fi, Bluetooth, foco, modo escuro e atalhos do sistema.");else if(action==="background")showGeneric("Em segundo plano","Chrome, Spotify, Teams e Discord estão em execução nesta simulação.");else if(action==="launcher")showGeneric("Aplicativos","A biblioteca reúne aplicativos instalados, atalhos e coleções.");else if(action==="search")showGeneric("Pesquisar","Na versão para Windows, a pesquisa encontra aplicativos, arquivos e comandos.");
});
function showGeneric(title,text){const panel=$("#widget-panel-content");panel.replaceChildren(element("h3","",title),element("p","",text));$("#widget-panel").hidden=false;}

$("#library-grid").addEventListener("click",event=>{const button=event.target.closest("[data-library-id]");if(!button||button.disabled)return;const id=button.dataset.libraryId;state[environment].items.push(id);save();render();renderLibrary();announce(`${library[id].name} adicionado a ${environment}.`);});
$$('[data-library-filter]').forEach(button=>button.addEventListener("click",()=>{libraryFilter=button.dataset.libraryFilter;$$('[data-library-filter]').forEach(item=>item.setAttribute("aria-pressed",String(item===button)));renderLibrary();}));
$$('[data-setting]').forEach(input=>input.addEventListener("change",()=>{state[environment][input.dataset.setting]=input.checked;save();render();}));
$("#dock-size").addEventListener("change",event=>{state[environment].size=event.target.value;save();render();});
$("#reset-demo").addEventListener("click",()=>{state[environment]=JSON.parse(JSON.stringify(defaults[environment]));save();render();announce(`Ambiente ${environment} restaurado.`);});
document.addEventListener("keydown",event=>{if(event.key==="Escape")$("#widget-panel").hidden=true;});

function renderCatalog(filter="todos"){
  const selected=widgets.filter(item=>filter==="todos"||item[1]===filter);
  $("#widget-grid").replaceChildren(...selected.map(([title,,preview,status,description,limit])=>{const card=element("article","widget-card","");const visual=element("div","widget-visual",preview);visual.setAttribute("aria-hidden","true");const heading=element("div","widget-heading","");heading.append(element("h3","",title),element("span",`badge ${status!=="Implementado"?"partial":""}`,status));card.append(visual,heading,element("p","",description),element("small","",limit));return card;}));
  $("#catalog-count").textContent=`${selected.length} recursos`;
}
$("#feature-grid").replaceChildren(...features.map(([symbol,title,text],index)=>{const card=element("article",`feature ${index===0?"feature-large":""}`,"");card.append(element("span","feature-icon",symbol),element("h3","",title),element("p","",text));if(index===0){const mini=element("div","mini-environments","");Object.keys(defaults).forEach(name=>mini.append(element("span","",`${name}　 ▣ ▦ ◉`)));card.append(mini);}return card;}));
$$('[data-filter]').forEach(button=>button.addEventListener("click",()=>{$$('[data-filter]').forEach(item=>item.setAttribute("aria-pressed",String(item===button)));renderCatalog(button.dataset.filter);}));

render();renderCatalog();
