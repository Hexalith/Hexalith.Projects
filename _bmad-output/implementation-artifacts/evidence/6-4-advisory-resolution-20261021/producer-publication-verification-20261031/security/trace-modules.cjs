// Record module resolutions only; no credentials, URLs, or lifecycle interception.
const fs = require('node:fs');
const { registerHooks } = require('node:module');
const out = process.env.STORY64_MODULE_TRACE;
const matches = /\/(?:glob|minimatch|ip-address|undici|node-gyp|socks|socks-proxy-agent|@semantic-release\/npm|@semantic-release\/github|@npmcli\/map-workspaces|@npmcli\/package-json)\//;
registerHooks({resolve(specifier, context, nextResolve) {
  const result = nextResolve(specifier, context);
  if (out && matches.test(result.url)) fs.appendFileSync(out, JSON.stringify({specifier,parent:context.parentURL,resolved:result.url})+'\n');
  return result;
}});
