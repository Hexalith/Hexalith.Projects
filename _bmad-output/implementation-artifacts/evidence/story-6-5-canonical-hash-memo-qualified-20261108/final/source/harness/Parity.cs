using System.Reflection;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Actual = Hexalith.EventStore.Contracts.Routing.RouteCatalogCodec;
using ControlCodec = Story65.Canonical.Control.RouteCatalogCodec;
using CandidateCodec = Story65.Canonical.Candidate.RouteCatalogCodec;
using Scope = Story65.Canonical.Candidate.RouteCatalogDecodeScope;
using Memo = Story65.Canonical.Candidate.RouteCatalogCanonicalHashMemo;
namespace Story65.Canonical.Harness;
/// <summary>Checks complete pipeline agreement and bounded snapshot ownership before timing; no native test counts.</summary>
internal static class Parity
{
    private static int _checks;
    private static void Require(bool value,string label) { Check.That(value,label); Interlocked.Increment(ref _checks); }
    internal static async Task<object> Run(Dataset[] datasets)
    {
        _checks=0;
        foreach(var dataset in datasets)
        {
            using IVariant production=Variants.Create("production"), control=Variants.Create("control"), candidate=Variants.Create("candidate");
            for(int iteration=0;iteration<3;iteration++)
            {
                string[] expected;
                using(production.Enter()) expected=production.Digests(dataset.Fresh());
                using(control.Enter()) Require(expected.SequenceEqual(control.Digests(dataset.Fresh()),StringComparer.Ordinal),"complete-control-digests");
                using(candidate.Enter()) Require(expected.SequenceEqual(candidate.Digests(dataset.Fresh()),StringComparer.Ordinal),"complete-candidate-digests");
                object identity;
                using(production.Enter()) identity=production.Capture(dataset.Fresh(),"identity");
                using(control.Enter()) Require(identity.Equals(control.Capture(dataset.Fresh(),"identity")),"complete-control-identity");
                using(candidate.Enter()) Require(identity.Equals(candidate.Capture(dataset.Fresh(),"identity")),"complete-candidate-identity");
            }
        }
        RouteOperationContract operation=datasets.Single(x=>x.Name=="compiled-sample-owner").Observation.Operations![0];
        IdempotencyDescriptorSchema descriptor=datasets.Single(x=>x.Name=="public-catalog-fixture").Observation.AdmissionPolicies![0].DescriptorSchema;
        using var bytes=new RouteCatalogValidatedBytesMemo();
        using var memo=new Memo();
        using(var scope=Scope.EnterWithCanonicalHashMemo(bytes,memo))
        {
            foreach(string raw in new[]{"{\"z\":1,\"a\":{\"b\":2,\"a\":3}}","{\"a\":{\"a\":3,\"b\":2},\"z\":1}",
                "{\"same\":1,\"same\":2}","{\"same\":2,\"same\":1}","{\"n\":1}","{\"n\":1.0}","{\"n\":1e0}"})
            {
                using var schema=JsonDocument.Parse(raw);
                var value=operation with {RequestSchema=schema.RootElement};
                Compare(()=>Actual.ComputeOperationContractDigest(value),()=>ControlCodec.ComputeOperationContractDigest(value),()=>CandidateCodec.ComputeOperationContractDigest(value),"schema-representation-parity");
                Compare(()=>Actual.ComputeOperationContractDigest(value),()=>ControlCodec.ComputeOperationContractDigest(value),()=>CandidateCodec.ComputeOperationContractDigest(value),"schema-representation-warm-parity");
            }
            foreach(var value in new[]{operation with {RouteId="changed-route"},operation with {ContractVersion="v2"},operation with {Ingress="internal"},
                operation with {Eligibility="confirmation-required"},operation with {Kind="query"},operation with {Domain="other"},operation with {MessageType="OtherMessage"},operation with {Digest="untrusted"}})
                Compare(()=>Actual.ComputeOperationContractDigest(value),()=>ControlCodec.ComputeOperationContractDigest(value),()=>CandidateCodec.ComputeOperationContractDigest(value),"all-operation-scalars");
            var fields=descriptor.Fields.ToList(); var mutable=descriptor with {Fields=fields};
            Compare(()=>Actual.ComputeDescriptorDigest(mutable),()=>ControlCodec.ComputeDescriptorDigest(mutable),()=>CandidateCodec.ComputeDescriptorDigest(mutable),"mutable-descriptor-before");
            fields[0]=fields[0] with {Name="changed-field"};
            Compare(()=>Actual.ComputeDescriptorDigest(mutable),()=>ControlCodec.ComputeDescriptorDigest(mutable),()=>CandidateCodec.ComputeDescriptorDigest(mutable),"mutable-descriptor-after");
            fields[0]=descriptor.Fields[0];
            Compare(()=>Actual.ComputeDescriptorDigest(mutable),()=>ControlCodec.ComputeDescriptorDigest(mutable),()=>CandidateCodec.ComputeDescriptorDigest(mutable),"mutable-descriptor-reversion");
            using var document=JsonDocument.Parse("{}");
            var disposed=operation with {RequestSchema=document.RootElement}; document.Dispose();
            Compare(()=>Actual.ComputeOperationContractDigest(disposed),()=>ControlCodec.ComputeOperationContractDigest(disposed),()=>CandidateCodec.ComputeOperationContractDigest(disposed),"disposed-schema-error");
            var invalid=disposed with {RouteId=""};
            Compare(()=>Actual.ComputeOperationContractDigest(invalid),()=>ControlCodec.ComputeOperationContractDigest(invalid),()=>CandidateCodec.ComputeOperationContractDigest(invalid),"scalar-refusal-before-disposed-schema");
            var bad=operation with {Eligibility="invalid"};
            Compare(()=>Actual.ComputeOperationContractDigest(bad),()=>ControlCodec.ComputeOperationContractDigest(bad),()=>CandidateCodec.ComputeOperationContractDigest(bad),"invalid-vocabulary");
            Compare(()=>Actual.ComputeDescriptorDigest(descriptor with {Fields=null!}),()=>ControlCodec.ComputeDescriptorDigest(descriptor with {Fields=null!}),()=>CandidateCodec.ComputeDescriptorDigest(descriptor with {Fields=null!}),"malformed-descriptor");
            object[] purposes=[new {purpose="a",schemaVersion=1,payload="same"},new {purpose="b",schemaVersion=1,payload="same"},new {purpose="a",schemaVersion=2,payload="same"}];
            var digests=new List<string>();
            foreach(object payload in purposes)
            {
                Compare(()=>Hash(typeof(Actual),payload),()=>Hash(typeof(ControlCodec),payload),()=>Hash(typeof(CandidateCodec),payload),"full-purpose-version-key");
                digests.Add(Hash(typeof(CandidateCodec),payload));
            }
            Require(digests.Distinct().Count()==3,"purpose-version-separation");
            var a=new ChangingPreimage();var b=new ChangingPreimage();var c=new ChangingPreimage();
            Compare(()=>Hash(typeof(Actual),a),()=>Hash(typeof(ControlCodec),b),()=>Hash(typeof(CandidateCodec),c),"same-snapshot-miss-computation");
            Require(a.Reads==1&&b.Reads==1&&c.Reads==1,"one-original-serialization-only");
            Require(memo.Hits>0&&memo.Count>0&&memo.Count<=512&&memo.RetainedCharge<=4*1024*1024,"actual-warm-bounded-participation");
            int count=memo.Count;
            using(Scope.Suppress()) CandidateCodec.ComputeOperationContractDigest(operation);
            Require(memo.Count==count,"suppressed-no-owner-participation");
            using(Scope.Enter()) CandidateCodec.ComputeOperationContractDigest(operation);
            Require(memo.Count==count,"nested-independent-scope-no-owner-participation");
            using var foreign=new Memo();using var foreignBytes=new RouteCatalogValidatedBytesMemo();
            using(Scope.EnterWithCanonicalHashMemo(foreignBytes,foreign)) CandidateCodec.ComputeOperationContractDigest(operation);
            Require(foreign.Count>0&&memo.Count==count,"explicit-foreign-owner-isolation");
        }
        Require(Scope.Current is null,"outer-scope-restoration");
        int before=memo.Count;CandidateCodec.ComputeOperationContractDigest(operation);Require(memo.Count==before,"outside-scope-original-path");
        using(var limit=new Memo())
        {
            for(int i=0;i<513;i++) limit.Remember("{\"ordinal\":"+i+"}",new string('0',64));
            Require(limit.Count==512&&limit.RetainedCharge<=Memo.MaximumRetainedCharge,"maximum-key-count");
            limit.Dispose();Require(limit.Count==0&&limit.RetainedCharge==0&&limit.IsClosed,"owner-clears-owned-strings");
            limit.Remember("later",new string('0',64));Require(limit.Count==0,"closed-owner-no-repopulate");
        }
        using(var small=new Memo())
        {
            for(int i=0;i<512;i++) small.Remember(new string('x',8192)+i,new string('0',64));
            Require(small.Count<512&&small.RetainedCharge<=Memo.MaximumRetainedCharge,"independent-four-mib-charge");
        }
        using(var oversized=new Memo())
        using(var oversizedBytes=new RouteCatalogValidatedBytesMemo())
        using(var scope=Scope.EnterWithCanonicalHashMemo(oversizedBytes,oversized))
        {
            var payload=new {purpose="oversize",value=new string('x',2*1024*1024)};
            Compare(()=>Hash(typeof(Actual),payload),()=>Hash(typeof(ControlCodec),payload),()=>Hash(typeof(CandidateCodec),payload),"oversize-original-success-fallback");
            Require(oversized.Count==0,"oversize-not-retained");
        }
        await ConcurrentMisses(operation);
        await Lifetime(operation);
        return new {Checks=_checks,Passed=_checks,AllPassed=true,NativeCases=0,ProductionApproval=false};
    }
    private static async Task ConcurrentMisses(RouteOperationContract operation)
    {
        string expected=Actual.ComputeOperationContractDigest(operation);
        using(var published=new Memo())
        using(var rendezvous=new Barrier(8))
        {
            Task[] misses=Enumerable.Range(0,8).Select(_=>Task.Run(()=>
            {
                Require(!published.TryGet("synthetic-complete-key",out string ignored),"simultaneous-empty-owner-miss");
                rendezvous.SignalAndWait();
                published.Remember("synthetic-complete-key",expected);
            })).ToArray();
            await Task.WhenAll(misses);
            Require(published.Count==1&&published.Misses==8,"concurrent-publication-single-charge");
            Require(published.RetainedCharge==128+2*("synthetic-complete-key".Length+expected.Length),"concurrent-publication-exact-charge");
        }
        using var owner=new Memo();using var bytes=new RouteCatalogValidatedBytesMemo();
        var allComputed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var afterClose=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int completed=0;
        Task[] scopes=Enumerable.Range(0,8).Select(_=>Task.Run(async()=>
        {
            using var scope=Scope.EnterWithCanonicalHashMemo(bytes,owner);
            Require(CandidateCodec.ComputeOperationContractDigest(operation)==expected,"open-concurrent-complete-hash");
            if(Interlocked.Increment(ref completed)==8) allComputed.SetResult();
            await afterClose.Task;
            Require(!scope.HasCanonicalHashMemo,"live-scope-closed-owner-refused");
            Require(CandidateCodec.ComputeOperationContractDigest(operation)==expected,"live-scope-closed-owner-original-fallback");
        })).ToArray();
        await allComputed.Task;
        Require(owner.Count==1,"open-concurrent-hash-single-entry");
        owner.Dispose();afterClose.SetResult();await Task.WhenAll(scopes);
        Require(owner.Count==0&&owner.RetainedCharge==0,"close-with-all-eight-scopes-live");
    }
    private static async Task Lifetime(RouteOperationContract operation)
    {
        using var owner=new Memo();using var bytes=new RouteCatalogValidatedBytesMemo();
        var go=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scope=Scope.EnterWithCanonicalHashMemo(bytes,owner);
        Task escaped=Task.Run(async()=>
        {
            await go.Task;
            Require(ReferenceEquals(Scope.Current,scope),"escaped-closed-scope-reference");
            Require(CandidateCodec.ComputeOperationContractDigest(operation)==Actual.ComputeOperationContractDigest(operation),"escaped-closed-scope-original-result");
        });
        scope.Dispose();go.SetResult();await escaped;Require(owner.Count==0,"escaped-closed-scope-no-publication");
        string expected=Actual.ComputeOperationContractDigest(operation);
        using var start=new ManualResetEventSlim();
        Task[] workers=Enumerable.Range(0,8).Select(_=>Task.Run(()=>
        {
            using var local=Scope.EnterWithCanonicalHashMemo(bytes,owner);start.Wait();
            for(int i=0;i<4;i++) Require(CandidateCodec.ComputeOperationContractDigest(operation)==expected,"opportunistic-close-race-result");
        })).ToArray();
        start.Set();owner.Dispose();await Task.WhenAll(workers);
        Require(owner.IsClosed&&owner.Count==0,"concurrent-close-cannot-repopulate");
        using(var open=Scope.EnterWithCanonicalHashMemo(bytes,owner))
            Require(CandidateCodec.ComputeOperationContractDigest(operation)==expected,"closed-owner-original-fallback");
    }
    private static string Hash(Type codec,object value)
        => (string)codec.GetMethod("Hash",BindingFlags.NonPublic|BindingFlags.Static)!.MakeGenericMethod(typeof(object)).Invoke(null,[value])!;
    private static string Projection(Func<string> call)
    {
        try { return "value:"+call(); }
        catch(Exception e)
        {
            while(e is TargetInvocationException invocation && invocation.InnerException is not null) e=invocation.InnerException;
            return e.GetType().FullName+":"+(e is RouteCatalogValidationException refusal?string.Join("|",refusal.Diagnostics.Select(d=>d.Path+":"+d.Code)):"");
        }
    }
    private static void Compare(Func<string> actual,Func<string> control,Func<string> candidate,string label)
    {
        string expected=Projection(actual);Require(expected==Projection(control),label+"-control");Require(expected==Projection(candidate),label+"-candidate");
    }
}
