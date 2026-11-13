# Receiver phase diagnostic: file preparation only

No production edit, restore, build, application assembly load, test, host, private
window or profile is authorized. This preparation changes only a private copy of
TechnicalHttpRouteCatalogReceiver. It is a wall-boundary diagnostic, not a repair.
DispatchDiagnostics1190 remains as originally guarded; no logger/privacy/configuration
switch is introduced or enabled.

Source.patch retains every original statement/expression/guard/read/await/token,
ConfigureAwait, catch/finally, remote-boundary and lease lifetime. The explicit
non-insertion changes are the RequestFingerprint private parameter/call suffix and
the native method move/rename plus a unique five-argument direct-Task forwarder.
The latter preserves the exact existing CreateDelegate shape used by
NativeDispatchDiagnosticsTests. It adds no await, Task.Run, AsTask, WaitAsync,
cancellation check or exception wrapper. Normalization reverses those four changes
and removes25 insertion lines, reproducing original71a9 byte-for-byte.

CaptureCounters is the unchanged112 helper. ReceiverPhaseBootstrap reads only fresh
entry assembly simple-name metadata and maps three literal names to closed enum
roles: Hexalith.Projects.Server/projects, Hexalith.Tenants/tenants and
Hexalith.Folders.Server/folders. Other names and null return before helper Begin.
The synchronous SuppressFlow/NoInlining boundary preserves an already suppressed
caller and restores ordinary flow before any original middleware statement. No
Activity, request, HttpContext, string value, configuration, logger or authority
object enters the bootstrap/recorder. There is no SetEntryAssembly or new switch.
Current metadata-only tool evidence under provenance confirms all three actual
entry names (including Hexalith.Tenants); project-source hashes are retained.

## Eight aliases in one local middleware capture

All child Stage calls explicitly use AllocationMode.None; Count is never called.
The serialization names are inherited aliases, with all count/allocation columns0.

| Alias | Intended interval |
| --- | --- |
|0 Whole|Begin timestamp through original middleware return/unwind and original disposals|
|1 ComponentFile|Original native endpoint precheck, including original image scopes; explicit stop before discovery next|
|2 ComponentPolicy|RequestFingerprint helper including original image disposal/restoration|
|3 AclFile|Actual native or technical Acquire await, after existing Start and before Complete|
|4 SettingsEnumerationSortDictionary|Actual leased native or technical next await, after Start and before Complete|
|5 AuthenticationSerialization|LAST completed/unwound original owned DisposeAsync interval; never a sum/count or proof of release callback|
|6 ActorBoundary|Original technical authentication/authorization if-block|
|7 ResultTail|Whole original native-or-callback branch including timed native body/lease disposal|

Stage7 overlaps3/4/5 and possibly all native execution. Whole/children/residual are
not an additive partition. Stage5 can overwrite an earlier interval when an HTTP
denied-grant disposal throws RouteCatalogUnavailableException and the existing catch
disposes again. The real lease resets its disposed flag after durable release failure,
so both callbacks may execute. Both original awaits remain; only the last interval
is serialized. Missing phases establish no request kind, result, cancellation,
absence of work or successful release. Recursive middleware entries get independent
local ordinals; there is no ambient/downstream/root/old-probe join.

Whole excludes entry eligibility and initial AdmissionGate wait. Helper112 takes its
timestamp inside that gate before the condition/newCapture/array allocation/SeenRole,
Active/Admitted bookkeeping and lock exit: those later diagnostic costs are included.
Whole stops before queue writes. Child construction before its own timestamp can
contribute to Whole outside that child. No exact ordinary latency or CPU attribution
is claimed. Discovery next and final control/probe next retain Whole/precheck only.

## Concrete differential qualification strategy (not executed)

Use two separate fresh Default-context processes with byte-identical copies of the
current compiled Server.Tests DLL, its deps/runtimeconfig and pinned dependency
closure. One folder has ordinary SDK6947/PDBffa2, the other the eventual isolated
private SDK/PDB; only that SDK pair differs. No project/application outputs are
modified. Business SDK/Server/Gateway/Admin/Contracts types resolve coherently in one
Default context per process. No fake Default interface is cast into a variant ALC.
Each process verifies its exact loaded module hashes and shared type identity.

Three genuine thin entries have the exact eligible assembly names; a fourth has a
genuine excluded name. They never change process entry identity. The native mode
loads the exact copied TestDLL and constructs xUnit ConsoleRunner(args,testAssembly),
then awaits EntryPoint(). Cached4.0.2 metadata proves this public constructor and
Task<int> API (provenance/xunit-*-api.json); do not use static Run() because its default
test assembly would be the thin entry. Resolver roots are the pinned variant copy,
then already loaded framework assemblies; SDK must never fall back to the other
folder or original workspace. No helper reflection occurs in excluded first business.

Run matched selected unchanged native fixtures to preserve actual built assertions,
not merely hand-written copies: TechnicalHttpInstalledMetadataTests (8),
TechnicalHttpNativeRpcTests (12), ReceiverImagePrecheckTests (20), and
NativeDispatchDiagnosticsTests (7) are the proposed47 executions per variant.
Final exact rendered-name sets/counts must be pinned before any later native run;
these are proposed diagnostic qualification, not additions to ordinary1048. The
first two fixtures start real loopback Kestrel/JWT/MVC in memory with no sidecar;
this requires separate explicit execution authorization. The precheck fixture builds
and invokes the actual middleware without starting a host. The direct native tests
retain their five-argument delegate unchanged but do not themselves admit a capture.

Existing fixture assertions compare state, fence retention, headers/results and
exception behavior inside each process. Paired runner comparison is assertion/case
parity, not cross-process reference equality. The custom focused cases below validate
original/private against the same exact synthetic expectations in RAM; their closed
summaries compare counts/types/booleans. They never serialize request/header/body,
snapshot, principal, binding, URI, trace, key or arbitrary exception-message values.

For injected cases, use a small test-only fixture companion with the existing SDK
friend identity Hexalith.EventStore.Server.LiveSidecar.Tests, which is distinct from
the original TestDLL identity and is not a dependency of Server.Tests. It compiles
against SDK6947, then the same companion bytes run in either Default-context folder.
It may construct real SDK lease/snapshot types and typed authority doubles; no new
production friend/API is needed. Reuse the compiled internal fixture via reflection,
not by cloning the whole test framework or referencing it under its own identity.
The exact companion closure/absence of an identity collision must be pinned before
its future build. All comparisons and expected payloads stay in this companion.

Native setup reuses ReceiverImagePrecheckFixture(customAuthority:true), its actual
closed Server observer, actor endpoints, fingerprint/Complete baseline and pipeline.
Configure its existing public authority substitute with a genuine SDK lease and
schema3 snapshot whose actual native descriptor/auxiliary binding is asserted first.
HTTP setup reuses TechnicalHttpCatalogFixture's actual MVC/startup fingerprint/auth
composition, injects the controlled authority through its existing services callback
and a controlled downstream through its endpoint callback. Ordinary fixture native
cases independently retain actual MVC action/RPC behavior; a custom terminal stub
does not claim to execute an MVC action. Technical denied-grant setup must assert
Contains(currentDescription) first and then exercise the separate invocation-grant
denial, rather than failing an earlier snapshot prerequisite.

Focused scenarios are intentionally limited to the actual boundaries: bypass;
invalid channel; native config discovery; native Allows refusal; not-ready refusal;
native success; native Acquire refusal; native snapshot refusal; native next fault;
native next cancellation; technical auth refusal; technical success; technical grant
denial; technical denied-grant first Dispose fault/second success; the same with a
distinct second fault; and technical next fault/release precedence (16 proposed).
Every injected fault must fire its exact expected count. Assert original order,
RequestAborted identity, response/header effects, retained/released lease/fence state,
remote-boundary restoration and caller ownership. Do not invent a postcancel check.

The double-release case holds the first callback, then records its throw timestamp
and holds/releases the second independently. Both callbacks and their order are
required. The final stage5 must lie inside the second await's enclosing timestamp
bracket and exclude the deliberately longer first hold; otherwise retain an incomplete
qualification rather than passing an inactive or ambiguous measurement. Whole still
contains both. No source callbacks or timing hooks are added to the production copy.

Run four unchanged recorder modes: disabled, active, pre-capped, failing sink. Positive
stage/admission coverage must be explicit per actual middleware entry after fixture
preparation; a long suite outliving the8s window is not positive coverage. Group only
fast cases that all prove exact admission; isolate any slower case in a fresh process.
Pre-cap must prove capDrops before cases; preserve all payload/error assertions after
cap/sink failure. Reuse the already qualified strict numeric protocol and owned cleanup;
do not change helper112 window/queue/row/byte bounds.

Three fresh first-business canaries precede helper Ready/reflection: unsafe named-thread
positive detector, normal actual middleware, and already-flow-suppressed middleware.
The first synchronous provider/issuer/next observation and genuine awaited continuations
must preserve the source-correct caller state; already suppressed async continuations
may have null inherited remote boundary while the caller retains its own boundary.
Require real helper worker/window closure and no synthetic canary inheritance. Additional
real Projects/Tenants thin-entry positives must prove their exact role output. A genuine
excluded entry exercises representative actual middleware, never reflects the helper,
and remains alive through the known8s window plus bounded closure opportunity; claim
observed no artifacts plus source gating, not invisible worker absence. All native and
focused modes require exact PID/group exit before guarded archive/removal.

## Remaining authorization and custody

This is a reviewable source/strategy preparation, not a completed harness. The exact
companion code/case catalog/controller and isolated SDK producer/output path audit
still need root+reader review. No compilation/loading/test/host/window execution follows
from this file. Bind current owner49/root56 source controls and actual ordinary SDK6947
with PDBffa2; never reuse5bb reference pins. Keep the latest ordinary cleanup's ten
crossmodule old-versus-current provenance limits explicit instead of whitelisting or
rebuilding them. Future private hosts require all affected ordinary carriers to be
force-restored and audited afterward even though owner source is unchanged.
