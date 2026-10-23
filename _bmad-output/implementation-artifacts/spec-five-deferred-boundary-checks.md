---
title: 'Complete five deferred boundary verification items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred obligations leave Folders response classification and release/qualification boundary verification incomplete: malformed-success fixtures, unlisted HTTP failure mapping, exact G-6 repository/argument controls, obsolete historical P1R replay in current CI, and ServiceDefaults package dependency agreement.

**Approach:** Make the Folders adapters return Unavailable for malformed or unexpected successful responses and Denied for otherwise unlisted failures; test the actual generated-client exception path. Require complete unique G-6 repository bindings and exact validator arguments. Keep routine P1R replay on the selected tuple while preserving manually executable historical tests and records. Verify the existing ServiceDefaults dependency manifest against a freshly built package and retain regression controls. Resolve exactly these five ledger entries, independently review, commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Baseline: bfbf05e41cc81b993fd9de382cbb2d2474e5b3fd (canonical revision captured by git before implementation). Clean main; no intent gaps. Changes are small local adapter/gate fixes and existing verification fixtures, without a new product API. The explicitly requested Git push is authorized. Package versions, historical evidence, owner decisions and sprint scheduling remain outside this work.
- Code map: Server/Folders/FoldersProject{FileReference,Folder}Directory.cs hold the two status maps; Project{FileReference,Folder}DirectoryTests.cs use the generated HTTP client. tests/tools/check_g6_packet_gitlinks.py and its two gate fixture modules cover historical packet binding. ci.yml and run-ci-workflow-gates.ps1 define the P1R job; test_p1r_candidate_evidence.py retains selected and historical classes. test_release_package_tools.py and tools/release-packages.json define package integrity expectations.
- Architecture requires safe denial at owner boundaries and package-only CI. The historical Folders.Client 1.0.0 failure is now on a newer pin; exercise current published behavior and the precise malformed-success fallback without downgrading dependencies.
- Verification: focused Python fixtures, workflow policy gate/mutations, freshly built server adapter tests via xUnit assembly, and a real ServiceDefaults package validation. No live G-6 or P1R acceptance is claimed.

- AppHost baseline: `aspire start --non-interactive --apphost src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj` succeeded. `aspire describe --apphost ... --format Json` showed identity parameters waiting for input and dependent services Waiting; the exact started AppHost was stopped successfully. No complete topology readiness is claimed.
- Source Debug and package-mode Release server builds both succeeded with zero warnings/errors. Each built test assembly passed all 81 targeted adapter cases with no skips, including malformed JSON and unexpected statuses at both Folder hops.
- Python gitlink/candidate/package fixtures passed 42 tests; archived packet reference fixtures passed 8. Selected P1R replay passed 8. Workflow policy gate and its positive plus two P1R command mutations passed. Fresh ServiceDefaults package dependencies exactly match the existing manifest, including all six OpenTelemetry 1.20.0 dependencies; package inventory companions were synthetic and no full-family archive proof is claimed.
- The first Python run found only a mismatch in the new assertion's expected diagnostic text; corrected it to the actual `dependency graph differs` and reran successfully.

## Review Triage Log

- BH1 — medium, patched: duplicate-path rejection was masked by the fixture's duplicate name. A distinct-name/same-path case now independently exercises path uniqueness.
- BH2 — low, patched: whitespace-only names were accepted. Identity validation now requires a non-whitespace name and a negative fixture covers it.
- BH3 — low, patched: the standalone check did not require a canonical root revision. It now validates the full lowercase SHA shape; actual root-source qualification remains the full validator's responsibility. Missing, empty, truncated, uppercase and non-string revisions are rejected.
- BH4 — low, rejected: the archive mutation fixture deliberately tests stale archive rejection and does not evaluate the catalog. The existing release path (`release.config.cjs` -> `run-package-dependency-gate.ps1 -SkipPack`) already checks real archive dependencies against the manifest. Separate fresh Debug and Release package checks establish present agreement; adding another catalog evaluator would duplicate that release boundary. No catalog-regression protection is claimed for the hermetic archive fixture.
- BH5 — low, patched: the selected replay does not read the rollback tag. Removed the unnecessary v3.70.1 fetch and its exact invariant.
- BH6 — low, patched: manual historical replay guidance now includes full-history preparation, all three explicit tag refspecs, and the full-module invocation. Historical records and tests remain unchanged.
- BH7 — low, patched: appended exact verification commands and retained output/archive locations below; frozen intent is unchanged. No review finding was deferred.
- BH8 — low, patched: a fresh reproduction needs the source restore graph before the no-restore Debug build. Added the actual initial Debug build (implicit restore), with its retained baseline log, before the final no-restore build.

## Implementation Notes — Reproducible Verification

Run from `/home/administrator/projects/hexalith/projects`. Logs from the completed checks are retained locally at the paths shown. These temporary logs and ignored package archives are developer verification outputs, not qualification acceptance artifacts.

```bash
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug -m:1 -p:NuGetAudit=false > /tmp/five-deferred-server-build.log 2>&1
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Debug --no-restore -m:1 -p:NuGetAudit=false > /tmp/five-deferred-server-build-final.log 2>&1
dotnet build tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -m:1 -p:NuGetAudit=false > /tmp/five-deferred-package-build.log 2>&1
dotnet tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.Tests.dll -class '*ProjectFileReferenceDirectoryTests' -class '*ProjectFolderDirectoryTests' -noColor > /tmp/five-deferred-adapter-tests.log 2>&1
dotnet tests/Hexalith.Projects.Server.Tests/bin/Release/net10.0/Hexalith.Projects.Server.Tests.dll -class '*ProjectFileReferenceDirectoryTests' -class '*ProjectFolderDirectoryTests' -noColor > /tmp/five-deferred-package-adapter-tests.log 2>&1
python3 -m unittest tests/tools/test_check_g6_packet_gitlinks.py tests/tools/test_run_g6_candidate_gate.py tests/tools/test_release_package_tools.py -q > /tmp/five-deferred-python-final.log 2>&1
python3 -m unittest tests/tools/test_g6_packet_references.py -q
python3 -m unittest tests.tools.test_p1r_candidate_evidence.SelectedPublishedEvidenceTests tests.tools.test_p1r_candidate_evidence.SelectedEvidenceMutationTests -v > /tmp/five-deferred-p1r.log 2>&1
pwsh -NoProfile -File tests/tools/run-ci-workflow-gates.ps1 > /tmp/five-deferred-workflow.log 2>&1
python3 -m unittest tests.tools.test_run_ci_workflow_gates.WorkflowGateTests.test_checkout_passes_without_submodules tests.tools.test_run_ci_workflow_gates.WorkflowGateTests.test_p1r_replay_cannot_restore_the_superseded_live_suite_or_omit_mutations -v > /tmp/five-deferred-workflow-tests.log 2>&1
```

ServiceDefaults was built by the server builds above. The actual archives are retained under `artifacts/five-deferred-service-defaults/` and `artifacts/five-deferred-service-defaults-release/` (version `91.92.93-ci.1`). Reproduce the packs with:

```bash
dotnet pack src/Hexalith.Projects.ServiceDefaults/Hexalith.Projects.ServiceDefaults.csproj --configuration Debug --no-restore -o artifacts/five-deferred-service-defaults -p:HexalithProjectsPackageVersion=91.92.93-ci.1 -p:RepositoryCommit=bfbf05e41cc81b993fd9de382cbb2d2474e5b3fd > /tmp/five-deferred-service-pack.log 2>&1
dotnet pack src/Hexalith.Projects.ServiceDefaults/Hexalith.Projects.ServiceDefaults.csproj --configuration Release --no-build --no-restore -p:UseHexalithProjectReferences=false -o artifacts/five-deferred-service-defaults-release -p:HexalithProjectsPackageVersion=91.92.93-ci.1 -p:RepositoryCommit=bfbf05e41cc81b993fd9de382cbb2d2474e5b3fd > /tmp/five-deferred-service-release-pack.log 2>&1
```

The following exact check was also retained as `/tmp/five-deferred-package-agreement.py`, with output in `/tmp/five-deferred-package-agreement.log`. The four synthetic companion archives satisfy inventory only; the ServiceDefaults archive is the real freshly packed output in each configuration.

```python
import shutil
import tempfile
from pathlib import Path
from tests.tools.test_release_package_tools import write_candidates, validate_packages
version = '91.92.93-ci.1'
sha = 'bfbf05e41cc81b993fd9de382cbb2d2474e5b3fd'
for directory in ('five-deferred-service-defaults', 'five-deferred-service-defaults-release'):
    archive = Path('artifacts') / directory / f'Hexalith.Projects.ServiceDefaults.{version}.nupkg'
    with tempfile.TemporaryDirectory() as temporary:
        output = Path(temporary)
        write_candidates(output, version, sha)
        shutil.copyfile(archive, output / archive.name)
        packages, _, _ = validate_packages(output, version, sha)
        service = next(item for item in packages if item.package_id == 'Hexalith.Projects.ServiceDefaults')
        print(f'PASS: {archive}: {service.dependencies}')
```

Execute it with `PYTHONPATH=. python3 /tmp/five-deferred-package-agreement.py > /tmp/five-deferred-package-agreement.log`.

- Final post-review run passed 43/43 gitlink/candidate/package tests in 14.124 seconds, and final workflow positive/mutation controls passed 2/2. The independent reviewer confirmed the review corrections, the reproduced identity-guard mutations, and the verification appendix against retained logs and archives. No unresolved introduced issue or deferred review finding remains. `git diff --check` and commitlint passed; the remote was fetched and main had zero divergence before commit.
