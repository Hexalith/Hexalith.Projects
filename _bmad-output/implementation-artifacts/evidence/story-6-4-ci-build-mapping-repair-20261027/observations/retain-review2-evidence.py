from pathlib import Path
import gzip,hashlib,json,shutil,subprocess
root=Path('/home/administrator/projects/hexalith/projects'); raw=Path('/tmp/story64-ci-repair-20261027'); target=root/'_bmad-output/implementation-artifacts/evidence/story-6-4-ci-build-mapping-repair-20261027'; builds=root/'references/Hexalith.Builds'
old=json.loads((target/'candidate-source-hashes.json').read_text())
locks=[]
for row in old:
 if row['owner']=='Hexalith.Conversations' and row['path'].startswith('tests/compatibility/MinimalModule/locks/'):
  data=(root/'references'/row['owner']/row['path']).read_bytes(); digest=hashlib.sha256(data).hexdigest(); assert digest==row['sha256'],row['path']; locks.append(dict(path=row['path'],sha256=digest,unchanged=True))
(raw/'review2-successor-lock-preservation.json').write_text(json.dumps(locks,indent=2)+'\n')
commands=[
 dict(label='review2-producer-build',command=['dotnet','build','test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj','-c','Debug','--no-restore','-m:1','-p:NuGetAudit=false','-p:MinVerVersionOverride=1.0.0'],cwd=str(builds),exit=0,observation='review2-producer-build.log.gz'),
 dict(label='review2-producer-helper8',command=['python3','-m','unittest','discover','-s','Github/prepare-module-build/tests','-v'],cwd=str(builds),exit=0,observation='review2-producer-helper-tests.log.gz'),
 dict(label='review2-preparation11',command=['dotnet','test/Hexalith.Builds.Module.Tests/bin/Debug/net10.0/Hexalith.Builds.Module.Tests.dll','-class','Hexalith.Builds.ModuleTool.Tests.Workspace.BuildPreparationTests','-failSkips','-result-xml',str(raw/'review2-producer-preparation-tests.xml'),'-noLogo'],cwd=str(builds),exit=0,observation='review2-producer-preparation-tests.log.gz'),
 dict(label='review2-cancellation-control1',command=['dotnet','test/Hexalith.Builds.Module.Tests/bin/Debug/net10.0/Hexalith.Builds.Module.Tests.dll','-method','*CancellationAfterMaterializationStartsRemovesOwnedOutputAsync','-failSkips','-result-xml',str(raw/'review2-cancellation-control.xml'),'-noLogo'],cwd=str(builds),exit=0,observation='review2-cancellation-control.log.gz'),
 dict(label='review2-compatibility-overlay3',command=['python3','-m','unittest','discover','-s','tests/tooling','-p','test_minimal_module_compatibility_overlay.py','-v'],cwd=str(raw/'standalone-compatibility'),exit=0,observation='review2-compatibility-overlay-tests.log.gz',fixture='Identical current source files and the exact recorded db897eac catalog blob; direct catalog only, no dependency initialization or lock generation.'),
]
(raw/'review2-verification-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
for filename in ('full-gate-summary.json','focused-summary.json','builds-module-full.xml','builds-focused.xml','pre-review-patch-source-hashes.json'):
 shutil.copy2(Path('/tmp/story64-ci-parent-20261027')/filename,raw/('parent-pre-review-'+filename))
for filename in ('builds-module-full.log','builds-focused.log'):
 shutil.copy2(Path('/tmp/story64-ci-parent-20261027')/filename,raw/('parent-pre-review-'+filename))
for path in (raw/'verification-commands.json',target/'observations/verification-commands.json'):
 rows=json.loads(path.read_text()); rows=[row for row in rows if not str(row.get('label','')).startswith('review2-')]; rows.extend(commands); path.write_text(json.dumps(rows,indent=2)+'\n')
readme=target/'README.md'; text=readme.read_text(); text+='''
## Review follow-up: targeted verification only

Normal Builds CI and release validation now execute the preparation helper's behavioral
suite. Its metadata path receives the same real-path/containment checks as imports,
and a boolean mapping mode refuses before environment export. Blank service arguments
use the existing HXC001 Usage phase/category. A portable catalog process double blocks
on a synchronization primitive and signals a file after mapping output is owned;
the test cancels the real preparation and requires that output to be removed. It runs
in an isolated native xUnit apphost child, without sleeps, new skips or production API
changes.

The compatibility overlay now evaluates the direct pinned Builds catalog independently
and refuses any EventStore version drift from its deliberately qualified selection.
The test derives expected versions from that catalog and verifies a mutated temporary
catalog fails. The isolated compatibility CI job initializes only its direct Builds
checkout. The exact db897eac9f2819220b32d237109d7255218b46a9 catalog bytes were read with
`git show` for the disposable standalone test layout; no user nested submodule was
initialized. All four successor locks and all 17 original frozen files remain unchanged.
No dependencies or locks were upgraded or regenerated.

After these review edits, only the affected producer build, preparation file's 11
cases (including the post-ownership cancellation and four usage cases), helper's eight
cases, and compatibility overlay's three cases ran; all passed. Exact commands and
fresh outputs are in `observations/review2-verification-commands.json` and the matching
review2 logs/XML. Earlier npm, package-build, seven-assertion and source-build evidence
above is retained as prior checkpoint evidence; none of those wider lanes was rerun
for this follow-up.

The parent independently recorded 2,174 full producer cases: 2,172 passed and two
existing layout/OS-dependent FAIL_SKIP failures. Its clean affected lane passed all
425 cases. Their original summaries, logs, XML and pre-review source hashes are
retained separately as `observations/parent-pre-review-*`; they describe the checkpoint
before this follow-up and are not presented as post-fix full verification.
'''; readme.write_text(text)
