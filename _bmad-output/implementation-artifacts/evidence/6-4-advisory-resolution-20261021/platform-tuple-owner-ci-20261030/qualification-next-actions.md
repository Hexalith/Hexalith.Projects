# Exact next qualification actions

Story 6.4 remains in progress. The authorized branch transport, manual workflow registration and exact-candidate CI operation is complete. There is a passing preflight; there is no accepted current-main runtime or published consumer tuple.

## Identities that must remain distinct

- Selected published candidate: Platform `6a186754102b33c3cfea823efc55e49264efbb84`, Builds/tool **4.38.3**, exact Builds source `d1931b1e803144048d927fff16cb8119afde541e`. Intended catalog: Tenants 5.11.1, EventStore 3.128.4, Conversations 1.3.1, FrontComposer 4.7.1, Memories 3.0.3. Its catalog SHA-256 is `501d72650e3a42afc8fde0345a69208669e7fabdb98b6300f20afca07585d405`.
- Passing diagnostic: run **41014652010**, attempt **1**, job **123928122261**. The workflow head is `1e4e447c84109f2d8ba6e7e2357953e5cdbef9a4`; its explicit checkout and clean input fingerprint bind the Platform candidate above. Workflow head and checked-out candidate are different fields.
- Remote main at **2026-10-30T06:37:55.783235Z**: Platform `e3822304436e69f4caa58df7def79a1c8899cece`, Builds gitlink `157b4dacdd90148de76e576396f61dd5859556f3`, tool 4.38.3. The exact official source catalog has blob `93a6e6a61c30d498c15267bc66f24718bbaf2338`, SHA-256 `fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10`. It matches the previously observed `a13d7972395f989955cf00f1047f1ba50b1a6b73` catalog and differs from published 4.38.3: Memories 3.0.10, Azure.Security.KeyVault.Secrets 4.12.0, HotChocolate 16.7.3, plus explicit FrontComposer.Contracts.UI 4.7.1. Official GitHub latest-release and NuGet-index observations in this packet still end at 4.38.3. No matching published identity for the later source was verified.
- Original umbrella Platform checkout remains `9f43cefe11bd246ce0de1cd0740261b557c0c7a0`; actual sibling Builds remains `e909ffd9fe9a6097e36d932010b983d029396968`. Neither is substituted for the released Builds source or the tested new Platform candidate.

Remote main advanced repeatedly through independent pushes during this operation. Those observations do not freeze it. Re-read the exact head, pin, catalog, tool and workflow bytes before the next action. Do not roll back newer main dependencies to make the frozen branch result appear applicable.

## Concrete dependency proposal requiring authorization

The retained `20.project.assets.json` for `TenantRegistry.Host` reveals an independent project-local central package file. It selects Gateway **3.124.0**, seven EventStore packages at 3.124.0 and Commons.UniqueIds 2.30.1, despite the root catalog's 3.128.4/2.30.3 selections. This is not a Tenants transitive-floor inference: the assets explicitly report `versionCentrallyManaged: true`, and the child's own `Directory.Packages.props` contains the 3.124.0 pin. Tenants.Contracts and Tenants.Server 5.11.1 are present elsewhere in the retained owner graphs. See `run-41014652010-restored-hexalith-packages.json`.

The [unapplied one-line patch](tenant-registry-catalog-alignment.UNAPPLIED.patch) changes that child Gateway pin **3.124.0 → 3.128.4**. The proposed file and both hashes are retained. `git apply --check` passes against the tested candidate; no source or restored dependency was changed by preparing it.

**Exact authorization needed:** apply that Gateway dependency change, integrate the reviewed CI/test corrections with the selected current Platform base, and qualify the new actual restored closure. If the owner intends the older host as a separately governed compatibility exception, obtain and document that specific decision instead; a preflight pass does not invent the exception. After any change, verify all expected and actual package versions and fresh full owner-host behavior before acceptance. Do not assume one direct change fixes every transitive identity.

## Current-main tuple correction

Keeping current main's newer catalog selections requires a correspondingly published and verified Builds/tool tuple. Prepare owner source/CI/release identities for that exact catalog and obtain separate authorization for its release once a concrete qualified producer candidate exists. Do not publish, tag, invent a future version or substitute latest source under 4.38.3 during this task. Alternatively, reverting the newer catalog selections requires explicit approval of those precise dependency changes; it is not authorized by the frozen-candidate CI operation.

Once an official tuple is selected, align Platform's real gitlink, exact tool, floor, direct AppHost packages and all archive/source/hash bindings together. The existing preflight deliberately enforces 4.38.3; it must be updated coherently and reviewed for any different tuple. Reconcile the current main source and record a new actual integration revision. PR #10 is a review point for the remaining worker-test correction, not an acceptance request.

## Exact revision and runtime gates

Obtain the real committed PR merge-result SHA for the selected exact head/base pair and fresh authenticated policy decision. Run the registered preflight with the selected full SHA; the final frozen-candidate command already executed was:

```bash
gh workflow run tuple-ci.yml --repo Hexalith/Hexalith.Platform --ref main \
  -f candidate-sha=6a186754102b33c3cfea823efc55e49264efbb84
```

Then run all remaining owner suites and real-service generated/direct-host package-mode integration on a disposable runner. Bind expected and actually loaded content, suite/configuration/required-set identities, decision/issuer/policy, source/artifact hashes and workflow/run/attempt/job. Services must be ready and data isolated; retain failures and cleanup results. The preflight deliberately keeps `candidateAcceptance: false`. The historical local EventStore readiness timeout and local Debug tests cannot be promoted to runtime acceptance.

## Publication and consumer package qualification

Platform publication needs separate approval after acceptance and exact official release/tag/source/package verification. Consumer adoption also needs separate authority: align Projects' exact Platform/Builds/tool selection, run actual package preparation, retain real per-package restored assets, resolve the existing 46 inventory differences across 18 IDs and verify exact package/symbol artifacts and constraints. The later source's UI catalog entry is not present in published 4.38.3; its source verification alone does not close the published FrontComposer.Contracts.UI selection gate. Preserve all historical evidence and do not reuse this branch preflight as consumer, merge-decision or release-decision acceptance.
