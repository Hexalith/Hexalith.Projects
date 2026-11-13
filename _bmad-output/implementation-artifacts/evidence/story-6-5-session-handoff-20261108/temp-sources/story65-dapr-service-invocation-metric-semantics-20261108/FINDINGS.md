# Dapr 1.18.7 service-invocation response latency extension

Read-only official-source extension; no scraping, process attachment, build, host, policy, configuration, or production change. The sealed retry research remains unchanged at /tmp/story65-dapr-retry-metric-semantics-20261108 (SHA256SUMS629a215598bc94c429c2122d2a05c69c5567b38fde5b073463436e4a2fb0ccbb). URLs and exact fetched SHA-256 digests are in source-urls.json. These conclusions describe source semantics, not any future scrape's coverage or actual policy activity.

## Exact record path and direction

The method is ServiceInvocationResponseReceived (not RecordServiceInvocationResponseReceived). The two reviewed direct_messaging.go callsites are invokeHTTPEndpoint:309 and invokeRemote:346. Both pass destination appID, imr.Status().GetCode(), start, only when imr is non-null. Consequently app_id is the recording caller sidecar; src_app_id is the destination/response peer despite the label's name. A local self-invocation returns through invokeLocal without these timers.

For the normal remote path, start is captured after connectionCreatorFn, span/header setup, client creation, and call-option preparation. It precedes request-sent metric bookkeeping and invokeRemoteStream. That routine transmits the request, receives the leading response chunk, establishes the response/pipe, starts a background body reader, and returns. The interval therefore includes request transmission, scheduling/network/receiver work until this response point and setup/bookkeeping; it excludes initial connection acquisition/resolution, later body draining/buffering, and outer API response forwarding. Unary fallback for an older peer is inside the measured call and has different buffering scope. The external HTTP-endpoint path starts before its channel.InvokeMethod; do not assume it has identical internals.

## Units, status, retries, omissions

ServiceInvocationResponseReceived records a distribution sample of ElapsedSince(start), whose implementation divides time.Duration by time.Millisecond as float64, retaining fractional milliseconds. status is decimal imr.Status().Code, not status.Code(err). HTTP channel response construction retains the application's HTTP status. A gRPC application channel constructs a response with gRPC code (success0 or converted error code). Thus the numeric label can represent HTTP or gRPC response status, depending on channel; do not uniformly classify it as an HTTP result or as a transport error.

The HTTP and gRPC service-invocation API handlers place directMessaging.Invoke inside their EndpointPolicy runner. Its retry can call the measured path repeatedly. Each response-bearing attempt may add its own latency sample; retry sleep/backoff is outside each such interval. Nil-response failures/cancellations and failures before timer start have no latency sample. A response can be recorded before a later body/read error changes the outer result. This is not whole-operation duration, all attempted-call count, or final-outcome evidence.

## Exact exported schema

- dapr_runtime_service_invocation_res_recv_latency_ms_sum
- dapr_runtime_service_invocation_res_recv_latency_ms_count
- Exactly three view labels: app_id, src_app_id, status. No namespace, route, method, request identifier, or type label.

The measure name is runtime/service_invocation/res_recv_latency_ms, with millisecond unit and the configured distribution aggregation. The pinned OpenCensus Prometheus exporter prefixes/sanitizes the descriptor and exports cumulative distribution Sum and Count via NewConstHistogram, producing the two names above (along with buckets, which the proposed parser must skip).

Optional dapr_runtime_service_invocation_res_recv_total is real but its view has an additional type label. Unary calls record type=unary; ServiceInvocationStreamingResponseReceived records type=streaming without recording this latency. Do not admit this third metric into a strict three-label parser or equate all total samples with histogram count. The two histogram components suffice for the requested mean.

## Safe same-body interpretation

Parse only the exact two names and exact three-label set from the already authorized scrape body. Match app/peer against the finite owned topology in RAM and retain only fixed role/peer enums and a closed status enum. Do not retain unknown label values, raw scrape text, routes, identities, or errors; buckets and all unrelated series remain ignored. Metric rewrite rules or unexpected label mappings must not be silently promoted into the known tuple.

For a same owned PID/start/version and identical tuple, both sum and count must be observed at both endpoints and valid finite cumulative values; counts must be integer, nonnegative and monotonic, sums nonnegative and monotonic. Require deltaCount>0 and deltaSum>=0 before computing meanMs=deltaSum/deltaCount. The planned positive-baseline requirement is conservative: the instrument itself does not make a positive sum essential. Missing/new/reset/decreased/invalid/duplicate data or zero deltaCount leaves mean null with a closed reason. Never synthesize absent zero. Detecting a process reset requires the external ownership guard, not just counter monotonicity.

The delta counts response samples recorded during the interval, including invocations started before baseline and response-bearing retries. It excludes calls still pending at close. No per-request, advisory, prior-probe, expiry, exclusive transport, server-only latency, or causal claim follows from this aggregate mean. It cannot exclude retry activity.

## Source anchors (downloaded 1-based lines)

- service-monitoring.go:216-224,271-272,621-661: measures, views, records.
- component-monitoring.go:491-499: elapsed fractional milliseconds.
- direct-messaging.go:163-189,227-275,299-350,382-562: selection, retry, timers, leading chunk/body pipe.
- http-directmessaging.go:125-164,196-223; grpc-api.go:317-370: outer policy placement and response handling.
- http-channel.go:659-680; grpc-channel.go:175-190; invoke-method-response.go:132-146: raw status domains.
- opencensus-prometheus.go:227-266: descriptor name and cumulative histogram conversion.
