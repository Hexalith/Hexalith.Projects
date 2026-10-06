// <copyright file="IdempotencyCollectionCanonicalizationTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Client.Tests;

using System;
using System.IO;

using Hexalith.Projects.Client.Generation.Shared;

using Shouldly;

using Xunit;

using YamlDotNet.RepresentationModel;

/// <summary>
/// Direct coverage of the field-scoped collection canonicalization policy that the idempotency-helper
/// generator applies to Contract Spine equivalence properties. Pure Tier-1: parses inline YAML fixtures.
/// </summary>
public sealed class IdempotencyCollectionCanonicalizationTests
{
    private const string Expression = "FileReferenceIds";

    [Fact]
    public void UnannotatedTopLevelArrayKeepsCallerOrder()
    {
        YamlMappingNode property = Parse("""
            type: array
            uniqueItems: true
            items:
              type: string
            """);

        new IdempotencyCollectionCanonicalization()
            .Apply("ConfirmNewProjectProposal", "file_reference_ids", property, Expression)
            .ShouldBe(Expression);
    }

    [Fact]
    public void AnnotatedArrayIsOrdinalSortedWithNullAsEmpty()
    {
        YamlMappingNode property = Parse("""
            type: array
            x-hexalith-idempotency-collection-canonicalization: ordinal-sort-null-to-empty
            items:
              type: string
            """);

        new IdempotencyCollectionCanonicalization()
            .Apply("ConfirmNewProjectProposal", "file_reference_ids", property, Expression)
            .ShouldBe("FileReferenceIds is null ? Array.Empty<string>() : FileReferenceIds.OrderBy(static item => item, StringComparer.Ordinal).ToArray()");
    }

    [Fact]
    public void UnsupportedPolicyFailsClosed()
    {
        YamlMappingNode property = Parse("""
            type: array
            x-hexalith-idempotency-collection-canonicalization: ordinal-sort
            """);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => new IdempotencyCollectionCanonicalization().Apply("ConfirmNewProjectProposal", "file_reference_ids", property, Expression));

        exception.Message.ShouldContain("ConfirmNewProjectProposal");
        exception.Message.ShouldContain("'file_reference_ids'");
        exception.Message.ShouldContain("unsupported x-hexalith-idempotency-collection-canonicalization policy 'ordinal-sort'");
    }

    [Fact]
    public void PolicyOnNonArrayFailsClosed()
    {
        YamlMappingNode property = Parse("""
            type: string
            x-hexalith-idempotency-collection-canonicalization: ordinal-sort-null-to-empty
            """);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => new IdempotencyCollectionCanonicalization().Apply("ConfirmNewProjectProposal", "description", property, Expression));

        exception.Message.ShouldContain("'description'");
        exception.Message.ShouldContain("is not an array schema");
    }

    [Fact]
    public void AppliedDeclarationPassesSpineCheck()
    {
        YamlMappingNode root = Parse("""
            components:
              schemas:
                ConfirmRequest:
                  properties:
                    fileReferenceIds:
                      type: array
                      x-hexalith-idempotency-collection-canonicalization: ordinal-sort-null-to-empty
            """);
        IdempotencyCollectionCanonicalization canonicalization = new();
        _ = canonicalization.Apply("ConfirmNewProjectProposal", "file_reference_ids", PropertyOf(root, "ConfirmRequest", "fileReferenceIds"), Expression);

        Should.NotThrow(() => canonicalization.EnsureAllDeclarationsApplied(root));
    }

    [Fact]
    public void DeclarationTheGeneratorDidNotApplyFailsClosed()
    {
        YamlMappingNode root = Parse("""
            paths:
              /projects/{projectId}/setup:
                put:
                  parameters:
                    - name: tags
                      in: query
                      schema:
                        type: array
                        x-hexalith-idempotency-collection-canonicalization: ordinal-sort-null-to-empty
            components:
              schemas:
                SetupRequest:
                  properties:
                    projectSetup:
                      type: object
                      properties:
                        goals:
                          type: array
                          x-hexalith-idempotency-collection-canonicalization: ordinal-sort-null-to-empty
            """);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => new IdempotencyCollectionCanonicalization().EnsureAllDeclarationsApplied(root));

        exception.Message.ShouldContain("'/paths/~1projects~1{projectId}~1setup/put/parameters/0/schema'");
        exception.Message.ShouldContain("'/components/schemas/SetupRequest/properties/projectSetup/properties/goals'");
    }

    private static YamlMappingNode PropertyOf(YamlMappingNode root, string schemaName, string propertyName)
    {
        YamlMappingNode schemas = YamlContractLoader.RequiredMapping(YamlContractLoader.RequiredMapping(root, "components"), "schemas");
        YamlMappingNode properties = YamlContractLoader.RequiredMapping(YamlContractLoader.RequiredMapping(schemas, schemaName), "properties");
        return YamlContractLoader.RequiredMapping(properties, propertyName);
    }

    private static YamlMappingNode Parse(string yaml)
    {
        YamlStream stream = new();
        stream.Load(new StringReader(yaml));
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }
}
