"""Verify the reviewed declaration with a fresh public producer environment."""
from run import *
import shutil
workspace=WORK/'projects';owner=workspace/'references/Hexalith.Conversations'
manifest=str(workspace/'module/hexalith.module-manifest.v2.json')
run('reviewed-manifest-validation',['dotnet','tool','run','hexalith-module','--','validate','--manifest',manifest,'--output','json'],owner)
run('reviewed-source-preparation',['dotnet','tool','run','hexalith-module','--','prepare-build','--manifest',manifest,'--mode','source','--workspace',str(WORK/'source-reviewed'),'--output','json'],owner)
prepared=json.loads((EVIDENCE/'reviewed-source-preparation.stdout').read_text());env=prepared['buildPreparation']['environment']
assert prepared['status']=='prepared'
shutil.copytree(WORK/'source-reviewed/source-mapping',EVIDENCE/'reviewed-generated-source-mapping')
assert (WORK/'source-reviewed/source-mapping/mapping.json').read_bytes()==(WORK/'source-final/source-mapping/mapping.json').read_bytes()
for layer,classes in [('Contracts',['ProjectAdvisoryContractTests']),('Server',['ProjectAdvisoryQueryTests','DelegatedProjectAdvisoryConversationDirectoryTests','ProjectQueryEnvelopePrincipalBindingTests','ProjectQueryTenantFilterTests'])]:
 label=layer.lower()
 run('reviewed-projects-'+label+'-build',['dotnet','build',f'tests/Hexalith.Projects.{layer}.Tests/Hexalith.Projects.{layer}.Tests.csproj','-c','Debug','-m:1','-p:NuGetAudit=false','-p:MinVerVersionOverride=1.0.0','-v:minimal'],workspace,env)
 command=['dotnet',f'tests/Hexalith.Projects.{layer}.Tests/bin/Debug/net10.0/Hexalith.Projects.{layer}.Tests.dll']
 for name in classes:command+=['-class','*'+name]
 command+=['-result-xml',str(EVIDENCE/('reviewed-projects-'+label+'-tests.xml')),'-failSkips']
 run('reviewed-projects-'+label+'-tests',command,workspace)
run('reviewed-package-preparation',['dotnet','tool','run','hexalith-module','--','prepare-build','--manifest',manifest,'--mode','package','--workspace',str(WORK/'package-reviewed'),'--output','json'],owner,check=False)
