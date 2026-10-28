"""Use the actual producer environment, never a reconstructed mapping."""
from run import *
import shutil
import xml.etree.ElementTree as ET

prepared = json.loads((EVIDENCE/'isolated-source-preparation.stdout').read_text())
assert prepared['status'] == 'prepared'
env = prepared['buildPreparation']['environment']
assert env['Configuration'] == 'Debug'
workspace = WORK/'projects'
owner = workspace/'references/Hexalith.Conversations'
mapping = WORK/'source/source-mapping'
retained = EVIDENCE/'generated-source-mapping'
assert not retained.exists()
shutil.copytree(mapping, retained)
(EVIDENCE/'generated-input-hashes.json').write_text(json.dumps({str(p.relative_to(mapping)):digest(p) for p in sorted(mapping.rglob('*')) if p.is_file()}, indent=2)+'\n')

build_args = ['-c', 'Debug', '-m:1', '-p:NuGetAudit=false', '-p:MinVerVersionOverride=1.0.0', '-v:minimal']
for project, label in [('Contracts', 'contracts'), ('Server', 'server')]:
    path = f'tests/Hexalith.Conversations.{project}.Tests/Hexalith.Conversations.{project}.Tests.csproj'
    run(label+'-build', ['dotnet', 'build', path, *build_args], owner, env, check=False)
    records = json.loads((EVIDENCE/'commands.json').read_text())
    record = next(r for r in records if r['label'] == label+'-build')
    if record['exit_code'] != 0:
        continue
    assembly = f'tests/Hexalith.Conversations.{project}.Tests/bin/Debug/net10.0/Hexalith.Conversations.{project}.Tests.dll'
    filters = ['PlatformEnrollmentMappingTest'] if project == 'Contracts' else [
        'ConversationAdvisoryMetadataQueryHandlerTest', 'ConversationResolutionMetadataQueryHandlerTest',
        'ConversationQueryRegistrationTest', 'ConversationTenantAccessRegistrationTest',
        'ConversationResolutionMetadataProjectionTest', 'ConversationResolutionMetadataRecoveryTest']
    command = ['dotnet', assembly]
    for name in filters:
        command += ['-class', '*'+name]
    command += ['-result-xml', str(EVIDENCE/(label+'-tests.xml')), '-failSkips']
    # These tests build their own fixture mappings. Launch with no inherited producer imports;
    # all production assemblies above were compiled using the exact producer environment.
    assert not any(k in os.environ for k in env), 'Unexpected ambient preparation imports'
    run(label+'-tests', command, owner, check=False)

run('isolated-package-refusal', ['dotnet','tool','run','hexalith-module','--','prepare-build','--manifest','module/hexalith.module-manifest.v2.json','--mode','package','--workspace',str(WORK/'package'),'--output','json'], owner, check=False)
run('projects-contracts-actual-mapping', ['dotnet','build','tests/Hexalith.Projects.Contracts.Tests/Hexalith.Projects.Contracts.Tests.csproj',*build_args], workspace, env, check=False)
run('projects-server-actual-mapping', ['dotnet','build','tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj',*build_args], workspace, env, check=False)
run('prepare-command-help', ['dotnet','tool','run','hexalith-module','--','prepare-build','--help'], owner)
run('original-preservation-audit', ['python3', str(EVIDENCE/'run.py'), 'audit'])
