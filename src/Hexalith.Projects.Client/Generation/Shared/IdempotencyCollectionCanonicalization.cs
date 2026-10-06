using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace Hexalith.Projects.Client.Generation.Shared;

/// <summary>
/// Applies the field-scoped <c>x-hexalith-idempotency-collection-canonicalization</c> policy that the
/// Contract Spine declares on an idempotency equivalence property, and fails closed when a declaration
/// is unsupported or would be silently ignored by the helper generator. Array order stays caller-defined
/// unless a property opts in explicitly.
/// </summary>
internal sealed class IdempotencyCollectionCanonicalization
{
    /// <summary>
    /// The Contract Spine extension that declares a collection canonicalization policy on a property schema.
    /// </summary>
    internal const string ExtensionName = "x-hexalith-idempotency-collection-canonicalization";

    /// <summary>
    /// The only supported policy: an absent collection hashes as an empty array and items are sorted with
    /// <see cref="StringComparer.Ordinal"/>.
    /// </summary>
    internal const string OrdinalSortNullToEmptyPolicy = "ordinal-sort-null-to-empty";

    private readonly HashSet<YamlMappingNode> _appliedDeclarations = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Returns the helper value expression for a top-level request-body property, wrapped in the declared
    /// canonicalization when the property schema carries the policy.
    /// </summary>
    /// <param name="operationId">The operation declaring the idempotency equivalence field.</param>
    /// <param name="field">The idempotency equivalence field path.</param>
    /// <param name="propertySchema">The request-body property schema that the field resolves to.</param>
    /// <param name="expression">The C# expression that reads the property value.</param>
    /// <returns>The unchanged expression, or the canonicalizing expression when the policy is declared.</returns>
    internal string Apply(string operationId, string field, YamlMappingNode propertySchema, string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        ArgumentNullException.ThrowIfNull(propertySchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        if (!propertySchema.Children.TryGetValue(new YamlScalarNode(ExtensionName), out YamlNode? policyNode))
        {
            return expression;
        }

        string policy = policyNode.ShouldBeScalar(ExtensionName).Value ?? string.Empty;
        if (!string.Equals(policy, OrdinalSortNullToEmptyPolicy, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Operation {operationId} field '{field}' declares unsupported {ExtensionName} policy '{policy}'. " +
                $"The only supported policy is '{OrdinalSortNullToEmptyPolicy}'.");
        }

        if (!string.Equals(YamlContractLoader.RequiredScalar(propertySchema, "type"), "array", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Operation {operationId} field '{field}' declares {ExtensionName} but is not an array schema.");
        }

        _ = _appliedDeclarations.Add(propertySchema);
        return $"{expression} is null ? Array.Empty<string>() : {expression}.OrderBy(static item => item, StringComparer.Ordinal).ToArray()";
    }

    /// <summary>
    /// Throws when the Contract Spine declares the policy anywhere <see cref="Apply"/> did not canonicalize
    /// it — a nested property, an operation parameter, a referenced component schema, a property no
    /// equivalence list names, or a property overridden by a <c>oneOf</c> branch. Such a declaration would
    /// otherwise be ignored and let client fingerprints drift from the server.
    /// </summary>
    /// <param name="root">The Contract Spine root mapping.</param>
    internal void EnsureAllDeclarationsApplied(YamlMappingNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        List<string> ignored = [];
        CollectIgnoredDeclarations(root, string.Empty, new HashSet<YamlNode>(ReferenceEqualityComparer.Instance), ignored);
        if (ignored.Count > 0)
        {
            throw new InvalidOperationException(
                $"Contract Spine declares {ExtensionName} at {string.Join(", ", ignored.Select(static pointer => $"'{pointer}'"))}, " +
                "but the helper generator applies it only to top-level request-body properties named in " +
                "x-hexalith-idempotency-equivalence. Move or remove the declaration.");
        }
    }

    private void CollectIgnoredDeclarations(YamlNode node, string pointer, HashSet<YamlNode> visited, List<string> ignored)
    {
        if (!visited.Add(node))
        {
            return;
        }

        if (node is YamlMappingNode mapping)
        {
            if (mapping.Children.ContainsKey(new YamlScalarNode(ExtensionName)) && !_appliedDeclarations.Contains(mapping))
            {
                ignored.Add(pointer.Length == 0 ? "/" : pointer);
            }

            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                string key = entry.Key is YamlScalarNode scalar ? scalar.Value ?? string.Empty : entry.Key.ToString();
                CollectIgnoredDeclarations(entry.Value, pointer + "/" + EscapePointerSegment(key), visited, ignored);
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            for (int index = 0; index < sequence.Children.Count; index++)
            {
                CollectIgnoredDeclarations(sequence.Children[index], pointer + "/" + index.ToString(CultureInfo.InvariantCulture), visited, ignored);
            }
        }
    }

    private static string EscapePointerSegment(string segment)
        => segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
}
