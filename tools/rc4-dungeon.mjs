// RC-4 dungeon and quest rewards. Headless Chrome, not a phone.
// Normal clear uses the attack button and the skill button. It does not
// set enemy HP, call killEnemy, skip waves, or write the clear flag.
// Wave gap stays 0.45s. Injected cases are marked INJECT.
import { spawn, execFileSync } from "node:child_process";
import { writeFileSync, unlinkSync } from "node:fs";
const base = "http://127.0.0.1:8765/";
const PORT = 9258;
const rc1Path = "/agent/repos/mengyu-legend/rc1-client.html";
const fail=[];
function sleep(ms){ return new Promise(r=>setTimeout(r,ms)); }
class CDP{
  constructor(ws){ this.ws=ws; this.id=0; this.pending=new Map();
    ws.addEventListener("message",ev=>{ const msg=JSON.parse(ev.data); if(msg.id&&this.pending.has(msg.id)){ const {resolve,reject}=this.pending.get(msg.id); this.pending.delete(msg.id); if(msg.error) reject(new Error(JSON.stringify(msg.error))); else resolve(msg.result); }}); }
  send(m,p={},s){ const id=++this.id; return new Promise((resolve,reject)=>{ this.pending.set(id,{resolve,reject}); const o={id,method:m,params:p}; if(s)o.sessionId=s; this.ws.send(JSON.stringify(o)); }); }
}
writeFileSync(rc1Path, execFileSync("git",["show","8f99a0c:index.html"],{cwd:"/agent/repos/mengyu-legend"}));
const chrome=spawn("google-chrome",["--headless=new","--disable-gpu","--no-sandbox","--disable-dev-shm-usage","--use-angle=swiftshader","--enable-webgl",`--remote-debugging-port=${PORT}`,"--window-size=1100,720","--user-data-dir=/tmp/chrome-rc4-"+Date.now(),"about:blank"],{stdio:"ignore"});
let browser; for(let i=0;i<50;i++){ try{ browser=await (await fetch(`http://127.0.0.1:${PORT}/json/version`)).json(); break;}catch{ await sleep(200);} }
if(!browser){ console.error("no chrome"); process.exit(1); }
const ws=new WebSocket(browser.webSocketDebuggerUrl); await new Promise(r=>ws.addEventListener("open",r));
const cdp=new CDP(ws);
const logs=[];
async function attach(url){
  const {targetId}=await cdp.send("Target.createTarget",{url:"about:blank"});
  const {sessionId}=await cdp.send("Target.attachToTarget",{targetId,flatten:true});
  await cdp.send("Page.enable",{},sessionId); await cdp.send("Runtime.enable",{},sessionId);
  cdp.ws.addEventListener("message",ev=>{
    const msg=JSON.parse(ev.data);
    if(msg.sessionId!==sessionId) return;
    if(msg.method==="Runtime.exceptionThrown") logs.push(((msg.params.exceptionDetails.exception&&msg.params.exceptionDetails.exception.description)||"").slice(0,240));
  });
  const ev=async(e)=>{ const r=await cdp.send("Runtime.evaluate",{expression:e,awaitPromise:true,returnByValue:true},sessionId); if(r.exceptionDetails) throw new Error((r.exceptionDetails.exception&&r.exceptionDetails.exception.description)||r.exceptionDetails.text); return r.result.value; };
  await cdp.send("Page.navigate",{url},sessionId);
  for(let i=0;i<80;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
  return {ev, sessionId};
}
function check(n,ok,x){ console.log((ok?"PASS ":"FAIL ")+n+(x?" "+x:"")); if(!ok) fail.push(n); }
const pulse=`(()=>{
  function tap(id){
    const el=document.getElementById(id);
    el.dispatchEvent(new PointerEvent("pointerdown",{bubbles:true,cancelable:true,pointerId:1,pointerType:"touch",clientX:40,clientY:40}));
    el.dispatchEvent(new PointerEvent("pointerup",{bubbles:true,cancelable:true,pointerId:1,pointerType:"touch",clientX:40,clientY:40}));
  }
  tap("atk");
  return true;
})()`;
try{
await cdp.send("Target.setDiscoverTargets",{discover:true});
const cur=await attach(base+"index.html?shot=play");
await cdp.send("Emulation.setDeviceMetricsOverride",{width:360,height:640,deviceScaleFactor:1,mobile:true},cur.sessionId);
await sleep(400);
const setup=await cur.ev(`(()=>{
  const m=window.__mengyu;
  m.finishCoach();
  const s=m.G.save;
  s.level=8; s.gold=500; s.xp=0; s.growth=s.growth||{skills:{},talents:{}};
  s.equip.weapon={uid:"sword-play",slot:"weapon",rar:2,lv:6,cls:"swordsman",atk:16,def:0,hp:0,mp:0,crit:0,price:40,name:"试炼长剑",plus:0,locked:false,enhSpent:0};
  m.recalcStats(); m.G.p.hp=m.ST.hp; m.G.p.mp=m.ST.mp; m.G.p.dead=false; m.G.paused=false; m.G.mode="play";
  const preview=m.previewDungeon("normal", s);
  m.G.p.x=2.2; m.G.p.z=12.0;
  return {preview, hp:m.G.p.hp, atk:m.ST.atk, gold:s.gold, xp:s.xp, pots:[s.pots.h,s.pots.m], bag:s.bag.length};
})()`);
await sleep(250);
await cur.ev(`document.getElementById("talk").click(); true`);
await sleep(100);
const entered=await cur.ev(`(()=>{
  const btn=[...document.querySelectorAll("#dlgRow button")].find(b=>b.textContent.indexOf("蚀雾回廊")>=0);
  if(!btn) return {ok:false, texts:[...document.querySelectorAll("#dlgRow button")].map(b=>b.textContent)};
  btn.click();
  const card=document.querySelector("#dunBody [data-dun='normal']");
  const text=document.getElementById("dunBody").innerText;
  const h=card.getBoundingClientRect().height;
  card.click();
  return {ok:true, h, text:text.slice(0,180), on:window.__mengyu.DUN.on, wave:window.__mengyu.DUN.wave};
})()`);
check("dungeon opens from the guard dialog", entered.ok===true && entered.h>=44 && entered.on===true, JSON.stringify(entered));
const before=setup;
let fight=null, leaveH=0;
const t0=Date.now();
while(Date.now()-t0<50000){
  fight=await cur.ev(`(()=>{
    ${pulse}
    if((Date.now()/1000|0)%2===0){
      const sk=document.getElementById("sk1");
      sk.dispatchEvent(new PointerEvent("pointerdown",{bubbles:true,cancelable:true,pointerId:2,pointerType:"touch",clientX:20,clientY:20}));
    }
    const m=window.__mengyu;
    const foes=m.G.enemies.filter(e=>e.dun&&!e.dead).map(e=>({hp:e.hp,boss:!!e.dunBoss,z:+(e.z-m.G.p.z).toFixed(2)}));
    const leave=document.getElementById("dunLeave").getBoundingClientRect();
    return {on:m.DUN.on, phase:m.DUN.phase, wave:m.DUN.wave, dead:!!m.G.p.dead, php:Math.round(m.G.p.hp), foes, leave:leave.height, voucher:!!(m.G.save.voucher)};
  })()`);
  if(fight.leave>leaveH) leaveH=fight.leave;
  if(fight.voucher || fight.dead || fight.phase==="win") break;
  await sleep(180);
}
const secs=Math.round((Date.now()-t0)/100)/10;
check("normal clear is won by attacking", fight && fight.voucher===true && fight.dead===false && fight.phase==="win" && leaveH>=44, JSON.stringify({secs,leaveH,fight}));
const shown=await cur.ev(`(()=>{
  const m=window.__mengyu, v=m.G.save.voucher, body=document.getElementById("dunBody").innerText;
  const btn=document.getElementById("dunClaim");
  return {gold:v&&v.gold, xp:v&&v.xp, name:v&&v.item&&v.item.name, def:v&&v.item&&v.item.def, hp:v&&v.item&&v.item.hp, body:body.indexOf(String(v.gold))>=0, h:btn?btn.getBoundingClientRect().height:0};
})()`);
check("settlement matches the entry preview", shown.gold===before.preview.gold && shown.xp===before.preview.xp && shown.name==="回廊优秀护符" && shown.body===true && shown.h>=44, JSON.stringify({preview:before.preview, shown}));
const shot1=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc4-dun-port.png", Buffer.from(shot1.data,"base64"));
const claimed=await cur.ev(`(()=>{
  const m=window.__mengyu;
  const g0=m.G.save.gold, x0=m.G.save.xp, b0=m.G.save.bag.length, p0=m.G.save.pots.h;
  document.getElementById("dunClaim").click();
  document.getElementById("dunClaim") && document.getElementById("dunClaim").click();
  const s=m.G.save;
  return {g0, gold:s.gold, xp:s.xp, bag:s.bag.length, pots:s.pots.h, voucher:s.voucher, clears:s.dun&&s.dun.clears, item:s.bag[s.bag.length-1]&&s.bag[s.bag.length-1].name};
})()`);
check("one claim pays the preview and a second click does not", claimed.gold===before.gold+shown.gold && claimed.xp===before.xp+shown.xp && claimed.bag===before.bag+1 && claimed.pots===before.pots[0]+1 && claimed.voucher==null && claimed.clears===1, JSON.stringify({before:{gold:before.gold,xp:before.xp,bag:before.bag,pots:before.pots}, claimed, secs}));

const injected=await cur.ev(`(async()=>{
  const m=window.__mengyu, s=m.G.save;
  const wait=()=>new Promise(r=>setTimeout(r,450));
  const out={};
  const gA=s.gold;
  s.quests.q2={st:"ready",pr:6,round:0};
  m.claimQuest(m.QUESTS.find(q=>q.id==="q2"));
  const afterMain=s.gold;
  await wait();
  s.quests.q2.st="ready";
  const again=m.claimQuest(m.QUESTS.find(q=>q.id==="q2"));
  out.main={before:gA, after:afterMain, again, gold:s.gold, paid:s.paid.q2, pots:s.pots.h};
  const gB=s.gold, xB=s.xp;
  s.quests.q6={st:"ready",pr:10,round:0};
  s.paid.q6=0; s.qc.q6=0;
  await wait();
  m.claimQuest(m.QUESTS.find(q=>q.id==="q6"));
  const r1=s.gold;
  await wait();
  s.quests.q6.st="ready";
  m.claimQuest(m.QUESTS.find(q=>q.id==="q6"));
  const r2=s.gold;
  await wait();
  s.quests.q6.st="ready"; s.quests.q6.round=1;
  const r3=m.claimQuest(m.QUESTS.find(q=>q.id==="q6"));
  out.repeat={before:gB, r1, r2, r3, gold:s.gold, xp:s.xp-xB, round:s.quests.q6.round};
  return out;
})()`);
check("INJECT main quest pays once", injected.main.after===injected.main.before+50 && injected.main.again===false && injected.main.gold===injected.main.after && injected.main.paid===1, JSON.stringify(injected.main));
check("INJECT repeat quest pays one round at a time", injected.repeat.r1===injected.repeat.before+70 && injected.repeat.r2===injected.repeat.r1+70 && injected.repeat.r3===false && injected.repeat.gold===injected.repeat.r2, JSON.stringify(injected.repeat));

const bagFull=await cur.ev(`(async()=>{
  const m=window.__mengyu, s=m.G.save;
  const keep=s.bag.slice();
  s.bag=Array.from({length:30},(_,i)=>({uid:"fill"+i,slot:"acc",rar:0,lv:1,atk:0,def:0,hp:1,mp:0,crit:0,price:1,name:"填",plus:0,locked:false,enhSpent:0}));
  s.voucher={id:"inject-full",diff:"normal",gold:10,xp:5,pots:[0,0],item:{uid:"pend-charm",slot:"acc",rar:1,lv:8,atk:0,def:3,hp:10,mp:0,crit:0,price:20,name:"待领护符",plus:0,locked:false,enhSpent:0},taken:false};
  const g0=s.gold;
  const ok=m.claimVoucher();
  const parked=s.pending.some(p=>p.item&&p.item.uid==="pend-charm");
  const blocked=m.takePending("inject-full");
  s.bag.pop();
  await new Promise(r=>setTimeout(r,450));
  const took=m.takePending("inject-full");
  const back={ok, gold:s.gold-g0, bag:s.bag.length, parked, blocked, took, name:s.bag[s.bag.length-1]&&s.bag[s.bag.length-1].name};
  s.bag=keep;
  return back;
})()`);
check("INJECT full bag keeps the charm pending", bagFull.ok===true && bagFull.gold===10 && bagFull.parked===true && bagFull.blocked===false && bagFull.took===true && bagFull.name==="待领护符", JSON.stringify(bagFull));

const quota=await cur.ev(`(()=>{
  const m=window.__mengyu, s=m.G.save;
  s.voucher={id:"inject-quota",diff:"normal",gold:15,xp:5,pots:[0,0],item:null,taken:false};
  const g0=s.gold;
  const old=localStorage.setItem.bind(localStorage);
  localStorage.setItem=function(k,v){ if(String(k).indexOf("MengYu")===0 && String(k).indexOf(".bak")<0){ const e=new Error("quota"); e.name="QuotaExceededError"; throw e; } return old(k,v); };
  const ok=m.claimVoucher();
  localStorage.setItem=old;
  return {ok, gold:s.gold-g0, still:!!s.voucher};
})()`);
check("INJECT quota failure does not spend the voucher", quota.ok===false && quota.gold===0 && quota.still===true, JSON.stringify(quota));

const rules=await cur.ev(`(async()=>{
  const m=window.__mengyu, s=m.G.save;
  s.voucher=null;
  const g0=s.gold, clears=s.dun.clears;
  m.DUN.token="inject-leave";
  s.run={id:"corridor",diff:"normal",token:"inject-leave"};
  m.DUN.on=true; m.DUN.diff="normal"; m.DUN.phase="fight"; m.DUN.wave=1; m.DUN.t=40;
  document.getElementById("dunHud").classList.add("on");
  document.getElementById("dunLeave").click();
  const asked=document.getElementById("confirm").classList.contains("on");
  document.getElementById("cfNo").click();
  const still=m.DUN.on===true && s.gold===g0;
  document.getElementById("dunLeave").click();
  document.getElementById("cfYes").click();
  const left=!m.DUN.on && s.gold===g0 && !s.voucher;
  m.startDungeon("normal");
  m.DUN.t=0.02;
  await new Promise(r=>setTimeout(r,400));
  const failed=!m.DUN.on && !s.voucher && s.gold===g0 && s.dun.clears===clears;
  const old={ver:4,classId:"swordsman",level:12,gold:777,xp:1,storyDone:true,hasSave:true,foo:"keep-dungeon",extra:{keep:1},quests:{q5:{st:"done",pr:1}},qc:{q5:1},growth:{skills:{},talents:{}}};
  const mig=m.migrateSave(JSON.parse(JSON.stringify(old)));
  const disk=m.loadSave();
  mig.rev=disk?disk.rev:0;
  const prev=m.G.save;
  m.G.save=mig;
  mig.quests.q5.st="ready";
  const paidAgain=m.claimQuest(m.QUESTS.find(q=>q.id==="q5"));
  const migGold2=mig.gold;
  m.G.save=prev;
  const badDiff=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:5,save:{ver:5,classId:"swordsman",level:8,hasSave:true,voucher:{id:"x",diff:"mythic",gold:10,xp:1}}}));
  const badPay=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:5,save:{ver:5,classId:"swordsman",level:8,hasSave:true,paid:{q1:-1}}}));
  const badPend=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:5,save:{ver:5,classId:"swordsman",level:8,hasSave:true,pending:[{id:"p",from:"q",item:{uid:"z",slot:"weapon",rar:0,lv:1,plus:9,locked:false}}]}}));
  const newer=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:9,save:{ver:9}}));
  const keep=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:5,save:{ver:5,classId:"mage",level:4,gold:20,xp:0,hasSave:true,storyDone:false,foo:"still",quests:{q1:{st:"avail",pr:0}}}}));
  return {asked, still, left, failed, migGold:mig.gold, migPaid:mig.paid.q5, migFoo:mig.foo, migExtra:mig.extra&&mig.extra.keep, migVer:mig.ver, paidAgain, migGold2, badDiff:badDiff.ok, badPay:badPay.ok, badPend:badPend.ok, newer:!!newer.newer, keep:keep.ok&&keep.save.foo==="still"};
})()`);
check("INJECT cancel leave then exit pays nothing", rules.asked===true && rules.still===true && rules.left===true, JSON.stringify(rules));
check("INJECT timeout and re-entry do not pay", rules.failed===true, JSON.stringify({failed:rules.failed}));
check("INJECT old clear migrates without paying again", rules.migGold===777 && rules.migPaid===1 && rules.migFoo==="keep-dungeon" && rules.migExtra===1 && rules.migVer===5 && rules.paidAgain===false && rules.migGold2===777, JSON.stringify(rules));
check("INJECT import rejects bad claims and keeps unknown fields", rules.badDiff===false && rules.badPay===false && rules.badPend===false && rules.newer===true && rules.keep===true, JSON.stringify(rules));

await cur.ev(`document.getElementById("qLogX").click(); window.__mengyu.openQuestLog(); true`);
await sleep(200);
const qshot=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc4-quest-port.png", Buffer.from(qshot.data,"base64"));
const qfit=await cur.ev(`(()=>{
  const sheet=document.querySelector("#qLog .sheet");
  sheet.scrollTop=sheet.scrollHeight;
  const btns=[...document.querySelectorAll("#qLog button")].filter(b=>b.getBoundingClientRect().height>0);
  const small=btns.filter(b=>{ const r=b.getBoundingClientRect(); return r.height<44||r.width<44; }).map(b=>b.textContent.slice(0,8));
  const x=document.getElementById("qLogX").getBoundingClientRect();
  return {small, xh:x.height, bottom:sheet.scrollHeight>sheet.clientHeight};
})()`);
check("quest panel buttons stay at least 44px", qfit.small.length===0 && qfit.xh>=44, JSON.stringify(qfit));
await cdp.send("Emulation.setDeviceMetricsOverride",{width:740,height:360,deviceScaleFactor:1,mobile:true},cur.sessionId);
await cur.ev(`document.documentElement.style.setProperty("--st","18px");document.documentElement.style.setProperty("--sb","12px");document.documentElement.style.setProperty("--sl","28px");document.documentElement.style.setProperty("--sr","16px");window.dispatchEvent(new Event("resize"));true`);
await sleep(250);
const land=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc4-quest-land.png", Buffer.from(land.data,"base64"));
const landFit=await cur.ev(`(()=>{
  const sheet=document.querySelector("#qLog .sheet").getBoundingClientRect();
  const x=document.getElementById("qLogX").getBoundingClientRect();
  const atk=document.getElementById("atk").getBoundingClientRect();
  return {top:sheet.top, xh:x.height, atk:atk.height, vh:innerHeight};
})()`);
check("landscape quest close and attack stay tappable", landFit.top>=16 && landFit.xh>=44 && landFit.atk>=44, JSON.stringify(landFit));

const others=await cur.ev(`(async()=>{
  const m=window.__mengyu;
  m.closeQuestLog();
  const rows=[];
  for(const id of ["mage","archer"]){
    m.startAdventure(id, true);
    m.finishCoach();
    m.G.save.level=8; m.recalcStats(); m.G.p.hp=m.ST.hp; m.G.p.mp=m.ST.mp; m.G.p.dead=false; m.G.paused=false;
    m.G.p.x=2.2; m.G.p.z=12; m.G.near={id:"guard", name:"守卫 石川"};
    m.tryTalk();
    const btn=[...document.querySelectorAll("#dlgRow button")].find(b=>b.textContent.indexOf("蚀雾回廊")>=0);
    if(btn) btn.click();
    const opened=document.getElementById("dunM").classList.contains("on");
    const g0=m.G.save.gold;
    const go=document.querySelector("#dunBody [data-dun='normal']");
    if(go) go.click();
    const started=m.DUN.on===true;
    document.getElementById("dunLeave").click();
    document.getElementById("cfYes").click();
    const left=!m.DUN.on && m.G.save.gold===g0;
    m.closeDlg();
    const e=m.spawnEnemy("slime", m.G.p.x, m.G.p.z+1.3, 1, "slope");
    const hp0=e.hp;
    m.tryAttack();
    await new Promise(r=>setTimeout(r,500));
    m.tryDodge();
    rows.push({id, opened, started, left, hp0, hp1:e.hp, dodge:m.G.p.dodgeT>0});
    m.clearEnemies();
  }
  return rows;
})()`);
check("INJECT mage and archer can enter and leave", others.every(r=>r.opened&&r.started&&r.left), JSON.stringify(others));
check("mage and archer attack or dodge still responds", others.every(r=>r.hp1<r.hp0 || r.dodge), JSON.stringify(others));

const raw=await cur.ev(`(()=>{ const m=window.__mengyu; m.G.save.foo="keep-dungeon"; m.G.save.extra={keep:1}; m.persist(m.G.save,{replace:true}); return localStorage.getItem(m.SAVE_KEY); })()`);
const old=await attach(base+"rc1-client.html?shot=play");
await sleep(700);
const rc1=await old.ev(`(()=>{
  const m=window.__mengyu;
  const before=localStorage.getItem("MengYuLegend_Save_v1");
  const wrote=m.persist({ver:2,classId:"swordsman",level:1,xp:0,gold:1,hasSave:true,quests:{q1:{st:"avail",pr:0}}});
  const after=localStorage.getItem("MengYuLegend_Save_v1");
  m.showTitle();
  document.getElementById("btnNew").click();
  document.getElementById("btnGo").click();
  document.getElementById("cfYes").click();
  const archived=localStorage.getItem("MengYuLegend_Save_v1.newer")||"";
  return {wrote, same:before===after, foo:archived.indexOf("keep-dungeon")>=0, extra:archived.indexOf('"keep":1')>=0, ver:archived.indexOf('"ver":5')>=0};
})()`);
check("RC-1 confirm still archives the newer save", rc1.wrote===false && rc1.same===true && rc1.foo===true && rc1.extra===true && rc1.ver===true, JSON.stringify(rc1));
const restored=await cur.ev(`(()=>{
  const m=window.__mengyu;
  m.showTitle();
  document.getElementById("btnRestore").click();
  document.getElementById("cfYes").click();
  const s=m.loadSave();
  return {foo:s&&s.foo, extra:s&&s.extra&&s.extra.keep, ver:s&&s.ver, gold:s&&s.gold};
})()`);
check("restore keeps unknown fields", restored.foo==="keep-dungeon" && restored.extra===1 && restored.ver===5, JSON.stringify(restored));
const err=logs.filter(l=>!/deprecated with r150/.test(l));
check("no console errors", err.length===0, err.join(" | ")||"none");
console.log("PLAY", JSON.stringify({secs, preview:before.preview, atk:before.atk, claimed, fight:{wave:fight&&fight.wave, php:fight&&fight.php}}, null, 2));
console.log("INJECT", JSON.stringify({injected, bagFull, quota, rules:{migGold:rules.migGold}, others}, null, 2));
}finally{
  try{ unlinkSync(rc1Path); }catch(e){}
  try{ ws.close(); chrome.kill(); }catch(e){}
}
console.log("FAILS", fail.length?fail.join(", "):"none");
setTimeout(()=>process.exit(fail.length?1:0),200);
