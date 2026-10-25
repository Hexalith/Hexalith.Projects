#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

/// <summary>Sanitized observation of every token POST, including ordinary and malformed OAuth attempts.</summary>
/// <param name="State">Whether authorization details were present and well formed.</param>
/// <param name="FieldCount">The transmitted form field count.</param>
/// <param name="DetailsCount">The transmitted authorization-details value count.</param>
/// <param name="Audience">The decoded profile audience, if usable.</param>
/// <param name="Operation">The decoded profile operation, if usable.</param>
/// <param name="BindingKeys">Binding names only; credentials and raw form values are never retained.</param>
/// <param name="ValidScope">Whether the scope exactly matched the decoded profile.</param>
internal sealed record ProjectAuthorityTokenAttempt(string State, int FieldCount, int DetailsCount, string? Audience, string? Operation, string[] BindingKeys, bool ValidScope)
{
    /// <summary>Gets the sanitized sequence entry.</summary>
    public string Summary => $"broker:{State}:{Audience ?? "-"}:{Operation ?? "-"}:{BindingKeys.Length}";
}
#endif
