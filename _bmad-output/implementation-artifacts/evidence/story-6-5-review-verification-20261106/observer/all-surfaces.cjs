const { chromium } = require(process.cwd()+'/tests/e2e/node_modules/playwright');
const { AxeBuilder } = require(process.cwd()+'/tests/e2e/node_modules/@axe-core/playwright');
const fs = require('node:fs');
const {execFileSync} = require('node:child_process');
const readline = require('node:readline');
const access=JSON.parse(fs.readFileSync(process.env.STORY65_ACCESS_FILE,'utf8'));
const protectedText=JSON.parse(fs.readFileSync(process.env.STORY65_PROTECTED_FILE,'utf8'));
const output=process.env.STORY65_OUTPUT;
const debug=process.env.STORY65_ORCA_DEBUG;
const pause=ms=>new Promise(r=>setTimeout(r,ms));
const report={qualification:'actual-assistive-technology-and-native-browser-zoom',humanTestPerformed:false,realOidc:true,routes:[],checks:[],failures:[],screenshots:[]};
function safe(value){const text=JSON.stringify(value);if(protectedText.some(v=>v&&text.includes(v))||/eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}/.test(text))throw new Error('Protected material reached an observer receipt');return value;}
function debugText(){try{return fs.readFileSync(debug,'utf8');}catch{return '';}}
function speechFrom(text){return text.split('\n').filter(s=>s.includes('SPEECH OUTPUT: ')).map(s=>s.slice(s.indexOf('SPEECH OUTPUT: ')+15).replace(/ \{.*$/,''));}
async function key(name){execFileSync('xdotool',['key','--clearmodifiers',name],{env:process.env,timeout:10000,stdio:'ignore'});await pause(250);}
async function capture(label,action){const before=debugText().length;await action();await pause(300);const speech=speechFrom(debugText().slice(before));const entry=safe({step:label,announcements:speech});report.checks.push(entry);return speech;}
function record(name,condition){report.checks.push({assertion:name,passed:!!condition});if(!condition)report.failures.push(name);}
async function openSettled(page,path,surface){
 const socket=page.waitForEvent('websocket',{predicate:s=>s.url().includes('/_blazor'),timeout:30000});
 await page.goto(new URL(path,access.uiUrl).href);await socket;
 const snapshot=surface==='inventory'?'project-inventory-row-snapshot':'project-open-snapshot';
 const button=surface==='inventory'?'project-inventory-restart':'project-detail-reread';
 await page.getByTestId(snapshot).first().waitFor();
 const prior=await page.getByTestId(snapshot).first().textContent();let ready=false;
 for(let attempt=0;attempt<8&&!ready;attempt++){
  await page.getByTestId(button).click();
  try{await page.waitForFunction(({testId,prior})=>{const e=document.querySelector('[data-testid="'+testId+'"]');return e&&e.textContent!==prior;},{testId:snapshot,prior},{timeout:2000});ready=true;}catch{}
 }
 if(!ready)throw new Error('Post-hydration read did not produce fresh rendered evidence');
 await page.getByTestId('project-navigation-loading').waitFor({state:'hidden'});
 await page.getByTestId(snapshot).first().waitFor();
 if(surface==='inventory')await page.getByTestId('project-inventory-tenant-scope').filter({hasText:access.tenant}).waitFor();
 const after=await page.getByTestId(snapshot).first().textContent();const date=/As of:\s*([0-9TZ:+.\-]+)/;const beforeAsOf=date.exec(prior)?.[1],afterAsOf=date.exec(after)?.[1];record(surface+': observed response timestamp changed',!!beforeAsOf&&!!afterAsOf&&beforeAsOf!==afterAsOf);report.checks.push({step:surface+': post-hydration readiness',webSocketObserved:true,beforeAsOf,afterAsOf});
}
function screenshotRoot(path){const code="import gi;gi.require_version('Gdk','3.0');from gi.repository import Gdk;w=Gdk.get_default_root_window();p=Gdk.pixbuf_get_from_window(w,0,0,w.get_width(),w.get_height());p.savev("+JSON.stringify(path)+",'png',[],[])";execFileSync('/usr/bin/python3',['-c',code],{env:process.env,stdio:'ignore',timeout:10000});}
async function contrastTarget(page,target){
 const element=page.locator(target.map(part=>Array.isArray(part)?part.join(' >> '):part).join(' >> ')).first();await element.evaluate(e=>e.scrollIntoView({block:'center',inline:'nearest'}));
 const styles=await element.evaluate(e=>{const cs=getComputedStyle(e);const chain=[];for(let current=e;current;){const s=getComputedStyle(current);chain.push({tag:current.tagName.toLowerCase(),part:current.getAttribute('part'),backgroundColor:s.backgroundColor,backgroundImage:s.backgroundImage,opacity:s.opacity,filter:s.filter,mixBlendMode:s.mixBlendMode});const root=current.getRootNode();current=current.parentElement||(root instanceof ShadowRoot?root.host:null);}return {nodeText:e.textContent?.replace(/\s+/g,' ').trim().slice(0,240),resolvedTag:e.tagName.toLowerCase(),resolvedPart:e.getAttribute('part'),foreground:cs.color,fontSize:cs.fontSize,fontWeight:cs.fontWeight,chain};});
 const rgba=color=>{const x=color.match(/[\d.]+/g)?.map(Number);return x?.length>=3?[x[0],x[1],x[2],x.length===4?x[3]:1]:null;};
 let bg=[255,255,255];let valid=styles.chain.every(x=>x.backgroundImage==='none'&&x.opacity==='1'&&x.filter==='none'&&x.mixBlendMode==='normal');
 for(const c of styles.chain.slice().reverse()){const x=rgba(c.backgroundColor);if(!x){valid=false;continue;}bg=bg.map((v,i)=>x[i]*x[3]+v*(1-x[3]));}
 const raw=rgba(styles.foreground);const fg=raw?bg.map((v,i)=>raw[i]*raw[3]+v*(1-raw[3])):[0,0,0];if(!raw)valid=false;
 const lum=rgb=>rgb.map(v=>{const n=v/255;return n<=0.04045?n/12.92:((n+0.055)/1.055)**2.4;}).reduce((v,n,i)=>v+n*[0.2126,0.7152,0.0722][i],0);
 const l1=lum(fg),l2=lum(bg),ratio=(Math.max(l1,l2)+.05)/(Math.min(l1,l2)+.05);const pixels=parseFloat(styles.fontSize),weight=parseFloat(styles.fontWeight);const required=pixels>=24||(pixels>=18.6666667&&weight>=700)?3:4.5;
 return safe({target,nodeText:styles.nodeText,resolvedTag:styles.resolvedTag,resolvedPart:styles.resolvedPart,foreground:styles.foreground,effectiveBackgroundRgb:bg,fontSize:styles.fontSize,fontWeight:styles.fontWeight,ancestorBackgroundWalk:styles.chain,solidOpaqueColorPath:valid,contrastRatio:ratio,minimumRequired:required,passes:valid&&ratio>=required,method:'W3C WCAG 2.2 G18 relative luminance and computed solid rendered colors',source:'https://www.w3.org/WAI/WCAG22/Techniques/general/G18'});
}
async function focused(page,cdp){const focus=await page.evaluate(()=>{let active=document.activeElement;while(active?.shadowRoot?.activeElement)active=active.shadowRoot.activeElement;if(!active||active===document.body||active===document.documentElement)return null;const root=active.getRootNode();const host=root instanceof ShadowRoot?root.host:active;const r=active.getBoundingClientRect();return {testId:host.closest('[data-testid]')?.getAttribute('data-testid')??null,tag:active.tagName.toLowerCase(),hostTag:host.tagName.toLowerCase(),visibleFocus:active.matches(':focus-visible')||host.matches(':focus-visible'),withinViewport:r.width>0&&r.height>0&&r.top>=-1&&r.left>=-1&&r.bottom<=innerHeight+1&&r.right<=innerWidth+1};});if(!focus)return null;const {nodes}=await cdp.send('Accessibility.getFullAXTree');const node=nodes.find(n=>n.properties?.some(p=>p.name==='focused'&&p.value.value===true)&&n.role?.value!=='RootWebArea');return safe({...focus,role:String(node?.role?.value??''),name:String(node?.name?.value??'').slice(0,160)});}
async function tabTo(page,cdp,target,label){for(let i=0;i<55;i++){await capture(`${label}: Tab ${i+1}`,()=>key('Tab'));const f=await focused(page,cdp);report.checks.push(safe({step:`${label}: focus ${i+1}`,focus:f}));if(f?.testId===target)return true;}record(label+': target reached',false);return false;}
async function scan(page,label){const result=await new AxeBuilder({page}).include('[role="main"]').withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa']).analyze();const checkData=node=>({target:node.target,checks:[...node.any,...node.all,...node.none].map(c=>({id:c.id,data:Object.fromEntries(['fgColor','bgColor','contrastRatio','expectedContrastRatio','fontSize','fontWeight','messageKey'].filter(k=>c.data&&typeof c.data==='object'&&k in c.data).map(k=>[k,c.data[k]]))})).filter(c=>Object.keys(c.data).length)});const receipt=safe({surface:label,testEngine:result.testEngine,timestamp:result.timestamp,passes:result.passes.length,colorContrastPassed:result.passes.some(x=>x.id==='color-contrast'),incomplete:result.incomplete.map(x=>({id:x.id,impact:x.impact,nodes:x.nodes.map(checkData)})),violations:result.violations.map(x=>({id:x.id,impact:x.impact,nodes:x.nodes.map(checkData)})),geometry:await page.evaluate(()=>({width:innerWidth,height:innerHeight,devicePixelRatio,cssZoom:getComputedStyle(document.documentElement).zoom,horizontalOverflow:document.documentElement.scrollWidth>document.documentElement.clientWidth+1}))});receipt.contrastDeterminations=[];for(const rule of result.incomplete){if(rule.id==='color-contrast'){for(const node of rule.nodes)receipt.contrastDeterminations.push(await contrastTarget(page,node.target));}}receipt.unresolvedIncompleteRuleCount=receipt.incomplete.filter(rule=>rule.id!=='color-contrast'||rule.nodes.some(n=>!receipt.contrastDeterminations.find(c=>JSON.stringify(c.target)===JSON.stringify(n.target))?.passes)).length;record(label+': no unresolved incomplete rules',receipt.unresolvedIncompleteRuleCount===0);fs.writeFileSync(output+'/'+label+'-axe.json',JSON.stringify(receipt,null,2),{mode:0o600});record(label+': axe no critical/serious violations',!receipt.violations.some(x=>['critical','serious'].includes(x.impact)));record(label+': no horizontal overflow',!receipt.geometry.horizontalOverflow);return receipt;}

async function readSpokenBlock(page, label, locator, expected, steps=18) {
 const start=debugText().length;
 await locator.focus();await pause(600);
 execFileSync('xdotool',['key','--clearmodifiers','--repeat','3','--delay','100','KP_Insert+a'],{env:process.env,timeout:10000,stdio:'ignore'});
 await pause(500);
 for(let line=0;line<steps;line++)await key('Down');
 await pause(800);
 const announcements=speechFrom(debugText().slice(start));
 report.checks.push(safe({step:label,method:'Actual Orca sticky browse mode and native Down navigation from the focused content anchor',announcements}));
 record(label+': actual spoken content',expected.test(announcements.join(' ')));
 await key('KP_Insert+a');await pause(300);
}

(async()=>{
 const context=await chromium.launchPersistentContext(process.env.STORY65_ZOOM_PROFILE,{executablePath:'/usr/bin/google-chrome',headless:false,viewport:null,ignoreHTTPSErrors:true,env:process.env,args:['--no-sandbox','--disable-gpu','--disable-dev-shm-usage','--ozone-platform=x11','--force-renderer-accessibility=complete','--window-size=1280,1000']});
 try{
  const page=context.pages()[0];await page.goto(access.uiUrl);await page.locator('#username').fill(access.username);await page.locator('#password').fill(access.password);await page.locator('#kc-login').click();await page.waitForURL(url=>url.origin===new URL(access.uiUrl).origin);await page.getByTestId('project-inventory-row').filter({hasText:'project-a'}).waitFor();
  const cdp=await context.newCDPSession(page);await cdp.send('Accessibility.enable');
  console.log(JSON.stringify({stage:'authenticated',safeMetadata:true}));
  const lines=readline.createInterface({input:process.stdin});for await(const line of lines){if(line==='run')break;}lines.close();
  const windows=execFileSync('xdotool',['search','--onlyvisible','--class','google-chrome'],{encoding:'utf8',timeout:10000}).trim().split('\n');execFileSync('xdotool',['windowfocus','--sync',windows.at(-1)],{timeout:10000,stdio:'ignore'});
  const modes=[['100',1],['200',2]];
  for(const [mode,ratio] of modes){
   await capture('native zoom reset',()=>key('ctrl+0'));
   if(ratio===2){for(let i=0;i<5;i++)await capture('native zoom increment '+(i+1),()=>key('ctrl+equal'));}
   await page.waitForFunction(r=>devicePixelRatio===r,ratio);await key('Escape');
   report.checks.push({step:'native browser zoom '+mode,metrics:await page.evaluate(()=>({width:innerWidth,height:innerHeight,devicePixelRatio,cssZoom:getComputedStyle(document.documentElement).zoom}))});
   for(const surface of ['inventory','detail','suggestions']){
    console.log(JSON.stringify({stage:'surface-start',surface,nativeZoom:Number(mode)}));
    const path=surface==='inventory'?'/inspect/projects':'/inspect/projects/project-a';
    const begin=debugText().length;await openSettled(page,path,surface==='inventory'?'inventory':'detail');
    if(surface==='inventory')await page.getByTestId('project-inventory-row').filter({hasText:'project-a'}).waitFor();else await page.getByTestId('project-open-snapshot').waitFor();
    await pause(1000);report.checks.push(safe({step:`${surface} ${mode}: route announcement`,announcements:speechFrom(debugText().slice(begin))}));
    await capture(`${surface} ${mode}: heading navigation`,async()=>{await key('ctrl+Home');await key('h');});
    if(surface==='inventory'){
     if(await tabTo(page,cdp,'project-inventory-filter-archived',`inventory ${mode}: archived filter`)){
      await capture(`inventory ${mode}: activate archived filter`,()=>key('space'));await page.getByTestId('project-inventory-empty').waitFor();record(`inventory ${mode}: empty inventory reachable by keyboard`,true);
     }
     if(await tabTo(page,cdp,'project-inventory-restart',`inventory ${mode}: restart`))await capture(`inventory ${mode}: activate restart`,()=>key('Return'));
     await openSettled(page,'/inspect/projects','inventory');await page.getByTestId('project-inventory-row').filter({hasText:'project-a'}).waitFor();
    }else{
     if(await tabTo(page,cdp,'project-detail-reread',`${surface} ${mode}: reread`)){
      const old=await page.getByTestId('project-open-snapshot').innerText();await capture(`${surface} ${mode}: activate reread`,()=>key('Return'));await page.getByTestId('project-open-snapshot').waitFor();record(`${surface} ${mode}: reread control remains focused`,(await focused(page,cdp))?.testId==='project-detail-reread');
     }
     if(surface==='detail'){
      for(const name of ['Context observation','Conversation-start observation']){
       const button=page.getByRole('button',{name,exact:true});for(let i=0;i<18;i++){const f=await focused(page,cdp);if(f?.name===name)break;await capture(`detail ${mode}: next observation control`,()=>key('Tab'));}await capture(`detail ${mode}: expand ${name}`,()=>key('space'));
      }
     }else{
      for(let i=0;i<24;i++){const f=await focused(page,cdp);if(f?.name==='Project suggestions')break;await capture(`suggestions ${mode}: next section`,()=>key('Tab'));}
      await capture(`suggestions ${mode}: expand Project suggestions`,()=>key('space'));await page.getByTestId('project-resolution-trace-workbench').waitFor();
      if(await tabTo(page,cdp,'project-resolution-trace-mode',`suggestions ${mode}: attachments mode`))await capture(`suggestions ${mode}: toggle attachments`,()=>key('space'));
      await page.getByRole('textbox',{name:/File IDs/}).waitFor();
      if(await tabTo(page,cdp,'project-resolution-trace-file-id',`suggestions ${mode}: file input`)){await capture(`suggestions ${mode}: enter unsupported file ID`,async()=>{execFileSync('xdotool',['type','--clearmodifiers','--delay','40','unsupported-file'],{stdio:'ignore',timeout:10000});await pause(300);});}
      if(await tabTo(page,cdp,'project-resolution-trace-run',`suggestions ${mode}: evaluate`))await capture(`suggestions ${mode}: evaluate by keyboard`,()=>key('Return'));
      await page.getByTestId('project-resolution-trace-outcome').waitFor();record(`suggestions ${mode}: incomplete coverage displayed`,(await page.getByTestId('project-resolution-trace-outcome').innerText()).includes('coverage incomplete'));
      for(let i=0;i<12;i++){const f=await focused(page,cdp);if(f?.name==='Evaluation and coverage')break;await capture(`suggestions ${mode}: next result section`,()=>key('Tab'));}
      await capture(`suggestions ${mode}: expand coverage`,()=>key('space'));await page.getByTestId('project-resolution-trace-coverage').waitFor();
     }
    }
    record(`${surface} ${mode}: Safe Metadata retained`,!(await page.locator('body').innerText()).includes('Synthetic project project-a'));
    const axe=await scan(page,`${surface}-native-${mode}`);console.log(JSON.stringify({stage:'surface-complete',surface,nativeZoom:Number(mode),violationCount:axe.violations.length,incompleteRuleCount:axe.incomplete.length}));report.routes.push({surface,path,nativeZoom:Number(mode),axeReceipt:`${surface}-native-${mode}-axe.json`,incompleteRules:axe.incomplete.map(x=>x.id)});
    const image=output+`/${surface}-native-${mode}.png`;screenshotRoot(image);report.screenshots.push({surface,zoom:Number(mode),privatePath:image});
    if(surface==='inventory') {
      await readSpokenBlock(page,`inventory ${mode}: spoken row observation`,page.getByTestId('project-inventory-row-link').first(),/State.{0,60}(?:Unavailable|Complete|Partial|Denied)/i,22);
    } else if(surface==='detail') {
      for(const heading of ['Open observation','Context observation','Conversation-start observation']) {
        await readSpokenBlock(page,`detail ${mode}: spoken ${heading}`,page.getByRole('button',{name:heading,exact:true}),/State.{0,60}(?:Unavailable|Complete|Partial|Denied)/i,22);
      }
    } else {
      await readSpokenBlock(page,`suggestions ${mode}: spoken coverage limits`,page.getByRole('button',{name:'Evaluation and coverage',exact:true}),/Incomplete.{0,80}True|coverage incomplete|No exhaustive match claim/i,18);
    }
   }
  }
  record('Orca produced actual speech output',report.checks.some(x=>x.announcements?.some(y=>/Projects|Project project|observation|coverage|check box|button/.test(y))));
  report.result=report.failures.length?'failed':'passed';
 }catch(error){report.result='failed';report.failureType=error.name;report.failureStep=String(error.message).replaceAll(access.password,'[redacted]').slice(0,300);}
 finally{await context.close();fs.writeFileSync(output+'/browser-assistive-checks.json',JSON.stringify(safe(report),null,2),{mode:0o600});console.log(JSON.stringify({stage:'complete',result:report.result,failureCount:report.failures.length}));}
})().catch(()=>{console.error('Owned assistive browser run failed');process.exitCode=1;});
