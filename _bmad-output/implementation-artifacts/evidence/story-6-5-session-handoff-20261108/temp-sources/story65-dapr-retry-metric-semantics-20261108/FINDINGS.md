# Dapr 1.18.7 retry activation semantics

Read-only primary-source research. No runtime scraping, build, host, dependency, configuration, policy, or production change was performed. Local CLI 1.18.2 does not determine the runtime implementation: this note targets Dapr runtime v1.18.7 and its pinned dapr/kit v0.18.4 and cenkalti/backoff/v4 v4.3.0 dependencies. Downloaded source bytes, official URLs, versions, and SHA-256 digests are in source-urls.json; the two tracked local policy sources are hash-bound in local-source-inputs.json. These are source findings, not proof of a future process's loaded policy or exporter health.

## What increments

Dapr's policy.go NewRunnerWithOptions passes a notify callback to kit NotifyRecoverWithData. That callback calls addRetryActivatedMetric, which addMetricsToPolicy wires to PolicyActivated(..., RetryPolicy, ...); PolicyActivated passes the empty status to PolicyWithStatusActivated, recording one Count sample.

The kit wrapper invokes notify only on its first successful atomic false-to-true transition for one runner execution. Therefore one operation can retry several times yet add at most one activation. The backoff loop checks operation error, permanent error, and NextBackOff==Stop BEFORE notify; notify precedes timer start and the cancellation-versus-timer select. An increment means a first retry notification was issued after a retryable failure and non-Stop backoff selection. It does not prove a second attempt ran or quantify actual waiting, attempt count, final success, transport cause, or application outcome.

maxRetries:0 wraps the backoff with WithMaxRetries(0), whose first NextBackOff returns Stop. A first failure with this setting does NOT activate the retry metric. Permanent/nonmatching errors and canceled contexts that cause Stop also bypass notification. PolicyExecuted still records whenever addMetricsToPolicy sees a non-null retry policy, including maxRetries:0; its count is policy construction/application, not a verified network attempt count.

BuiltInPolicy returns a PolicyDefinition without addMetricsToPolicy. Those built-in runners do not receive the activation callback here. Other client, component, transport, and application retries are outside this metric's exclusion scope.

## Names and exact label schema

The Dapr exporter uses the dapr namespace prefix; slash sanitization yields dapr_resiliency_loaded, dapr_resiliency_count, dapr_resiliency_activations_total, and dapr_resiliency_cb_state. No extra _total suffix is appended by the shown exporter conversion.

Execution and activation keys are exactly app_id, name, namespace, policy, flow_direction, target, status. Retry policy is retry; direction is outbound (or inbound for an inbound component policy); retry status is empty. name is the Resiliency resource metadata.name, NOT its internal retry policy name. App target is app_ plus the destination app ID. The statestore component type is case-sensitive Statestore, so the configured component target is Statestore_statestore. Loaded metrics have only app_id/name/namespace.

Namespace has two unrelated meanings here: the exporter prefix dapr versus the namespace label from the Resiliency resource's metadata.Namespace. The local loader preserves that field; the current tracked YAML omits it and the supported profile changes only resource name/scopes/policy contents. Thus the source-derived expected metric label is namespace="", not the private runtime NAMESPACE. Empty tag values are skipped by WithTags and exported as empty label values for the registered view keys. A different observed label must not be silently accepted or persisted.

## Source-derived expected profile tuples

All following tuples require policy=retry, status="", namespace="". These are a finite source-derived allowlist, not evidence that a target executed.

| Sidecar app | Resource name | Direction/target | Retry configuration |
| --- | --- | --- | --- |
| projects | supported-reads-project-state | outbound/app_eventstore | exponential, maxRetries5 |
| eventstore | supported-reads-owner-deadline | outbound/app_folders | constant, maxRetries0 |
| eventstore | supported-reads-owner-deadline | outbound/app_projects | constant, maxRetries0 |
| projects | supported-reads-project-state | outbound/Statestore_statestore | constant, maxRetries0 |
| eventstore | supported-reads-owner-deadline | outbound/Statestore_statestore | constant, maxRetries0 |
| tenants or projects-workers | projects-local-resiliency | outbound/app_eventstore, app_tenants, app_projects, app_projects-workers | exponential, maxRetries5 |
| tenants or projects-workers | projects-local-resiliency | outbound/Statestore_statestore | exponential, maxRetries8 |

The baseline resource scopes only eventstore, tenants, projects, projects-workers. Folders is not scoped to it; do not manufacture a baseline Folder activation series. Other targets/directions exist in the YAML but should be admitted only if the collector explicitly needs and source-validates those finite tuples. Do not persist unknown labels, dynamic identifiers, endpoints, namespaces, errors, or raw scrape text.

The YAML's duration250ms/500ms does not establish those delays for exponential policies: kit NewBackOff uses Duration only for constant policies. Exponential mode uses InitialInterval and its randomization settings; the pinned defaults are 500ms initial, 0.5 randomization, multiplier1.5. This source fact does not attribute any measured application span to retry.

## Observability and safe closed result labels

OpenCensus creates a tag-signature aggregation only on addSample; viewToMetric returns nil for no rows. The Prometheus collector reads and exports existing time series on a scrape. Registering the activation view or observing a loaded/execution counter does not precreate a zero activation series. Absence is NoSeries/unobservable, never a synthesized zero. The pull exporter does not require waiting for Dapr's one-minute periodic view reporting interval to perform its own read; queued recording, scrape cutoffs/errors, metric rules, and process lifecycle still limit observations.

With the same owned PID/start/version, identical exact label tuple, successful bounded complete scrapes, no resets, and finite nondecreasing integer values at both ends, use PositiveDelta or ObservedUnchanged. PositiveDelta counts new first notifications; ObservedUnchanged means only no new first notifications for that observed tuple/window. Require a positive existing baseline for the clean cumulative-counter comparison. Missing either end is NoSeries; changed identity/reset/decrease/noninteger/duplicate tuple/parse or scrape failure is Unobservable. A later series appearing without a baseline may be reported as NewlyObservedPositive, not an exact window delta.

Even ObservedUnchanged CANNOT exclude actual retry attempts: an operation first activated before baseline can continue its retry loop within the window without more increments. It also cannot exclude uninstrumented built-ins or other retry layers. No request correlation, per-advisory attribution, backoff-duration claim, or global no-retry conclusion is supported.

## Narrow source anchors

- dapr-policy.go:264-302; kit-retry.go:108-138,161-175; backoff-retry.go:71-117; backoff-tries.go:22-33.
- dapr-resiliency.go:354-365,593-611,615-665,832-869,912-920; dapr-resiliency-monitoring.go:43-80,141-174.
- dapr-service-monitoring.go:18-34; dapr-metrics.go:27,77-78; dapr-metrics-utils.go:55-99.
- dapr-metrics-exporter.go:35-36,92-96,112-118; ocprom-prometheus.go:135-141,187-201,227-246.
- oc-view-collector.go:35-52; oc-view-data.go:130-146; oc-view-worker.go:398-410.
- Local SupportedProjectReadFixtureProfile.cs:337-374; DaprComponents/resiliency.yaml:1-9,13-20,39-66.
