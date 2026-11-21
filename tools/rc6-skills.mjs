// Skill book measurements. Headless Chrome, not a phone.
// Effect numbers are from the combat functions with crit off and roll fixed at 1.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const PORT=9266;
const fail=[];
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
class CDP{
  constructor(ws){ this.ws=ws; this.id=0; this.pending=new Map();
    ws.addEventListener("message",ev=>{ const msg=JSON.parse(ev.data); if(msg.id&&this.pending.has(msg.id)){ const {resolve,reject}=this.pending.get(msg.id); this.pending.delete(msg.id); if(msg.error) reject(new Error(JSON.stringify(msg.error))); else resolve(msg.result);} }); }
  send(m,p={},s){ const id=++this.id; return new Promise((resolve,reject)=>{ this.pending.set(id,{resolve,reject}); const o={id,method:m,params:p}; if(s)o.sessionId=s; this.ws.send(JSON.stringify(o)); }); }
}
const chrome=spawn("google-chrome",["--headless=new","--disable-gpu","--no-sandbox","--disable-dev-shm-usage","--use-angle=swiftshader","--enable-webgl",`--remote-debugging-port=${PORT}`,"--window-size=1100,720","--user-data-dir=/tmp/chrome-rc6-"+Date.now(),"about:blank"],{stdio:"ignore"});
let browser; for(let i=0;i<50;i++){ try{ browser=await (await fetch(`http://127.0.0.1:${PORT}/json/version`)).json(); break;}catch{ await sleep(200);} }
if(!browser){ console.error("no chrome"); process.exit(1); }
const ws=new WebSocket(browser.webSocketDebuggerUrl); await new Promise(r=>ws.addEventListener("open",r));
const cdp=new CDP(ws);
const logs=[];
function check(n,ok,x){ console.log((ok?"PASS ":"FAIL ")+n+(x?" "+x:"")); if(!ok) fail.push(n); }
try{
const {targetId}=await cdp.send("Target.createTarget",{url:"about:blank"});
const {sessionId}=await cdp.send("Target.attachToTarget",{targetId,flatten:true});
await cdp.send("Page.enable",{},sessionId); await cdp.send("Runtime.enable",{},sessionId);
cdp.ws.addEventListener("message",ev=>{ const msg=JSON.parse(ev.data); if(msg.sessionId===sessionId && msg.method==="Runtime.exceptionThrown") logs.push(((msg.params.exceptionDetails.exception&&msg.params.exceptionDetails.exception.description)||"").slice(0,180)); });
const ev=async e=>{ const r=await cdp.send("Runtime.evaluate",{expression:e,awaitPromise:true,returnByValue:true},sessionId); if(r.exceptionDetails) throw new Error((r.exceptionDetails.exception&&r.exceptionDetails.exception.description)||r.exceptionDetails.text); return r.result.value; };
await cdp.send("Page.navigate",{url:"http://127.0.0.1:8765/index.html?shot=play"},sessionId);
for(let i=0;i<80;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
await cdp.send("Emulation.setDeviceMetricsOverride",{width:360,height:640,deviceScaleFactor:1,mobile:true},sessionId);
const fx=await ev(`(()=>{
  const m=window.__mengyu;
  m.finishCoach();
  const s=m.G.save; s.level=10; s.gold=5000; s.foo="keep-skill"; s.xp=0;
  m.recalcStats(); m.G.p.hp=m.ST.hp; m.G.p.mp=m.ST.mp; m.G.p.dead=false; m.G.paused=false;
  m.G.forceCrit=0; m.G.forceRoll=1;
  m.G.p.x=0; m.G.p.z=24; m.G.p.yaw=0;
  const out={};
  function foe(){ const e=m.spawnEnemy("slime", 0, 26.2, 1, "slope"); e.hp=500; e.max=500; return e; }
  function ready(){ const P=m.G.p; P.spinT=0; P.atkT=0; P.dashT=0; P.dodgeT=0; P.hurtT=0; P.mp=m.ST.mp; }
  m.equipSkill(2,"s_guard");
  const hp0=m.G.p.hp; ready(); m.trySkillSlot(2); const shield=m.G.buff.guard;
  m.damagePlayer(40,0,27); const afterGuard=hp0-m.G.p.hp;
  m.clearCombatBuffs(); m.G.p.hp=m.ST.hp;
  const e1=foe(); ready(); m.trySkillSlot(0); // default slot0 is spin, not shock
  m.equipSkill(0,"s_shock"); m.G.cd.s_shock=0; m.G.p.mp=m.ST.mp;
  const e2=foe(); ready(); m.trySkillSlot(0); const slimeSlow=e2.slow;
  const boss=m.spawnEnemy("boss", 1, 27, 8, "altar"); boss.hp=5000; m.G.cd.s_shock=0; m.G.p.mp=m.ST.mp; ready(); m.trySkillSlot(0); const bossSlow=boss.slow;
  m.equipSkill(0,"s_leap"); m.G.cd.s_leap=0; m.G.p.mp=m.ST.mp; const z0=m.G.p.z; ready(); m.trySkillSlot(0); const leaped=+(m.G.p.z-z0).toFixed(2);
  m.equipSkill(0,"s_rally"); m.G.cd.s_rally=0; m.G.p.mp=m.ST.mp; const base=m.outgoing(18,"atk"); ready(); m.trySkillSlot(0); const rallied=m.outgoing(18,"atk");
  m.G.classId="mage"; m.recalcStats(); m.G.p.mp=m.ST.mp; m.G.p.hp=m.ST.hp;
  m.equipSkill(0,"m_frost"); m.G.cd.m_frost=0; const em=foe(); ready(); m.trySkillSlot(0); const frost=em.hp<500 && em.slow===0.4;
  m.equipSkill(0,"m_blink"); m.G.cd.m_blink=0; const mz=m.G.p.z; ready(); m.trySkillSlot(0); const blinked=+(m.G.p.z-mz).toFixed(2); const inv=m.G.p.invT;
  m.equipSkill(0,"m_ward"); m.G.cd.m_ward=0; ready(); m.trySkillSlot(0); const barrier=m.G.buff.barrier;
  m.equipSkill(0,"m_focus"); m.G.cd.m_focus=0; const plain=m.outgoing(30,"m_nova"); ready(); m.trySkillSlot(0); const focused=m.outgoing(30,"m_nova");
  m.G.classId="archer"; m.recalcStats(); m.G.p.mp=m.ST.mp;
  m.clearEnemies();
  const ea=foe(); m.G.p.x=ea.x; m.G.p.z=ea.z-1.5; m.G.p.yaw=0;
  const plainHit=(()=>{ const hp=ea.hp; m.hitEnemy(ea, 20, 0,1,0,{}); return hp-ea.hp; })();
  m.equipSkill(0,"a_mark"); m.G.cd.a_mark=0; ready(); m.trySkillSlot(0); const marked=ea.mark===0.15;
  m.G.forceCrit=0; m.G.forceRoll=1;
  const d0=(()=>{ const hp=ea.hp; m.hitEnemy(ea, 20, 0,1,0,{}); return hp-ea.hp; })();
  outMark={plainHit, markedHit:d0};
  m.equipSkill(0,"a_back"); m.G.cd.a_back=0; const az=m.G.p.z; ready(); m.trySkillSlot(0); const backed=+(m.G.p.z-az).toFixed(2);
  m.equipSkill(0,"a_trap"); m.G.cd.a_trap=0; ready(); m.trySkillSlot(0); const trap=!!(m.G.buff.trap && m.G.buff.trap.r===2.2);
  m.equipSkill(0,"a_steady"); m.G.cd.a_steady=0; ready(); m.trySkillSlot(0); const steady=m.G.buff.steadyN;
  const cd=m.G.cd.a_steady;
  m.equipSkill(1,"a_mark");
  const cdAfter=m.G.cd.a_steady;
  return {shield, afterGuard, slimeSlow, bossSlow, leaped, base:+base.toFixed(2), rallied:+rallied.toFixed(2), frost, blinked, inv:+inv.toFixed(2), barrier, plain:+plain.toFixed(2), focused:+focused.toFixed(2), marked, plainHit, d0, backed, trap, steady, cd:+cd.toFixed(2), cdAfter:+cdAfter.toFixed(2)};
})()`);
check("guard shield absorbs part of a hit", fx.shield>0 && fx.afterGuard<40 && fx.afterGuard>=0, JSON.stringify(fx));
check("shock slows a slime more than a boss", fx.slimeSlow===0.4 && fx.bossSlow===0.15, JSON.stringify(fx));
check("leap is bounded and rally raises melee", fx.leaped>2 && fx.leaped<=4.25 && fx.rallied>fx.base, JSON.stringify(fx));
check("mage frost, blink, shield and focus differ", fx.frost===true && fx.blinked>1.5 && fx.blinked<=3.25 && fx.inv<=0.15 && fx.barrier>0 && fx.focused>fx.plain, JSON.stringify(fx));
check("archer mark, backstep, trap and steady are distinct", fx.marked===true && fx.d0>fx.plainHit && fx.backed<0 && fx.backed>=-3.05 && fx.trap===true && fx.steady===3, JSON.stringify(fx));
check("swapping slots does not reset the cooldown", fx.cd>0 && Math.abs(fx.cd-fx.cdAfter)<0.05, JSON.stringify(fx));
const keep=await ev(`(()=>{
  const m=window.__mengyu;
  const old={ver:4,classId:"swordsman",level:8,gold:100,xp:0,hasSave:true,foo:"keep-skill",growth:{skills:{swordsman:{atk:3,sk1:4,sk2:2}},talents:{swordsman:["s_edge"]}}};
  const mig=m.migrateSave(JSON.parse(JSON.stringify(old)));
  m.G.save=mig; m.G.classId="swordsman";
  const bad=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:5,save:{ver:5,classId:"swordsman",level:10,hasSave:true,growth:{skills:{swordsman:{atk:9}},talents:{},loadout:{swordsman:["s_spin","s_spin","nope"]}}}}));
  const ok=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:5,save:{ver:5,classId:"mage",level:6,gold:20,xp:0,hasSave:true,foo:"still",growth:{skills:{mage:{atk:1,sk1:2,sk2:0}},talents:{},loadout:{mage:["m_nova","m_miss","m_frost"]}}}}));
  return {rank:m.skillRank("swordsman","s_spin"), sk1:mig.growth.skills.swordsman.sk1, foo:mig.foo, load:mig.growth.loadout.swordsman, bad:bad.ok, keep:ok.ok&&ok.save.foo==="still"&&ok.save.growth.loadout.mage[2]==="m_frost"};
})()`);
check("old skill ranks survive and bad loadouts are refused", keep.sk1===4 && keep.rank===4 && keep.foo==="keep-skill" && keep.bad===false && keep.keep===true, JSON.stringify(keep));
await cdp.send("Emulation.setDeviceMetricsOverride",{width:360,height:640,deviceScaleFactor:1,mobile:true},sessionId);
await ev(`window.__mengyu.openGrowth(); true`);
await sleep(250);
const shot=await cdp.send("Page.captureScreenshot",{format:"png"},sessionId);
writeFileSync("/opt/cursor/artifacts/rc6-skillbook.png", Buffer.from(shot.data,"base64"));
const ui=await ev(`(()=>{
  const btns=[...document.querySelectorAll("#growth button")].filter(b=>b.getBoundingClientRect().height>0);
  const small=btns.filter(b=>b.getBoundingClientRect().height<44||b.getBoundingClientRect().width<44).map(b=>b.textContent.slice(0,10));
  const sk=document.getElementById("sk3").getBoundingClientRect();
  return {small, sk:sk.height, n:btns.length, w:window.innerWidth, h:window.innerHeight};
})()`);
check("skill book controls are at least 44px at 360x640", ui.w===360 && ui.small.length===0 && ui.sk>=44, JSON.stringify(ui));
await ev(`window.__mengyu.closeGrowth(); true`);
await sleep(200);
const hudPort=await cdp.send("Page.captureScreenshot",{format:"png"},sessionId);
writeFileSync("/opt/cursor/artifacts/rc6-hud-360.png", Buffer.from(hudPort.data,"base64"));
await cdp.send("Emulation.setDeviceMetricsOverride",{width:390,height:844,deviceScaleFactor:1,mobile:true},sessionId);
await ev(`window.__mengyu.openGrowth(); true`);
await sleep(200);
const shot2=await cdp.send("Page.captureScreenshot",{format:"png"},sessionId);
writeFileSync("/opt/cursor/artifacts/rc6-skillbook-390.png", Buffer.from(shot2.data,"base64"));
await cdp.send("Emulation.setDeviceMetricsOverride",{width:740,height:360,deviceScaleFactor:1,mobile:true},sessionId);
await ev(`document.documentElement.style.setProperty("--st","18px");document.documentElement.style.setProperty("--sb","12px");document.documentElement.style.setProperty("--sl","28px");window.dispatchEvent(new Event("resize")); window.__mengyu.closeGrowth(); true`);
await sleep(200);
const hud=await ev(`(()=>{
  function b(id){ const r=document.getElementById(id).getBoundingClientRect(); return {h:r.height,w:r.width,t:r.top,l:r.left,r:r.right,b:r.bottom}; }
  const ids=["atk","sk1","sk2","sk3","dg","potH","joy","talk"];
  const box={}; ids.forEach(id=>box[id]=b(id));
  let overlap="";
  const keys=Object.keys(box);
  for(let i=0;i<keys.length;i++) for(let j=i+1;j<keys.length;j++){
    const a=box[keys[i]], c=box[keys[j]];
    if(a.h<1||c.h<1) continue;
    if(!(a.r<=c.l||c.r<=a.l||a.b<=c.t||c.b<=a.t)) overlap+=keys[i]+"/"+keys[j]+" ";
  }
  return {overlap:overlap.trim(), sk3:box.sk3.h, atk:box.atk.h};
})()`);
const land=await cdp.send("Page.captureScreenshot",{format:"png"},sessionId);
writeFileSync("/opt/cursor/artifacts/rc6-hud-land.png", Buffer.from(land.data,"base64"));
check("combat buttons do not overlap and stay tappable", !hud.overlap && hud.sk3>=44 && hud.atk>=44, JSON.stringify(hud));
const buffs=await ev(`(()=>{
  const m=window.__mengyu;
  function ready(){ m.G.p.spinT=0; m.G.p.atkT=0; m.G.p.dashT=0; m.G.p.dodgeT=0; m.G.p.hurtT=0; m.G.p.invT=0; m.G.p.dead=false; m.G.paused=false; m.G.mode="play"; m.G.p.mp=m.ST.mp; }
  m.G.classId="swordsman"; m.G.save.level=10; m.recalcStats(); ready();
  const disk=JSON.parse(localStorage.getItem(m.SAVE_KEY)||"{}");
  m.G.save.rev=disk.rev|0;
  m.persist(m.G.save,{replace:true});
  const equipped=m.equipSkill(0,"s_guard"); m.G.cd.s_guard=0;
  const before={load:m.loadoutOf("swordsman").slice(), mp:m.G.p.mp, act:m.G.mode, dead:m.G.p.dead, paused:m.G.paused, lv:m.G.save.level};
  m.trySkillSlot(0);
  const guard=m.G.buff.guard;
  const cd=m.G.cd.s_guard;
  m.G.p.hp=1; m.damagePlayer(500, 0, 0);
  const afterDeath={guard:m.G.buff.guard, trap:m.G.buff.trap, cd:m.G.cd.s_guard, dead:m.G.p.dead};
  m.resurrect();
  m.G.classId="archer"; m.recalcStats(); ready();
  m.G.p.x=0; m.G.p.z=28; m.equipSkill(0,"a_trap"); m.G.cd.a_trap=0; m.trySkillSlot(0);
  const trap=!!m.G.buff.trap;
  m.startAdventure("mage", false);
  const afterClass={trap:m.G.buff.trap, guard:m.G.buff.guard};
  m.G.save.level=8; m.recalcStats(); ready();
  m.equipSkill(0,"m_ward"); m.G.cd.m_ward=0; m.trySkillSlot(0);
  const ward=m.G.buff.barrier;
  m.startDungeon("normal");
  m.endDungeon("leave");
  return {equipped, before, guard, cd:+cd.toFixed(2), afterDeath, trap, afterClass, ward, afterLeave:m.G.buff.barrier, dun:m.DUN.on};
})()`);
check("shield, trap and ward clear on death, class change and dungeon exit", buffs.guard>0 && buffs.afterDeath.dead===true && buffs.afterDeath.guard===0 && buffs.afterDeath.cd>0 && buffs.trap===true && buffs.afterClass.trap==null && buffs.ward>0 && buffs.afterLeave===0 && buffs.dun===false, JSON.stringify(buffs));
const err=logs.filter(l=>!/deprecated with r150/.test(l));
check("no console errors", err.length===0, err.join(" | ")||"none");
console.log("FX", JSON.stringify(fx,null,2));
}finally{ try{ ws.close(); chrome.kill(); }catch(e){} }
console.log("FAILS", fail.length?fail.join(", "):"none");
setTimeout(()=>process.exit(fail.length?1:0),200);
