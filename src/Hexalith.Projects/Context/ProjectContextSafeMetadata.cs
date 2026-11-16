namespace Hexalith.Projects.Context;

using Hexalith.Projects.Contracts.Models;

/// <summary>Withholds descriptive labels on ordinary context reads without changing identities or array order.</summary>
public static class ProjectContextSafeMetadata
{
    /// <summary>Applies the ordinary context disclosure policy to supported admission.</summary>
    public static ProjectContextAdmission Sanitize(ProjectContextAdmission value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value with { ProjectFolder = Safe(value.ProjectFolder), Conversations = value.Conversations.Select(SafeRequired).ToArray(),
            FileReferences = value.FileReferences.Select(SafeRequired).ToArray(), MemoryReferences = value.MemoryReferences.Select(SafeRequired).ToArray() };
    }

    /// <summary>Applies the same ordinary disclosure policy while retaining the legacy response shape.</summary>
    public static ProjectContext Sanitize(ProjectContext value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value with { ProjectFolder = Safe(value.ProjectFolder), Conversations = value.Conversations.Select(SafeRequired).ToArray(),
            FileReferences = value.FileReferences.Select(SafeRequired).ToArray(), MemoryReferences = value.MemoryReferences.Select(SafeRequired).ToArray() };
    }

    private static ProjectContextReference? Safe(ProjectContextReference? value) => value is null ? null : SafeRequired(value);
    private static ProjectContextReference SafeRequired(ProjectContextReference value)
        => new(value.ReferenceKind, value.ReferenceId, null, value.ReferenceState, value.ReasonCode, value.ObservedAt);
}
