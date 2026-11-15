# Story 6.5 ordinary acceptance attempts — 2026-11-15 (second session)

Feature acceptance is still incomplete. Product sources are unchanged from the natively qualified `eaa95558` (the only later root change, `6bed1ee4`, touched the launcher and evidence), so the [current-source native packet](../story-6-5-current-verification-20261115/README.md) still covers the product: 1,086 selected native passes and all five frozen rows bound to twelve executed UI cases. This session repairs one launcher startup defect and retries the unchanged ordinary managed command four times. No product, owner, policy, deadline, observation, privacy-floor, generated-client, package, sprint, index or frozen-intent change was made.

Every attempt ran from root `6bed1ee4f3d58ea0bb4d57ae9663c20d97a710ec` with the root-declared owner gitlinks recorded in each `run.json`:

```bash
E2E_SUPPORTED_READS_ONLY=1 E2E_SUPPORTED_EVIDENCE=<this packet>/<attempt> run-with-temp npm --prefix tests/e2e run test:live:managed
```

| Attempt | Outcome | SINGLE | MULTIPLE | Seed / browser | Cleanup and preservation |
| --- | --- | --- | --- | --- | --- |
| `live/` | Source preparation failed before any resource | — | — | 0 / 0 | nothing created; preserved |
| `live-retry/` | Owned OIDC `docker start` refused | — | — | 0 / 0 | exact container removed; preserved |
| `live-retry-2/` | Readiness timed out | 0 of 30 | not reached | 0/1 / 0 | six exact removals; sidecars absent; preserved |
| `live-oidc-port-repair/` | Readiness timed out | positive on attempt 6 | 0 of 27 in the 90 s budget | 0/1 / 0 | six exact removals; sidecars absent; preserved |

All completed readiness attempts returned HTTP 200 with a null result, incomplete coverage and zero candidates, except the single verified SINGLE positive. Elapsed values are 2.41–2.76 s, and they include the fixture's own evidence reads. `attempts-summary.json` holds the scalar facts for each attempt.

## Environment and launcher findings

- **`live/`.** `pwsh` is installed as a .NET global tool in `~/.dotnet/tools`, but that directory was not on this session's `PATH`. The `Hexalith.Builds.Tooling` step `write-platform-version-catalog.ps1` therefore exited 127 during `prepare-build`. The later attempts prepend `~/.dotnet/tools` to `PATH`. This is environment-only and needed no source change.
- **`live-retry/`.** `free_port()` chose 127.0.0.1:50213. That port lies inside a Windows-excluded TCP range (50201–50300 on this host). Docker Desktop answers `Error response from daemon: ports are not available: exposing port TCP 127.0.0.1:50213 -> …`, which was reproduced directly with a probe container. This explains the earlier intermittent "owned OIDC container start" failures.
- **Repair.** `tools/qualification/supported_browser_reads.py` now starts the OIDC container through `start_owned_oidc`. A new port is chosen only after that exact refusal, or the Linux engine's exact `Bind for 127.0.0.1:<port> failed: port is already allocated` response, for the exact requested port. Before replacing anything, the launcher checks that the container has the exact ID, carries the owner label and was never running. It then removes the container, proves it absent and deletes the cidfile. Attempts are bounded at five. Any other failure keeps the original behavior, and the last container stays owned for the ordinary exact cleanup. Six new launcher controls cover these cases, and all 164 launcher controls pass inside the `live-oidc-port-repair` run. `oidc-port-real-control.json` records a real-Docker control: the excluded port 50213 was refused, the exact owned container was replaced on a fresh port and started, and cleanup left zero owned containers.

## Readiness CPU observation

`readiness-cpu-observation.json` comes from private, numeric-only `/proc` counters taken during the final ~60 s of MULTIPLE readiness, with role names only:

- The EventStore gateway process used about 1.17 CPU-seconds per wall second, and its Dapr sidecar 0.45. Folders used 0.24 and Projects 0.08.
- The host was about 50% busy overall (mean load 7.8 on 14 CPUs), so the host was not CPU-saturated.
- In one 10 s sample, the gateway issued about 9.2 GB of read-syscall input with zero storage reads. That is about 2.3 GB per sequential MULTIPLE attempt.

This is consistent with the mandated fresh full assembly-image reads and comparisons in every gateway route-catalog observation. That attribution is a source-based inference, not a per-call measurement. The CPU figures identify no expiring stage, await, lock or per-request cost.

## Status

The advisory critical path keeps four sequential delegated Folder-owner reads: initial inputs, discovery, final inputs and final candidates. Each one fans out through gateway-mediated owner authorization, and all of it shares the unchanged two-second deadline. With every required observation, its order, the Debug/source lane and the deadline preserved, SINGLE remains borderline and MULTIPLE does not complete. No safe change inside the approved invariants was established in this session. Positive MULTIPLE readiness, the authenticated browser/persisted/privacy/accessibility journey and the subsequent build review remain open, so the story stays `in-progress`. Logs (`*.log`) stay local and untracked. Private trace/CPU scratch material was deleted. All owned containers, AppHosts and private directories are absent after the runs.

Closed out of Story 6.5 by sprint-change-proposal-2026-11-15-close-story-6-5.md; continues under DW-80.
