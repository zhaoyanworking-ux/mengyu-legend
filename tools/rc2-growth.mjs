// RC-2 growth smoke. Headless Chrome, not a phone.
import { spawn } from "node:child_process";
import { writeFileSync } from "node:fs";
const URL = process.argv[2] || "http://127.0.0.1:8765/index.html";
const PORT = 9246;
const fail=[];
function sleep(ms){ return new Promise(r=>setTimeout(r,ms)); }
class CDP{
  constructor(ws){ this.ws=ws; this.id=0; this.pending=new Map();
    ws.addEventListener("message",ev=>{ const msg=JSON.parse(ev.data); if(msg.id&&this.pending.has(msg.id)){ const {resolve,reject}=this.pending.get(msg.id); this.pending.delete(msg.id); if(msg.error) reject(new Error(JSON.stringify(msg.error))); else resolve(msg.result); } }); }
  send(m,p={},s){ const id=++this.id; return new Promise((resolve,reject)=>{ this.pending.set(id,{resolve,reject}); const o={id,method:m,params:p}; if(s)o.sessionId=s; this.ws.send(JSON.stringify(o)); }); }
}
const chrome=spawn("google-chrome",["--headless=new","--disable-gpu","--no-sandbox","--disable-dev-shm-usage","--use-angle=swiftshader","--enable-webgl",`--remote-debugging-port=${PORT}`,"--window-size=1100,720","--user-data-dir=/tmp/chrome-rc2-"+Date.now(),"about:blank"],{stdio:"ignore"});
let browser; for(let i=0;i<50;i++){ try{ browser=await (await fetch(`http://127.0.0.1:${PORT}/json/version`)).json(); break; }catch{ await sleep(200);} }
if(!browser){ console.error("no chrome"); process.exit(1); }
const ws=new WebSocket(browser.webSocketDebuggerUrl); await new Promise(r=>ws.addEventListener("open",r));
const cdp=new CDP(ws);
const logs=[];
const {targetId}=await cdp.send("Target.createTarget",{url:"about:blank"});
const {sessionId}=await cdp.send("Target.attachToTarget",{targetId,flatten:true});
const S=sessionId;
await cdp.send("Page.enable",{},S); await cdp.send("Runtime.enable",{},S);
cdp.ws.addEventListener("message",ev=>{
  const msg=JSON.parse(ev.data);
  if(msg.method==="Runtime.exceptionThrown") logs.push(((msg.params.exceptionDetails.exception&&msg.params.exceptionDetails.exception.description)||"").slice(0,240));
  if(msg.method==="Runtime.consoleAPICalled" && msg.params.type==="error") logs.push(msg.params.args.map(a=>a.value||a.description||"").join(" ").slice(0,200));
});
async function ev(e){ const r=await cdp.send("Runtime.evaluate",{expression:e,awaitPromise:true,returnByValue:true},S); if(r.exceptionDetails) throw new Error((r.exceptionDetails.exception&&r.exceptionDetails.exception.description)||r.exceptionDetails.text); return r.result.value; }
function check(n,ok,x){ console.log((ok?"PASS ":"FAIL ")+n+(x?" "+x:"")); if(!ok) fail.push(n); }
async function shot(name){ const {data}=await cdp.send("Page.captureScreenshot",{format:"png"},S); writeFileSync("/opt/cursor/artifacts/"+name, Buffer.from(data,"base64")); }

await cdp.send("Emulation.setDeviceMetricsOverride",{width:390,height:844,deviceScaleFactor:1,mobile:true},S);
await cdp.send("Page.navigate",{url:URL+"?shot=play"},S);
for(let i=0;i<80;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
await sleep(500);

const grow=await ev(`(async()=>{
  const m=window.__mengyu;
  const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  function prep(id, level, gold){
    m.startAdventure(id, id==="swordsman" && !m.G.save.growth);
    if(m.G.classId!==id) m.startAdventure(id, false);
    m.G.save.level=level; m.G.save.gold=gold;
    m.G.save.equip={weapon:null,armor:null,acc:null};
    m.G.p.dead=false; m.G.mode="play"; m.G.paused=false; m.G.dlg=null;
    m.recalcStats();
  }
  function hit(base, key, heavy){
    m.G.forceCrit=0; m.G.forceRoll=1;
    m.recalcStats();
    const e=m.spawnEnemy("slime", 8, 28, 1, "slope");
    e.hp=e.max=100000; e.dead=false; e.state="stun"; e.t=999;
    const hp=e.hp;
    m.hitEnemy(e, m.outgoing(base, key, !!heavy), 0, 1, 0, {});
    const dmg=hp-e.hp; e.dead=true;
    return dmg;
  }
  m.startAdventure("swordsman", true);
  m.G.save.level=12; m.G.save.gold=5000;
  m.G.save.equip={weapon:null,armor:null,acc:null};
  m.G.save.foo="keep-me";
  m.recalcStats();
  m.persist(m.G.save);
  const baseHp=m.ST.hp;
  const base=hit(18*0.85,"atk",false);
  document.getElementById("btnGrow").click();
  await sleep(450);
  const open=document.getElementById("growth").classList.contains("on");
  const up=document.querySelector("#growth [data-up='atk']");
  const uh=up.getBoundingClientRect().height;
  const text=document.getElementById("growBody").innerText;
  const gold0=m.G.save.gold;
  const direct=m.upgradeSkill("atk");
  up.click();
  const afterDouble={gold:m.G.save.gold, rank:m.skillRank("swordsman","atk"), direct, level:m.G.save.level, dis:up.disabled, note:m.G.saveNote, err:m.G.saveErr, html:up.outerHTML.slice(0,160)};
  for(let i=0;i<4;i++){ await sleep(450); document.querySelector("#growth [data-up='atk']").click(); }
  const rank5=m.skillRank("swordsman","atk");
  const dmg5=hit(18*0.85,"atk",false);
  document.querySelector("#growth [data-tn='s_break']").click();
  const breakFirst=m.G.save.growth.talents.swordsman.slice();
  document.querySelector("#growth [data-tn='s_edge']").click();
  await sleep(450);
  document.querySelector("#growth [data-tn='s_break']").click();
  const edge=m.G.save.growth.talents.swordsman.slice();
  const dmgEdge=hit(18*0.85,"atk",false);
  const dmgHeavy=hit(18*0.85,"atk",true);
  const poorGold=m.G.save.gold;
  m.G.save.gold=10; m.renderGrowth();
  await sleep(450);
  const denied=m.upgradeSkill("sk1");
  const poor={gold:m.G.save.gold, sk1:m.skillRank("swordsman","sk1"), denied};
  m.G.save.gold=poorGold; m.renderGrowth();
  document.getElementById("growRespec").click();
  const asked=document.getElementById("confirm").classList.contains("on");
  const goldAsk=m.G.save.gold;
  document.getElementById("cfNo").click();
  await sleep(40);
  const cancel={gold:m.G.save.gold, talents:m.G.save.growth.talents.swordsman.slice()};
  await sleep(450);
  document.getElementById("growRespec").click();
  document.getElementById("cfYes").click();
  await sleep(40);
  const confirmed={gold:m.G.save.gold, talents:m.G.save.growth.talents.swordsman.slice(), rank:m.skillRank("swordsman","atk")};
  const swordSkills=JSON.parse(JSON.stringify(m.G.save.growth.skills.swordsman));
  m.startAdventure("mage", false);
  m.G.save.level=12; m.G.save.equip={weapon:null,armor:null,acc:null}; m.recalcStats();
  const mageRank=m.skillRank("mage","atk");
  const swordStill=m.skillRank("swordsman","atk");
  m.openGrowth();
  document.querySelector("#growth [data-tn='m_focus']").click();
  await sleep(450);
  document.querySelector("#growth [data-tn='m_chant']").click();
  const mageSpell=hit(30,"atk",false);
  const mageCd=m.cdScale(7,"skill");
  m.closeGrowth();
  m.G.paused=false; m.G.p.mp=999; m.G.cd.sk1=0; m.G.p.dead=false; m.G.mode="play";
  m.trySkill(1); const cdNow=m.G.cd.sk1;
  m.startAdventure("archer", false);
  m.G.save.level=12; m.G.save.equip={weapon:null,armor:null,acc:null}; m.recalcStats();
  const hp0=m.ST.hp;
  m.openGrowth();
  document.querySelector("#growth [data-tn='a_light']").click();
  await sleep(450);
  document.querySelector("#growth [data-tn='a_steady']").click();
  m.recalcStats();
  const hp1=m.ST.hp;
  const dg=m.cdScale(1.2,"dg");
  m.closeGrowth();
  m.G.cd.dg=0; m.G.p.dead=false; m.G.mode="play"; m.G.paused=false;
  m.tryDodge();
  const dodgeCd=m.G.cd.dg;
  const isolated={
    mageTal:m.G.save.growth.talents.mage.slice(),
    archTal:m.G.save.growth.talents.archer.slice(),
    swordTal:m.G.save.growth.talents.swordsman.slice(),
    swordRank:m.skillRank("swordsman","atk")
  };
  m.persist(m.G.save);
  const again=m.loadSave();
  const dup=again.growth.skills.swordsman.atk===swordStill && again.growth.talents.archer.length===2;
  const foo=again.foo;
  const disk=JSON.parse(localStorage.getItem(m.SAVE_KEY));
  disk.rev=again.rev+4; disk.gold=1; disk.growth.skills.swordsman.atk=1;
  localStorage.setItem(m.SAVE_KEY, JSON.stringify(disk));
  m.G.save.gold=7777;
  const stale=m.persist(m.G.save);
  const disk2=JSON.parse(localStorage.getItem(m.SAVE_KEY));
  const newer=JSON.stringify(disk2);
  disk2.ver=7; localStorage.setItem(m.SAVE_KEY, JSON.stringify(disk2));
  const loaded=m.loadSave();
  const untouched=localStorage.getItem(m.SAVE_KEY)===JSON.stringify(disk2);
  const badRank=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:3,save:{ver:3,classId:"swordsman",level:12,hasSave:true,growth:{skills:{swordsman:{atk:9}},talents:{}}}}));
  const badTal=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:3,save:{ver:3,classId:"swordsman",level:2,hasSave:true,growth:{skills:{},talents:{swordsman:["s_edge","s_break","s_haste","s_guard"]}}}}));
  const round=m.previewImport(m.exportSaveText(again));
  m.G.forceCrit=null; m.G.forceRoll=null;
  return {base, dmg5, dmgEdge, dmgHeavy, mul:m.ST.mul, baseHp, open, uh, text:text.slice(0,80), afterDouble, rank5, breakFirst, edge, poor,
    cancel, confirmed, swordSkills, mageRank, swordStill, mageSpell, mageCd:+mageCd.toFixed(3), cdNow:+cdNow.toFixed(3),
    hp0, hp1, dg:+dg.toFixed(3), dodgeCd:+dodgeCd.toFixed(3), isolated, dup, foo, stale, diskGold:disk2.gold, diskRank:JSON.parse(newer).growth.skills.swordsman.atk,
    loaded:loaded&&loaded.level, untouched, badRank:badRank.ok, badTal:badTal.ok, round:round.ok&&round.save.growth.skills.swordsman.atk===5};
})()`);
check("panel opens from the craft button", grow.open===true, "early h "+grow.uh);
check("double click buys one rank", grow.afterDouble.rank===1 && grow.afterDouble.gold===5000-40, JSON.stringify(grow.afterDouble));
check("five ranks cost the published sum", grow.rank5===5, "rank "+grow.rank5);
const expect=(rank,melee,heavy)=>Math.max(1,Math.round(18*0.85*(1+0.08*rank)*melee*heavy*grow.mul));
check("rank and talent change the same hit", grow.base===expect(0,1,1) && grow.dmg5===expect(5,1,1) && grow.dmgEdge===expect(5,1.18,1), JSON.stringify({base:grow.base,r5:grow.dmg5,edge:grow.dmgEdge,mul:grow.mul,want:[expect(0,1,1),expect(5,1,1),expect(5,1.18,1)]}));
check("third hit adds the heavy talent", grow.dmgHeavy===expect(5,1.18,1.12), "heavy "+grow.dmgHeavy+" want "+expect(5,1.18,1.12));
check("prerequisite blocks and then accepts", grow.breakFirst.length===0 && grow.edge.indexOf("s_edge")>=0, JSON.stringify({first:grow.breakFirst,edge:grow.edge}));
check("poor gold does not upgrade", grow.poor.denied===false && grow.poor.gold===10 && grow.poor.sk1===0, JSON.stringify(grow.poor));
check("respec cancel keeps gold and talents", grow.cancel.gold===grow.poor.gold || grow.cancel.talents.indexOf("s_edge")>=0, JSON.stringify(grow.cancel));
check("respec confirm clears talents, keeps the skill rank, spends once", grow.confirmed.talents.length===0 && grow.confirmed.rank===5 && grow.confirmed.gold===grow.cancel.gold-(80+15*12), JSON.stringify(grow.confirmed));
check("other class ranks stay isolated", grow.mageRank===0 && grow.swordStill===5, JSON.stringify({mage:grow.mageRank,sword:grow.swordStill}));
check("mage skill cooldown matches the talent", Math.abs(grow.mageCd-6.16)<0.001 && Math.abs(grow.cdNow-grow.mageCd)<0.001, JSON.stringify({cd:grow.mageCd,now:grow.cdNow,spell:grow.mageSpell}));
check("archer survival changes health and dodge cooldown", grow.hp1>grow.hp0 && Math.abs(grow.dg-0.984)<0.001 && Math.abs(grow.dodgeCd-grow.dg)<0.001, JSON.stringify({hp0:grow.hp0,hp1:grow.hp1,dg:grow.dg,now:grow.dodgeCd}));
check("reload does not duplicate points", grow.dup===true && grow.isolated.swordRank===5 && grow.isolated.archTal.length===2 && grow.isolated.mageTal.length===2);
check("unknown field and stale tab are preserved", grow.foo==="keep-me" && grow.stale===false && grow.diskRank===1, JSON.stringify({foo:grow.foo,stale:grow.stale,disk:grow.diskRank}));
check("higher version is not loaded or overwritten", grow.loaded==null && grow.untouched===true);
check("import rejects illegal ranks and point budgets", grow.badRank===false && grow.badTal===false && grow.round===true);

await ev(`(()=>{ const m=window.__mengyu; localStorage.clear(); m.startAdventure("swordsman", true); m.G.save.level=12; m.G.save.gold=2000; m.G.paused=false; m.openGrowth(); return true; })()`);
await sleep(200);
await shot("rc2-growth-port.png");
const lay=await ev(`(()=>{
  function box(el){ const r=el.getBoundingClientRect(); return {w:r.width,h:r.height,x:r.x,y:r.y,r:r.right,b:r.bottom}; }
  function hit(a,b){ return !(a.r<=b.x||b.r<=a.x||a.b<=b.y||b.b<=a.y); }
  const btns=[...document.querySelectorAll("#growth button")].filter(b=>b.getBoundingClientRect().height>0);
  const small=btns.filter(b=>b.getBoundingClientRect().height<44||b.getBoundingClientRect().width<44).map(b=>b.textContent.slice(0,12));
  const menu=[...document.querySelectorAll("#menuRow button")].map(b=>({id:b.id,...box(b)}));
  let overlap="";
  for(let i=0;i<menu.length;i++) for(let j=i+1;j<menu.length;j++) if(hit(menu[i],menu[j])) overlap+=menu[i].id+"/"+menu[j].id+" ";
  return {small, n:btns.length, overlap:overlap.trim(), grow:box(document.getElementById("btnGrow"))};
})()`);
check("portrait growth controls are 44px and menu buttons do not overlap", lay.small.length===0 && !lay.overlap && lay.grow.h>=44, JSON.stringify(lay));

await cdp.send("Emulation.setDeviceMetricsOverride",{width:740,height:360,deviceScaleFactor:1,mobile:true},S);
await ev(`document.documentElement.style.setProperty("--st","20px");document.documentElement.style.setProperty("--sb","16px");document.documentElement.style.setProperty("--sl","36px");document.documentElement.style.setProperty("--sr","20px");window.dispatchEvent(new Event("resize"));true`);
await sleep(400);
await shot("rc2-growth-land.png");
const land=await ev(`(()=>{
  const btns=[...document.querySelectorAll("#growth button")].filter(b=>b.getBoundingClientRect().height>0);
  const small=btns.filter(b=>{ const r=b.getBoundingClientRect(); return r.height<44||r.width<44||r.left<0||r.top<0; }).length;
  const sheet=document.querySelector("#growth .sheet").getBoundingClientRect();
  return {small, sheetTop:+sheet.top.toFixed(1), n:btns.length};
})()`);
check("landscape growth panel stays on screen with 44px targets", land.small===0 && land.sheetTop>=0, JSON.stringify(land));

const err=logs.filter(l=>!/deprecated with r150/.test(l));
check("no console errors", err.length===0, err.join(" | ")||"none");
console.log("MEASURED", JSON.stringify({base:grow.base,rank5:grow.dmg5,withTalent:grow.dmgEdge,heavy:grow.dmgHeavy,mageCd:grow.mageCd,archerHp:[grow.hp0,grow.hp1],dodge:grow.dodgeCd,respecGold:[grow.cancel.gold,grow.confirmed.gold]},null,2));
console.log("FAILS", fail.length?fail.join(", "):"none");
ws.close(); chrome.kill();
setTimeout(()=>process.exit(fail.length?1:0),200);
