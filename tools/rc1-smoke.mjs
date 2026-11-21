// RC-1 smoke: local headless Chrome. Not a real-device test.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const URL = process.argv[2] || "http://127.0.0.1:8765/index.html";
const PORT = 9244;
const fail = [];
function sleep(ms){ return new Promise(r=>setTimeout(r, ms)); }
class CDP{
  constructor(ws){ this.ws=ws; this.id=0; this.pending=new Map();
    ws.addEventListener("message", ev=>{ const msg=JSON.parse(ev.data); if(msg.id && this.pending.has(msg.id)){ const {resolve,reject}=this.pending.get(msg.id); this.pending.delete(msg.id); if(msg.error) reject(new Error(JSON.stringify(msg.error))); else resolve(msg.result); } }); }
  send(m,p={},s){ const id=++this.id; return new Promise((resolve,reject)=>{ this.pending.set(id,{resolve,reject}); const o={id,method:m,params:p}; if(s) o.sessionId=s; this.ws.send(JSON.stringify(o)); }); }
}
const chrome=spawn("google-chrome",["--headless=new","--disable-gpu","--no-sandbox","--disable-dev-shm-usage","--use-angle=swiftshader","--enable-webgl",`--remote-debugging-port=${PORT}`,"--window-size=1100,720","--user-data-dir=/tmp/chrome-rc1-"+Date.now(),"about:blank"],{stdio:"ignore"});
let browser; for(let i=0;i<50;i++){ try{ browser=await (await fetch(`http://127.0.0.1:${PORT}/json/version`)).json(); break; }catch{ await sleep(200); } }
if(!browser){ console.error("no chrome"); process.exit(1); }
const ws=new WebSocket(browser.webSocketDebuggerUrl); await new Promise(r=>ws.addEventListener("open",r));
const cdp=new CDP(ws);
const logs=[];
const {targetId}=await cdp.send("Target.createTarget",{url:"about:blank"});
const {sessionId}=await cdp.send("Target.attachToTarget",{targetId,flatten:true});
const S=sessionId;
await cdp.send("Page.enable",{},S); await cdp.send("Runtime.enable",{},S);
cdp.ws.addEventListener("message", ev=>{
  const msg=JSON.parse(ev.data);
  if(msg.method==="Runtime.exceptionThrown") logs.push("EXC "+((msg.params.exceptionDetails.exception&&msg.params.exceptionDetails.exception.description)||msg.params.exceptionDetails.text||"").slice(0,300));
  if(msg.method==="Runtime.consoleAPICalled" && msg.params.type==="error") logs.push("ERR "+msg.params.args.map(a=>a.value||a.description||"").join(" ").slice(0,240));
});
async function ev(expr){
  const r=await cdp.send("Runtime.evaluate",{expression:expr,awaitPromise:true,returnByValue:true},S);
  if(r.exceptionDetails) throw new Error((r.exceptionDetails.exception&&r.exceptionDetails.exception.description)||r.exceptionDetails.text);
  return r.result.value;
}
function check(name, ok, extra){ console.log((ok?"PASS ":"FAIL ")+name+(extra?" "+extra:"")); if(!ok) fail.push(name); }
async function shot(name){
  const {data}=await cdp.send("Page.captureScreenshot",{format:"png"},S);
  writeFileSync("/opt/cursor/artifacts/"+name, Buffer.from(data,"base64"));
}

await cdp.send("Emulation.setDeviceMetricsOverride",{width:390,height:844,deviceScaleFactor:1,mobile:true},S);
await cdp.send("Page.navigate",{url:URL+"?shot=title"},S);
for(let i=0;i<80;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
await sleep(400);

const save = await ev(`(()=>{
  const m=window.__mengyu;
  const out={};
  localStorage.clear();
  const base={ver:2,classId:"mage",level:12,xp:3,gold:777,shards:1,kills:4,storyDone:true,hasSave:true,bossKills:2,
    pots:{h:4,m:1},bag:[],equip:{weapon:null,armor:null,acc:null},
    quests:{q1:{st:"done",pr:1},q5:{st:"done",pr:1}}};
  m.persist(JSON.parse(JSON.stringify(base)));
  const back=m.loadSave();
  out.keep={level:back.level,gold:back.gold,story:back.storyDone,q5:back.quests.q5.st,ver:back.ver,classId:back.classId};
  const raw=localStorage.getItem(m.SAVE_KEY);
  out.rawVer=JSON.parse(raw).ver;
  const bakBefore=localStorage.getItem("MengYuLegend_Save_v1.bak");
  const next=JSON.parse(JSON.stringify(back)); next.gold=900;
  m.persist(next);
  out.bakIsOld = JSON.parse(localStorage.getItem("MengYuLegend_Save_v1.bak")).gold===777;
  localStorage.setItem(m.SAVE_KEY, "{not json");
  const rec=m.loadSave();
  out.recovered=!!(rec&&rec.gold===777&&rec.storyDone===true);
  out.badKept=(localStorage.getItem("MengYuLegend_Save_v1.bad")||"").includes("not json");
  out.recGold=rec&&rec.gold;
  localStorage.setItem(m.SAVE_KEY, "[[[");
  localStorage.setItem("MengYuLegend_Save_v1.bak", "nope");
  const none=m.loadSave();
  out.corruptNull=none===null;
  out.corruptKept=localStorage.getItem(m.SAVE_KEY)==="[[[";
  const newer=JSON.stringify({ver:9,classId:"swordsman",level:3,hasSave:true,storyDone:true,quests:{}});
  localStorage.setItem(m.SAVE_KEY, newer);
  const blocked=m.loadSave();
  out.newerNull=blocked===null;
  out.newerUntouched=localStorage.getItem(m.SAVE_KEY)===newer;
  localStorage.setItem(m.SAVE_KEY, raw);
  const oldSet=localStorage.setItem.bind(localStorage);
  localStorage.setItem=function(k,v){ if(String(k).indexOf("MengYu")===0){ const err=new Error("quota"); err.name="QuotaExceededError"; throw err; } return oldSet(k,v); };
  const beforeQ=localStorage.getItem? null : null;
  const q=m.persist(JSON.parse(raw));
  localStorage.setItem=oldSet;
  out.quotaFalse=q===false;
  out.quotaKept=localStorage.getItem(m.SAVE_KEY)===raw;
  const badImp=m.previewImport("hello");
  const newImp=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:9,save:{ver:9}}));
  const bagImp=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:2,save:{ver:2,classId:"swordsman",bag:new Array(40).fill({uid:"x",slot:"acc"})}}));
  const okImp=m.previewImport(m.exportSaveText(JSON.parse(raw)));
  out.import={bad:badImp.ok, newer:!!newImp.newer, bag:bagImp.ok, ok:okImp.ok&&okImp.save.gold===777&&okImp.save.storyDone===true};
  out.exportNote=m.exportSaveText(JSON.parse(raw)).includes("不是云存档");
  out.build=m.BUILD.name;
  const legacy={ver:1,classId:"archer",level:6,gold:50,storyDone:true,quests:{q4:{st:"active",pr:0}}};
  const mig=m.migrateSave(JSON.parse(JSON.stringify(legacy)));
  out.legacy={story:mig.storyDone,q4:mig.quests.q4.st,ver:mig.ver};
  localStorage.setItem(m.SAVE_KEY, raw);
  return out;
})()`);
check("ver2 keeps level gold story and migrates to schema 6", save.keep.level===12 && save.keep.gold===777 && save.keep.story===true && save.keep.q5==="done" && save.keep.ver===6 && save.rawVer===6, JSON.stringify(save.keep));
check("backup rotates previous good save", save.bakIsOld===true);
check("corrupt primary restores backup and keeps the bad text", save.recovered===true && save.badKept===true);
check("double corrupt does not invent or delete the raw", save.corruptNull===true && save.corruptKept===true);
check("newer schema is not loaded or rewritten", save.newerNull===true && save.newerUntouched===true);
check("quota failure does not replace the save", save.quotaFalse===true && save.quotaKept===true);
check("import rejects junk, newer schema and oversized bag", save.import.bad===false && save.import.newer===true && save.import.bag===false && save.import.ok===true);
check("export text says it is not a cloud save", save.exportNote===true && save.build==="RC-4");
check("legacy save keeps story and quest instead of wiping", save.legacy.story===true && save.legacy.q4==="active" && save.legacy.ver===6);

const ui = await ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  function box(id){ const e=document.getElementById(id); const r=e.getBoundingClientRect(); return {w:r.width,h:r.height,x:r.x,y:r.y,r:r.right,b:r.bottom}; }
  function hit(a,b){ return !(a.r<=b.x||b.r<=a.x||a.b<=b.y||b.b<=a.y); }
  localStorage.setItem(m.SAVE_KEY, JSON.stringify({ver:2,classId:"swordsman",level:9,xp:0,gold:40,storyDone:false,hasSave:true,quests:{q1:{st:"avail",pr:0}}}));
  m.showTitle();
  await sleep(50);
  document.getElementById("btnCont").click();
  await sleep(200);
  const cont=m.G.save.level;
  m.showTitle();
  await sleep(80);
  const note=document.getElementById("saveNote").textContent;
  const ver=document.getElementById("buildTag").textContent;
  const exp=box("btnExport"), imp=box("btnImport"), contB=box("btnCont");
  document.getElementById("btnExport").click();
  const text=JSON.stringify({format:"mengyu-legend-save",schema:2,note:"x",save:{ver:2,classId:"mage",level:4,xp:0,gold:15,hasSave:true,storyDone:false,quests:{q1:{st:"avail",pr:0}}}});
  const before=localStorage.getItem(m.SAVE_KEY);
  m.requestImport(text);
  const open=document.getElementById("confirm").classList.contains("on");
  document.getElementById("cfNo").click();
  await sleep(40);
  const afterNo=localStorage.getItem(m.SAVE_KEY)===before;
  m.requestImport(text);
  document.getElementById("cfYes").click();
  await sleep(80);
  const imported=m.loadSave();
  return {cont, note, ver, exp, imp, contB, open, afterNo, importedLv:imported&&imported.level, importedClass:imported&&imported.classId, cloud:note.includes("不是云存档")};
})()`);
check("continue uses the saved level", ui.cont===9, "level "+ui.cont);
check("title names RC-4 and local save", ui.ver==="RC-4" && ui.cloud===true, ui.note);
check("export and import targets are at least 44px", ui.exp.h>=44 && ui.exp.w>=44 && ui.imp.h>=44 && ui.contB.h>=44, JSON.stringify({exp:ui.exp.h,imp:ui.imp.h,cont:ui.contB.h}));
check("import confirm cancel keeps the save and accept writes it", ui.open===true && ui.afterNo===true && ui.importedLv===4 && ui.importedClass==="mage");

const onboard = await ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  localStorage.clear();
  m.showTitle();
  document.getElementById("btnNew").click();
  await sleep(100);
  document.getElementById("btnGo").click();
  await sleep(250);
  const shown=getComputedStyle(document.getElementById("coach")).display!=="none";
  const skip=document.getElementById("coachSkip").getBoundingClientRect();
  const joy=document.getElementById("joy");
  const r=joy.getBoundingClientRect();
  const pid=1;
  joy.dispatchEvent(new PointerEvent("pointerdown",{pointerId:pid,clientX:r.left+r.width/2,clientY:r.top+8,bubbles:true}));
  const t0=performance.now();
  while(performance.now()-t0<2500 && m.G.coach===0){
    joy.dispatchEvent(new PointerEvent("pointermove",{pointerId:pid,clientX:r.left+r.width/2,clientY:r.top+4,bubbles:true}));
    await sleep(60);
  }
  joy.dispatchEvent(new PointerEvent("pointerup",{pointerId:pid,clientX:r.left+r.width/2,clientY:r.top+4,bubbles:true}));
  const afterMove=m.G.coach;
  document.getElementById("atk").dispatchEvent(new PointerEvent("pointerdown",{pointerId:2,bubbles:true,clientX:10,clientY:10}));
  await sleep(200);
  const afterAtk=m.G.coach;
  document.getElementById("dg").dispatchEvent(new PointerEvent("pointerdown",{pointerId:3,bubbles:true,clientX:10,clientY:10}));
  await sleep(300);
  const afterDg=m.G.coach;
  const seen=m.G.save&&m.G.save.seen&&m.G.save.seen.onboard;
  return {shown, skipH:skip.height, afterMove, afterAtk, afterDg, seen, z:m.G.p.z};
})()`);
check("coach appears for a new game", onboard.shown===true);
check("coach skip is at least 44px", onboard.skipH>=44, "h "+onboard.skipH);
check("joystick pointer advances the coach", onboard.afterMove===1, "coach "+onboard.afterMove+" z "+onboard.z);
check("attack pointer advances the coach", onboard.afterAtk===2, "coach "+onboard.afterAtk);
check("dodge pointer finishes onboarding into the save", onboard.afterDg==null && onboard.seen===1, "coach "+onboard.afterDg);

const play = await ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  function settle(){
    m.G.mode="play"; m.G.paused=false; m.G.backgrounded=false; m.G.p.dead=false; m.G.hitStop=0; m.G.timeScale=1;
    m.G.dlg=null; m.G.p.atkT=0; m.G.p.spinT=0; m.G.p.dashT=0; m.G.p.dodgeT=0; m.G.p.invT=0; m.G.atkCd=0;
    m.G.cd.sk1=0; m.G.cd.sk2=0; m.G.cd.dg=0; m.G.p.mp=400; m.G.combo=0; m.G.comboQueued=false;
  }
  function plant(dist){
    m.clearEnemies(); settle();
    m.G.p.x=0; m.G.p.z=28; m.G.p.yaw=0;
    const e=m.spawnEnemy("slime", 0, 28+dist, 1, "slope");
    e.hp=e.max=400; e.spd=0; e.state="stun"; e.t=999; e.aggro=false; e.hx=e.x; e.hz=e.z;
    return e;
  }
  async function until(e, ms){
    const hp0=e.hp, t0=performance.now();
    while(performance.now()-t0<ms){ await sleep(150); if(e.hp<hp0||e.dead) break; }
    return {before:hp0, after:e.dead?0:e.hp, dropped:(e.dead?0:e.hp)<hp0};
  }
  const out={};
  for(const id of ["swordsman","mage","archer"]){
    m.startAdventure(id, true);
    const basic=plant(id==="swordsman"?1.05:2.4); m.tryAttack(); const atk=await until(basic, 3500);
    await sleep(300);
    const a=plant(1.15); m.trySkill(1); const sk1=await until(a, 3500);
    await sleep(400);
    const b=plant(id==="swordsman"?1.0:2.2); m.trySkill(2); const sk2=await until(b, 3500);
    await sleep(300);
    settle(); const hp0=m.G.p.hp; m.tryDodge(); await sleep(60);
    const dodging=m.G.p.dodgeT>0; m.damagePlayer(80, m.G.p.x, m.G.p.z-1); const blocked=m.G.p.hp===hp0;
    await sleep(900); m.G.p.invT=0; m.G.p.dodgeT=0; m.damagePlayer(30, m.G.p.x, m.G.p.z-1);
    out[id]={atk:atk.dropped, sk1:sk1.dropped, sk2:sk2.dropped, dodging, blocked, hurt:m.G.p.hp<hp0, atkN:atk};
  }
  await sleep(600);
  out.perf={frames:m.G.perf.frames, draws:m.G.perf.draws, tris:m.G.perf.tris, dtMs:m.G.perf.dtMs};
  return out;
})()`);
for(const id of ["swordsman","mage","archer"]){
  const c=play[id];
  check(id+" attack skill dodge", c.atk && c.sk1 && c.sk2 && c.dodging && c.blocked && c.hurt, JSON.stringify(c));
}
check("frame stats recorded without claiming a phone", play.perf.frames>0 && play.perf.draws>0, JSON.stringify(play.perf));

const trial = await ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  m.startAdventure("swordsman", true);
  m.G.boss=null;
  m.DROPS.forEach(d=>{ d.on=false; });
  m.G.save.level=6; m.recalcStats();
  m.G.mode="play"; m.G.paused=false; m.G.p.dead=false;
  m.G.p.invT=999; m.G.p.dead=false;
  m.G.p.x=m.NPCS.guard.x; m.G.p.z=m.NPCS.guard.z+1.1;
  await sleep(400);
  m.tryTalk();
  let started=false, why="";
  for(let i=0;i<8 && !m.TRIAL.on;i++){
    const opt=[...document.querySelectorAll("#dlgRow button")].find(b=>b.textContent.indexOf("试炼")>=0);
    if(opt){
      why=JSON.stringify({mode:m.G.mode,dead:m.G.p.dead,lv:m.G.save.level,boss:!!(m.G.boss&&!m.G.boss.dead),on:m.TRIAL.on,paused:m.G.paused});
      opt.click(); started=true; if(m.TRIAL.on) m.TRIAL.t=1e12; break;
    }
    const n=document.querySelector("#dlgRow button.next"); if(n) n.click();
    await sleep(40);
  }
  if(m.TRIAL.on) m.TRIAL.t=1e12;
  m.G.p.dead=false; m.G.p.invT=999; m.G.mode="play"; m.G.paused=false;
  for(let w=0; w<12 && m.TRIAL.on && !(m.G.save.trial&&m.G.save.trial.clears); w++){
    m.TRIAL.t=1e12; m.TRIAL.gap=0;
    m.G.p.dead=false; m.G.p.invT=1e9; m.G.mode="play"; m.G.paused=false;
    await sleep(250);
    m.G.enemies.filter(e=>e.trial&&!e.dead).forEach(e=>m.killEnemy(e,0,1));
    await sleep(250);
  }
  const wave=m.TRIAL.wave;
  const goldAfter=m.G.save.gold;
  const clears=m.G.save.trial&&m.G.save.trial.clears;
  m.G.save.level=6; m.G.p.dead=false; m.G.mode="play"; m.G.boss=null;
  m.DROPS.forEach(d=>{ d.on=false; });
  m.startTrial();
  const goldBeforeFail=m.G.save.gold;
  m.TRIAL.t=0.05;
  await sleep(800);
  return {started, why, wave, on:m.TRIAL.on, clears, goldAfter, failOn:m.TRIAL.on, goldFail:m.G.save.gold, goldBeforeFail};
})()`);
check("trial starts from the guard dialog and success clears", trial.started===true && trial.clears>=1 && trial.on===false, JSON.stringify(trial));
check("trial failure does not add another clear", trial.failOn===false && trial.goldFail===trial.goldBeforeFail, JSON.stringify({goldFail:trial.goldFail,before:trial.goldBeforeFail}));

const boss = await ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  m.startAdventure("swordsman", true);
  m.clearEnemies();
  m.G.mode="play"; m.G.paused=false; m.G.hitStop=0; m.G.p.dead=false;
  const A=m.ALTAR;
  m.G.p.x=A.x; m.G.p.z=A.z+2;
  const b=m.spawnEnemy("boss", A.x, A.z, 8, "altar");
  b.atkN=4; b.cd=0; b.state="chase"; b.hp=b.max;
  let maxAdds=0, saw="";
  const t0=performance.now();
  while(performance.now()-t0<8000){
    const adds=m.G.enemies.filter(e=>!e.dead && e.zone==="altar" && !e.boss);
    if(adds.length>maxAdds) maxAdds=adds.length;
    if(b.atk==="summon"||b.state==="windup") saw=b.state+" "+b.atk+" t "+(b.t||0);
    await sleep(200);
  }
  return {maxAdds, state:b.state, atk:b.atk, atkN:b.atkN, saw};
})()`);
check("boss actually summons adds", boss.maxAdds>=1, JSON.stringify(boss));

const story = await ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  m.startAdventure("swordsman", true);
  m.gainXP(8000);
  m.G.paused=false; m.G.mode="play"; m.G.p.dead=false;
  async function stand(id){
    const n=m.NPCS[id];
    m.G.p.x=n.x; m.G.p.z=n.z+1.1; m.G.paused=false; m.G.dlg=null;
    await sleep(350);
    m.tryTalk();
    for(let i=0;i<14;i++){
      if(!m.G.dlg) break;
      const b=document.querySelector("#dlgRow button.next")||document.querySelector("#dlgRow button");
      if(!b) break;
      b.click();
      await sleep(30);
    }
    m.G.paused=false;
  }
  function st(id){ return m.G.save.quests[id]&&m.G.save.quests[id].st; }
  await stand("mayor");
  await stand("guard");
  await stand("guard");
  for(let i=0;i<6;i++){ const e=m.spawnEnemy(i%2?"spike":"slime", 2+i, 28, 1, "slope"); m.killEnemy(e,0,1); }
  await stand("guard");
  await stand("herb");
  let guard=0;
  while(st("q3")==="active" && guard++<24){
    const e=m.spawnEnemy("wisp", 0, 30, 3, "wood"); m.killEnemy(e,0,1);
    await sleep(120);
    const picks=m.G.pickups||[];
    for(const p of picks){ m.G.p.x=p.x; m.G.p.z=p.z; m.G.paused=false; m.G.hitStop=0; await sleep(250); }
  }
  await stand("herb");
  await stand("guard");
  const elite=m.spawnEnemy("elite", 8, 36, 6, "wood"); m.killEnemy(elite,0,1);
  await stand("guard");
  await stand("mayor");
  const bossE=m.spawnEnemy("boss", m.ALTAR.x, m.ALTAR.z, 8, "altar"); m.killEnemy(bossE,0,1);
  await stand("mayor");
  return {q:["q1","q2","q3","q4","q5"].map(st), story:!!m.G.save.storyDone, shards:m.G.save.shards, level:m.G.save.level};
})()`);
check("story reaches storyDone through quests", story.story===true && story.q.every(s=>s==="done"), JSON.stringify(story));

const layout = await ev(`(()=>{
  const m=window.__mengyu;
  m.G.mode="play"; m.G.paused=true;
  document.getElementById("playHud").style.display="block";
  function box(id){ const e=document.getElementById(id); if(!e) return null; const r=e.getBoundingClientRect(); return {w:+r.width.toFixed(1),h:+r.height.toFixed(1),x:+r.x.toFixed(1),y:+r.y.toFixed(1),r:+r.right.toFixed(1),b:+r.bottom.toFixed(1)}; }
  function hit(a,b){ if(!a||!b||a.w<1||b.w<1) return false; return !(a.r<=b.x||b.r<=a.x||a.b<=b.y||b.b<=a.y); }
  const ids=["atk","sk1","sk2","dg","potH","btnBag"];
  const boxes={}; ids.forEach(id=>boxes[id]=box(id));
  let overlap="";
  for(let i=0;i<ids.length;i++) for(let j=i+1;j<ids.length;j++) if(hit(boxes[ids[i]],boxes[ids[j]])) overlap+=ids[i]+"/"+ids[j]+" ";
  const small=ids.filter(id=>boxes[id] && (boxes[id].w<44 || boxes[id].h<44));
  const p=document.getElementById("pframe").getBoundingClientRect();
  return {boxes, overlap:overlap.trim(), small, pTop:+p.top.toFixed(1), canvasTop:+document.querySelector("canvas").getBoundingClientRect().top.toFixed(1)};
})()`);
check("combat targets stay at least 44px and apart", layout.small.length===0 && !layout.overlap, JSON.stringify({small:layout.small,overlap:layout.overlap,atk:layout.boxes.atk}));

await cdp.send("Emulation.setDeviceMetricsOverride",{width:360,height:640,deviceScaleFactor:1,mobile:true},S);
await cdp.send("Emulation.setUserAgentOverride",{userAgent:"Mozilla/5.0 (Linux; Android 14; Vivo) AppleWebKit/537.36 Chrome/120.0.0.0 Mobile Safari/537.36"},S);
await sleep(400);
await ev(`window.dispatchEvent(new Event("resize")); true`);
await sleep(400);
const small = await ev(`(()=>{
  const r=document.getElementById("atk").getBoundingClientRect();
  const s=document.getElementById("sk2").getBoundingClientRect();
  const gap=r.left-s.right;
  const top=document.getElementById("pframe").getBoundingClientRect().top;
  if(window.__mengyu&&window.__mengyu.G){ try{ document.documentElement.getBoundingClientRect(); }catch(e){} }
  return {aw:+r.width.toFixed(1),ah:+r.height.toFixed(1),gap:+gap.toFixed(1),top:+top.toFixed(1),st:getComputedStyle(document.documentElement).getPropertyValue("--st").trim(),ax:+r.left.toFixed(1),sx:+s.left.toFixed(1)};
})()`);
check("small android portrait keeps attack size and no double top inset", small.ah>=44 && small.aw>=44 && small.gap>=4 && small.top<30, JSON.stringify(small));
await shot("rc1-after-play-android.png");

await cdp.send("Page.navigate",{url:URL+"?shot=title"},S);
for(let i=0;i<60;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'&&document.getElementById('title').style.display!=='none'`)) break; }
await sleep(300);
await shot("rc1-after-title.png");

const err=logs.filter(l=>!/deprecated with r150/.test(l) && !/GLB load failed/.test(l) && !/town GLB failed/.test(l));
check("no console errors", err.length===0, err.join(" | ")||"none");
console.log("FAILS", fail.length? fail.join(", "):"none");
ws.close(); chrome.kill();
setTimeout(()=>process.exit(fail.length?1:0), 200);
