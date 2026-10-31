// Load configured plugins without executing any lifecycle hook, then probe actual GitHub fetch wrapper over loopback.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
import http from 'node:http';
const cwd = process.argv[2];
const pkg = JSON.parse(fs.readFileSync(path.join(cwd,'package.json')));
const require = createRequire(path.join(cwd,'package.json'));
const plugins = (await import(pathToFileURL(path.join(cwd,'node_modules/semantic-release/lib/plugins/index.js')))).default;
const events = [];
const logger = {log(...x){events.push(x)},error(){},success(){},warn(){}};
await plugins({cwd,options:{...pkg.release},logger,env:{}}, new Map());
const {toOctokitOptions} = await import(pathToFileURL(path.join(cwd,'node_modules/@semantic-release/github/lib/octokit.js')));
const req = toOctokitOptions({githubToken:'local-fixture',proxy:undefined});
const server = http.createServer((request,response)=>{response.writeHead(200,{'Content-Type':'application/json'});response.end('{"fixture":"loopback"}');});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
try {
 const reply = await req.request.fetch(`http://127.0.0.1:${server.address().port}/`);
 assert.deepEqual(await reply.json(),{fixture:'loopback'});
} finally {await new Promise(r=>server.close(r));}
const resolveFrom = (file,name) => createRequire(path.join(cwd,file)).resolve(name);
const results={node:process.version,builtinUndici:process.versions.undici,plugins:pkg.release.plugins.map(x=>Array.isArray(x)?x[0]:x),loadedHooks:events,githubFetchLoopback:'passed',githubUndici:resolveFrom('node_modules/@semantic-release/github/lib/octokit.js','undici'),nodeGypUndici:resolveFrom('node_modules/npm/node_modules/node-gyp/lib/download.js','undici'),npmVersion:JSON.parse(fs.readFileSync(path.join(cwd,'node_modules/npm/package.json'))).version};
assert.ok(results.githubUndici.includes('/node_modules/undici/'));
console.log(JSON.stringify(results,null,2));
