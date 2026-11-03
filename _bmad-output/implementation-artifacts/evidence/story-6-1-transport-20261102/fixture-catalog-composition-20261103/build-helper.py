import os,json,subprocess
root='/home/administrator/projects/hexalith/projects'
env=os.environ.copy();env.update(json.load(open('/tmp/story61-prerequisite-dev-mapping-20261102.json'))['buildPreparation']['environment'])
env.update({'MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','DOTNET_PROCESSOR_COUNT':'2','UseHexalithProjectReferences':'true','UseNuGetDeps':'false','EnablePrivateCatalogInvocationProof':'true'})
for m in ['EventStore','Tenants','Folders','Commons','Conversations','Memories','FrontComposer','Parties','McpCli']:
 env['Hexalith'+m+'Root']=root+'/references/Hexalith.'+m
env['HexalithTenantsBasePath']=root+'/references/Hexalith.Tenants/src'
import sys
raise SystemExit(subprocess.call(sys.argv[1:],cwd=root,env=env))
