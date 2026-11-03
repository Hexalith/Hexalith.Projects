using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.DomainService;
using Hexalith.Projects.Contracts.Commands;

[assembly: RouteCatalogAdapterSource(typeof(Hexalith.Projects.Server.ProjectsIntentAdapters))]

namespace Hexalith.Projects.Server;

/// <summary>Binds the actual processor payloads through the standard SDK intent adapter.</summary>
public static class ProjectsIntentAdapters
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Gets all owner adapters without enabling admission or replacing the processor.</summary>
    public static IReadOnlyList<IDeclaredIdempotencyIntentAdapter> Create() =>
    [
        Adapter<CreateProjectPayload>(), Adapter<UpdateProjectSetupPayload>(), Adapter<ArchiveProjectPayload>(),
        Adapter<RestoreProjectPayload>(), Adapter<SetProjectFolderPayload>(), Adapter<LinkFileReferencePayload>(),
        Adapter<UnlinkFileReferencePayload>(), Adapter<LinkMemoryPayload>(), Adapter<UnlinkMemoryPayload>(),
        Adapter<ConfirmProjectResolutionPayload>(), Adapter<GrantProjectChatBotAuthority>(), Adapter<RevokeProjectChatBotAuthority>(),
    ];

    private static DeclaredIdempotencyIntentAdapter<T> Adapter<T>() => new(
        ProjectsRuntimeCatalogDeclarations.Commands.Single(command => command.Route.Execution!.HandlerType == typeof(T).FullName),
        Fields,
        bytes => Fields(JsonSerializer.Deserialize<T>(bytes.Span, PayloadOptions)
            ?? throw new InvalidOperationException("The command payload is missing.")));

    // Include every bound field, preserving null, list ordering, typed nested metadata and actor attribution.
    // The descriptor stays inside platform idempotency admission and never becomes response or audit payload.
    private static IReadOnlyDictionary<string, object?> Fields<T>(T payload) => new Dictionary<string, object?>
    {
        ["semantic-payload"] = JsonSerializer.Serialize(payload, PayloadOptions),
    };
}
