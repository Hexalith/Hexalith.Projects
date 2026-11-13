using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Candidate = Story65.TechnicalFingerprint.Candidate;
namespace Story65.TechnicalFingerprint.Harness;

internal static class Parity
{
    private static string Hash(object value) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static byte[] Bytes(object value) => JsonSerializer.SerializeToUtf8Bytes(value);
    private static Exception? Failure(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static string Signature(Exception? error) => error is null ? "success" : error.GetType().FullName + ":" + error.Message;
    private static string? Key(bool response, string?[] fields, int number = 200, int? behavior = 0)
    {
        var key = new Candidate.FingerprintKeyBuilder(response); key.Count(fields.Length);
        foreach (string? field in fields) key.String(field);
        key.Integer(number); key.NullableEnum(behavior); return key.Complete();
    }

    internal static async Task<ParityResult> RunAsync(MvcCorpus corpus, string fixtureDirectory)
    {
        int checks = 0;
        void Check(bool condition, string label) { Require.That(condition, label); checks++; }
        Require.That(!Directory.Exists(fixtureDirectory), "fresh-parity-fixture-directory");
        Directory.CreateDirectory(fixtureDirectory);
        try
        {
            Check(!corpus.ApplicationStarted && !corpus.OwnerQueryMarked && !corpus.OwnerQueryEligible, "source-correct-no-start-unmarked-core-witness");
            Check(corpus.AssemblyCounts.Count == 2 && corpus.AssemblyCounts.Values.All(v => v.Marked == v.Eligible + v.Refused)
                && corpus.AssemblyCounts.Values.Sum(v => v.Eligible) == corpus.EligibleEndpoints.Count, "complete-two-assembly-eligible-counts");
            byte[] expected;
            using (var actual = VariantHosts.Create("actual")) using (actual.BeginObservation()) expected = Bytes(actual.Observe(corpus));
            foreach (string variant in VariantHosts.Names)
            {
                using var host = VariantHosts.Create(variant);
                for (int pass = 0; pass < 3; pass++)
                {
                    using var scope = host.BeginObservation();
                    Check(Bytes(host.Observe(corpus)).AsSpan().SequenceEqual(expected), "whole-cold-warm-descriptor-agreement");
                }
                if (variant == "candidate") Check(host.Counters.Stores > 0 && host.Counters.Entries <= 256 && host.Counters.ChargedBytes <= 2 * 1024 * 1024, "candidate-selected-bounded-owner-path");
            }

            string?[] unicode = [null, "", "a|b:;", "\0", "\"\n\\", "é", "e\u0301", "\ud800", "\udc00", "\ufffd", "🙂"];
            var keys = unicode.Select(s => Key(false, [s])).ToArray();
            Check(keys.All(k => k is not null) && keys.Distinct(StringComparer.Ordinal).Count() == unicode.Length, "ordinal-unicode-null-empty-key-injective");
            foreach (string? value in unicode)
            {
                Check(Key(false, [value]) != Key(true, [value]), "request-response-domain-separated");
                Check(Key(false, [value, "x"]) != Key(false, ["x", value]) || value == "x", "ordered-string-fields");
            }
            Check(Key(false, ["a", "bc"]) != Key(false, ["ab", "c"]), "length-prefix-no-concatenation-alias");
            Check(Key(false, ["x"]) != Key(false, ["x", ""]), "sequence-count-in-key");
            Check(new[] { int.MinValue, 0, 200, int.MaxValue }.Select(i => Key(true, ["x"], i)).Distinct().Count() == 4, "all-int32-status-bits");
            Check(new int?[] { null, 0, 1, 2 }.Select(i => Key(false, ["x"], behavior: i)).Distinct().Count() == 4, "known-nullable-enum-values-separated");
            Check(Key(false, ["x"], behavior: 17) is null, "unknown-enum-unsupported-fallback-key");
            Check(Key(false, [new string('x', 1024 * 1024 + 1)]) is null, "oversize-key-declines-before-retention");

            using (var owner = new Candidate.FingerprintMemoOwner())
            {
                string[] caller = ["original", "schema"];
                string[] owned = caller.ToArray();
                var preimage = new { fields = owned };
                string key = Key(false, owned)!; byte[] originalBytes = Bytes(preimage); string digest = Hash(preimage);
                Check(owner.TryStore(key, digest), "cold-owned-key-digest-publication");
                caller[0] = "caller-poison"; owned[0] = "later-array-poison";
                Check(owner.TryGet(key, out var kept) && kept == Convert.ToHexStringLower(SHA256.HashData(originalBytes)), "retained-key-digest-not-caller-array");
                string newKey = Key(false, owned)!; string changed = Hash(preimage);
                Check(newKey != key && !owner.TryGet(newKey, out _) && owner.TryStore(newKey, changed), "changed-owned-input-misses-exact-old-key");
                Check(owner.TryGet(key, out kept) && kept == digest && owner.TryGet(newKey, out var fresh) && fresh == changed && changed != digest, "old-and-fresh-owned-snapshots-not-poisoned");
                Check(!owner.TryStore("technical-http-request:v2;", digest) && !owner.TryStore(null, digest), "version-and-unsupported-keys-not-retained");
                Check(!owner.TryStore(Key(true, ["x"]), "not-a-digest"), "only-successful-digest-shape-retained");
                owner.Dispose(); owner.Dispose();
                Check(owner.Snapshot().Entries == 0 && owner.Snapshot().ChargedBytes == 0 && !owner.TryGet(key, out _) && !owner.TryStore(key, digest), "closed-owner-idempotent-clear-and-fallback");
            }
            using (var owner = new Candidate.FingerprintMemoOwner())
            {
                for (int i = 0; i < 256; i++) Check(owner.TryStore(Key(i % 2 != 0, [i.ToString()]), new string('a', 64)), "combined-domain-entry-publication");
                Check(!owner.TryStore(Key(false, ["entry257"]), new string('a', 64)) && owner.Snapshot().Entries == 256, "one-combined-256-entry-bound");
            }
            using (var owner = new Candidate.FingerprintMemoOwner())
            {
                const string prefix = "technical-http-request:v1;";
                int length = (int)((Candidate.FingerprintMemoOwner.MaximumChargedBytes - Candidate.FingerprintMemoOwner.EntryCharge) / 2);
                string full = prefix + new string('x', length - prefix.Length);
                Check(owner.TryStore(full, new string('b', 64)) && owner.Snapshot().ChargedBytes == 2 * 1024 * 1024, "exact-combined-two-mib-charge");
                Check(!owner.TryStore(Key(true, ["other-domain"]), new string('c', 64)) && owner.TryGet(full, out _), "other-domain-cannot-use-second-byte-budget");
            }
            foreach (bool distinct in new[] { false, true })
            {
                using var owner = new Candidate.FingerprintMemoOwner(); using var barrier = new Barrier(4);
                string shared = Key(false, ["shared"])!; string digest = Hash(new { value = "synthetic" });
                var tasks = Enumerable.Range(0, 4).Select(index => Task.Run(() =>
                {
                    string key = distinct ? Key(false, [index.ToString()])! : shared;
                    Require.That(!owner.TryGet(key, out _), "concurrent-all-cold-before-publication"); barrier.SignalAndWait();
                    Require.That(owner.TryStore(key, digest), "concurrent-successful-publication");
                    Require.That(owner.TryGet(key, out var value) && value == digest, "concurrent-exact-digest");
                })).ToArray();
                await Task.WhenAll(tasks);
                Check(owner.Snapshot().Entries == (distinct ? 4 : 1) && owner.Snapshot().Stores == (distinct ? 4 : 1), "same-and-distinct-cold-publication-linearized");
            }
            using (var owner = new Candidate.FingerprintMemoOwner())
            {
                string key = Key(false, ["close-race"])!; owner.TryStore(key, new string('a', 64));
                using var barrier = new Barrier(2);
                var reader = Task.Run(() => { barrier.SignalAndWait(); for (int i = 0; i < 1000; i++) owner.TryGet(key, out _); });
                barrier.SignalAndWait(); owner.Dispose(); await reader;
                Check(owner.Snapshot().Entries == 0 && !owner.TryGet(key, out _), "close-race-all-postclose-fallback-no-stronger-return-order");
            }
            using (var host = new CandidateHost())
            {
                using (host.BeginObservation()) host.Observe(corpus);
                MemoCounters before = host.Counters;
                Check(Bytes(host.Observe(corpus)).AsSpan().SequenceEqual(expected) && host.Counters.Stores == before.Stores, "missing-scope-original-hash-fallback");
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var go = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<byte[]> escaped;
                using (host.BeginObservation()) escaped = Task.Run(async () => { ready.SetResult(); await go.Task; return Bytes(host.Observe(corpus)); });
                await ready.Task; go.SetResult();
                Check((await escaped).AsSpan().SequenceEqual(expected) && host.Counters.Stores == before.Stores, "escaped-closed-scope-still-original-hash");
                host.Dispose();
                using (host.BeginObservation()) Check(Bytes(host.Observe(corpus)).AsSpan().SequenceEqual(expected) && host.Counters.Entries == 0, "closed-host-no-retained-graph-fallback");
            }
            using (var host = new CandidateHost())
            {
                var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => { using var scope = host.BeginObservation(); return Bytes(host.Observe(corpus)); })));
                Check(results.All(value => value.AsSpan().SequenceEqual(expected)) && host.Counters.Entries <= 256 && host.Counters.ChargedBytes <= 2 * 1024 * 1024, "four-independent-passes-one-owner-agree");
            }

            var constrainedSource = corpus.EligibleEndpoints.First(e => e.RoutePattern.Parameters.Any(p => e.RoutePattern.RawText!.Contains("{" + p.Name + "}", StringComparison.Ordinal)));
            string parameterName = constrainedSource.RoutePattern.Parameters.First(p => constrainedSource.RoutePattern.RawText!.Contains("{" + p.Name + "}", StringComparison.Ordinal)).Name;
            var constrained = new RouteEndpoint(constrainedSource.RequestDelegate!, RoutePatternFactory.Parse(constrainedSource.RoutePattern.RawText!.Replace("{" + parameterName + "}", "{" + parameterName + ":int}", StringComparison.Ordinal)), constrainedSource.Order, constrainedSource.Metadata, constrainedSource.DisplayName);
            byte[]? constraintOracle = null;
            foreach (string variant in VariantHosts.Names)
            {
                using var host = VariantHosts.Create(variant); using var observation = host.BeginObservation();
                var pass = VariantHosts.Pass(variant, corpus.Services); var descriptor = pass.Describe(constrained)!; pass.Complete();
                byte[] value = Bytes(descriptor); constraintOracle ??= value;
                Check(value.AsSpan().SequenceEqual(constraintOracle), "fresh-nonempty-constraint-original-request-hash");
                if (variant == "candidate") Check(host.Counters.ConstraintFallbacks == 1, "nonempty-constraint-fallback-executed");
            }

            var target = corpus.EligibleEndpoints.Single(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is { } a && a.ControllerTypeInfo.Name == "AdminStorageCommandController" && a.MethodInfo.Name == "SetSnapshotPolicy");
            var action = target.Metadata.GetMetadata<ControllerActionDescriptor>()!;
            var body = action.Parameters.OfType<ControllerParameterDescriptor>().Single(p => p.Name == "request");
            byte[]? oversizeOracle = null;
            foreach (string variant in VariantHosts.Names)
            {
                var options = new TrackedJsonOptions(); options.Trace.ExtraPropertyType = body.ParameterType;
                options.Trace.ExtraPropertyName = new string('x', 1024 * 1024 + 1);
                var services = new ProviderOverlay(corpus.Services, new Dictionary<Type, object> { [typeof(IOptions<JsonOptions>)] = options });
                using var host = VariantHosts.Create(variant); using var observation = host.BeginObservation();
                var pass = VariantHosts.Pass(variant, services); byte[] value = Bytes(pass.Describe(target)!); pass.Complete();
                oversizeOracle ??= value;
                Check(value.AsSpan().SequenceEqual(oversizeOracle), "actual-oversize-schema-original-hash-fallback");
                if (variant == "candidate") Check(host.Counters.UnsupportedKeyFallbacks > 0 && host.Counters.ChargedBytes <= 2 * 1024 * 1024, "oversize-checkpoint-fallback-reached-without-retention");
            }
            var orderedEndpoint = corpus.EligibleEndpoints.First(e => e.Metadata.GetMetadata<ControllerActionDescriptor>()!.Parameters
                .OfType<ControllerParameterDescriptor>().Count(p => p.ParameterType != typeof(CancellationToken) && p.BindingInfo?.BindingSource?.Id != "Services") >= 2);
            var orderedAction = orderedEndpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!;
            var originalParameters = orderedAction.Parameters.ToArray();
            byte[]? reorderedOracle = null;
            try
            {
                foreach (string variant in VariantHosts.Names)
                {
                    using var host = VariantHosts.Create(variant); using var observation = host.BeginObservation();
                    var baselinePass = VariantHosts.Pass(variant, corpus.Services); var baseline = baselinePass.Describe(orderedEndpoint)!; baselinePass.Complete();
                    orderedAction.Parameters.Clear(); foreach (var parameter in originalParameters.Reverse()) orderedAction.Parameters.Add(parameter);
                    var changedPass = VariantHosts.Pass(variant, corpus.Services); var changed = changedPass.Describe(orderedEndpoint)!; changedPass.Complete();
                    reorderedOracle ??= Bytes(changed);
                    Check(Bytes(changed).AsSpan().SequenceEqual(reorderedOracle) && changed.RequestDigest != baseline.RequestDigest, "actual-request-order-changes-owned-key-and-digest");
                    orderedAction.Parameters.Clear(); foreach (var parameter in originalParameters) orderedAction.Parameters.Add(parameter);
                }
            }
            finally { orderedAction.Parameters.Clear(); foreach (var parameter in originalParameters) orderedAction.Parameters.Add(parameter); }
            string? originalName = body.BindingInfo!.BinderModelName;
            string? failureOracle = null;
            try
            {
                body.BindingInfo.BinderModelName = "changed-installed-binding";
                foreach (string variant in VariantHosts.Names)
                {
                    using var host = VariantHosts.Create(variant); using var observation = host.BeginObservation();
                    Exception? error = Failure(() => VariantHosts.Pass(variant, corpus.Services).Describe(target)); failureOracle ??= Signature(error);
                    Check(error is RouteCatalogUnavailableException && Signature(error) == failureOracle, "fresh-binding-mutation-refused-before-cache");
                }
            }
            finally { body.BindingInfo.BinderModelName = originalName; }

            foreach (string scenario in new[] { "request-before-response", "schema-drift", "policy-drift", "policy-fault", "policy-cancel" })
            {
                string? oracle = null; string? callbackOracle = null;
                foreach (string variant in VariantHosts.Names)
                {
                    var options = new TrackedJsonOptions();
                    var policies = new MutablePolicyProvider(corpus.Services.GetRequiredService<IAuthorizationPolicyProvider>());
                    var replacements = new Dictionary<Type, object> { [typeof(IOptions<JsonOptions>)] = options, [typeof(IAuthorizationPolicyProvider)] = policies };
                    var wrong = new WrongConstraintResolver();
                    if (scenario == "request-before-response")
                    {
                        options.Trace.ThrowType = target.Metadata.GetOrderedMetadata<ProducesResponseTypeAttribute>().First(r => r.StatusCode == 200).Type;
                        replacements[typeof(IInlineConstraintResolver)] = wrong;
                    }
                    var services = new ProviderOverlay(corpus.Services, replacements);
                    using var host = VariantHosts.Create(variant); using var observation = host.BeginObservation();
                    if (scenario != "request-before-response") { var warm = VariantHosts.Pass(variant, services); warm.Describe(target); warm.Complete(); }
                    var pass = VariantHosts.Pass(variant, services);
                    Exception injected = scenario == "policy-cancel" ? new OperationCanceledException("fixed-policy-cancel", new CancellationToken(true)) : new InvalidOperationException("fixed-policy-fault");
                    Exception? error = Failure(() =>
                    {
                        if (scenario is "policy-fault" or "policy-cancel") policies.Failure = injected;
                        pass.Describe(target);
                        if (scenario == "schema-drift") options.Options.JsonSerializerOptions.PropertyNamingPolicy = new PrefixNamingPolicy();
                        if (scenario == "policy-drift") policies.NullPolicy = policies.Calls.First();
                        pass.Complete();
                    });
                    oracle ??= Signature(error);
                    string callbacks = options.Reads + ":" + string.Join('|', policies.Calls) + ":" + string.Join('|', options.Trace.Types.Select(t => t.AssemblyQualifiedName));
                    callbackOracle ??= callbacks;
                    Check(error is not null && Signature(error) == oracle && callbacks == callbackOracle, "original-fresh-observation-failure-and-callback-order");
                    if (scenario is "policy-fault" or "policy-cancel") Check(ReferenceEquals(error, injected), "original-policy-exception-identity");
                    else Check(error is RouteCatalogUnavailableException, "original-refusal-type-preserved");
                    if (scenario == "request-before-response") Check(wrong.Calls == 0 && !options.Trace.Types.Contains(options.Trace.ThrowType!), "request-failure-precedes-response-schema");
                }
            }
            foreach (string scenario in new[] { "changed", "missing", "truncated", "different-module" })
            {
                string? oracle = null;
                foreach (string variant in VariantHosts.Names)
                {
                    using var image = new ImageFixture(fixtureDirectory); using var host = VariantHosts.Create(variant); using var observation = host.BeginObservation();
                    var pass = VariantHosts.Pass(variant, corpus.Services); pass.Describe(target); pass.AssemblyIdentity(image.Assembly);
                    if (scenario == "changed") { using var stream = new FileStream(image.Path, FileMode.Append); stream.WriteByte(0); }
                    if (scenario == "missing") File.Delete(image.Path);
                    if (scenario == "truncated") { using var stream = new FileStream(image.Path, FileMode.Open, FileAccess.Write); stream.SetLength(64); }
                    if (scenario == "different-module") File.WriteAllBytes(image.Path, File.ReadAllBytes(typeof(RouteCatalogEnvelope).Assembly.Location));
                    Exception? error = Failure(pass.Complete); oracle ??= Signature(error);
                    Check(error is not null && Signature(error) == oracle, "fresh-complete-image-failure-parity");
                    if (scenario == "changed" || scenario == "different-module") Check(error is RouteCatalogUnavailableException, "image-refusal-type-preserved");
                    if (scenario == "missing") Check(error is FileNotFoundException, "missing-file-observed-fresh");
                    if (scenario == "truncated") Check(error is BadImageFormatException, "fresh-pe-validation-not-memoized");
                }
            }
            Check(Bytes(corpus.EligibleEndpoints.Select(e => new { e.Order, Pattern = e.RoutePattern.RawText }).ToArray()).Length > 0 && !corpus.ApplicationStarted, "corpus-still-no-start-after-controls");
            return new(checks, checks, true);
        }
        finally { Directory.Delete(fixtureDirectory, recursive: true); }
    }
}
