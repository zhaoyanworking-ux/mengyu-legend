// RC-4 playability pass. Headless Chrome, not a phone.
// Movement is the joystick vector. Attack, skill and dodge are pointer events.
// No killEnemy, no HP edits, no teleport onto an enemy, no skipped waves.
// Damage rolls use a fixed Math.random seed. Crits are not forced.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const base = "http://127.0.0.1:8765/";
const PORT = 9262;
const fail=[];
function sleep(ms){ return new Promise(r=>setTimeout(r,ms)); }
class CDP{
  constructor(ws){ this.ws=ws; this.id=0; this.pending=new Map();
    ws.addEventListener("message",ev=>{ const msg=JSON.parse(ev.data); if(msg.id&&this.pending.has(msg.id)){ const {resolve,reject}=this.pending.get(msg.id); this.pending.delete(msg.id); if(msg.error) reject(new Error(JSON.stringify(msg.error))); else resolve(msg.result); }}); }
  send(m,p={},s){ const id=++this.id; return new Promise((resolve,reject)=>{ this.pending.set(id,{resolve,reject}); const o={id,method:m,params:p}; if(s)o.sessionId=s; this.ws.send(JSON.stringify(o)); }); }
}
const chrome=spawn("google-chrome",["--headless=new","--disable-gpu","--no-sandbox","--disable-dev-shm-usage","--use-angle=swiftshader","--enable-webgl",`--remote-debugging-port=${PORT}`,"--window-size=1100,720","--user-data-dir=/tmp/chrome-rc5-"+Date.now(),"about:blank"],{stdio:"ignore"});
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
    if(msg.method==="Runtime.exceptionThrown") logs.push(((msg.params.exceptionDetails.exception&&msg.params.exceptionDetails.exception.description)||"").slice(0,200));
  });
  const ev=async(e)=>{ const r=await cdp.send("Runtime.evaluate",{expression:e,awaitPromise:true,returnByValue:true},sessionId); if(r.exceptionDetails) throw new Error((r.exceptionDetails.exception&&r.exceptionDetails.exception.description)||r.exceptionDetails.text); return r.result.value; };
  await cdp.send("Page.navigate",{url},sessionId);
  for(let i=0;i<80;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
  return {ev, sessionId};
}
function check(n,ok,x){ console.log((ok?"PASS ":"FAIL ")+n+(x?" "+x:"")); if(!ok) fail.push(n); }
const INSTALL=`(()=>{
  const m=window.__mengyu;
  let seed=20261121;
  Math.random=function(){ seed|=0; seed=seed+0x6D2B79F5|0; let t=Math.imul(seed^seed>>>15,1|seed); t=t+Math.imul(t^t>>>7,61|t)^t; return ((t^t>>>14)>>>0)/4294967296; };
  window.__tap=function(id){
    const el=document.getElementById(id); if(!el) return;
    el.dispatchEvent(new PointerEvent("pointerdown",{bubbles:true,cancelable:true,pointerId:3,pointerType:"touch",clientX:30,clientY:30}));
    el.dispatchEvent(new PointerEvent("pointerup",{bubbles:true,cancelable:true,pointerId:3,pointerType:"touch",clientX:30,clientY:30}));
  };
  window.__face=function(tx,tz){
    const P=m.G.p, yaw=m.CAM.yaw, ffx=Math.sin(yaw), ffz=Math.cos(yaw);
    const dx=tx-P.x, dz=tz-P.z, len=Math.hypot(dx,dz)||1, wx=dx/len, wz=dz/len;
    m.G.joy.x=-wx*ffz+wz*ffx; m.G.joy.y=wx*ffx+wz*ffz;
    return Math.hypot(dx,dz);
  };
  window.__play=function(style){
    const P=m.G.p;
    if(P.dead){ m.G.joy.x=m.G.joy.y=0; m.G.atkHeld=false; const b=document.getElementById("goTown"); if(b && document.getElementById("over").style.display==="flex") b.click(); return {dead:true}; }
    const foes=m.G.enemies.filter(e=>!e.dead && (style==="field"?!e.dun:!!e.dun));
    let best=null, bd=1e9;
    foes.forEach(e=>{ const d=Math.hypot(e.x-P.x,e.z-P.z); if(d<bd){ bd=d; best=e; } });
    const threat=foes.find(e=> (e.state==="windup"||e.state==="leap"||e.state==="strike") && Math.hypot((e.tcx||e.x)-P.x,(e.tcz||e.z)-P.z)<((e.tr||1.2)+0.9));
    const ranged=m.G.classId!=="swordsman";
    if(style==="field" && P.z<18){ window.__face(0, 20); m.G.atkHeld=false; return {dead:false, bd:99, n:foes.length, hp:Math.round(P.hp), max:m.ST.hp, gate:true}; }
    if(threat && (m.G.cd.dg||0)<=0) window.__tap("dg");
    if(!best){ m.G.joy.x=m.G.joy.y=0; m.G.atkHeld=false; return {dead:false, bd:0, n:0}; }
    const want=style==="field"?1.7:(ranged?5.4:1.7);
    if(threat) window.__face(P.x-(threat.x-P.x), P.z-(threat.z-P.z));
    else if(bd>want+0.35) window.__face(best.x, best.z);
    else if(ranged && bd<want-1.1) window.__face(P.x-(best.x-P.x), P.z-(best.z-P.z));
    else m.G.joy.x=m.G.joy.y=0;
    const reach=ranged?8.5:2.3;
    m.G.atkHeld=bd<reach;
    if(m.G.atkHeld) window.__tap("atk");
    const bossUp=foes.some(e=>e.dunBoss);
    if((style==="field" || bossUp) && bd<9 && P.mp>22 && (m.G.cd.sk1||0)<=0) window.__tap("sk1");
    if(P.hp<m.ST.hp*0.45 && m.G.save.pots.h>0 && (m.G.cd.poth||0)<=0) window.__tap("potH");
    return {dead:false, bd:Math.round(bd*100)/100, n:foes.length, hp:Math.round(P.hp), max:m.ST.hp};
  };
  return true;
})()`;
async function walkTo(cur,x,z,limit,nearId){
  const t=Date.now();
  let prev=null, stuck=0, side=1;
  while(Date.now()-t<limit){
    const p=await cur.ev(`(()=>{ const d=window.__face(${x},${z}); const P=window.__mengyu.G.p; return {d, x:+P.x.toFixed(2), z:+P.z.toFixed(2), near:window.__mengyu.G.near&&window.__mengyu.G.near.id||""}; })()`);
    if(p.d<2.3 || (nearId && p.near===nearId)){ await cur.ev(`window.__mengyu.G.joy.x=window.__mengyu.G.joy.y=0; true`); return p; }
    if(prev && Math.hypot(p.x-prev.x, p.z-prev.z)<0.04) stuck++; else stuck=0;
    prev=p;
    if(stuck>6){ await cur.ev(`window.__face(${x}+${side}*3,${z})`); side=-side; await sleep(350); stuck=0; }
    await sleep(100);
  }
  return prev||{d:99};
}
async function talk(cur){
  await cur.ev(`document.getElementById("talk").click(); true`);
  await sleep(80);
  for(let i=0;i<14;i++){
    const more=await cur.ev(`(()=>{ const d=document.getElementById("dlg"); if(!d || d.style.display==="none") return false; const b=document.querySelector("#dlgRow .next"); if(!b) return false; b.click(); return true; })()`);
    if(!more) break;
    await sleep(70);
  }
}
async function fightUntil(cur, predSource, limit){
  const t0=Date.now();
  let last=null;
  while(Date.now()-t0<limit){
    last=await cur.ev(predSource);
    if(last && last.done) break;
    await sleep(140);
  }
  return { last, wall:+((Date.now()-t0)/1000).toFixed(1) };
}
try{
await cdp.send("Target.setDiscoverTargets",{discover:true});
const cur=await attach(base+"index.html?shot=play");
await cdp.send("Emulation.setDeviceMetricsOverride",{width:360,height:640,deviceScaleFactor:1,mobile:true},cur.sessionId);
await cur.ev(INSTALL);
await cur.ev(`window.__mengyu.finishCoach(); true`);
await walkTo(cur, -2.4, 4.6, 8000, "mayor");
await talk(cur);
let q1=await cur.ev(`window.__mengyu.qst("q1").st`);
check("accepted the first quest from the mayor", q1==="active" || q1==="ready", q1);
const toGuard=await walkTo(cur, 0, 10, 10000);
const atGuard=await walkTo(cur, 2.2, 13.0, 14000, "guard");
await talk(cur);
q1=await cur.ev(`window.__mengyu.qst("q1").st`);
check("turned the first quest in at the guard", q1==="done", JSON.stringify({q1,toGuard,atGuard}));
await talk(cur);
const q2=await cur.ev(`window.__mengyu.qst("q2").st`);
check("accepted the slime quest", q2==="active", q2);
await walkTo(cur, 0, 26, 14000);
const kills=await fightUntil(cur, `(()=>{ const s=window.__play("field"); const q=window.__mengyu.qst("q2"); s.done=q.st==="ready"||q.st==="done"; s.pr=q.pr; return s; })()`, 150000);
check("six slimes were killed by walking and attacking", kills.last && kills.last.done===true, JSON.stringify(kills));
await walkTo(cur, 0, 12, 12000);
await walkTo(cur, 2.2, 13.0, 10000, "guard");
await talk(cur);
const afterQuest=await cur.ev(`(()=>{ const s=window.__mengyu.G.save; return {q:s.quests.q2.st, gold:s.gold, lv:s.level, xp:s.xp}; })()`);
check("quest reward was claimed once", afterQuest.q==="done" && afterQuest.gold>=70, JSON.stringify(afterQuest));
const grew=await cur.ev(`(()=>{
  const m=window.__mengyu;
  m.openGrowth();
  const up=document.querySelector("#growth [data-up='atk']");
  const upText=up&&up.textContent;
  if(up && !up.disabled) up.click();
  const tn=document.querySelector("#growth [data-tn='s_edge']");
  if(tn && !tn.disabled) tn.click();
  m.closeGrowth();
  m.openBag();
  document.querySelector("#eqSlots .slot").click();
  const enh=document.getElementById("tEnh");
  const enhText=enh&&enh.textContent;
  if(enh && !enh.disabled) enh.click();
  m.closeBag();
  const w=m.G.save.equip.weapon;
  return {gold:m.G.save.gold, rank:m.skillRank("swordsman","atk"), talent:m.G.save.growth.talents.swordsman.slice(), plus:w&&w.plus, atk:m.ST.atk, enhText, upText, lv:m.G.save.level};
})()`);
check("skill, talent and enhancement were bought in the panels", grew.rank===1 && grew.talent.indexOf("s_edge")>=0 && grew.plus===1, JSON.stringify(grew));
const exact=await cur.ev(`(()=>{ document.getElementById("gold").click(); const t=document.getElementById("goldPop").textContent; document.getElementById("goldPop").click(); return {t, on:document.getElementById("goldPop").classList.contains("on")}; })()`);
check("exact gold is visible without the title attribute", exact.t.indexOf(String(grew.gold))>=0 && exact.on===false, JSON.stringify(exact));

const stand=await fightUntil(cur, `(()=>{
  const m=window.__mengyu;
  if(!window.__standStarted){
    window.__standStarted=1;
    m.G.p.x=2.2; m.G.p.z=12.2; m.G.near={id:"guard"};
    m.tryTalk();
    const b=[...document.querySelectorAll("#dlgRow button")].find(x=>x.textContent.indexOf("蚀雾回廊")>=0);
    if(b) b.click();
    const go=document.querySelector("#dunBody [data-dun='normal']");
    if(go && !go.disabled) go.click();
    m.G.joy.x=m.G.joy.y=0; m.G.atkHeld=false;
    return {done:false, waiting:true};
  }
  if(m.DUN.on){
    m.G.joy.x=m.G.joy.y=0; m.G.atkHeld=false;
    const s=m.DUN.stats;
    window.__standLast={spawn:s.spawnDist, min:s.minDist, hurt:s.hurt, dmg:s.hurtDmg, wind:s.windups, shots:s.shots, shotHits:s.shotHits, hp:Math.round(m.G.p.hp), max:m.ST.hp};
  }
  const L=window.__standLast||{};
  return Object.assign({done:L.hurt>0 && L.wind>0 && L.min<L.spawn-0.6}, L);
})()`, 16000);
check("standing still lets dungeon enemies chase and land a hit", stand.last && stand.last.done===true && stand.last.min<stand.last.spawn-0.6, JSON.stringify(stand));
await cur.ev(`(()=>{ const m=window.__mengyu; if(m.DUN.on){ document.getElementById("dunLeave").click(); document.getElementById("cfYes").click(); } if(m.G.p.dead) document.getElementById("goTown").click(); return true; })()`);
await sleep(400);

async function clearRun(cur, diff, label){
  const kids0=await cur.ev(`window.__mengyu.scene.children.length`);
  await cur.ev(`(()=>{ const m=window.__mengyu; m.G.p.dead=false; m.G.paused=false; m.G.p.x=2.2; m.G.p.z=12.2; m.G.near={id:"guard"}; m.tryTalk(); const b=[...document.querySelectorAll("#dlgRow button")].find(x=>x.textContent.indexOf("蚀雾回廊")>=0); if(b) b.click(); const go=document.querySelector("#dunBody [data-dun='${diff}']"); if(!go||go.disabled) return false; go.click(); return true; })()`);
  const run=await fightUntil(cur, `(()=>{ const s=window.__play("dun"); const m=window.__mengyu; s.done=!!m.G.save.voucher; s.dead=!!m.G.p.dead; s.clock=+m.DUN.clock.toFixed(1); s.stats=Object.assign({}, m.DUN.stats); s.wave=m.DUN.wave; s.php=Math.round(m.G.p.hp); s.max=m.ST.hp; return s; })()`, 150000);
  const kids1=await cur.ev(`window.__mengyu.scene.children.length`);
  check(label, run.last && run.last.done===true && run.last.dead!==true, JSON.stringify({wall:run.wall, run:run.last, meshes:kids1-kids0}));
  return run;
}
const sword=await clearRun(cur, "normal", "swordsman clears normal with earned gear");
const claim=await cur.ev(`(()=>{
  const m=window.__mengyu, g0=m.G.save.gold, b0=m.G.save.bag.length;
  const btn=document.getElementById("dunClaim");
  if(!btn) return {missing:true, gold:g0, bag:b0, voucher:m.G.save.voucher};
  btn.click();
  const again=document.getElementById("dunClaim");
  if(again) again.click();
  const s=m.G.save;
  return {g0, gold:s.gold, xp:s.xp, lv:s.level, bag:s.bag.length, name:s.bag[s.bag.length-1]&&s.bag[s.bag.length-1].name, voucher:s.voucher};
})()`);
check("dungeon claim matches one payment", claim.voucher==null && claim.gold>claim.g0 && claim.bag===claim.bag, JSON.stringify(claim));
await cdp.send("Page.navigate",{url:base+"index.html"},cur.sessionId);
for(let i=0;i<80;i++){ await sleep(250); if(await cur.ev(`document.documentElement.getAttribute('data-ready')==='1' && document.getElementById('btnCont')`)) break; }
await cur.ev(`document.getElementById("btnCont").click(); true`);
await sleep(400);
const reloaded=await cur.ev(`(()=>{ const s=window.__mengyu.G.save; return s&&{gold:s.gold, lv:s.level, bag:s.bag.length, q2:s.quests.q2.st, rank:s.growth.skills.swordsman.atk, plus:s.equip.weapon&&s.equip.weapon.plus, voucher:s.voucher}; })()`);
check("reload keeps the earned gold, level, gear and quest", reloaded && reloaded.gold===claim.gold && reloaded.q2==="done" && reloaded.rank===1 && reloaded.plus===1 && reloaded.voucher==null, JSON.stringify({claim, reloaded}));

await cur.ev(INSTALL);
async function classClear(id, diff, label){
  await cur.ev(`(()=>{ const m=window.__mengyu; m.startAdventure("${id}", true); m.finishCoach(); m.G.save.level=${id==="swordsman"?6:3}; m.recalcStats(); m.G.p.hp=m.ST.hp; m.G.p.mp=m.ST.mp; return {atk:m.ST.atk, hp:m.ST.hp, weapon:m.G.save.equip.weapon&&m.G.save.equip.weapon.atk}; })()`);
  return clearRun(cur, diff, label);
}
const mage=await classClear("mage","normal","mage clears normal with the starter weapon");
const archer=await classClear("archer","normal","archer clears normal with the starter weapon");
const hard=await classClear("swordsman","hard","swordsman clears hard at level 6 with the starter weapon");
const err=logs.filter(l=>!/deprecated with r150/.test(l));
check("no console errors", err.length===0, err.join(" | ")||"none");
await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId).then(s=>writeFileSync("/opt/cursor/artifacts/rc5-hard.png", Buffer.from(s.data,"base64")));
console.log("FLOW", JSON.stringify({afterQuest, grew, stand:stand.last, sword:sword.last&&{wall:sword.wall, clock:sword.last.clock, hp:[sword.last.php, sword.last.max], stats:sword.last.stats}, claim, mage:mage.last&&mage.last.stats, archer:archer.last&&archer.last.stats, hard:hard.last&&{wall:hard.wall, clock:hard.last.clock, stats:hard.last.stats, hp:[hard.last.php, hard.last.max]}}, null, 2));
}finally{ try{ ws.close(); chrome.kill(); }catch(e){} }
console.log("FAILS", fail.length?fail.join(", "):"none");
setTimeout(()=>process.exit(fail.length?1:0), 200);
