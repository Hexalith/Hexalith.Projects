import {chromium, expect} from '/home/administrator/projects/hexalith/projects/tests/e2e/node_modules/@playwright/test/index.mjs';
import {readFileSync} from 'node:fs';
const browser = await chromium.launch({executablePath:'/usr/bin/google-chrome'});
try {
 const context = await browser.newContext(); const page = await context.newPage();
 await page.route('http://localhost/**', async route => route.fulfill({contentType:route.request().url().endsWith('module.js')?'text/javascript':'text/html',body:route.request().url().endsWith('module.js')?readFileSync('/home/administrator/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.1/staticwebassets/Microsoft.FluentUI.AspNetCore.Components.lib.module.js'):`<!DOCTYPE html><html lang="en"><title>Fluent controls check</title><main style="display:grid;grid-template-columns:minmax(0,1fr);gap:24px;width:264px">
<label for="conversation" id="conversation-label">Conversation ID</label><fluent-text-input id="conversation" aria-labelledby="conversation-label" style="width:100%"></fluent-text-input>
<label for="files" id="files-label">File IDs (unsupported coverage)</label><fluent-textarea id="files" aria-labelledby="files-label" style="width:100%"></fluent-textarea>
<fluent-button id="run" role="button">Evaluate suggestions</fluent-button>
<fluent-link role="link" aria-label="Open project project-a" href="/inspect/projects/project-a">project-a</fluent-link>
<output id="result"></output></main></html>`}));
 await page.goto('http://localhost/check');
 await page.evaluate(async () => {
  const module = await import('/module.js');
  window.Blazor = {navigateTo(path){ window.microNavigation = path; }};
  module.beforeWebStart({}); module.afterWebStarted({registerCustomEventType(){},addEventListener(){}});
  await new Promise(resolve => requestAnimationFrame(resolve));
  for (const [id, name] of [['conversation', 'Conversation ID'], ['files', 'File IDs (unsupported coverage)']]) {
    window.Microsoft.FluentUI.Blazor.Utilities.Attributes.copyToShadow(document.getElementById(id), '.control', 'aria-label', name);
  }
  document.querySelector('#run').addEventListener('click', () => document.querySelector('#result').textContent = 'evaluated');
 });
 console.log('native', (await (await context.newCDPSession(page)).send('Accessibility.getFullAXTree')).nodes.filter(n=>['textbox','button','link'].includes(n.role?.value)).map(n=>[n.role?.value,n.name?.value]));
 console.log('playwright', await page.locator('main').ariaSnapshot());
 await page.getByRole('textbox',{name:'Conversation ID'}).fill('conversation-a');
 await expect(page.getByRole('textbox',{name:'Conversation ID'})).toHaveValue('conversation-a');
 await page.getByRole('textbox',{name:/File IDs/}).fill('unsupported-file');
 await expect(page.getByRole('textbox',{name:/File IDs/})).toHaveValue('unsupported-file');
 await page.getByRole('button',{name:'Evaluate suggestions',exact:true}).focus();
 await page.getByRole('button',{name:'Evaluate suggestions',exact:true}).press('Enter');
 await expect(page.locator('#result')).toHaveText('evaluated');
 await page.getByRole('link',{name:'Open project project-a',exact:true}).click();
 await expect.poll(() => page.evaluate(() => window.microNavigation)).toBe('/inspect/projects/project-a');
 console.log(JSON.stringify({package:'5.0.1',browser:browser.version(),textInputRoleFill:true,textAreaRoleFill:true,buttonKeyboardActivation:true,linkNavigation:true}));
} finally { await browser.close(); }
