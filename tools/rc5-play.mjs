// Continuous play and separate dungeon clears. Headless Chrome, not a phone.
// Fresh flow uses the title, the coach pointers, the joystick vector and the real buttons.
// It does not teleport and does not write quests, level or gold.
// The later class clears start from a labeled save with a level and the starter weapon.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const base = "http://127.0.0.1:8765/";
const PORT = 9272;
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
  await cdp.send("Network.enable",{},sessionId);
  cdp.ws.addEventListener("message",ev=>{
    const msg=JSON.parse(ev.data);
    if(msg.sessionId!==sessionId) return;
    if(msg.method==="Runtime.exceptionThrown") logs.push(((msg.params.exceptionDetails.exception&&msg.params.exceptionDetails.exception.description)||"").slice(0,240));
  });
  const ev=async(e)=>{ const r=await cdp.send("Runtime.evaluate",{expression:e,awaitPromise:true,returnByValue:true},sessionId); if(r.exceptionDetails) throw new Error((r.exceptionDetails.exception&&r.exceptionDetails.exception.description)||r.exceptionDetails.text); return r.result.value; };
  await cdp.send("Page.navigate",{url},sessionId);
  for(let i=0;i<90;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
  return {ev, sessionId};
}
function check(n,ok,x){ console.log((ok?"PASS ":"FAIL ")+n+(x?" "+x:"")); if(!ok) fail.push(n); }
async function shot(cur, name){
  const s=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
  writeFileSync("/opt/cursor/artifacts/"+name, Buffer.from(s.data,"base64"));
}
const INSTALL=`(()=>{
  const m=window.__mengyu;
  let seed=20261121;
  Math.random=function(){ seed|=0; seed=seed+0x6D2B79F5|0; let t=Math.imul(seed^seed>>>15,1|seed); t=t+Math.imul(t^t>>>7,61|t)^t; return ((t^t>>>14)>>>0)/4294967296; };
  window.__tap=function(id){
    const el=document.getElementById(id); if(!el) return false;
    const r=el.getBoundingClientRect();
    const x=r.left+r.width/2, y=r.top+Math.min(r.height/2, 20);
    el.dispatchEvent(new PointerEvent("pointerdown",{bubbles:true,cancelable:true,pointerId:3,pointerType:"touch",clientX:x,clientY:y}));
    el.dispatchEvent(new PointerEvent("pointerup",{bubbles:true,cancelable:true,pointerId:3,pointerType:"touch",clientX:x,clientY:y}));
    return true;
  };
  window.__face=function(tx,tz){
    const P=m.G.p, yaw=m.CAM.yaw, ffx=Math.sin(yaw), ffz=Math.cos(yaw);
    const dx=tx-P.x, dz=tz-P.z, len=Math.hypot(dx,dz)||1, wx=dx/len, wz=dz/len;
    m.G.joy.x=-wx*ffz+wz*ffx; m.G.joy.y=wx*ffx+wz*ffz;
    return Math.hypot(dx,dz);
  };
  window.__goto=function(tx,tz){
    const P=m.G.p, g=m.TOWN.grid;
    const dist=Math.hypot(tx-P.x, tz-P.z);
    if(dist<1.15){ m.G.joy.x=m.G.joy.y=0; return {ok:true,d:+dist.toFixed(2),x:+P.x.toFixed(2),z:+P.z.toFixed(2),near:m.G.near&&m.G.near.id||""}; }
    if(!g || P.z>=16.4 || tz>=17){
      const step=1.2;
      let aim=null, best=1e9;
      for(let a=0;a<12;a++){
        const ang=a/12*Math.PI*2;
        const nx=P.x+Math.cos(ang)*step, nz=P.z+Math.sin(ang)*step;
        if(!m.inTownBounds(nx,nz)) continue;
        const nd=Math.hypot(tx-nx, tz-nz);
        if(nd<best){ best=nd; aim={x:nx,z:nz}; }
      }
      if(!aim){ m.G.joy.x=m.G.joy.y=0; return {ok:false,blocked:true,d:+dist.toFixed(2),x:+P.x.toFixed(2),z:+P.z.toFixed(2),near:""}; }
      window.__face(aim.x, aim.z);
      return {ok:false,d:+dist.toFixed(2),x:+P.x.toFixed(2),z:+P.z.toFixed(2),near:m.G.near&&m.G.near.id||""};
    }
    const sx=Math.floor((P.x-g.ox)/g.cell), sz=Math.floor((P.z-g.oz)/g.cell);
    const key=(x,z)=>z*g.nx+x, start=key(sx,sz);
    const prev=new Int32Array(g.nx*g.nz); prev.fill(-1);
    const q=[sx,sz]; prev[start]=start;
    let found=-1;
    for(let qi=0; qi<q.length && found<0;){
      const cx=q[qi++], cz=q[qi++];
      const wx=g.ox+(cx+0.5)*g.cell, wz=g.oz+(cz+0.5)*g.cell;
      if(!(cx===sx&&cz===sz) && Math.hypot(wx-tx,wz-tz)<1.2){ found=key(cx,cz); break; }
      for(const d of [[1,0],[-1,0],[0,1],[0,-1]]){
        const jx=cx+d[0], jz=cz+d[1];
        if(jx<0||jz<0||jx>=g.nx||jz>=g.nz) continue;
        const k=key(jx,jz); if(prev[k]!==-1 || !g.walk[k]) continue;
        const x=g.ox+(jx+0.5)*g.cell, z=g.oz+(jz+0.5)*g.cell;
        if(!m.inTownBounds(x,z)) continue;
        prev[k]=key(cx,cz); q.push(jx,jz);
      }
    }
    if(found<0){ m.G.joy.x=m.G.joy.y=0; return {ok:false,blocked:true,d:+dist.toFixed(2),x:+P.x.toFixed(2),z:+P.z.toFixed(2),near:m.G.near&&m.G.near.id||""}; }
    let k=found;
    while(prev[k]!==start && prev[k]!==k) k=prev[k];
    const nx=k%g.nx, nz=(k-nx)/g.nx;
    window.__face(g.ox+(nx+0.5)*g.cell, g.oz+(nz+0.5)*g.cell);
    return {ok:false,d:+dist.toFixed(2),x:+P.x.toFixed(2),z:+P.z.toFixed(2),near:m.G.near&&m.G.near.id||""};
  };
  window.__gotoNpc=function(id){
    const n=m.NPCS[id], P=m.G.p;
    if(m.G.near && m.G.near.id===id){ m.G.joy.x=m.G.joy.y=0; return {ok:true,near:id,x:+P.x.toFixed(2),z:+P.z.toFixed(2),d:0}; }
    let gx=n.x, gz=n.z, bd=1e9, any=false;
    for(let a=0;a<16;a++){
      const x=n.x+Math.cos(a*Math.PI/8)*1.15, z=n.z+Math.sin(a*Math.PI/8)*1.15;
      if(!m.inTownBounds(x,z)) continue;
      any=true; const d=Math.hypot(x-P.x,z-P.z); if(d<bd){ bd=d; gx=x; gz=z; }
    }
    const s=window.__goto(any?gx:n.x, any?gz:n.z);
    s.near=m.G.near&&m.G.near.id||"";
    s.ok=s.near===id;
    return s;
  };
  window.__play=function(style){
    const P=m.G.p;
    if(P.dead){ m.G.joy.x=m.G.joy.y=0; m.G.atkHeld=false; const b=document.getElementById("goTown"); if(b && document.getElementById("over").style.display==="flex") b.click(); return {dead:true, bd:0, n:0, hp:0}; }
    const foes=m.G.enemies.filter(e=>!e.dead && (style==="field"?(!e.dun && (e.type==="slime"||e.type==="spike")):!!e.dun));
    let best=null, bd=1e9;
    foes.forEach(e=>{ const d=Math.hypot(e.x-P.x,e.z-P.z); if(d<bd){ bd=d; best=e; } });
    if(!best){
      if(style==="field") window.__goto(0, P.z<24?28:34);
      else m.G.joy.x=m.G.joy.y=0;
      m.G.atkHeld=false;
      return {dead:false, bd:0, n:0, hp:Math.round(P.hp), max:m.ST.hp};
    }
    const melee=m.G.classId==="swordsman";
    const want=style==="field"?1.7:(melee?1.65:(best.ranged?5.4:4.6));
    const threat=foes.find(e=>!e.ranged && (e.state==="windup"||e.state==="leap"||e.state==="strike") && Math.hypot((e.tcx||e.x)-P.x,(e.tcz||e.z)-P.z)<((e.tr||1.5)+1.3));
    if(bd>want+0.45) window.__face(best.x, best.z);
    else if(!melee && style!=="field" && bd<want-1.3) window.__face(P.x-(best.x-P.x), P.z-(best.z-P.z));
    else m.G.joy.x=m.G.joy.y=0;
    let skill=false, pot=false;
    if(threat && (m.G.cd.dg||0)<=0) window.__tap("dg");
    const reach=melee||style==="field"?2.3:11;
    m.G.atkHeld=bd<reach && !threat;
    const bossUp=foes.some(e=>e.dunBoss);
    if((style==="field" || bossUp) && bd<8 && P.mp>16 && (m.G.cd.sk1||0)<=0 && !threat){ window.__tap("sk1"); skill=true; }
    if(P.hp<m.ST.hp*0.55 && m.G.save.pots.h>0 && (m.G.cd.poth||0)<=0){ window.__tap("potH"); pot=true; }
    return {dead:false, bd:+bd.toFixed(2), n:foes.length, hp:Math.round(P.hp), max:m.ST.hp, skill, pot};
  };
  window.__simClear=function(maxSec){
    let skills=0, pots=0;
    const max=Math.round((maxSec||90)*60);
    for(let i=0;i<max;i++){
      const s=window.__play("dun");
      if(s&&s.skill) skills++;
      if(s&&s.pot) pots++;
      m.simSteps(1, 1/60);
      if(m.G.p.dead || m.G.save.voucher || !m.DUN.on) break;
    }
    const st=m.DUN.stats||{};
    return {win:!!m.G.save.voucher, dead:!!m.G.p.dead, clock:+(m.DUN.clock||0).toFixed(1), hp:Math.round(m.G.p.hp), max:m.ST.hp, hurt:st.hurt||0, dmg:st.hurtDmg||0, wind:st.windups||0, shots:st.shots||0, shotHits:st.shotHits||0, dodges:st.dodges||0, skills, pots, src:st.src||{}, spawn:st.spawnDist, min:st.minDist};
  };
  return true;
})()`;
async function until(cur, body, limit){
  const t0=Date.now();
  let last=null;
  while(Date.now()-t0<limit){
    last=await cur.ev(body);
    if(last && last.ok) break;
    await sleep(120);
  }
  return { last, wall:+((Date.now()-t0)/1000).toFixed(1) };
}
async function talk(cur){
  await cur.ev(`window.__mengyu.G.joy.x=window.__mengyu.G.joy.y=0; document.getElementById("talk").click(); true`);
  await sleep(120);
  for(let i=0;i<16;i++){
    const more=await cur.ev(`(()=>{ const d=document.getElementById("dlg"); if(!d || d.style.display==="none") return false; const b=document.querySelector("#dlgRow .next"); if(!b) return false; b.click(); return true; })()`);
    if(!more) break;
    await sleep(80);
  }
}
try{
await cdp.send("Target.setDiscoverTargets",{discover:true});
const cur=await attach(base+"index.html");
await cdp.send("Emulation.setDeviceMetricsOverride",{width:360,height:640,deviceScaleFactor:1,mobile:true},cur.sessionId);
await cur.ev(`document.documentElement.style.setProperty("--st","0px");document.documentElement.style.setProperty("--sb","0px"); true`);
const title=await cur.ev(`(()=>{ const b=document.getElementById("btnNew"); const t=document.getElementById("buildTag"); return {shown:!!(b && b.offsetParent), build:t&&t.textContent}; })()`);
check("title shows the same build as the code", title.shown===true && title.build==="RC-6", JSON.stringify(title));
await shot(cur, "rc5-title.png");
await cur.ev(`document.getElementById("btnNew").click(); document.getElementById("btnGo").click(); true`);
await sleep(400);
const coach=await cur.ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  const joy=document.getElementById("joy");
  const r=joy.getBoundingClientRect();
  const shown=getComputedStyle(document.getElementById("coach")).display!=="none";
  joy.dispatchEvent(new PointerEvent("pointerdown",{pointerId:1,clientX:r.left+r.width/2,clientY:r.top+8,bubbles:true}));
  const t0=performance.now();
  while(performance.now()-t0<3000 && m.G.coach===0){
    joy.dispatchEvent(new PointerEvent("pointermove",{pointerId:1,clientX:r.left+r.width/2,clientY:r.top+4,bubbles:true}));
    await sleep(60);
  }
  joy.dispatchEvent(new PointerEvent("pointerup",{pointerId:1,bubbles:true,clientX:r.left+r.width/2,clientY:r.top+4}));
  document.getElementById("atk").dispatchEvent(new PointerEvent("pointerdown",{pointerId:2,bubbles:true,clientX:10,clientY:10}));
  await sleep(250);
  document.getElementById("dg").dispatchEvent(new PointerEvent("pointerdown",{pointerId:3,bubbles:true,clientX:10,clientY:10}));
  await sleep(400);
  return {shown, coach:m.G.coach, seen:m.G.save&&m.G.save.seen&&m.G.save.seen.onboard, z:+m.G.p.z.toFixed(2)};
})()`);
check("coach is finished with the joystick, attack and dodge", coach.shown===true && coach.coach==null && coach.seen===1, JSON.stringify(coach));
await cur.ev(INSTALL);
const mayor=await until(cur, `window.__gotoNpc("mayor")`, 20000);
await talk(cur);
const q1=await cur.ev(`window.__mengyu.qst("q1").st`);
check("accepted the first quest from the mayor", q1==="active" || q1==="ready", JSON.stringify({q1, mayor:mayor.last}));
const guard=await until(cur, `window.__gotoNpc("guard")`, 25000);
await talk(cur);
const q1b=await cur.ev(`window.__mengyu.qst("q1").st`);
check("turned the first quest in at the guard", q1b==="done", JSON.stringify({q1:q1b, guard:guard.last}));
await talk(cur);
const q2=await cur.ev(`window.__mengyu.qst("q2").st`);
check("accepted the slime quest", q2==="active", q2);
await until(cur, `window.__goto(0,16)`, 15000);
await until(cur, `window.__goto(0,28)`, 20000);
const kills=await until(cur, `(()=>{ const s=window.__play("field"); const q=window.__mengyu.qst("q2"); s.ok=q.st==="ready"||q.st==="done"; s.pr=q.pr; s.lv=window.__mengyu.G.save.level; s.xp=window.__mengyu.G.save.xp; return s; })()`, 120000);
check("six slimes were killed by walking and attacking", kills.last && kills.last.ok===true, JSON.stringify(kills));
await until(cur, `window.__goto(0,16)`, 20000);
const back=await until(cur, `window.__gotoNpc("guard")`, 20000);
await talk(cur);
const afterQuest=await cur.ev(`(()=>{ const s=window.__mengyu.G.save; return {q:s.quests.q2.st, gold:s.gold, lv:s.level, xp:s.xp}; })()`);
check("quest reward was claimed from the guard dialog", afterQuest.q==="done" && afterQuest.gold>30, JSON.stringify({afterQuest, back:back.last}));
const grew=await cur.ev(`(()=>{
  const m=window.__mengyu;
  m.openGrowth();
  const up=document.querySelector("#growth [data-up='atk']");
  if(up && !up.disabled) up.click();
  const tn=document.querySelector("#growth [data-tn='s_edge']");
  if(tn && !tn.disabled) tn.click();
  const cards=document.querySelectorAll("#growth .book button").length;
  return {gold:m.G.save.gold, rank:m.skillRank("swordsman","atk"), talent:(m.G.save.growth.talents.swordsman||[]).slice(), lv:m.G.save.level, cards, up:up&&up.textContent};
})()`);
await shot(cur, "rc5-growth.png");
await cur.ev(`window.__mengyu.closeGrowth(); window.__mengyu.openQuestLog(); true`);
await sleep(200);
await shot(cur, "rc5-quest.png");
await cur.ev(`window.__mengyu.closeQuestLog(); window.__mengyu.openBag(); true`);
await sleep(200);
const gear=await cur.ev(`(()=>{
  const m=window.__mengyu;
  const slot=document.querySelector("#eqSlots .slot");
  if(slot) slot.click();
  const enh=document.getElementById("tEnh");
  const text=enh&&enh.textContent;
  if(enh && !enh.disabled) enh.click();
  const w=m.G.save.equip.weapon;
  return {plus:w&&w.plus, atk:+m.ST.atk.toFixed(1), text, gold:m.G.save.gold};
})()`);
await shot(cur, "rc5-bag.png");
await cur.ev(`window.__mengyu.closeBag(); true`);
check("skill, talent or enhancement was bought from the panels", grew.rank===1 && grew.talent.indexOf("s_edge")>=0 && gear.plus===1 && grew.cards===6, JSON.stringify({grew, gear}));
const exact=await cur.ev(`(()=>{ document.getElementById("gold").click(); const t=document.getElementById("goldPop").textContent; document.getElementById("goldPop").click(); return {t, on:document.getElementById("goldPop").classList.contains("on")}; })()`);
check("exact gold is visible without the title attribute", exact.t.indexOf(String(gear.gold))>=0 && exact.on===false, JSON.stringify(exact));
const atDoor=await until(cur, `window.__gotoNpc("guard")`, 15000);
const opened=await cur.ev(`(()=>{
  document.getElementById("talk").click();
  const b=[...document.querySelectorAll("#dlgRow button")].find(x=>x.textContent.indexOf("蚀雾回廊")>=0);
  if(!b) return {ok:false, buttons:[...document.querySelectorAll("#dlgRow button")].map(x=>x.textContent), lv:window.__mengyu.G.save.level};
  b.click();
  const go=document.querySelector("#dunBody [data-dun='normal']");
  return {ok:!!(go && !go.disabled), h:go?go.getBoundingClientRect().height:0, lv:window.__mengyu.G.save.level};
})()`);
await shot(cur, "rc5-dungeon.png");
check("dungeon opens from the guard after the earned quests", opened.ok===true && opened.h>=44, JSON.stringify({opened, atDoor:atDoor.last}));
const kids0=await cur.ev(`window.__mengyu.scene.children.length`);
const mus0=await cur.ev(`(()=>{ const g=window.__mengyu.MUS.gain; if(g) g.userMark=7; return !!(g && g.userMark===7); })()`);
await cur.ev(`(()=>{ const go=document.querySelector("#dunBody [data-dun='normal']"); if(go && !go.disabled) go.click(); return true; })()`);
const run=await cur.ev(`window.__simClear(90)`);
const swordOk=run && run.win===true && run.dead!==true && run.hurt>0 && run.shotHits>0 && run.clock>=12;
check("fresh swordsman clears normal on the fixed 60fps clock", swordOk, JSON.stringify(run));
const claim=await cur.ev(`(()=>{
  const m=window.__mengyu, g0=m.G.save.gold;
  const btn=document.getElementById("dunClaim");
  if(!btn) return {missing:true, gold:g0, voucher:m.G.save.voucher};
  btn.click();
  const s=m.G.save;
  return {g0, gold:s.gold, lv:s.level, bag:s.bag.length, voucher:s.voucher, q2:s.quests.q2.st, rank:s.growth.skills.swordsman.atk, plus:s.equip.weapon&&s.equip.weapon.plus};
})()`);
check("dungeon claim pays once", claim.voucher==null && claim.gold>claim.g0, JSON.stringify(claim));
await cdp.send("Page.navigate",{url:base+"index.html"},cur.sessionId);
for(let i=0;i<80;i++){
  await sleep(250);
  try{
    if(await cur.ev(`!!(document.documentElement && document.documentElement.getAttribute('data-ready')==='1' && document.getElementById('btnCont'))`)) break;
  }catch(e){}
}
await cur.ev(`document.getElementById("btnCont").click(); true`);
await sleep(500);
const reloaded=await cur.ev(`(()=>{ const s=window.__mengyu.G.save; return s&&{gold:s.gold, lv:s.level, q2:s.quests.q2.st, rank:s.growth.skills.swordsman.atk, plus:s.equip.weapon&&s.equip.weapon.plus, voucher:s.voucher, foo:s.foo}; })()`);
check("reload keeps the earned gold, quest, skill and gear", reloaded && reloaded.gold===claim.gold && reloaded.q2==="done" && reloaded.rank===claim.rank && reloaded.plus===claim.plus && reloaded.voucher==null, JSON.stringify({claim, reloaded}));

await cur.ev(INSTALL);
const loops=await cur.ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  await sleep(600);
  if(m.MUS.gain) m.MUS.gain.userMark=7;
  const before=m.scene.children.length;
  for(let i=0;i<4;i++){
    m.G.p.dead=false; m.G.paused=false;
    const started=m.startDungeon("normal");
    await sleep(400);
    if(m.DUN.on) m.endDungeon("leave");
    await sleep(200);
    if(!started) return {started:false, i, lv:m.G.save.level, voucher:!!m.G.save.voucher};
  }
  return {before, after:m.scene.children.length, had:!!m.MUS.gain, same:!!(m.MUS.gain&&m.MUS.gain.userMark===7), dun:m.DUN.on};
})()`);
check("leaving the dungeon four times does not stack scenes or music", loops && loops.after-loops.before<40 && loops.same===true && loops.dun===false, JSON.stringify(loops));

async function prepClear(id, level, diff, label){
  await cur.ev(`(()=>{ const m=window.__mengyu; m.startAdventure("${id}", true); m.finishCoach(); m.G.save.level=${level}; m.recalcStats(); m.G.p.hp=m.ST.hp; m.G.p.mp=m.ST.mp; return true; })()`);
  await cur.ev(INSTALL);
  const door=await until(cur, `window.__gotoNpc("guard")`, 90000);
  const go=await cur.ev(`(()=>{
    const m=window.__mengyu;
    m.G.joy.x=m.G.joy.y=0;
    if(!(m.G.near && m.G.near.id==="guard")) return {ok:false, near:m.G.near&&m.G.near.id||"", x:+m.G.p.x.toFixed(2), z:+m.G.p.z.toFixed(2)};
    document.getElementById("talk").click();
    const b=[...document.querySelectorAll("#dlgRow button")].find(x=>x.textContent.indexOf("蚀雾回廊")>=0);
    if(!b) return {ok:false, buttons:[...document.querySelectorAll("#dlgRow button")].map(x=>x.textContent), open:document.getElementById("dlg").style.display};
    b.click();
    const card=document.querySelector("#dunBody [data-dun='${diff}']");
    if(!card||card.disabled) return {ok:false, card:!!card};
    card.click();
    return {ok:m.DUN.on===true};
  })()`);
  const fought=go.ok?await cur.ev(`window.__simClear(90)`):null;
  const pace=fought && fought.win===true && fought.dead!==true && fought.clock>=18 && fought.shotHits>=1 && fought.dodges>=1 && fought.hurt>0;
  const hard=diff!=="hard" || (fought && fought.dmg>=150 && fought.src && fought.src.after>0 && fought.shotHits>=3 && fought.clock>=24);
  check(label, go.ok===true && pace && hard, JSON.stringify({door:door.last, go, run:fought, note:"fixed 1/60 step, level "+level+", starter weapon"}));
  await cur.ev(`(()=>{ const b=document.getElementById("dunClaim"); if(b) b.click(); if(window.__mengyu.DUN.on) window.__mengyu.endDungeon("leave"); if(window.__mengyu.G.p.dead) document.getElementById("goTown").click(); return true; })()`);
  return fought;
}
const clocks=await cur.ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  m.G.p.dead=false; m.G.paused=false; m.G.p.hp=m.ST.hp;
  m.startDungeon("normal");
  const c0=m.DUN.clock, w0=performance.now();
  await sleep(1000);
  const live={game:+(m.DUN.clock-c0).toFixed(3), wall:+((performance.now()-w0)/1000).toFixed(3), step:m.G.perf.stepMs};
  m.endDungeon("leave");
  m.G.p.dead=false; m.G.paused=false; m.G.p.hp=m.ST.hp;
  m.startDungeon("normal");
  m.simSteps(60, 1/60);
  const fixed=+m.DUN.clock.toFixed(3);
  m.endDungeon("leave");
  m.G.backgrounded=true;
  await sleep(400);
  m.G.backgrounded=false;
  await sleep(120);
  return {live, fixed, resume:m.G.perf.stepMs};
})()`);
check("realtime headless is slower than the game clock, and a fixed step matches 60fps", clocks.live.game<clocks.live.wall*0.6 && Math.abs(clocks.fixed-1)<0.08 && clocks.resume<=50, JSON.stringify(clocks));
const stand=await cur.ev(`(()=>{
  const m=window.__mengyu;
  m.startAdventure("swordsman", true); m.finishCoach();
  m.G.save.level=2; m.recalcStats(); m.G.p.hp=m.ST.hp; m.G.p.mp=m.ST.mp; m.G.p.dead=false; m.G.paused=false;
  m.startDungeon("normal");
  for(let i=0;i<20*60;i++){
    m.G.joy.x=m.G.joy.y=0; m.G.atkHeld=false;
    m.simSteps(1, 1/60);
    if(m.G.p.dead) break;
    if((m.DUN.stats.shotHits||0)>0 && m.DUN.stats.hurt>0 && m.DUN.clock>6) break;
  }
  return {dead:!!m.G.p.dead, hp:Math.round(m.G.p.hp), max:m.ST.hp, clock:+m.DUN.clock.toFixed(1), hurt:m.DUN.stats.hurt, dmg:m.DUN.stats.hurtDmg, shotHits:m.DUN.stats.shotHits, src:m.DUN.stats.src};
})()`);
check("standing still is hit by a bite and a shot without being scripted to die", stand.hurt>0 && stand.shotHits>0, JSON.stringify(stand));
await cur.ev(INSTALL);
await prepClear("swordsman", 2, "normal", "SEPARATE swordsman normal at level 2");
await prepClear("mage", 2, "normal", "SEPARATE mage normal at level 2");
await prepClear("archer", 2, "normal", "SEPARATE archer normal at level 2");
await prepClear("swordsman", 6, "hard", "SEPARATE swordsman hard at level 6");

const slow=await attach(base+"index.html");
let throttled=false, throttleErr="";
try{
  await cdp.send("Network.emulateNetworkConditions",{offline:false,latency:500,downloadThroughput:40*1024,uploadThroughput:20*1024},slow.sessionId);
  await cdp.send("Page.navigate",{url:base+"index.html"},slow.sessionId);
  for(let i=0;i<80;i++){ await sleep(250); if(await slow.ev(`document.documentElement.getAttribute('data-ready')==='1'`)){ throttled=true; break; } }
}catch(e){ throttleErr=String(e).slice(0,180); }
check("throttled load still reaches the title", throttled===true, throttleErr||"ready");
const blocked=await attach(base+"about:blank");
let townFail=null, blockErr="";
try{
  await cdp.send("Network.setCacheDisabled",{cacheDisabled:true},blocked.sessionId);
  await cdp.send("Network.setBlockedURLs",{urls:["*town.glb*","*models/town*"]},blocked.sessionId);
  await cdp.send("Page.navigate",{url:base+"index.html"},blocked.sessionId);
  for(let i=0;i<80;i++){ await sleep(250); if(await blocked.ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
  townFail=await blocked.ev(`(()=>{ const n=document.getElementById("saveNote"); return {note:n&&n.textContent, retry:document.getElementById("btnReload")&&document.getElementById("btnReload").style.display}; })()`);
}catch(e){ blockErr=String(e).slice(0,180); }
check("a failed town download shows the fallback instead of a blank page", !!(townFail && /备用造型|重新加载|没能下载/.test(townFail.note||"")), JSON.stringify(townFail||blockErr));

const err=logs.filter(l=>!/deprecated with r150/.test(l));
check("no console errors", err.length===0, err.join(" | ")||"none");
}finally{
  try{ ws.close(); chrome.kill(); }catch(e){}
}
console.log("FAILS", fail.length?fail.join(", "):"none");
setTimeout(()=>process.exit(fail.length?1:0),200);
