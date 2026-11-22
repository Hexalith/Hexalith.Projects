namespace Hexalith.Projects.Contracts.Queries;

/// <summary>One stable, committed observation safe to disclose in the Project timeline.</summary>
/// <param name="AuditEventId">The opaque, Tenant-bound identity of the event.</param>
/// <param name="Operation">The allow-listed operation.</param>
/// <param name="Outcome">The committed outcome.</param>
/// <param name="OccurredAt">The timestamp recorded by the producer.</param>
/// <param name="ReceiptId">The opaque upstream receipt, when one was recorded.</param>
/// <param name="ProjectCount">The Project count for a descriptive inspection.</param>
/// <param name="FieldClass">The inspected class of fields.</param>
public sealed record ProjectAuditFact(string AuditEventId, string Operation, string Outcome, DateTimeOffset OccurredAt,
    string? ReceiptId = null, int? ProjectCount = null, string? FieldClass = null);
