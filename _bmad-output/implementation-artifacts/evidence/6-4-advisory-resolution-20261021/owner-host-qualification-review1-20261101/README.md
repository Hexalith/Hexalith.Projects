# Owner host review fixes — focused verification

This successor receipt records the requested review repairs. The original owner-host qualification receipt remains historical evidence. Full verification belongs to the parent workflow and was not run here.

All 216 selected native cases pass with zero failures, skips or errors and unchanged executable closures:

| Selected native XML | Cases | Scope |
| --- | ---: | --- |
| `folders-host-final-focused.xml` | 89 | Actual Server composition, authentication, signed success in V1Only/Coexistence/V2Only, V2 handler rewrite, monitored readiness, probes, canonical catalog policies/refusals, callbacks, startup and gateway transport |
| `folders-observability-focused.xml` | 10 | Owner-specific Server/Worker probe declarations, gate reporting and operational conformance |
| `routing-schema-projects-query-focused.xml` | 117 | Six owner contract schema pairs, supported gateway/advisory requests, state and owner-dispatch preservation around missing-surface 403, read and authority fixtures |

The Folders run completed in 193.063 seconds. Exact commands, explicit source environments, native results, closure hashes and private stdout/stderr digests accompany each selection. PowerShell parsing and both owner repository whitespace checks also pass. The Projects tests used separately owned loopback Redis/Dapr; `routing-schema-cleanup.json` proves their processes and listeners are absent and no shared resources were stopped.

The production Server now uses one SDK defaults registration, including Dapr target circuit partitioning. It adds Folders module signals and monitored readiness without adding another exporter or resilience registration. It also maps the SDK's six catalog lifecycle/readback endpoints through their existing canonical workload policies. Server infrastructure probes use `/alive` and `/ready`; Workers retain `/health/live` and `/health/ready`. The README gives the qualified standalone Debug source properties and retains the plain Debug limitation.

`final-source-changes.json` binds the 15 edited/new owner files to their verified scratch-source overlays. `preservation.json` records unchanged Git heads, refs, indexes and gitlinks, exact source scope, and preservation of the original receipt. No dependency version or SDK transport change is part of this repair.

## OTLP delivery remains unqualified

The retained initial `folders-host-focused.xml` contains the exact failed export-delivery assertion, alongside a separate rejected zero-retry test setting. Its command receipt binds the private raw diagnostics. The SDK's existing `RouteCatalogInvocationHandler` implements asynchronous send only; the HttpProtobuf OTLP traces/metrics path uses synchronous `HttpClient.Send` and throws `NotSupportedException`. This SDK producer issue was not repaired here. `otlp-delivery-limitation.json` records its unchanged SDK source hash and the exact diagnostic provenance. Delivery gates remain open for parent triage.

The passing replacement test explicitly checks configured OTLP provider startup and module subscriptions. It checks the real activity listener and a local metric reader; it makes no OTLP delivery claim. The independent resilience case counts the effective actual handler chain and observes separate Dapr target circuit breakers.

Earlier build/assertion failures and the accidentally started stale-binary command remain separately recorded. That stale command was terminated immediately after the failed build was identified and is excluded from selected evidence. No failed or interrupted execution was overwritten or presented as passing.
