#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;

public sealed partial class SupportedProjectGatewayHttpTests
{
    private static bool MatrixAdmissionAssertions(string operation, string scenario, JsonElement payload,
        HttpStatusCode status, IReadOnlyDictionary<string, ProjectionRequest> histories, string projectId, DateTimeOffset now)
    {
        if (status != HttpStatusCode.OK) return status == HttpStatusCode.NotFound;
        try
        {
            if (operation.StartsWith("resolve-", StringComparison.Ordinal))
            {
                var result = Read<ProjectAdvisoryResolutionResponse>(payload);
                bool partialCoverage = scenario != "partial" || result.Incomplete && (operation == "resolve-conversation"
                    ? result.Result is null && result.Candidates.Count == 0 && result.UnsupportedInputKinds.Count == 0
                    : result.Result == Hexalith.Projects.Contracts.Ui.ResolutionResult.SingleCandidate && result.UnsupportedInputKinds.SequenceEqual(["file"])
                        && result.Candidates is [{ ProjectId: "project-a", Rank: 1, Score: 45 } first]
                        && first.ReasonCodes.SequenceEqual([Hexalith.Projects.Contracts.Ui.ProjectReasonCode.ProjectFolderMatched]));
                return partialCoverage && result.EvaluatedAt == now && result.UnsupportedInputKinds.Distinct(StringComparer.Ordinal).Count() == result.UnsupportedInputKinds.Count
                    && result.Candidates.All(row => histories.ContainsKey(row.ProjectId) && row.Rank > 0 && row.Score >= 0)
                    && (result.Result is not null || result.Incomplete && result.Candidates.Count == 0);
            }
            if (operation == "list")
            {
                var list = Read<ProjectListPage>(payload);
                if (list.Snapshot.AsOf != now || list.Snapshot.ProjectVersion != 0 || list.Cursor is not null || list.HasMore
                    || list.Snapshot.Components.Count != 1 || list.Snapshot.Components[0] is not
                        { Name: "Enumeration", Included: true, Freshness: EvidenceFreshnessState.Current, Reason: "Current" }
                    || list.Snapshot.Components[0].LastVerifiedAt != histories.Values.Max(source => source.Events.Max(item => item.Timestamp))) return false;
                return list.Items.All(row => MetadataSnapshot(row.Snapshot, row.ProjectId, scenario, histories, now));
            }
            if (operation == "open") return MetadataSnapshot(Read<GetProjectResponse>(payload).Snapshot, projectId, scenario, histories, now);
            if (operation == "explain") payload = payload.GetProperty("context");
            JsonElement snapshot = payload.GetProperty("snapshot");
            string state = snapshot.GetProperty("responseState").GetString()!;
            var components = snapshot.GetProperty("components").EnumerateArray().ToArray();
            string[] expected = operation == "conversation-start" && scenario != "partial" ? ["Project", "Folder", "Setup", "FirstResponseAuthorization"]
                : ["Project", "Folder", "Setup", "FirstResponseAuthorization", "References"];
            if (!components.Select(component => component.GetProperty("name").GetString()).SequenceEqual(expected)
                || snapshot.GetProperty("asOf").GetDateTimeOffset() != now
                || snapshot.GetProperty("projectVersion").GetInt64() != histories[projectId].Events.Max(item => item.SequenceNumber)) return false;
            bool unavailable = scenario is "unavailable" or "stale-authority" or "conversation-binding" or "historical-pending-folderless";
            if (state != (unavailable ? "Unavailable" : scenario == "partial" ? "Partial" : "Complete")) return false;
            if (components.Any(component => component.TryGetProperty("lastVerifiedAt", out _))) return false;
            foreach (var component in components)
            {
                string name = component.GetProperty("name").GetString()!;
                bool included = component.GetProperty("included").GetBoolean();
                string freshness = component.GetProperty("freshness").GetString()!;
                string? reason = component.GetProperty("reason").GetString();
                if (name == "Project" && (!included || freshness != "Current" || reason != "current")) return false;
                if (!unavailable && (!included || freshness != "Current" || reason != (name == "FirstResponseAuthorization" ? "envelope-authorized"
                    : name == "References" && scenario == "partial" ? "optional-omission" : "current"))) return false;
                if (unavailable && name is "Folder" or "Setup" && (included || freshness != "Unavailable" || reason != (name == "Folder" ? "missing" : "unavailable"))) return false;
                if (unavailable && name == "FirstResponseAuthorization")
                {
                    bool authorized = scenario == "historical-pending-folderless";
                    if (included != authorized || freshness != (authorized ? "Current" : "Stale")
                        || reason != (authorized ? "envelope-authorized" : "stale")) return false;
                }
                if (unavailable && name == "References" && (included || freshness != "Unavailable" || reason != (scenario == "historical-pending-folderless" ? "materialization-in-progress"
                    : "required-evidence-unavailable"))) return false;
            }
            if (scenario == "partial")
            {
                if (!snapshot.GetProperty("recoveryActions").EnumerateArray().Select(item => item.GetString()).SequenceEqual(["None"])) return false;
                if (operation == "conversation-start")
                {
                    var bounded = Read<ConversationStartSetupResponse>(payload);
                    if (bounded.Setup is null || !bounded.Setup.ExcludedSourceKinds.SequenceEqual([ProjectContextSourceKind.Memory])) return false;
                }
                else
                {
                    var context = Read<ProjectContextReadResponse>(payload);
                    if (context.Setup is null || !context.Setup.ExcludedSourceKinds.SequenceEqual([ProjectContextSourceKind.Memory])
                        || context.MemoryReferences.Count != 0 || context.Excluded is not
                        [{ ReferenceKind: "memory", ReferenceId: "opaque-memory", ReferenceState: Hexalith.Projects.Contracts.Ui.ReferenceState.Excluded,
                            FailedCheck: Hexalith.Projects.Contracts.Ui.ProjectContextInclusionCheck.ReferenceKindAllowlist, ReasonCode: null, Diagnostic: null }]) return false;
                }
            }
            if (unavailable)
            {
                if (payload.GetProperty("setup").ValueKind != JsonValueKind.Null) return false;
                if (operation != "conversation-start" && (payload.GetProperty("projectFolder").ValueKind != JsonValueKind.Null
                    || new[] { "conversations", "fileReferences", "memoryReferences", "excluded" }.Any(field => payload.GetProperty(field).GetArrayLength() != 0))) return false;
                string[] recovery = snapshot.GetProperty("recoveryActions").EnumerateArray().Select(item => item.GetString()!).ToArray();
                if (scenario == "historical-pending-folderless" && !recovery.SequenceEqual(["PollTask"]) || recovery.Length == 0) return false;
            }
            return true;
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException) { return false; }
    }

    private static bool MetadataSnapshot(AdmissionSnapshot snapshot, string projectId, string scenario,
        IReadOnlyDictionary<string, ProjectionRequest> histories, DateTimeOffset now)
    {
        ProjectionRequest source = histories[projectId];
        DateTimeOffset persisted = source.Events.Max(item => item.Timestamp);
        bool pending = source.Events.Any(item => item.EventTypeName == typeof(Hexalith.Projects.Contracts.Events.ProjectFolderCreationPending).FullName);
        bool folder = !pending && scenario != "unavailable";
        bool references = !source.Events.Any(item => item.EventTypeName == typeof(Hexalith.Projects.Contracts.Events.MemoryLinked).FullName);
        if (snapshot.AsOf != now || snapshot.ProjectVersion != source.Events.Max(item => item.SequenceNumber)
            || snapshot.ResponseState != (!folder ? AdmissionResponseState.Unavailable : references ? AdmissionResponseState.Complete : AdmissionResponseState.Partial)
            || !snapshot.Components.Select(component => component.Name).SequenceEqual(["Project", "Folder", "Setup", "Authorization", "References"])) return false;
        return snapshot.Components.All(component => component.Name switch
        {
            "Project" or "Setup" => component.Included && component.Freshness == EvidenceFreshnessState.Current && component.Reason == "Current" && component.LastVerifiedAt == persisted,
            "Authorization" => component.Included && component.Freshness == EvidenceFreshnessState.Current && component.Reason == "Current" && component.LastVerifiedAt == now,
            "Folder" => component.Included == folder && component.Freshness == (folder ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable)
                && component.Reason == (folder ? "Current" : "FolderUnavailable") && component.LastVerifiedAt == (folder ? now : null),
            "References" => component.Included == references && component.Freshness == (references ? EvidenceFreshnessState.Current : EvidenceFreshnessState.Unavailable)
                && component.Reason == (references ? "Current" : "OptionalReferencesOmitted") && component.LastVerifiedAt == (references ? persisted : null),
            _ => false,
        }) && snapshot.RecoveryActions.SequenceEqual(pending ? [AdmissionRecoveryAction.ResolveNeedsAttention]
            : snapshot.ResponseState == AdmissionResponseState.Unavailable ? [AdmissionRecoveryAction.Retry] : [AdmissionRecoveryAction.None]);
    }
}
#endif
