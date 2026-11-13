from pathlib import Path
import difflib,hashlib,json,shutil
BASE=Path('/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService')
OUT=Path(__file__).parent
sha=lambda b:hashlib.sha256(b).hexdigest()
changes={}
def replace(text,old,new,operations):
 assert text.count(old)==1,old[:80]
 operations.append({'old':old,'new':new});return text.replace(old,new,1)
for name in ['DomainRouteCatalogFilter','DomainRouteCatalogGuard','DomainRouteCatalogResult']:
 original=(BASE/(name+'.cs')).read_bytes();assert b'\r'not in original
 text=original.decode();ops=[]
 text=replace(text,'using Hexalith.EventStore.ServiceDefaults.Routing;','using Hexalith.EventStore.ServiceDefaults.Routing;\nusing Hexalith.PrivateCaptureCounter;',ops)
 if name=='DomainRouteCatalogFilter':
  text=replace(text,'        ArgumentNullException.ThrowIfNull(context);','        var counters = ReceiverPhaseBootstrap.Begin();\n        bool captureTransferred = false;\n        try\n        {\n        using var filterBody = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.ResultTail, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n        ArgumentNullException.ThrowIfNull(context);',ops)
  text=replace(text,'            decision = guard is DomainRouteCatalogGuard receiver','            using var guardDecision = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.ComponentFile, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n            decision = guard is DomainRouteCatalogGuard receiver',ops)
  text=replace(text,'await receiver.AcquireDecisionAsync(context).ConfigureAwait(false)','await receiver.AcquireDecisionWithTimingAsync(context, counters).ConfigureAwait(false)',ops)
  text=replace(text,'                : new(await guard.AcquireAsync(context).ConfigureAwait(false));','                : new(await guard.AcquireAsync(context).ConfigureAwait(false));\n            guardDecision?.Dispose();',ops)
  text=replace(text,'            object? result = decision.TerminalRefusal ?? await next(context).ConfigureAwait(false);','            using var filterNext = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.SettingsEnumerationSortDictionary, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n            object? result = decision.TerminalRefusal ?? await next(context).ConfigureAwait(false);\n            filterNext?.Dispose();',ops)
  text=replace(text,'            if (result is IResult response && lease is not null) return new DomainRouteCatalogResult(response, lease, guard is DomainRouteCatalogGuard);','            if (result is IResult response && lease is not null)\n            {\n                var ownedResult = new DomainRouteCatalogResult(response, lease, guard is DomainRouteCatalogGuard, counters);\n                captureTransferred = true;\n                return ownedResult;\n            }',ops)
  text=replace(text,'            if (lease is not null) await lease.DisposeAsync().ConfigureAwait(false);\n            return result;','            if (lease is not null)\n            {\n                using var leaseRelease = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.AuthenticationSerialization, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n                await lease.DisposeAsync().ConfigureAwait(false);\n                leaseRelease?.Dispose();\n            }\n            return result;',ops)
  text=replace(text,'        catch\n        {\n            if (lease is not null) await lease.DisposeAsync().ConfigureAwait(false);\n            throw;\n        }','        catch\n        {\n            captureTransferred = false;\n            if (lease is not null)\n            {\n                using var leaseRelease = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.AuthenticationSerialization, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n                await lease.DisposeAsync().ConfigureAwait(false);\n                leaseRelease?.Dispose();\n            }\n            throw;\n        }\n        }\n        finally\n        {\n            if (!captureTransferred) counters?.Dispose();\n        }',ops)
 elif name=='DomainRouteCatalogGuard':
  text=replace(text,'    private async Task<DomainRouteCatalogDecision> AcquireCoreAsync(EndpointFilterInvocationContext context, bool allowRefusal)','    /// <summary>Passes a private diagnostic capture through the existing concrete filter guard.</summary>\n    internal Task<DomainRouteCatalogDecision> AcquireDecisionWithTimingAsync(EndpointFilterInvocationContext context, CaptureCounters? counters)\n        => AcquireCoreAsync(context, allowRefusal: true, counters);\n\n    private async Task<DomainRouteCatalogDecision> AcquireCoreAsync(EndpointFilterInvocationContext context, bool allowRefusal, CaptureCounters? counters = null)',ops)
  text=replace(text,'        RouteCatalogDispatchLease lease = await authority.AcquireAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);','        using var authorityAcquire = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.ComponentPolicy, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n        RouteCatalogDispatchLease lease = await authority.AcquireAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);\n        authorityAcquire?.Dispose();',ops)
  text=replace(text,'        try\n        {\n            var snapshot = lease.Snapshot;','        try\n        {\n            using var validation = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.AclFile, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n            var snapshot = lease.Snapshot;',ops)
  text=replace(text,'        catch\n        {\n            await lease.DisposeAsync().ConfigureAwait(false);\n            throw;\n        }','        catch\n        {\n            using var leaseRelease = counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.AuthenticationSerialization, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n            await lease.DisposeAsync().ConfigureAwait(false);\n            leaseRelease?.Dispose();\n            throw;\n        }',ops)
 else:
  text=replace(text,'{\n    /// <summary>Preserves ordinary extension-guard response execution','{\n    private readonly CaptureCounters? _counters;\n\n    /// <summary>Transfers only the private diagnostic capture after wrapper construction.</summary>\n    internal DomainRouteCatalogResult(IResult result, RouteCatalogDispatchLease lease, bool trustedReceiver, CaptureCounters? counters)\n        : this(result, lease, trustedReceiver) => _counters = counters;\n\n    /// <summary>Preserves ordinary extension-guard response execution',ops)
  text=replace(text,'        ArgumentNullException.ThrowIfNull(httpContext);','        CaptureCounters.Interval? releaseInterval = null;\n        try\n        {\n        ArgumentNullException.ThrowIfNull(httpContext);',ops)
  text=replace(text,'        using var scope = trustedReceiver ? RouteCatalogRemoteCall.EnterReceiver(lease, httpContext.RequestAborted)','        try\n        {\n        using var scope = trustedReceiver ? RouteCatalogRemoteCall.EnterReceiver(lease, httpContext.RequestAborted)',ops)
  text=replace(text,'        await result.ExecuteAsync(httpContext).ConfigureAwait(false);','        using var resultExecution = _counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.ActorBoundary, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n        await result.ExecuteAsync(httpContext).ConfigureAwait(false);\n        resultExecution?.Dispose();\n        }\n        finally\n        {\n            // Original remote scope has unwound; original await-using lease disposal follows.\n            releaseInterval = _counters?.Stage(global::Hexalith.PrivateCaptureCounter.Stage.AuthenticationSerialization, global::Hexalith.PrivateCaptureCounter.AllocationMode.None);\n        }\n        }\n        finally\n        {\n            releaseInterval?.Dispose();\n            _counters?.Dispose();\n        }',ops)
 # Formatting-only indentation of the two inserted outer try bodies is explicit and reversible.
 # Keep original statements byte-exact apart from their enclosing diagnostic braces/indentation.
 original_path=OUT/(name+'.original.cs.txt');private_path=OUT/(name+'.private.cs')
 original_path.write_bytes(original);private_path.write_text(text)
 inverse=text
 for op in reversed(ops):
  assert inverse.count(op['new'])==1
  inverse=inverse.replace(op['new'],op['old'],1)
 assert inverse.encode()==original
 changes[name]={'originalPath':str(BASE/(name+'.cs')),'originalSha256':sha(original),'privatePath':str(private_path),'privateSha256':sha(text.encode()),'byteExactReverseNormalization':True,'operations':ops}
for source,name,expected in [(Path('/tmp/story65-invocation-transport-prep-20261107/CaptureCounters.cs'),'CaptureCounters.cs','112af486d745427be6fc181c01e10ee9367f27a7163dcac603a92d15504da498'),(Path('/tmp/story65-receiver-phase-prep-20261107/ReceiverPhaseBootstrap.cs'),'ReceiverPhaseBootstrap.cs','ff22ad6362bf4f7dea2ffaa782066dc719f1fcbf6e9205e50ee3df9df30df495')]:
 assert sha(source.read_bytes())==expected;(OUT/name).write_bytes(source.read_bytes())
(OUT/'source-normalization.json').write_text(json.dumps({'filePreparationOnly':True,'allThreeOriginalsByteExactAfterReversingDocumentedEdits':True,'preserveOriginalAwaitUsingLeaseLiteral':True,'stagesAllocationNoneOnly':True,'countCallsAdded':0,'changes':changes},indent=2)+'\n')
patch=''
for name in changes:
 patch+=''.join(difflib.unified_diff((OUT/(name+'.original.cs.txt')).read_text().splitlines(True),(OUT/(name+'.private.cs')).read_text().splitlines(True),fromfile='a/'+name+'.cs',tofile='private/'+name+'.cs'))
(OUT/'source.patch').write_text(patch)
print(json.dumps({name:{k:v for k,v in x.items()if k!='operations'}for name,x in changes.items()},indent=2))
