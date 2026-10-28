"""Repeat the same narrow lanes with effective environment and output closure binding."""
from run import *

workspace = WORK/'projects'
owner = workspace/'references/Hexalith.Conversations'
env = json.loads((EVIDENCE/'isolated-source-preparation.stdout').read_text())['buildPreparation']['environment']
run('final-dotnet-resolution',['python3','-c','import os,shutil; p=shutil.which("dotnet"); print(p); print(os.path.realpath(p))'],owner)
run('final-dotnet-info',['dotnet','--info'],owner)
args = ['-c','Debug','-m:1','-p:NuGetAudit=false','-p:MinVerVersionOverride=1.0.0','-v:minimal']
for project in ['Contracts','Server']:
    label = project.lower()
    run('final-'+label+'-build',['dotnet','build',f'tests/Hexalith.Conversations.{project}.Tests/Hexalith.Conversations.{project}.Tests.csproj',*args],owner,env)

def closure():
    return {str(p.relative_to(workspace)):digest(p)
            for name in ['Contracts','Server']
            for p in sorted((owner/f'tests/Hexalith.Conversations.{name}.Tests/bin/Debug/net10.0').rglob('*'))
            if p.is_file()}

before = closure()
started = datetime.datetime.now(datetime.timezone.utc).isoformat()
for project in ['Contracts','Server']:
    label = project.lower()
    names = ['PlatformEnrollmentMappingTest'] if project=='Contracts' else [
        'ConversationAdvisoryMetadataQueryHandlerTest','ConversationResolutionMetadataQueryHandlerTest',
        'ConversationQueryRegistrationTest','ConversationTenantAccessRegistrationTest',
        'ConversationResolutionMetadataProjectionTest','ConversationResolutionMetadataRecoveryTest']
    cmd = ['dotnet',f'tests/Hexalith.Conversations.{project}.Tests/bin/Debug/net10.0/Hexalith.Conversations.{project}.Tests.dll']
    for name in names:
        cmd += ['-class','*'+name]
    cmd += ['-result-xml',str(EVIDENCE/('final-'+label+'-tests.xml')),'-failSkips']
    assert not any(k in os.environ for k in env)
    run('final-'+label+'-tests',cmd,owner)
after = closure()
assert before == after, 'Executable output closure changed during tests'
(EVIDENCE/'executable-closure.json').write_text(json.dumps(dict(
    before_utc=started,after_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
    before=before,after=after,description='All files under both executable output directories, including production/dependency DLLs, native binaries, .deps.json and runtime configuration; equal before and after the repeated focused tests'),indent=2)+'\n')
run('reviewed-source-capture',['python3',str(EVIDENCE/'capture-inputs.py')])
