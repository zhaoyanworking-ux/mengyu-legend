// RC-3 gear/bag smoke plus a real RC-1 page on the same origin. Not a phone.
import { spawn, execFileSync } from "node:child_process";
import { writeFileSync, unlinkSync } from "node:fs";
const base = "http://127.0.0.1:8765/";
const PORT = 9249;
const rc1Path = "/agent/repos/mengyu-legend/rc1-client.html";
const fail=[];
function sleep(ms){ return new Promise(r=>setTimeout(r,ms)); }
class CDP{
  constructor(ws){ this.ws=ws; this.id=0; this.pending=new Map();
    ws.addEventListener("message",ev=>{ const msg=JSON.parse(ev.data); if(msg.id&&this.pending.has(msg.id)){ const {resolve,reject}=this.pending.get(msg.id); this.pending.delete(msg.id); if(msg.error) reject(new Error(JSON.stringify(msg.error))); else resolve(msg.result); }}); }
  send(m,p={},s){ const id=++this.id; return new Promise((resolve,reject)=>{ this.pending.set(id,{resolve,reject}); const o={id,method:m,params:p}; if(s)o.sessionId=s; this.ws.send(JSON.stringify(o)); }); }
}
writeFileSync(rc1Path, execFileSync("git",["show","8f99a0c:index.html"],{cwd:"/agent/repos/mengyu-legend"}));
const chrome=spawn("google-chrome",["--headless=new","--disable-gpu","--no-sandbox","--disable-dev-shm-usage","--use-angle=swiftshader","--enable-webgl",`--remote-debugging-port=${PORT}`,"--window-size=1100,720","--user-data-dir=/tmp/chrome-rc3-"+Date.now(),"about:blank"],{stdio:"ignore"});
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
    if(msg.method==="Runtime.exceptionThrown") logs.push(((msg.params.exceptionDetails.exception&&msg.params.exceptionDetails.exception.description)||"").slice(0,220));
  });
  const ev=async(e)=>{ const r=await cdp.send("Runtime.evaluate",{expression:e,awaitPromise:true,returnByValue:true},sessionId); if(r.exceptionDetails) throw new Error((r.exceptionDetails.exception&&r.exceptionDetails.exception.description)||r.exceptionDetails.text); return r.result.value; };
  await cdp.send("Page.navigate",{url},sessionId);
  for(let i=0;i<80;i++){ await sleep(250); if(await ev(`document.documentElement.getAttribute('data-ready')==='1'`)) break; }
  return {ev, sessionId};
}
function check(n,ok,x){ console.log((ok?"PASS ":"FAIL ")+n+(x?" "+x:"")); if(!ok) fail.push(n); }
try{
await cdp.send("Target.setDiscoverTargets",{discover:true});
const cur=await attach(base+"index.html?shot=play");
await cdp.send("Emulation.setDeviceMetricsOverride",{width:390,height:844,deviceScaleFactor:1,mobile:true},cur.sessionId);
await sleep(400);
const gear=await cur.ev(`(async()=>{
  const m=window.__mengyu; const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  m.startAdventure("swordsman", true);
  m.G.save.level=8; m.G.save.gold=2000; m.G.save.foo="keep-gear";
  m.G.save.equip={weapon:null,armor:null,acc:null};
  const sword={uid:"sword-1",slot:"weapon",rar:4,lv:3,cls:"swordsman",atk:10,def:0,hp:0,mp:0,crit:0,price:100,name:"星辉骑士剑名字很长用来省略",plus:0,locked:false,enhSpent:0};
  const junk={uid:"junk-1",slot:"acc",rar:0,lv:1,atk:0,def:0,hp:0,mp:4,crit:1,price:20,name:"旧护符",plus:0,locked:false,enhSpent:0};
  const rare={uid:"rare-1",slot:"armor",rar:3,lv:5,atk:0,def:4,hp:20,mp:0,crit:0,price:80,name:"史诗护胸",plus:2,locked:true,enhSpent:40};
  m.G.save.bag=[sword,junk,rare];
  m.recalcStats();
  m.openBag();
  const early=document.querySelector("#rarRow button").getBoundingClientRect().height;
  await sleep(40);
  const settled=document.querySelector("#rarRow button").getBoundingClientRect().height;
  document.querySelector("#bagGrid .slot.r4").click();
  const cmp=document.querySelector("#itemTip .cmp")?document.querySelector("#itemTip .cmp").textContent:"";
  const beforeAtk=m.ST.atk;
  function strike(){
    m.G.forceCrit=0; m.G.forceRoll=1; m.recalcStats();
    const e=m.spawnEnemy("slime", 12, 28, 1, "slope");
    e.hp=100000; e.dead=false; const hp=e.hp;
    m.hitEnemy(e, m.outgoing(18*0.85,"atk",false), 0,1,0,{});
    const dmg=hp-e.hp; e.dead=true; return dmg;
  }
  document.getElementById("tEq").click();
  const wornAtk=m.ST.atk; const dmgWorn=strike();
  document.querySelector("#eqSlots .slot").click();
  const gold0=m.G.save.gold; const plus0=m.G.save.equip.weapon.plus;
  document.getElementById("tEnh").click(); document.getElementById("tEnh").click();
  const enh={gold:m.G.save.gold, plus:m.G.save.equip.weapon.plus, atk:m.G.save.equip.weapon.atk, st:m.ST.atk, dmg:strike()};
  document.getElementById("tLock").click();
  const locked=!!m.G.save.equip.weapon.locked;
  m.G.save.gold=5;
  const poor=m.enhanceItem("sword-1");
  const poorState={gold:m.G.save.gold, plus:m.G.save.equip.weapon.plus};
  m.G.save.gold=gold0;
  document.querySelector("#bagGrid .slot").click();
  const sellLocked=document.getElementById("tSell")&&document.getElementById("tSell").disabled;
  document.getElementById("rarRow").querySelector("[data-rar='3']").click();
  const shown=[...document.querySelectorAll("#bagGrid button")].map(b=>b.className);
  document.getElementById("rarRow").querySelector("[data-rar='all']").click();
  document.querySelector("[data-sort='plus']").click();
  const order=[...document.querySelectorAll("#bagGrid button .lv")].map(n=>n.textContent);
  const beforeBulk=m.bulkQuote();
  document.getElementById("btnBulk").click();
  const asked=document.getElementById("confirm").classList.contains("on") && document.getElementById("cfText").textContent;
  document.getElementById("cfNo").click();
  const afterCancel={n:m.G.save.bag.length, gold:m.G.save.gold, quote:m.bulkQuote()};
  document.getElementById("btnBulk").click();
  document.getElementById("cfYes").click();
  await sleep(30);
  const afterYes={n:m.G.save.bag.length, gold:m.G.save.gold, lockedStill:m.G.save.bag.some(it=>it.uid==="rare-1"&&it.locked), worn:!!m.G.save.equip.weapon};
  m.G.save.bag=m.G.save.bag.concat(Array.from({length:40},(_,i)=>({uid:"fill-"+i,slot:"acc",rar:0,lv:1,atk:0,def:0,hp:1,mp:0,crit:0,price:10,name:"填"+i,plus:0,locked:false,enhSpent:0}))).slice(0,30);
  const fullGold=m.G.save.gold;
  const added=m.addItem({uid:"overflow",slot:"acc",rar:1,lv:1,atk:0,def:0,hp:1,mp:0,crit:0,price:50,name:"溢出",plus:0,locked:false,enhSpent:0});
  const full={added, gold:m.G.save.gold, n:m.G.save.bag.length};
  m.persist(m.G.save);
  const again=m.loadSave();
  const round=m.previewImport(m.exportSaveText(again));
  const badPlus=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:4,save:{ver:4,classId:"swordsman",level:8,hasSave:true,bag:[{uid:"x",slot:"weapon",rar:0,lv:1,plus:9,locked:false}]}}));
  const badLock=m.previewImport(JSON.stringify({format:"mengyu-legend-save",schema:4,save:{ver:4,classId:"swordsman",level:8,hasSave:true,bag:[{uid:"x",slot:"weapon",rar:0,lv:1,plus:1,locked:"yes"}]}}));
  await sleep(450);
  const oldSet=localStorage.setItem.bind(localStorage);
  localStorage.setItem=function(k,v){ if(String(k).indexOf("MengYu")===0 && String(k).indexOf(".bak")<0){ const err=new Error("quota"); err.name="QuotaExceededError"; throw err; } return oldSet(k,v); };
  const plusBefore=m.G.save.equip.weapon.plus, goldBefore=m.G.save.gold;
  const quota=m.enhanceItem("sword-1");
  localStorage.setItem=oldSet;
  m.G.classId="mage"; m.G.save.equip.weapon=null; m.recalcStats();
  const mageBare=m.ST.atk;
  m.G.save.equip.weapon=again.equip.weapon; m.recalcStats();
  const mageIgnored=m.ST.atk;
  m.G.classId="swordsman"; m.G.save.equip.weapon=null; m.recalcStats();
  const swordBare=m.ST.atk;
  m.G.save.equip.weapon=again.equip.weapon; m.recalcStats();
  const swordWorn=m.ST.atk;
  return {early, settled, cmp, beforeAtk, wornAtk, dmgWorn, enh, poor, poorState, locked, sellLocked, shown, order, beforeBulk, asked, afterCancel, afterYes, full, kept:again&&again.foo, plus:again&&again.equip.weapon.plus, round:round.ok&&round.save.equip.weapon.plus===enh.plus, badPlus:badPlus.ok, badLock:badLock.ok, quota, plusBefore, plusAfter:m.G.save.equip.weapon.plus, goldBefore, goldAfterQuota:m.G.save.gold, mageBare, mageIgnored, swordBare, swordWorn};
})()`);
check("bag controls stay at least 44px on the first frame", gear.early>=44 && gear.settled>=44, JSON.stringify({early:gear.early,settled:gear.settled}));
check("equip raises attack and the same hit", gear.wornAtk>gear.beforeAtk && gear.dmgWorn>0, JSON.stringify({before:gear.beforeAtk,worn:gear.wornAtk,dmg:gear.dmgWorn,cmp:gear.cmp}));
check("one enhance click raises plus once and keeps the base attack", gear.enh.plus===1 && gear.enh.atk===10 && gear.enh.st>gear.wornAtk && gear.enh.dmg>gear.dmgWorn, JSON.stringify(gear.enh));
check("poor gold does not enhance", gear.poor===false && gear.poorState.gold===5 && gear.poorState.plus===1, JSON.stringify(gear.poorState));
check("lock sticks and blocks the sell button", gear.locked===true && gear.sellLocked===true);
check("rarity filter shows the epic piece", gear.shown.length===1 && gear.shown[0].indexOf("r3")>=0, JSON.stringify(gear.shown));
check("plus sort puts the enhanced piece first", gear.order[0]==="+2", JSON.stringify(gear.order));
check("bulk cancel changes nothing", gear.afterCancel.n===gear.beforeBulk.n+1 || gear.afterCancel.quote.n===gear.beforeBulk.n, JSON.stringify({before:gear.beforeBulk,cancel:gear.afterCancel}));
check("bulk confirm keeps the lock and the worn weapon", gear.afterYes.lockedStill===true && gear.afterYes.worn===true && gear.afterYes.gold>gear.afterCancel.gold, JSON.stringify(gear.afterYes));
check("full bag does not convert the item into gold", gear.full.added===false && gear.full.gold===gear.afterYes.gold && gear.full.n===30, JSON.stringify(gear.full));
check("reload, export and import keep enhance and the extra field", gear.kept==="keep-gear" && gear.plus===1 && gear.round===true);
check("import rejects illegal plus and lock", gear.badPlus===false && gear.badLock===false);
check("quota failure rolls the enhance back", gear.quota===false && gear.plusAfter===gear.plusBefore && gear.goldAfterQuota===gear.goldBefore, JSON.stringify({quota:gear.quota,plus:[gear.plusBefore,gear.plusAfter],gold:[gear.goldBefore,gear.goldAfterQuota]}));
check("wrong class ignores the sword and switching back counts it", gear.mageIgnored===gear.mageBare && gear.swordWorn>gear.swordBare, JSON.stringify({mage:[gear.mageBare,gear.mageIgnored],sword:[gear.swordBare,gear.swordWorn]}));

const raw=await cur.ev(`localStorage.getItem("MengYuLegend_Save_v1")`);
const old=await attach(base+"rc1-client.html?shot=play");
await sleep(800);
const rc1=await old.ev(`(()=>{
  const m=window.__mengyu;
  const before=localStorage.getItem("MengYuLegend_Save_v1");
  const mem={ver:2,classId:"swordsman",level:1,xp:0,gold:1,hasSave:true,quests:{q1:{st:"avail",pr:0}}};
  const wrote=m.persist(mem);
  const after=localStorage.getItem("MengYuLegend_Save_v1");
  m.showTitle();
  document.getElementById("btnNew").click();
  document.getElementById("btnGo").click();
  const asked=document.getElementById("confirm").classList.contains("on");
  document.getElementById("cfYes").click();
  const primary=localStorage.getItem("MengYuLegend_Save_v1");
  const archived=localStorage.getItem("MengYuLegend_Save_v1.newer");
  return {wrote, same:before===after, asked, archivedIsV4:archived&&archived.indexOf('"ver":4')>=0, primaryVer:primary&&JSON.parse(primary).ver, build:m.BUILD.name};
})()`);
check("real RC-1 autosave does not overwrite a v4 save", rc1.build==="RC-1" && rc1.wrote===false && rc1.same===true, JSON.stringify({wrote:rc1.wrote,same:rc1.same,build:rc1.build}));
check("RC-1 new game confirms, archives the raw v4, then writes its own save", rc1.asked===true && rc1.archivedIsV4===true && rc1.primaryVer===2, JSON.stringify({asked:rc1.asked,arch:rc1.archivedIsV4,ver:rc1.primaryVer}));
const restored=await cur.ev(`(async()=>{
  const m=window.__mengyu; const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
  m.showTitle(); await sleep(50);
  const visible=document.getElementById("btnRestore").style.display!=="none";
  document.getElementById("btnRestore").click();
  document.getElementById("cfYes").click();
  await sleep(50);
  const s=m.loadSave();
  return {visible, gold:s&&s.gold, plus:s&&s.equip&&s.equip.weapon&&s.equip.weapon.plus, foo:s&&s.foo, ver:s&&s.ver};
})()`);
check("current build can restore the archived v4 save", restored.visible===true && restored.plus===1 && restored.foo==="keep-gear" && restored.ver===4, JSON.stringify(restored));

await cdp.send("Emulation.setDeviceMetricsOverride",{width:360,height:640,deviceScaleFactor:1,mobile:true},cur.sessionId);
const layout=await cur.ev(`(()=>{
  const m=window.__mengyu;
  m.finishCoach();
  const box=document.getElementById("toasts"); if(box) box.innerHTML="";
  m.startAdventure("swordsman", false);
  m.finishCoach();
  if(box) box.innerHTML="";
  m.G.save.gold=1000000000;
  const piece=m.G.save.bag[0]||m.G.save.equip.weapon;
  if(piece) piece.name="雾石坡北缘拾到的超长传说护甲名号雾石坡北缘拾到的超长传说护甲名号";
  m.G.mode="play"; m.G.paused=false; m.G.p.dead=false;
  m.openBag();
  document.querySelector("#bagGrid button").click();
  const tip=document.getElementById("itemTip");
  tip.scrollIntoView({block:"center"});
  m.refreshHUD();
  const nm=document.querySelector("#itemTip .nm");
  const goldB=document.querySelector("#statBox .st b[title]");
  const goldHud=document.getElementById("goldv");
  const coach=document.getElementById("coach");
  return {
    name:nm&&nm.textContent,
    nameClipped:!!(nm&&nm.scrollWidth>nm.clientWidth+1),
    nameInside:!!(nm&&nm.getBoundingClientRect().right<=tip.getBoundingClientRect().right+1),
    goldTitle:goldB&&goldB.title,
    goldText:goldB&&goldB.textContent,
    goldFits:!!(goldB&&goldB.scrollWidth<=goldB.clientWidth+1),
    hud:goldHud&&goldHud.textContent,
    hudFits:!!(goldHud&&goldHud.scrollWidth<=goldHud.clientWidth+1),
    coachHidden:coach.style.display==="none"
  };
})()`);
await sleep(250);
const {data}=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc3-bag-port.png", Buffer.from(data,"base64"));
const scrolled=await cur.ev(`(()=>{
  const sheet=document.querySelector("#bag .sheet");
  sheet.scrollTop=sheet.scrollHeight;
  const bulk=document.getElementById("btnBulk").getBoundingClientRect();
  const sheetR=sheet.getBoundingClientRect();
  const sb=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sb"))||0;
  return {bulkH:bulk.height, bulkTop:bulk.top, bulkBottom:bulk.bottom, sheetTop:sheetR.top, sheetBottom:sheetR.bottom, vh:innerHeight, sb};
})()`);
const bottomShot=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc3-bag-port-bottom.png", Buffer.from(bottomShot.data,"base64"));
await cur.ev(`document.getElementById("btnBulk").click(); true`);
await sleep(500);
const confirmShot=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc3-bulk-confirm.png", Buffer.from(confirmShot.data,"base64"));
const portFit=await cur.ev(`(()=>{
  const yes=document.getElementById("cfYes").getBoundingClientRect();
  const no=document.getElementById("cfNo").getBoundingClientRect();
  const sb=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sb"))||0;
  const st=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--st"))||0;
  const op=getComputedStyle(document.querySelector("#confirm .sheet")).opacity;
  return {yesH:yes.height, noH:no.height, yesTop:yes.top, yesBottom:yes.bottom, vh:innerHeight, sb, st, op};
})()`);
await cdp.send("Emulation.setDeviceMetricsOverride",{width:740,height:360,deviceScaleFactor:1,mobile:true},cur.sessionId);
await cur.ev(`document.documentElement.style.setProperty("--st","18px");document.documentElement.style.setProperty("--sb","12px");document.documentElement.style.setProperty("--sl","30px");document.documentElement.style.setProperty("--sr","16px");window.dispatchEvent(new Event("resize")); document.getElementById("cfNo").click(); true`);
await sleep(280);
const land=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc3-bag-land.png", Buffer.from(land.data,"base64"));
await cur.ev(`document.getElementById("btnBulk").click(); true`);
await sleep(500);
const landConfirm=await cdp.send("Page.captureScreenshot",{format:"png"},cur.sessionId);
writeFileSync("/opt/cursor/artifacts/rc3-bag-land-confirm.png", Buffer.from(landConfirm.data,"base64"));
const fit=await cur.ev(`(()=>{
  const yes=document.getElementById("cfYes").getBoundingClientRect();
  const bulk=document.getElementById("btnBulk").getBoundingClientRect();
  const sheet=document.querySelector("#bag .sheet").getBoundingClientRect();
  const st=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--st"))||0;
  return {yesH:yes.height, yesTop:yes.top, yesBottom:yes.bottom, bulkH:bulk.height, sheetTop:sheet.top, sheetBottom:sheet.bottom, vh:innerHeight, st};
})()`);
check("large gold and long names stay inside their boxes", layout.goldTitle==="1000000000" && layout.goldText==="10亿" && layout.goldFits && layout.hud==="10亿" && layout.hudFits && layout.nameClipped && layout.nameInside && layout.coachHidden, JSON.stringify(layout));
check("portrait scrolled bulk button and confirm stay inside the safe viewport", scrolled.bulkH>=44 && scrolled.bulkBottom<=scrolled.vh+1 && scrolled.bulkTop>=scrolled.sheetTop-1 && portFit.yesH>=44 && portFit.noH>=44 && portFit.op==="1" && portFit.yesTop>=portFit.st && portFit.yesBottom<=portFit.vh-portFit.sb+1, JSON.stringify({scrolled,portFit}));
check("landscape confirm and bulk buttons stay tappable", fit.yesH>=44 && fit.bulkH>=44 && fit.yesTop>=fit.st && fit.yesBottom<=fit.vh+1 && fit.sheetTop>=0, JSON.stringify(fit));
const err=logs.filter(l=>!/deprecated with r150/.test(l));
check("no console errors", err.length===0, err.join(" | ")||"none");
console.log("GEAR", JSON.stringify({early:gear.early,atk:[gear.beforeAtk,gear.wornAtk,gear.enh.st],dmg:[gear.dmgWorn,gear.enh.dmg],enh:gear.enh,bulk:gear.afterYes,full:gear.full,rc1,restored},null,2));
}finally{
  try{ unlinkSync(rc1Path); }catch(e){}
  try{ ws.close(); chrome.kill(); }catch(e){}
}
console.log("FAILS", fail.length?fail.join(", "):"none");
setTimeout(()=>process.exit(fail.length?1:0),200);
