namespace Hexalith.Projects.Contracts.Queries;

/// <summary>An operator status observation with no reserved Project or resource identities.</summary>
/// <param name="State">Availability of the authoritative observation.</param>
/// <param name="TaskId">Opaque task identity after target authorization.</param>
/// <param name="Status">The last committed Durable Task status.</param>
/// <param name="Reason">Safe reason code.</param>
/// <param name="RecoveryActions">Applicable recovery codes.</param>
/// <param name="CreatedAt">Time of admission.</param>
/// <param name="UpdatedAt">Time of the last committed transition.</param>
/// <param name="ExpiresAt">Expiry recorded by the durable authority.</param>
public sealed record ProjectTaskStatusResponse(string State, string? TaskId, string? Status, string Reason,
    IReadOnlyList<string> RecoveryActions, DateTimeOffset? CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset? ExpiresAt)
{
    /// <summary>The committed reconciliation outcome when activation and current authority permit disclosure.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ReconciliationOutcome { get; init; }
}
