#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Projects.Contracts.Models;
using Hexalith.Projects.Contracts.Queries;
using Hexalith.Projects.Contracts.Ui;

public sealed partial class SupportedProjectGatewayHttpTests
{
    private static readonly JsonSerializerOptions MatrixTypedJson = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    // Every omitted legacy field below is checked against its own exact source. Optional nulls
    // are normalized only through declared CLR fields; unknown JSON members remain a failure.
    private static bool MatrixCanonical(string operation, JsonElement legacy, JsonElement supported,
        HttpStatusCode legacyStatus, HttpStatusCode supportedStatus, string correlation,
        IReadOnlyDictionary<string, ProjectionRequest> histories, DateTimeOffset now,
        bool currentEmptySetup, out JsonElement legacyCanonical, out JsonElement supportedCanonical)
    {
        legacyCanonical = legacy; supportedCanonical = supported;
        try
        {
            if (legacyStatus != supportedStatus) return false;
            if (legacyStatus == HttpStatusCode.NotFound)
            {
                MatrixLegacyError a = Read<MatrixLegacyError>(legacy);
                MatrixGatewayError b = Read<MatrixGatewayError>(supported);
                if (a.Status != 404 || b.Status != 404 || a.Code != "resource_unavailable"
                    || a.Category != "tenant_access_denied" || a.CorrelationId != correlation || b.CorrelationId != correlation
                    || a.Type != "https://hexalith.dev/errors/projects/resource_unavailable"
                    || a.Title != "Access unavailable" || a.Message != "The requested resource is unavailable."
                    || a.Retryable || a.ClientAction != "no_action" || a.Details.Visibility != "redacted"
                    || b.Type != "https://hexalith.io/problems/not-found" || b.Title != "Not Found"
                    || b.Detail != "The requested resource was not found." || b.Instance != "/api/v1/queries"
                    || b.ReasonCode != "query_projection_missing") return false;
                legacyCanonical = supportedCanonical = Canonical(new { denied = true, correlation }); return true;
            }
            if (legacyStatus != HttpStatusCode.OK) return false;
            switch (operation)
            {
                case "list":
                    MatrixLegacyList a = Read<MatrixLegacyList>(legacy);
                    ProjectListPage b = Read<ProjectListPage>(supported);
                    if (b.HasMore || b.Cursor is not null || !LegacyListSource(a, histories)) return false;
                    legacyCanonical = Canonical(a.Items.Select(row => new { row.ProjectId, row.Name, Lifecycle = Lifecycle(row.LifecycleState) }));
                    supportedCanonical = Canonical(b.Items.Select(row => new { row.ProjectId, row.Name, Lifecycle = row.LifecycleState }));
                    break;
                case "open":
                    MatrixLegacyOpen old = Read<MatrixLegacyOpen>(legacy);
                    GetProjectResponse current = Read<GetProjectResponse>(supported);
                    if (!LegacyOpenSource(old, histories) || old.ContextActivation.Enabled != (current.LifecycleState == ProjectLifecycle.Active)
                        || old.ContextActivation.BlockedReasonCode != (current.LifecycleState == ProjectLifecycle.Active ? null : "archived")) return false;
                    ProjectSetup? setup = old.ProjectSetup;
                    if (setup is null && current.Setup is not null)
                    {
                        if (!currentEmptySetup || !Same(current.Setup, ProjectSetup.Empty)) return false;
                        setup = ProjectSetup.Empty;
                    }
                    legacyCanonical = Canonical(new { old.ProjectId, old.Name, old.Description, Lifecycle = Lifecycle(old.LifecycleState),
                        old.CreatedAt, old.UpdatedAt, old.SetupMetadata, Setup = setup,
                        References = old.References.Select(row => new { Kind = ReferenceKind(row.ReferenceKind), Id = row.ReferenceId,
                            row.DisplayName, State = State(row.ReferenceState),
                            ReasonCode = Hexalith.Projects.Testing.Reads.ReadCutoverComparator.SupportedReferenceReasonCode(row.ReasonCode),
                            ObservedAt = row.Freshness.ObservedAt }) });
                    supportedCanonical = Canonical(new { current.ProjectId, current.Name, current.Description, Lifecycle = current.LifecycleState,
                        current.CreatedAt, current.UpdatedAt, current.SetupMetadata, current.Setup,
                        References = current.References.Select(row => new { Kind = row.ReferenceKind.ToString(), Id = row.FileReferenceId ?? row.MemoryReferenceId ?? row.FolderId,
                            row.DisplayName, State = row.ReferenceState, row.ReasonCode, row.ObservedAt }) });
                    break;
                case "conversation-start":
                    ConversationStartSetup start = Read<ConversationStartSetup>(legacy);
                    ConversationStartSetupResponse response = Read<ConversationStartSetupResponse>(supported);
                    if (start.ObservedAt != now || response.Setup?.ObservedAt != now) return false;
                    legacyCanonical = Canonical(start); supportedCanonical = Canonical(response.Setup);
                    break;
                case "context":
                    return ContextCanonical(Read<ProjectContext>(legacy), Read<ProjectContextReadResponse>(supported), now,
                        currentEmptySetup, out legacyCanonical, out supportedCanonical);
                case "explain":
                    ProjectContextExplanation explanation = Read<ProjectContextExplanation>(legacy);
                    ExplainContextSelectionResponse next = Read<ExplainContextSelectionResponse>(supported);
                    bool context = ContextCanonical(explanation.Context, next.Context, now, currentEmptySetup, out var left, out var right);
                    legacyCanonical = Canonical(new { Context = left, explanation.Evaluations });
                    supportedCanonical = Canonical(new { Context = right, next.Evaluations });
                    return context && JsonElement.DeepEquals(legacyCanonical, supportedCanonical);
                default:
                    ProjectResolution prior = Read<ProjectResolution>(legacy);
                    ProjectAdvisoryResolutionResponse advisory = Read<ProjectAdvisoryResolutionResponse>(supported);
                    if (prior.ObservedAt != now || advisory.EvaluatedAt != now || prior.Excluded.Count != 0
                        || prior.Candidates.Any(row => row.DisplayName is not null) || advisory.Incomplete || advisory.UnsupportedInputKinds.Count != 0) return false;
                    legacyCanonical = Canonical(new { prior.Result, Candidates = prior.Candidates.Select(row => new ProjectAdvisoryCandidate(row.ProjectId, row.ReasonCodes, row.Rank, row.Score)), EvaluatedAt = prior.ObservedAt });
                    supportedCanonical = Canonical(new { advisory.Result, advisory.Candidates, advisory.EvaluatedAt });
                    break;
            }
            return JsonElement.DeepEquals(legacyCanonical, supportedCanonical);
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        { return false; }
    }

    private static bool ContextCanonical(ProjectContext old, ProjectContextReadResponse current, DateTimeOffset now,
        bool currentEmptySetup, out JsonElement left, out JsonElement right)
    {
        ProjectSetup? setup = old.Setup;
        bool valid = old.ObservedAt == now && old.Freshness == ProjectContextFreshness.Fresh
            && old.AssemblyOutcome == ProjectContextAssemblyOutcome.Assembled;
        if (setup is null && current.Setup is not null)
        {
            valid &= currentEmptySetup && Same(current.Setup, ProjectSetup.Empty);
            setup = ProjectSetup.Empty;
        }
        left = Canonical(new { old.ProjectId, old.Lifecycle, Setup = setup, old.ProjectFolder, old.Conversations, old.FileReferences, old.MemoryReferences, old.Excluded });
        right = Canonical(new { current.ProjectId, current.Lifecycle, current.Setup, current.ProjectFolder, current.Conversations, current.FileReferences, current.MemoryReferences, current.Excluded });
        return valid && JsonElement.DeepEquals(left, right);
    }

    private static bool LegacyListSource(MatrixLegacyList list, IReadOnlyDictionary<string, ProjectionRequest> histories)
        => list.Items.All(row => histories.TryGetValue(row.ProjectId, out var source)
            && LegacyDates(row.CreatedAt, row.UpdatedAt, source) && LegacyFreshness(row.Freshness, source.Events.Max(item => item.Timestamp), source.Events.Max(item => item.GlobalPosition)))
            && LegacyFreshness(list.Freshness, list.Items.Count == 0 ? DateTimeOffset.UnixEpoch : list.Items.Max(row => row.UpdatedAt),
                list.Items.Count == 0 ? 0 : list.Items.Max(row => histories[row.ProjectId].Events.Max(item => item.GlobalPosition)));

    private static bool LegacyOpenSource(MatrixLegacyOpen detail, IReadOnlyDictionary<string, ProjectionRequest> histories)
        => histories.TryGetValue(detail.ProjectId, out var source) && LegacyDates(detail.CreatedAt, detail.UpdatedAt, source)
            && LegacyFreshness(detail.Freshness, source.Events.Max(item => item.Timestamp), source.Events.Max(item => item.GlobalPosition))
            && detail.References.All(row => LegacyFreshness(row.Freshness, source.Events.Max(item => item.Timestamp), source.Events.Max(item => item.GlobalPosition)));

    private static bool LegacyDates(DateTimeOffset created, DateTimeOffset updated, ProjectionRequest source)
        => created == source.Events.Min(item => item.Timestamp) && updated == source.Events.Max(item => item.Timestamp);

    private static bool LegacyFreshness(MatrixLegacyFreshness freshness, DateTimeOffset observation, long global)
        => freshness.ReadConsistency == "eventually_consistent" && freshness.ObservedAt == observation
            && freshness.ProjectionWatermark == (global == 0 ? null : $"watermark_{global:D8}") && !freshness.Stale && freshness.TrustState == "trusted";

    private static T Read<T>(JsonElement input) => input.Deserialize<T>(MatrixTypedJson) ?? throw new JsonException("Missing typed response.");
    private static JsonElement Canonical<T>(T value) => JsonSerializer.SerializeToElement(value, MatrixTypedJson);
    private static bool Same<T>(T left, T right) => JsonElement.DeepEquals(Canonical(left), Canonical(right));
    private static ProjectLifecycle Lifecycle(string value) => Enum.Parse<ProjectLifecycle>(value, ignoreCase: true);
    private static ReferenceState State(string value) => Enum.Parse<ReferenceState>(value, ignoreCase: true);
    private static string ReferenceKind(string value) => value switch { "folder" => "ProjectFolder", "file" => "FileReference", "memory" => "MemoryReference", _ => throw new JsonException("Unknown reference kind.") };

}
#endif
