namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;

using Hexalith.Projects.Client;
using Hexalith.Projects.Contracts.Routing;
using Hexalith.Projects.Server;
using Hexalith.Projects.Testing.Reads;

using Shouldly;
using Xunit;

/// <summary>Cutover refusal controls; these synthetic controls never count as authenticated equivalence evidence.</summary>
public sealed class ReadCutoverTests
{
    [Fact]
    public void ReviewDistinctCursorContractsRequireIndependentAssertionsAndRealStorageAddresses()
    {
        var legacy = Observation() with { PhysicalAddresses = ["statestore/tenant-journal"], TransportCursorAssertions = true,
            CursorEvidence = JsonSerializer.SerializeToElement(new { transport = "retained-v1", pagingSupported = false, rejectedStatus = 400 }) };
        var current = Observation() with { PhysicalAddresses = ["statestore/read-detail"], TransportCursorAssertions = true,
            CursorEvidence = JsonSerializer.SerializeToElement(new { transport = "supported-Gateway", scope = "independently-checked", expiry = "exact", rejectedStatus = 400 }) };
        ReadCutoverComparator.Compare(legacy, current).ShouldBeEmpty();
        ReadCutoverComparator.Compare(legacy, current with { TransportCursorAssertions = false }).ShouldContain("cursor-scope-behavior");
        ReadCutoverComparator.Compare(legacy, current with { TransportCursorAssertions = null }).ShouldContain("cursor-scope-behavior");
        ReadCutoverComparator.Compare(legacy, current with { PhysicalAddresses = null }).ShouldContain("physical-addresses");
        ReadCutoverComparator.Compare(legacy, current with { PhysicalAddresses = ["statestore/detail", "statestore/detail"] }).ShouldContain("physical-addresses");
    }

    [Fact]
    public void EveryDeclaredSupportedQueryHasTheSameTypedResultInTheHost()
    {
        var declared = typeof(Hexalith.Projects.Contracts.Queries.ListProjectsQuery).Assembly.GetTypes()
            .Select(type => (Type: type, Contract: type.GetCustomAttributes(typeof(Hexalith.EventStore.Contracts.Queries.QueryResultContractAttribute), false)
                .Cast<Hexalith.EventStore.Contracts.Queries.QueryResultContractAttribute>().SingleOrDefault()))
            .Where(item => item.Contract is not null).ToArray();
        declared.Length.ShouldBe(9);
        foreach (var item in declared)
        {
            var route = ProjectsRuntimeCatalogDeclarations.Routes.Single(route => route.MessageType == item.Contract!.QueryType);
            var operation = ProjectsRuntimeCatalogDeclarations.Operations.Single(operation => operation.RouteId == route.RouteId);
            var expected = Hexalith.EventStore.Contracts.Routing.RouteOperationSchema.Create(route, item.Type,
                item.Contract!.ResultType, "public", "agent-eligible", JsonSerializerOptions.Web);
            JsonElement.DeepEquals(operation.RequestSchema, expected.RequestSchema).ShouldBeTrue();
            JsonElement.DeepEquals(operation.ResultSchema, expected.ResultSchema).ShouldBeTrue();
            operation.Digest.ShouldBe(expected.Digest);
        }
    }

    [Theory]
    [InlineData("payload")]
    [InlineData("array-order")]
    [InlineData("timestamp")]
    [InlineData("null-setup")]
    [InlineData("key")]
    [InlineData("collision")]
    [InlineData("position")]
    [InlineData("boundary")]
    [InlineData("scope")]
    [InlineData("continuation")]
    [InlineData("expiry")]
    [InlineData("rejection")]
    [InlineData("assertions")]
    public void ComparisonRejectsEveryUnapprovedDifference(string fault)
    {
        ReadCutoverObservation original = Observation();
        ReadCutoverObservation changed = fault switch
        {
            "payload" => original with { CanonicalPayload = JsonSerializer.SerializeToElement(new { value = "protected" }) },
            "array-order" => original with { CanonicalPayload = JsonSerializer.SerializeToElement(new { setup = new { }, ids = new[] { "b", "a" }, asOf = "2026-11-16T00:00:00Z" }) },
            "timestamp" => original with { CanonicalPayload = JsonSerializer.SerializeToElement(new { setup = new { }, ids = new[] { "a", "b" }, asOf = "2026-11-16T00:00:01Z" }) },
            "null-setup" => original with { CanonicalPayload = JsonSerializer.SerializeToElement(new { setup = (object?)null, ids = new[] { "a", "b" }, asOf = "2026-11-16T00:00:00Z" }) },
            "key" => original with { AggregateIdentities = ["foreign:projects:opaque"] },
            "collision" => original with { RawKeys = ["a", "b"], AggregateIdentities = ["tenant:projects:opaque", "tenant:projects:opaque"], Positions = [8, 8] },
            "position" => original with { Positions = [9] },
            "assertions" => original with { SupportedOnlyAssertions = false },
            _ => original with { CursorEvidence = JsonSerializer.SerializeToElement(new Dictionary<string, string>
                { ["boundary"] = "opaque", ["scope"] = "tenant-actor-workload-delegation", ["continuation"] = "next", ["expiry"] = "exact", ["rejection"] = "safe", [fault] = "changed" }) },
        };
        IReadOnlyList<string> differences = ReadCutoverComparator.Compare(original, changed);
        differences.ShouldNotBeEmpty();
        JsonSerializer.Serialize(differences).ShouldNotContain("protected");
    }

    [Fact]
    public void OnlyObjectPropertyOrderIsUnconditionallyNormalized()
    {
        ReadCutoverObservation original = Observation();
        ReadCutoverComparator.Compare(original, original with { CanonicalPayload = JsonSerializer.SerializeToElement(new { asOf = "2026-11-16T00:00:00Z", ids = new[] { "a", "b" }, setup = new { } }) }).ShouldBeEmpty();
        ProjectReadShadowComparator.NormalizationRevision.ShouldBe("story61-local-2026-10-17-v1");
    }

    [Fact]
    public void MissingComparisonOrRollbackSecurityEvidenceCannotActivateEitherRoute()
    {
        using JsonDocument document = JsonDocument.Parse("{}");
        ProjectReadCutoverGate.CanActivate(document.RootElement, new string('a', 40), new string('b', 64)).ShouldBeFalse();
        ProjectReadCutoverGate.CanRollback(document.RootElement, new string('a', 40)).ShouldBeFalse();
        ProjectsReadRouteSelection.Create(new ProjectsReadRoutingOptions { Mode = "LegacyRollback" }).Mode.ShouldBe("Unavailable");
        ProjectsReadRouteSelection.Create(new ProjectsReadRoutingOptions { Mode = "Supported" }).Mode.ShouldBe("Unavailable");
    }

    [Theory]
    [InlineData("reference_unavailable", "ReferenceUnavailable")]
    [InlineData("owner_read_unverified", "OwnerReadUnverified")]
    [InlineData("ReferenceUnavailable", "ReferenceUnavailable")]
    [InlineData("folder_create_external_unavailable", "folder_create_external_unavailable")]
    [InlineData(null, null)]
    public void V4MapsOnlyTheTwoRetainedReferenceReasonSpellings(string? legacy, string? supported)
        => ReadCutoverComparator.SupportedReferenceReasonCode(legacy).ShouldBe(supported);

    [Fact]
    public void V2KeepsOriginalGlobalPositionsSeparateFromAggregateVersions()
    {
        var legacy = Observation() with { Positions = [101], SourceGlobalPositions = [101], SourceSequenceNumbers = [2], ProjectVersions = null };
        var supported = legacy with { ProjectVersions = [2] };
        ReadCutoverComparator.NormalizationRevision.ShouldBe("story67-read-normalization-2026-11-22-v4");
        ReadCutoverComparator.Compare(legacy, supported).ShouldBeEmpty();
        ReadCutoverComparator.Compare(legacy, supported with { Positions = [2] }).ShouldNotBeEmpty();
        ReadCutoverComparator.Compare(legacy, supported with { SourceGlobalPositions = null }).ShouldNotBeEmpty();
        ReadCutoverComparator.Compare(legacy, supported with { ProjectVersions = [101] }).ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("context", "native-binding-unavailable", true)]
    [InlineData("context", "pending-folder-materialization", true)]
    [InlineData("explain", "pending-folder-materialization", true)]
    [InlineData("conversation-start", "pending-folder-materialization", true)]
    [InlineData("open", "pending-folder-materialization", false)]
    [InlineData("context", "missing-folder", false)]
    [InlineData("context", "stale-tenant", true)]
    [InlineData("explain", "stale-tenant", true)]
    [InlineData("conversation-start", "stale-tenant", true)]
    [InlineData("context", "unbound-membership", false)]
    [InlineData("context", "unsupported-file", false)]
    [InlineData("open", "stale-tenant", false)]
    public void OnlyTheClosedUnavailablePairsCanBeCompared(string operation, string cause, bool approved)
    {
        var legacy = JsonSerializer.SerializeToElement(new { code = "read_model_unavailable" });
        var context = JsonSerializer.SerializeToElement(new { setup = (object?)null, snapshot = new
        { responseState = "Unavailable", components = new[] { "Project", "Folder", "Setup", "Authorization" }, recoveryActions = new[] { cause == "pending-folder-materialization" ? "PollTask" : "Retry" } } });
        var payload = operation == "explain" ? JsonSerializer.SerializeToElement(new { context }) : context;
        ReadCutoverComparator.IsApprovedUnavailablePair(operation, cause, 503, legacy, 200, payload, true, true).ShouldBe(approved);
        ReadCutoverComparator.IsApprovedUnavailablePair(operation, cause, 503, legacy, 200, payload, false, true).ShouldBeFalse();
        ReadCutoverComparator.IsApprovedUnavailablePair(operation, cause, 503, legacy, 200, payload, true, false).ShouldBeFalse();
        ReadCutoverComparator.IsApprovedUnavailablePair(operation, cause, 404, legacy, 200, payload, true, true).ShouldBeFalse();
    }

    [Theory]
    [InlineData("context", "projectFolder")]
    [InlineData("context", "conversations")]
    [InlineData("explain", "fileReferences")]
    [InlineData("conversation-start", "memoryReferences")]
    public void NativeBindingUnavailableNeverAcceptsIncludedReferences(string operation, string reference)
    {
        var context = System.Text.Json.Nodes.JsonNode.Parse("{\"setup\":null,\"snapshot\":{\"responseState\":\"Unavailable\",\"components\":[1,2,3,4],\"recoveryActions\":[\"Retry\"]}}")!;
        var item = new System.Text.Json.Nodes.JsonObject { ["referenceState"] = "Included", ["referenceId"] = "protected" };
        context[reference] = reference == "projectFolder" ? item : new System.Text.Json.Nodes.JsonArray(item);
        var legacy = JsonSerializer.SerializeToElement(new { code = "read_model_unavailable" });
        JsonElement Payload() => operation == "explain" ? JsonSerializer.SerializeToElement(new { context }) : JsonSerializer.SerializeToElement(context);
        foreach (string state in new[] { "Included", "included", "1", "unknown" })
        {
            item["referenceState"] = state;
            ReadCutoverComparator.IsApprovedUnavailablePair(operation, "native-binding-unavailable", 503, legacy, 200, Payload(), true, true).ShouldBeFalse();
        }
        item["referenceState"] = "Excluded";
        ReadCutoverComparator.IsApprovedUnavailablePair(operation, "native-binding-unavailable", 503, legacy, 200, Payload(), true, true).ShouldBeTrue();
    }

    private static ReadCutoverObservation Observation() => new(
        JsonSerializer.SerializeToElement(new { setup = new { }, ids = new[] { "a", "b" }, asOf = "2026-11-16T00:00:00Z" }),
        ["raw-key"], ["tenant:projects:opaque"], [8],
        JsonSerializer.SerializeToElement(new { boundary = "opaque", scope = "tenant-actor-workload-delegation", continuation = "next", expiry = "exact", rejection = "safe" }), true)
        { SourceGlobalPositions = [8], SourceSequenceNumbers = [2], ProjectVersions = [2] };
}
