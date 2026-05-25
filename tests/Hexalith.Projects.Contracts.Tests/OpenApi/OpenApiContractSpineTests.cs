// <copyright file="OpenApiContractSpineTests.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Tests.OpenApi;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Hexalith.Projects.Contracts.Models;

using Shouldly;

using Xunit;

using YamlDotNet.RepresentationModel;

/// <summary>
/// Spine-shape, vocabulary, and payload-safety assertions for the OpenAPI 3.1 Contract Spine
/// (Story 1.3). The class name contains <c>OpenApi</c> so the CI fingerprint/contract gate filter
/// (<c>FullyQualifiedName~...OpenApi</c>) can target it. Pure Tier-1: reads the checked-in spine from
/// disk, no network/containers/Dapr.
/// </summary>
public sealed class OpenApiContractSpineTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string OpenApiPath = Path.Combine(RepositoryRoot, "src", "Hexalith.Projects.Contracts", "openapi", "hexalith.projects.v1.yaml");

    [Fact]
    public void Spine_IsOpenApi31WithSharedEnvelope()
    {
        YamlMappingNode root = LoadYamlMapping(OpenApiPath);

        GetScalar(root, "openapi").ShouldBe("3.1.0");
        GetScalar(RequiredMapping(root, "info"), "title").ShouldBe("Hexalith.Projects API");
        GetScalar(RequiredMapping(root, "info"), "version").ShouldBe("v1");

        // URL-versioned /api/v1 server.
        YamlSequenceNode servers = RequiredSequence(root, "servers");
        servers.OfType<YamlMappingNode>().Select(s => GetScalar(s, "url")).ShouldContain("/api/v1");

        // OIDC bearer security applied globally.
        RequiredSequence(root, "security").Children.ShouldNotBeEmpty();
        RequiredMapping(RequiredMapping(root, "paths"), "/api/v1/projects");

        YamlMappingNode components = RequiredMapping(root, "components");
        RequiredMapping(RequiredMapping(components, "securitySchemes"), "oidcBearer");

        // Reusable header parameters.
        YamlMappingNode parameters = RequiredMapping(components, "parameters");
        RequiredMapping(parameters, "IdempotencyKey");
        RequiredMapping(parameters, "CorrelationId");
        RequiredMapping(parameters, "TaskId");
        RequiredMapping(parameters, "Freshness");
        RequiredMapping(parameters, "ProjectId");
        RequiredMapping(parameters, "LifecycleFilter");

        // Reusable response headers.
        YamlMappingNode headers = RequiredMapping(components, "headers");
        RequiredMapping(headers, "CorrelationId");
        RequiredMapping(headers, "TaskId");
        RequiredMapping(headers, "Freshness");

        // Cross-cutting envelope schemas.
        YamlMappingNode schemas = RequiredMapping(components, "schemas");
        RequiredMapping(schemas, "OpaqueIdentifier");
        RequiredMapping(schemas, "UtcDateTime");
        RequiredMapping(schemas, "AcceptedCommand");
        RequiredMapping(schemas, "FreshnessMetadata");
        RequiredMapping(schemas, "ProblemDetails");
        RequiredMapping(schemas, "SafeAuthorizationDenial");
        RequiredMapping(schemas, "ValidationFailure");
        RequiredMapping(schemas, "IdempotencyConflict");
        RequiredMapping(schemas, "CanonicalErrorCategory");

        // Shared responses.
        YamlMappingNode responses = RequiredMapping(components, "responses");
        RequiredMapping(responses, "AcceptedCommand");
        RequiredMapping(responses, "ValidationFailure");
        RequiredMapping(responses, "IdempotencyConflict");
        RequiredMapping(responses, "SafeAuthorizationDenial401");
        RequiredMapping(responses, "SafeAuthorizationDenial403");
        RequiredMapping(responses, "SafeAuthorizationDenial404");

        // Every $ref resolves.
        ResolveRefs(root);
    }

    [Fact]
    public void Spine_ProblemDetails_CarriesRfc9457AndHexalithExtensions()
    {
        YamlMappingNode schemas = Schemas();
        YamlMappingNode problem = RequiredMapping(schemas, "ProblemDetails");

        // RFC 9457 keeps additionalProperties: true for the extension surface (matches Folders).
        GetScalar(problem, "additionalProperties").ShouldBe("true");

        string[] problemFields = RequiredMapping(problem, "properties").Children.Keys
            .OfType<YamlScalarNode>()
            .Select(key => key.Value ?? string.Empty)
            .ToArray();

        foreach (string required in new[]
        {
            "type", "title", "status", "category", "code", "message",
            "correlationId", "retryable", "clientAction", "details",
        })
        {
            problemFields.ShouldContain(required);
        }

        // clientAction enum carries the canonical Hexalith vocabulary.
        string[] clientActions = RequiredEnumValues(RequiredMapping(RequiredMapping(problem, "properties"), "clientAction"));
        clientActions.ShouldBe(new[]
        {
            "retry", "revise_request", "check_credentials",
            "wait_for_reconciliation", "contact_operator", "no_action",
        });

        // details is metadata-only (string|number|boolean), never a free-form payload.
        YamlMappingNode details = RequiredMapping(RequiredMapping(problem, "properties"), "details");
        details.Children.ContainsKey(new YamlScalarNode("additionalProperties")).ShouldBeTrue();
    }

    [Fact]
    public void Spine_AcceptedCommand_IsClosedEnvelopeForCommandAsync()
    {
        YamlMappingNode accepted = RequiredMapping(Schemas(), "AcceptedCommand");

        // Closed envelope: additionalProperties: false.
        GetScalar(accepted, "additionalProperties").ShouldBe("false");

        string[] required = RequiredSequence(accepted, "required")
            .OfType<YamlScalarNode>().Select(n => n.Value ?? string.Empty).ToArray();
        required.ShouldContain("acceptedAt");
        required.ShouldContain("correlationId");
        required.ShouldContain("taskId");
        required.ShouldContain("status");

        string[] statusEnum = RequiredEnumValues(RequiredMapping(RequiredMapping(accepted, "properties"), "status"));
        statusEnum.ShouldBe(new[] { "accepted" });

        // idempotentReplay is the optional replay flag.
        RequiredMapping(accepted, "properties").Children.ContainsKey(new YamlScalarNode("idempotentReplay")).ShouldBeTrue();
    }

    [Fact]
    public void Spine_SeedMutation_IsCommandAsync202WithRequiredIdempotencyKeyAndSafeDenial()
    {
        YamlMappingNode post = SeedMutation();

        post.Children.ContainsKey(new YamlScalarNode("requestBody")).ShouldBeTrue();

        // 202 AcceptedCommand (command-async, no read-after-write).
        YamlMappingNode responses = RequiredMapping(post, "responses");
        GetScalar(RequiredMapping(responses, "202"), "$ref").ShouldBe("#/components/responses/AcceptedCommand");
        GetScalar(RequiredMapping(responses, "400"), "$ref").ShouldBe("#/components/responses/ValidationFailure");
        GetScalar(RequiredMapping(responses, "401"), "$ref").ShouldBe("#/components/responses/SafeAuthorizationDenial401");
        GetScalar(RequiredMapping(responses, "403"), "$ref").ShouldBe("#/components/responses/SafeAuthorizationDenial403");
        GetScalar(RequiredMapping(responses, "404"), "$ref").ShouldBe("#/components/responses/SafeAuthorizationDenial404");
        GetScalar(RequiredMapping(responses, "409"), "$ref").ShouldBe("#/components/responses/IdempotencyConflict");

        // Idempotency-Key required on mutations, rejected on queries.
        YamlMappingNode idempotency = RequiredMapping(post, "x-hexalith-idempotency-key");
        GetScalar(idempotency, "required").ShouldBe("true");
        GetScalar(idempotency, "queryBehavior").ShouldBe("rejected-if-present");

        // Field-scoped equivalence list declared and ordinal-ascending.
        string[] equivalence = RequiredSequence(post, "x-hexalith-idempotency-equivalence")
            .OfType<YamlScalarNode>().Select(n => n.Value ?? string.Empty).ToArray();
        equivalence.ShouldNotBeEmpty();
        equivalence.ShouldBe(equivalence.OrderBy(f => f, StringComparer.Ordinal).ToArray());

        // Mutation parameters thread the idempotency key + correlation + task headers.
        string[] parameterRefs = RequiredSequence(post, "parameters")
            .OfType<YamlMappingNode>().Select(p => GetScalar(p, "$ref") ?? string.Empty).ToArray();
        parameterRefs.ShouldContain("#/components/parameters/IdempotencyKey");
        parameterRefs.ShouldContain("#/components/parameters/CorrelationId");
        parameterRefs.ShouldContain("#/components/parameters/TaskId");
    }

    [Fact]
    public void Spine_QueryReads_CarryFreshnessAndRejectIdempotencyKey()
    {
        YamlMappingNode get = SeedQuery();
        YamlMappingNode list = ListQuery();

        // Idempotency-Key is NOT a parameter on the query.
        foreach (YamlMappingNode operation in new[] { get, list })
        {
            string[] parameterRefs = RequiredSequence(operation, "parameters")
                .OfType<YamlMappingNode>().Select(p => GetScalar(p, "$ref") ?? string.Empty).ToArray();
            parameterRefs.ShouldContain("#/components/parameters/Freshness");
            parameterRefs.ShouldContain("#/components/parameters/CorrelationId");
            parameterRefs.ShouldNotContain("#/components/parameters/IdempotencyKey");
        }

        // 200 response carries the X-Hexalith-Freshness header + a freshness body field.
        YamlMappingNode ok = RequiredMapping(RequiredMapping(get, "responses"), "200");
        YamlMappingNode okHeaders = RequiredMapping(ok, "headers");
        okHeaders.Children.ContainsKey(new YamlScalarNode("X-Hexalith-Freshness")).ShouldBeTrue();

        // Safe denial set on the query.
        YamlMappingNode responses = RequiredMapping(get, "responses");
        responses.Children.ContainsKey(new YamlScalarNode("401")).ShouldBeTrue();
        responses.Children.ContainsKey(new YamlScalarNode("403")).ShouldBeTrue();
        responses.Children.ContainsKey(new YamlScalarNode("404")).ShouldBeTrue();

        // Status body schema carries freshness metadata.
        YamlMappingNode statusSchema = RequiredMapping(Schemas(), "Project");
        RequiredMapping(statusSchema, "properties").Children.ContainsKey(new YamlScalarNode("freshness")).ShouldBeTrue();
    }

    [Fact]
    public void Spine_GetProject_IsExpandedOpenProjectQuery()
    {
        YamlMappingNode get = SeedQuery();
        GetScalar(get, "operationId").ShouldBe("GetProject");

        YamlMappingNode ok = RequiredMapping(RequiredMapping(get, "responses"), "200");
        string okSchemaRef = GetScalar(
            RequiredMapping(RequiredMapping(RequiredMapping(ok, "content"), "application/json"), "schema"),
            "$ref") ?? string.Empty;
        okSchemaRef.ShouldBe("#/components/schemas/Project");

        // The Project schema is a closed, metadata-only Open Project envelope carrying lifecycle,
        // setup metadata, context activation, empty-safe reference summaries, and freshness/trust state.
        YamlMappingNode project = RequiredMapping(Schemas(), "Project");
        GetScalar(project, "additionalProperties").ShouldBe("false");
        string[] required = RequiredSequence(project, "required")
            .OfType<YamlScalarNode>().Select(n => n.Value ?? string.Empty).ToArray();
        required.ShouldContain("projectId");
        required.ShouldContain("name");
        required.ShouldContain("lifecycleState");
        required.ShouldContain("createdAt");
        required.ShouldContain("updatedAt");
        required.ShouldContain("setupMetadata");
        required.ShouldContain("contextActivation");
        required.ShouldContain("references");
        required.ShouldContain("freshness");

        YamlMappingNode properties = RequiredMapping(project, "properties");
        GetScalar(RequiredMapping(properties, "setupMetadata"), "type").ShouldBe("string");
        GetScalar(RequiredMapping(properties, "contextActivation"), "$ref").ShouldBe("#/components/schemas/ContextActivation");
        GetScalar(RequiredMapping(RequiredMapping(properties, "references"), "items"), "$ref").ShouldBe("#/components/schemas/ProjectReferenceSummary");
    }

    [Fact]
    public void Spine_ListProjects_IsTenantScopedLifecycleFilterableQuery()
    {
        YamlMappingNode get = ListQuery();
        GetScalar(get, "operationId").ShouldBe("ListProjects");

        string[] parameterRefs = RequiredSequence(get, "parameters")
            .OfType<YamlMappingNode>().Select(p => GetScalar(p, "$ref") ?? string.Empty).ToArray();
        parameterRefs.ShouldContain("#/components/parameters/LifecycleFilter");
        parameterRefs.ShouldContain("#/components/parameters/CorrelationId");
        parameterRefs.ShouldContain("#/components/parameters/Freshness");
        parameterRefs.ShouldNotContain("#/components/parameters/IdempotencyKey");

        string[] lifecycleFilter = RequiredEnumValues(RequiredMapping(RequiredMapping(Parameters(), "LifecycleFilter"), "schema"));
        lifecycleFilter.ShouldBe(new[] { "active", "archived", "all" });

        YamlMappingNode ok = RequiredMapping(RequiredMapping(get, "responses"), "200");
        RequiredMapping(ok, "headers").Children.ContainsKey(new YamlScalarNode("X-Hexalith-Freshness")).ShouldBeTrue();
        string okSchemaRef = GetScalar(
            RequiredMapping(RequiredMapping(RequiredMapping(ok, "content"), "application/json"), "schema"),
            "$ref") ?? string.Empty;
        okSchemaRef.ShouldBe("#/components/schemas/ProjectListResponse");

        YamlMappingNode list = RequiredMapping(Schemas(), "ProjectListResponse");
        GetScalar(list, "additionalProperties").ShouldBe("false");
        string[] required = RequiredSequence(list, "required")
            .OfType<YamlScalarNode>().Select(n => n.Value ?? string.Empty).ToArray();
        required.ShouldContain("items");
        required.ShouldContain("freshness");

        YamlMappingNode item = RequiredMapping(Schemas(), "ProjectListItem");
        GetScalar(item, "additionalProperties").ShouldBe("false");
        string[] itemProperties = RequiredMapping(item, "properties").Children.Keys
            .OfType<YamlScalarNode>().Select(n => n.Value ?? string.Empty).ToArray();
        itemProperties.ShouldContain("projectId");
        itemProperties.ShouldContain("name");
        itemProperties.ShouldContain("lifecycleState");
        itemProperties.ShouldContain("createdAt");
        itemProperties.ShouldContain("updatedAt");
        itemProperties.ShouldContain("freshness");
        itemProperties.ShouldNotContain("tenantId");
    }

    [Fact]
    public void Spine_CreateProject_IsTheRealCommandAsyncMutation()
    {
        // The grown CreateProject mutation keeps the real command-async envelope: 202 + ProblemDetails
        // set + required idempotency key + the unchanged equivalence list (so the generated client stays
        // byte-stable).
        YamlMappingNode post = SeedMutation();
        GetScalar(post, "operationId").ShouldBe("CreateProject");

        string[] equivalence = RequiredSequence(post, "x-hexalith-idempotency-equivalence")
            .OfType<YamlScalarNode>().Select(n => n.Value ?? string.Empty).ToArray();
        equivalence.ShouldBe(new[] { "project_metadata.display_name", "request_schema_version" });

        // Request body is the CreateProjectRequest schema (display name + request schema version).
        string requestSchemaRef = GetScalar(
            RequiredMapping(
                RequiredMapping(
                    RequiredMapping(RequiredMapping(post, "requestBody"), "content"),
                    "application/json"),
                "schema"),
            "$ref") ?? string.Empty;
        requestSchemaRef.ShouldBe("#/components/schemas/CreateProjectRequest");
    }

    [Fact]
    public void Spine_UpdateSetupAndArchive_AreCommandAsyncMutations()
    {
        YamlMappingNode update = RequiredMapping(RequiredMapping(RequiredMapping(LoadYamlMapping(OpenApiPath), "paths"), "/api/v1/projects/{projectId}/setup"), "patch");
        YamlMappingNode archive = RequiredMapping(RequiredMapping(RequiredMapping(LoadYamlMapping(OpenApiPath), "paths"), "/api/v1/projects/{projectId}/archive"), "post");

        GetScalar(update, "operationId").ShouldBe("UpdateProjectSetup");
        GetScalar(archive, "operationId").ShouldBe("ArchiveProject");

        foreach (YamlMappingNode operation in new[] { update, archive })
        {
            YamlMappingNode responses = RequiredMapping(operation, "responses");
            GetScalar(RequiredMapping(responses, "202"), "$ref").ShouldBe("#/components/responses/AcceptedCommand");
            GetScalar(RequiredMapping(responses, "400"), "$ref").ShouldBe("#/components/responses/ValidationFailure");
            GetScalar(RequiredMapping(responses, "404"), "$ref").ShouldBe("#/components/responses/SafeAuthorizationDenial404");
            GetScalar(RequiredMapping(responses, "409"), "$ref").ShouldBe("#/components/responses/IdempotencyConflict");

            string[] parameterRefs = RequiredSequence(operation, "parameters")
                .OfType<YamlMappingNode>().Select(p => GetScalar(p, "$ref") ?? string.Empty).ToArray();
            parameterRefs.ShouldContain("#/components/parameters/ProjectId");
            parameterRefs.ShouldContain("#/components/parameters/IdempotencyKey");
            parameterRefs.ShouldContain("#/components/parameters/CorrelationId");
            parameterRefs.ShouldContain("#/components/parameters/TaskId");

            string[] equivalence = RequiredSequence(operation, "x-hexalith-idempotency-equivalence")
                .OfType<YamlScalarNode>().Select(n => n.Value ?? string.Empty).ToArray();
            equivalence.ShouldNotBeEmpty();
            equivalence.ShouldBe(equivalence.OrderBy(f => f, StringComparer.Ordinal).ToArray());
        }
    }

    [Fact]
    public void Spine_ProjectSetupSchemas_AreClosedCamelCaseAndMetadataOnly()
    {
        YamlMappingNode schemas = Schemas();
        foreach (string schemaName in new[] { "UpdateProjectSetupRequest", "ArchiveProjectRequest", "ProjectSetup", "ConversationStartDefaults" })
        {
            YamlMappingNode schema = RequiredMapping(schemas, schemaName);
            GetScalar(schema, "additionalProperties").ShouldBe("false");
            foreach (string property in RequiredMapping(schema, "properties").Children.Keys
                .OfType<YamlScalarNode>().Select(k => k.Value ?? string.Empty))
            {
                Regex.IsMatch(property, "^[a-z][A-Za-z0-9]*$").ShouldBeTrue($"property '{property}' on {schemaName} must be camelCase");
            }
        }

        RequiredMapping(RequiredMapping(Schemas(), "Project"), "properties")
            .Children.ContainsKey(new YamlScalarNode("projectSetup")).ShouldBeTrue();

        RequiredEnumValues(RequiredMapping(schemas, "ProjectContextSourceKind"))
            .ShouldBe(["conversation", "projectFolder", "fileReference", "memory"]);
        RequiredEnumValues(RequiredMapping(schemas, "LinkedSourcePolicy"))
            .ShouldBe(["none", "projectsOwnedMetadataOnly", "authorizedReferences"]);
    }

    [Fact]
    public void Spine_UsesCamelCaseAndIsoDateTimeAndOpaqueProjectIds()
    {
        YamlMappingNode schemas = Schemas();

        // ISO-8601 DateTimeOffset for dates.
        YamlMappingNode utc = RequiredMapping(schemas, "UtcDateTime");
        GetScalar(utc, "type").ShouldBe("string");
        GetScalar(utc, "format").ShouldBe("date-time");

        // Project ids are opaque strings (NOT GUIDs) — pattern rejects dots/colons.
        YamlMappingNode opaque = RequiredMapping(schemas, "OpaqueIdentifier");
        GetScalar(opaque, "type").ShouldBe("string");

        // camelCase property names across closed envelope schemas.
        foreach (string schemaName in new[]
        {
            "AcceptedCommand",
            "FreshnessMetadata",
            "CreateProjectRequest",
            "ProjectLifecycleStatus",
            "Project",
            "ProjectListResponse",
            "ProjectListItem",
            "ProjectReferenceSummary",
            "ContextActivation",
        })
        {
            foreach (string property in RequiredMapping(RequiredMapping(schemas, schemaName), "properties").Children.Keys
                .OfType<YamlScalarNode>().Select(k => k.Value ?? string.Empty))
            {
                Regex.IsMatch(property, "^[a-z][A-Za-z0-9]*$").ShouldBeTrue($"property '{property}' on {schemaName} must be camelCase");
            }
        }
    }

    [Fact]
    public void Spine_ReusesSharedVocabularyForLifecycleAndErrorCategories()
    {
        YamlMappingNode schemas = Schemas();

        // Lifecycle state values mirror the shared ProjectLifecycle vocabulary (name-based, lowercased
        // on the wire) — never a parallel enum. The shared C# enum members are Active / Archived.
        string[] lifecycle = RequiredEnumValues(RequiredMapping(schemas, "ProjectLifecycleState"));
        lifecycle.ShouldBe(new[] { "active", "archived" });

        foreach (string member in Enum.GetNames<Hexalith.Projects.Contracts.Ui.ProjectLifecycle>())
        {
            lifecycle.ShouldContain(member.ToLowerInvariant(), $"spine lifecycle must include shared vocabulary member '{member}'.");
        }

        // Canonical error categories are a curated subset alignment (not a parallel enum).
        string[] categories = RequiredEnumValues(RequiredMapping(schemas, "CanonicalErrorCategory"));
        categories.ShouldContain("validation_error");
        categories.ShouldContain("idempotency_conflict");
        categories.ShouldContain("tenant_access_denied");
        categories.ShouldContain("not_found");
        categories.ShouldContain("internal_error");
    }

    [Fact]
    public void Spine_ExamplesContainNoForbiddenPayloadOrSecretsOrLocalPaths()
    {
        string spine = File.ReadAllText(OpenApiPath);

        // Tenant authority is documented as claims/envelope-derived, never client-controlled.
        spine.ShouldContain("Tenant authority comes from authenticated principal claims and EventStore envelopes");

        // Cross-check Story 1.2 forbidden-content denylist against the spine's EXAMPLE values only.
        // The denylist enumerates payload categories (transcript text, file contents, secrets, raw
        // tokens, local paths, ...) that must never appear as concrete example material. We scope the
        // check to example node values (not the whole document) because the cross-cutting envelope
        // legitimately *documents* these categories in prose ("File contents, diffs, tokens, secrets,
        // ... are forbidden") and declares a metadata-sensitivity classification vocabulary
        // (SensitiveMetadataTier: public_metadata / tenant_sensitive / credential_sensitive / secret).
        // Those are contract vocabulary, not leaked payloads — the canonical Hexalith.Folders spine
        // contains the same prose and enum. Synthetic example *values* must still stay payload-free.
        string exampleText = CollectExampleText(LoadYamlMapping(OpenApiPath));
        foreach (string forbidden in PayloadClassification.ForbiddenContent)
        {
            exampleText.ShouldNotContain(forbidden, Case.Insensitive, $"spine examples must not embed forbidden content category '{forbidden}'.");
            string snake = ToSnake(forbidden);
            exampleText.ShouldNotContain(snake, Case.Insensitive, $"spine examples must not embed forbidden content category '{snake}'.");
        }

        // No machine-local absolute paths.
        foreach (string prefix in new[] { "C:\\", "D:\\", "E:\\", "/home/", "/Users/", "/root/", "/var/" })
        {
            spine.ShouldNotContain(prefix, Case.Sensitive, $"spine must not embed host-specific path prefix {prefix}.");
        }

        // No token-shaped strings or real hosts.
        foreach (string pattern in new[]
        {
            @"eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}",
            @"sk_(live|test)_[A-Za-z0-9]{16,}",
            @"ghp_[A-Za-z0-9]{20,}",
            @"-----BEGIN",
        })
        {
            Regex.IsMatch(spine, pattern).ShouldBeFalse($"spine must not contain token-shaped strings matching {pattern}.");
        }

        // No tenant-authority field is client-controllable.
        AssertNoTenantAuthorityField(LoadYamlMapping(OpenApiPath));
    }

    /// <summary>
    /// Concatenates the scalar values of every <c>example</c>/<c>examples</c> node in the spine so the
    /// payload-safety denylist can be enforced against concrete example material only (not the
    /// envelope's descriptive prose or its classification-vocabulary enums).
    /// </summary>
    private static string CollectExampleText(YamlNode node)
    {
        System.Text.StringBuilder builder = new();
        CollectExampleText(node, builder);
        return builder.ToString();
    }

    private static void CollectExampleText(YamlNode node, System.Text.StringBuilder builder)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
                {
                    if (child.Key is YamlScalarNode key && key.Value is "example" or "examples")
                    {
                        AppendScalars(child.Value, builder);
                    }

                    CollectExampleText(child.Value, builder);
                }

                break;
            case YamlSequenceNode sequence:
                foreach (YamlNode child in sequence.Children)
                {
                    CollectExampleText(child, builder);
                }

                break;
        }
    }

    private static void AppendScalars(YamlNode node, System.Text.StringBuilder builder)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                builder.Append(scalar.Value).Append('\n');
                break;
            case YamlMappingNode mapping:
                foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
                {
                    AppendScalars(child.Value, builder);
                }

                break;
            case YamlSequenceNode sequence:
                foreach (YamlNode child in sequence.Children)
                {
                    AppendScalars(child, builder);
                }

                break;
        }
    }

    private static string ToSnake(string pascal)
    {
        System.Text.StringBuilder builder = new(pascal.Length + 8);
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (char.IsUpper(c) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static void AssertNoTenantAuthorityField(YamlNode node)
    {
        if (node is YamlMappingNode mapping)
        {
            if (mapping.Children.TryGetValue(new YamlScalarNode("properties"), out YamlNode? propsNode) && propsNode is YamlMappingNode props)
            {
                foreach (YamlNode key in props.Children.Keys)
                {
                    if (key is YamlScalarNode scalar && scalar.Value is { } value)
                    {
                        value.Equals("tenantId", StringComparison.OrdinalIgnoreCase).ShouldBeFalse($"tenant authority must not be a client-controlled field; found '{value}'.");
                        value.Equals("tenant_id", StringComparison.OrdinalIgnoreCase).ShouldBeFalse($"tenant authority must not be a client-controlled field; found '{value}'.");
                        value.Equals("managedTenantId", StringComparison.OrdinalIgnoreCase).ShouldBeFalse($"tenant authority must not be a client-controlled field; found '{value}'.");
                    }
                }
            }

            foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
            {
                AssertNoTenantAuthorityField(child.Value);
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                AssertNoTenantAuthorityField(child);
            }
        }
    }

    private static YamlMappingNode Schemas() =>
        RequiredMapping(RequiredMapping(LoadYamlMapping(OpenApiPath), "components"), "schemas");

    private static YamlMappingNode Parameters() =>
        RequiredMapping(RequiredMapping(LoadYamlMapping(OpenApiPath), "components"), "parameters");

    private static YamlMappingNode SeedMutation() =>
        RequiredMapping(RequiredMapping(RequiredMapping(LoadYamlMapping(OpenApiPath), "paths"), "/api/v1/projects"), "post");

    private static YamlMappingNode ListQuery() =>
        RequiredMapping(RequiredMapping(RequiredMapping(LoadYamlMapping(OpenApiPath), "paths"), "/api/v1/projects"), "get");

    private static YamlMappingNode SeedQuery() =>
        RequiredMapping(RequiredMapping(RequiredMapping(LoadYamlMapping(OpenApiPath), "paths"), "/api/v1/projects/{projectId}"), "get");

    private static string[] RequiredEnumValues(YamlMappingNode schema) =>
        RequiredSequence(schema, "enum").OfType<YamlScalarNode>().Select(v => v.Value ?? string.Empty).ToArray();

    private static void ResolveRefs(YamlMappingNode root)
    {
        foreach (string reference in EnumerateRefs(root))
        {
            reference.StartsWith("#/", StringComparison.Ordinal).ShouldBeTrue(reference);
            ResolvePointer(root, reference[2..].Split('/')).ShouldNotBeNull(reference);
        }
    }

    private static IEnumerable<string> EnumerateRefs(YamlNode node)
    {
        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
            {
                if (child.Key is YamlScalarNode { Value: "$ref" } && child.Value is YamlScalarNode reference)
                {
                    yield return reference.Value ?? string.Empty;
                }

                foreach (string nested in EnumerateRefs(child.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                foreach (string reference in EnumerateRefs(child))
                {
                    yield return reference;
                }
            }
        }
    }

    private static YamlNode? ResolvePointer(YamlNode root, IReadOnlyList<string> segments)
    {
        YamlNode current = root;
        foreach (string segment in segments)
        {
            string key = segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            if (current is not YamlMappingNode mapping || !mapping.Children.TryGetValue(new YamlScalarNode(key), out current!))
            {
                return null;
            }
        }

        return current;
    }

    private static YamlMappingNode LoadYamlMapping(string path)
    {
        File.Exists(path).ShouldBeTrue(path);
        using StreamReader reader = File.OpenText(path);
        YamlStream yaml = new();
        yaml.Load(reader);
        return yaml.Documents[0].RootNode.ShouldBeOfType<YamlMappingNode>();
    }

    private static YamlMappingNode RequiredMapping(YamlMappingNode mapping, string key)
    {
        mapping.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlMappingNode>();
    }

    private static YamlSequenceNode RequiredSequence(YamlMappingNode mapping, string key)
    {
        mapping.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlSequenceNode>();
    }

    private static string? GetScalar(YamlMappingNode mapping, string key)
    {
        mapping.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlScalarNode>().Value;
    }

    private static string FindRepositoryRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "Hexalith.Projects.slnx")))
            {
                return current;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            current = parent?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
