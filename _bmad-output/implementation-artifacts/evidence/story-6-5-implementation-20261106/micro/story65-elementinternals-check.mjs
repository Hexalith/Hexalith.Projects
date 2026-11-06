import {chromium} from '/home/administrator/projects/hexalith/projects/tests/e2e/node_modules/@playwright/test/index.mjs';
import {AxeBuilder} from '/home/administrator/projects/hexalith/projects/tests/e2e/node_modules/@axe-core/playwright/dist/index.mjs';
import {readFileSync} from 'node:fs';
const b=await chromium.launch({executablePath:'/usr/bin/google-chrome'});
try {
 const context=await b.newContext(); const p=await context.newPage();
 await p.route('http://localhost/**',async r=>r.fulfill({contentType:r.request().url().endsWith('module.js')?'text/javascript':'text/html',body:r.request().url().endsWith('module.js')?readFileSync('/home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.1/staticwebassets/Microsoft.FluentUI.AspNetCore.Components.lib.module.js'):'<!DOCTYPE html><html lang="en"><title>Role check</title><main><fluent-checkbox aria-label="Test checkbox"></fluent-checkbox><fluent-button>Test button</fluent-button><fluent-link href="/test">Link text</fluent-link></main></html>'}));
 await p.goto('http://localhost/test');
 await p.evaluate(async()=>{const m=await import('/module.js');m.beforeWebStart({});m.afterWebStarted({registerCustomEventType(){},addEventListener(){}});});
 const c=await context.newCDPSession(p); const before=await c.send('Accessibility.getFullAXTree');
 console.log(JSON.stringify({browser:b.version(),nativeRoles:before.nodes.map(n=>[n.role?.value,n.name?.value]).filter(x=>['checkbox','button','link'].includes(x[0])),playwrightCheckboxes:await p.getByRole('checkbox').count()}));
 await p.evaluate(()=>{for(const [tag,role]of[['fluent-checkbox','checkbox'],['fluent-button','button'],['fluent-link','link']])document.querySelector(tag).setAttribute('role',role);const checkbox=document.querySelector('fluent-checkbox');checkbox.setAttribute('aria-checked','false');checkbox.addEventListener('change',e=>e.target.setAttribute('aria-checked',String(e.target.checked)));document.querySelector('fluent-link').setAttribute('aria-label','Test link');});
 await p.getByRole('checkbox',{name:'Test checkbox'}).check();
 console.log(JSON.stringify({explicitRoleCheckPassed:await p.getByRole('checkbox',{name:'Test checkbox'}).isChecked()}));
 const x=await new AxeBuilder({page:p}).include('main').withTags(['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa']).analyze();
 console.log(JSON.stringify({violations:x.violations.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.map(n=>({html:n.html,summary:n.failureSummary}))}))},null,2));
}finally{await b.close();}
