#if SUPPORTED_PROJECT_READ_DISTRIBUTED
namespace Hexalith.Projects.Integration.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;
using Xunit;

/// <summary>Focused controls for the distributed fixture's independent evidence checks.</summary>
public sealed class SupportedProjectReadEvidenceTests
{
    /// <summary>New domain keys are observed while permitted audit and operational writes stay separate.</summary>
    [Fact]
    public void DomainFamilyEnumerationIncludesNewKeysAndExcludesAuditAndOperationalKeys()
    {
        foreach (string key in new[] { "projects:reads:v1:5:owned:9:new-entry", "projects:tenant-access:owned",
            "projection:folders:new-domain-family:owned:new-folder", "eventstore||AggregateActor||owned:projects:new-project||journal",
            "eventstore||AggregateActor||system:tenants:owned||journal" })
            Assert.True(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"), "Owned domain family was omitted.");
        foreach (string key in new[] { "projects:read-audit:v1:owned", "admin:catalog:owned", "projection:folders:folder-permissions:unrelated:folder",
            "projects:reads:v1:9:unrelated:detail" })
            Assert.False(SupportedProjectReadDistributedFixture.IsOwnedDomainKey(key, "owned", "foreign"), "Unrelated or permitted state entered the domain comparison.");
    }

    /// <summary>Closed audit values, types, hash, count, time and sensitive exclusions are all required.</summary>
    [Fact]
    public void AuditPrivacyRequiresClosedValuesAndSafeMetadata()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Dictionary<string, object> valid = new()
        {
            ["requestSurrogate"] = new string('A', 64), ["outcome"] = "Inspection", ["projectCount"] = 2,
            ["fieldClass"] = "ProjectName", ["recordedAt"] = now,
        };
        bool Safe(Dictionary<string, object> value, params string[] exclusions) => SupportedProjectReadDistributedFixture.IsSafeAudit(
            JsonSerializer.SerializeToElement(value), now.AddMinutes(-1), now, exclusions);
        Assert.True(Safe(valid));
        foreach ((string field, object value) in new (string, object)[]
        {
            ("requestSurrogate", "protected-description"), ("requestSurrogate", new string('a', 64)), ("outcome", "ProtectedAlphabeticDescription"),
            ("fieldClass", "Owned folder label"), ("fieldClass", 4), ("projectCount", -1), ("projectCount", 201), ("projectCount", "2"),
            ("recordedAt", now.AddHours(-1)), ("recordedAt", now.AddHours(1)), ("recordedAt", "actor-1"),
        })
        {
            Dictionary<string, object> invalid = new(valid) { [field] = value };
            Assert.False(Safe(invalid), "Invalid audit metadata was accepted.");
        }
        Assert.False(Safe(valid, new string('A', 64)), "A seeded sensitive value was retained in audit metadata.");
        Dictionary<string, object> extra = new(valid) { ["credential"] = "protected-value" };
        Assert.False(Safe(extra), "An extra audit field was accepted.");
        Dictionary<string, object> denial = new(valid) { ["outcome"] = "SafeDenial", ["fieldClass"] = "None", ["projectCount"] = 0 };
        Assert.True(Safe(denial));
        denial["projectCount"] = 1;
        Assert.False(Safe(denial), "Safe denial retained a disclosure count.");
    }

    /// <summary>Real admission component names remain public while each descriptive response path is omitted.</summary>
    [Fact]
    public void NonDescriptiveFaultControlsValidateContractPathsAndPopulatedCompleteResults()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AdmissionSnapshot snapshot = new(AdmissionResponseState.Complete, now, 2,
            [new("Project", true, EvidenceFreshnessState.Current, "current"), new("Folder", true, EvidenceFreshnessState.Current, "current")], []);
        GetProjectResponse opened = new("project-a", null, null, ProjectLifecycle.Active, now, now, null, null,
            [new("ProjectFolder", "folder-project-a", null, null, null, ReferenceState.Included, null, now)], snapshot);
        JsonElement open = JsonSerializer.SerializeToElement(opened, JsonSerializerOptions.Web);
        Assert.True(open.GetProperty("snapshot").GetProperty("components")[0].TryGetProperty("name", out _), "Positive control omitted public admission component names.");
        Assert.True(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(open, open: true));
        foreach (string field in new[] { "name", "description", "setup", "setupMetadata" })
            foreach (string? value in new string?[] { null, "protected-descriptive-value" })
            {
                JsonObject invalid = JsonNode.Parse(open.GetRawText())!.AsObject();
                invalid[field] = value;
                Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: true), "An open descriptive field was accepted.");
            }
        foreach (string? value in new string?[] { null, "protected-reference-label" })
        {
            JsonObject invalid = JsonNode.Parse(open.GetRawText())!.AsObject();
            invalid["references"]![0]!["displayName"] = value;
            Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: true), "An open reference display name was accepted.");
        }
        ProjectListPage listed = new([new("project-a", null, ProjectLifecycle.Active, ReferenceState.Included, snapshot),
            new("project-b", null, ProjectLifecycle.Active, ReferenceState.Included, snapshot)], null, false, snapshot);
        JsonElement list = JsonSerializer.SerializeToElement(listed, JsonSerializerOptions.Web);
        Assert.True(list.GetProperty("items")[0].GetProperty("snapshot").GetProperty("components")[0].TryGetProperty("name", out _), "Positive list control omitted public admission component names.");
        Assert.True(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(list, open: false));
        foreach (int index in new[] { 0, 1 })
            foreach (string? value in new string?[] { null, "protected-project-name" })
            {
                JsonObject invalid = JsonNode.Parse(list.GetRawText())!.AsObject();
                invalid["items"]![index]!["name"] = value;
                Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(invalid), open: false), "A list row name was accepted.");
            }
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(listed with { Items = [] }, JsonSerializerOptions.Web), open: false), "An empty Complete list control qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(opened with { Snapshot = snapshot with { ResponseState = AdmissionResponseState.Unavailable } }, JsonSerializerOptions.Web), open: true), "An unavailable ordinary open control qualified.");
        Assert.False(SupportedProjectReadDistributedFixture.IsNonDescriptiveControl(JsonSerializer.SerializeToElement(listed with { Snapshot = snapshot with { ResponseState = AdmissionResponseState.Unavailable } }, JsonSerializerOptions.Web), open: false), "An unavailable ordinary list control qualified.");
    }

    /// <summary>Storage access remains bound to the creation ID even if its reusable name is replaced.</summary>
    [Fact]
    public void RedisIdentityRefusesReplacementAndForeignOwnership()
    {
        string expected = new('a', 64);
        Assert.True(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = expected, owner = "scope" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = new string('b', 64), owner = "scope" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity(JsonSerializer.Serialize(new { id = expected, owner = "foreign" }), expected, "scope"));
        Assert.False(SupportedProjectReadDistributedFixture.IsOwnedRedisIdentity("{}", expected, "scope"));
    }
}
#endif
