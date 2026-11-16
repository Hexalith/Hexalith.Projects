namespace Hexalith.Projects.Client.Tests;

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Projects.Client;

using Shouldly;
using Xunit;

/// <summary>Exercises exhaustive evidence admission, explicit rollback and immutable startup selection.</summary>
public sealed class ReadCutoverRoutingTests
{
    private const string Revision = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Contract = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData("null-receipt")]
    [InlineData("boolean-receipt")]
    [InlineData("inconsistent-total")]
    [InlineData("not-run")]
    [InlineData("inconsistent-passed")]
    [InlineData("missing-not-run")]
    public void MalformedOrUnexecutedNativeReceiptsSelectUnavailable(string fault)
    {
        string directory = Path.Combine(Path.GetTempPath(), "story67-xml-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string xml = "<assemblies><assembly total=\"1\" passed=\"1\" failed=\"0\" skipped=\"0\" errors=\"0\" not-run=\"0\"><collection><test name=\"control\" result=\"Pass\" /></collection></assembly></assemblies>";
            xml = fault switch
            {
                "inconsistent-total" => xml.Replace("total=\"1\"", "total=\"2\"", StringComparison.Ordinal),
                "not-run" => xml.Replace("not-run=\"0\"", "not-run=\"1\"", StringComparison.Ordinal),
                "missing-not-run" => xml.Replace(" not-run=\"0\"", string.Empty, StringComparison.Ordinal),
                "inconsistent-passed" => xml.Replace("passed=\"1\"", "passed=\"0\"", StringComparison.Ordinal),
                _ => xml,
            };
            byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(xml);
            File.WriteAllBytes(Path.Combine(directory, "native.xml"), xmlBytes);
            string xmlHash = Convert.ToHexString(SHA256.HashData(xmlBytes));
            JsonObject evidence = Evidence();
            evidence["nativeReceipts"] = new JsonArray(new JsonObject { ["path"] = "native.xml", ["sha256"] = xmlHash });
            if (fault == "null-receipt") ((JsonArray)evidence["nativeReceipts"]!)[0] = null;
            if (fault == "boolean-receipt") ((JsonArray)evidence["nativeReceipts"]!)[0] = true;
            foreach (JsonNode? cell in (JsonArray)evidence["cases"]!) { cell!["nativeXmlHash"] = xmlHash; cell["nativeTestName"] = "control"; }
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(evidence);
            string path = Path.Combine(directory, "receipt.json");
            File.WriteAllBytes(path, bytes);
            ProjectsReadRouteSelection.Create(new ProjectsReadRoutingOptions
            {
                Mode = "Supported", EvidencePath = path, EvidenceHash = Convert.ToHexString(SHA256.HashData(bytes)), SourceRevision = Revision, ContractHash = Contract,
            }).Mode.ShouldBe("Unavailable");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void CompleteSyntheticGateControlRequiresEveryExecutedCell()
    {
        JsonObject evidence = Evidence();
        Activate(evidence).ShouldBeTrue();
        var cases = (JsonArray)evidence["cases"]!;
        foreach (JsonNode? cell in cases.ToArray())
        {
            int position = cases.IndexOf(cell);
            cases.RemoveAt(position);
            Activate(evidence).ShouldBeFalse();
            cases.Insert(position, cell);
        }
        cases.Add(cases[0]!.DeepClone());
        Activate(evidence).ShouldBeFalse();
    }

    [Theory]
    [InlineData("executed")]
    [InlineData("passed")]
    [InlineData("canonicalOutput")]
    [InlineData("independentKeys")]
    [InlineData("exactPositions")]
    [InlineData("cursorScope")]
    [InlineData("isolation")]
    [InlineData("supportedOnlyAssertions")]
    [InlineData("noDomainWrites")]
    public void AnyFailedOrUnexecutedComparisonBlocksActivation(string field)
    {
        JsonObject evidence = Evidence();
        evidence["cases"]![0]![field] = false;
        Activate(evidence).ShouldBeFalse();
        ((JsonObject)evidence["cases"]![0]!).Remove(field);
        Activate(evidence).ShouldBeFalse();
    }

    [Fact]
    public void SkipsWrongRevisionContractOrRulesCannotPass()
    {
        foreach (string field in new[] { "sourceRevision", "contractHash", "normalizationRevision" })
        {
            JsonObject evidence = Evidence();
            evidence[field] = "other";
            Activate(evidence).ShouldBeFalse();
        }
        JsonObject skipped = Evidence();
        skipped["cases"]![0]!["skipped"] = true;
        Activate(skipped).ShouldBeFalse();
    }

    [Fact]
    public void ExplicitRollbackRequiresItsOwnSecurityProofAndFailureNeverChangesSelection()
    {
        string directory = Path.Combine(Path.GetTempPath(), "story67-rollback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            JsonObject evidence = Evidence();
            byte[] xml = System.Text.Encoding.UTF8.GetBytes("<assemblies><assembly total=\"1\" passed=\"1\" failed=\"0\" skipped=\"0\" errors=\"0\" not-run=\"0\"><collection><test name=\"synthetic-control\" result=\"Pass\" /></collection></assembly></assemblies>");
            File.WriteAllBytes(Path.Combine(directory, "native.xml"), xml);
            string xmlHash = Convert.ToHexString(SHA256.HashData(xml));
            evidence["nativeReceipts"] = new JsonArray(new JsonObject { ["path"] = "native.xml", ["sha256"] = xmlHash });
            foreach (JsonNode? cell in (JsonArray)evidence["cases"]!) { cell!["nativeXmlHash"] = xmlHash; cell["nativeTestName"] = "synthetic-control"; }
            string path = Path.Combine(directory, "receipt.json");
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(evidence);
            File.WriteAllBytes(path, bytes);
            ProjectsReadRoutingOptions options = new()
            {
                Mode = "Supported", EvidencePath = path, EvidenceHash = Convert.ToHexString(SHA256.HashData(bytes)),
                SourceRevision = Revision, ContractHash = Contract,
            };
            ProjectsReadRouteSelection selected = ProjectsReadRouteSelection.Create(options);
            selected.Mode.ShouldBe("Supported");
            options.Mode = "LegacyRollback";
            ProjectsReadRouteSelection.Create(options).Mode.ShouldBe("Unavailable");
            selected.Mode.ShouldBe("Supported");

            evidence["legacySecurity"] = new JsonArray(ProjectReadCutoverGate.Operations.Select(operation => (JsonNode)new JsonObject
            {
                ["operation"] = operation, ["executed"] = true, ["passed"] = true, ["currentAuthority"] = true,
                ["safeMetadata"] = true, ["safeDenial"] = true, ["noDomainWrites"] = true, ["nativeXmlHash"] = xmlHash, ["nativeTestName"] = "synthetic-control",
            }).ToArray());
            bytes = JsonSerializer.SerializeToUtf8Bytes(evidence);
            File.WriteAllBytes(path, bytes);
            options.EvidenceHash = Convert.ToHexString(SHA256.HashData(bytes));
            ProjectsReadRouteSelection.Create(options).Mode.ShouldBe("LegacyRollback");
            evidence["legacySecurity"]![0]!["currentAuthority"] = false;
            bytes = JsonSerializer.SerializeToUtf8Bytes(evidence);
            File.WriteAllBytes(path, bytes);
            options.EvidenceHash = Convert.ToHexString(SHA256.HashData(bytes));
            ProjectsReadRouteSelection.Create(options).Mode.ShouldBe("Unavailable");
            selected.Mode.ShouldBe("Supported");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("evidence-hash", "evidence-hash-mismatch")]
    [InlineData("receipt-hash", "native-receipts-rejected")]
    [InlineData("receipt-traversal", "native-receipts-rejected")]
    [InlineData("duplicate-property", "duplicate-evidence-property")]
    [InlineData("missing-test-name", "native-receipts-rejected")]
    [InlineData("contractParity", "supported-gate-rejected")]
    [InlineData("identityCompatibility", "supported-gate-rejected")]
    [InlineData("mode-case", "invalid-mode")]
    [InlineData("missing-evidence", "evidence-file-missing")]
    public void InvalidStartupEvidenceHasABoundedRefusalReason(string fault, string reason)
    {
        string directory = Path.Combine(Path.GetTempPath(), "story67-refusal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            byte[] xml = System.Text.Encoding.UTF8.GetBytes("<assemblies><assembly total=\"1\" passed=\"1\" failed=\"0\" skipped=\"0\" errors=\"0\" not-run=\"0\"><collection><test name=\"control\" result=\"Pass\" /></collection></assembly></assemblies>");
            File.WriteAllBytes(Path.Combine(directory, "native.xml"), xml);
            string hash = Convert.ToHexString(SHA256.HashData(xml));
            JsonObject evidence = Evidence();
            evidence["nativeReceipts"] = new JsonArray(new JsonObject { ["path"] = "native.xml", ["sha256"] = hash });
            foreach (JsonNode? cell in (JsonArray)evidence["cases"]!) { cell!["nativeXmlHash"] = hash; cell["nativeTestName"] = "control"; }
            if (fault == "receipt-hash") evidence["nativeReceipts"]![0]!["sha256"] = new string('c', 64);
            if (fault == "receipt-traversal") evidence["nativeReceipts"]![0]!["path"] = "../native.xml";
            if (fault == "missing-test-name") ((JsonObject)evidence["cases"]![0]!).Remove("nativeTestName");
            if (fault is "contractParity" or "identityCompatibility") evidence[fault] = false;
            string json = evidence.ToJsonString();
            if (fault == "duplicate-property") json = json.Insert(1, "\"contractParity\":true,");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            string path = Path.Combine(directory, "receipt.json");
            File.WriteAllBytes(path, bytes);
            var options = new ProjectsReadRoutingOptions { Mode = fault == "mode-case" ? "supported" : "Supported",
                EvidencePath = path, EvidenceHash = fault == "evidence-hash" ? new string('c', 64) : Convert.ToHexString(SHA256.HashData(bytes)),
                SourceRevision = Revision, ContractHash = Contract };
            if (fault == "missing-evidence") File.Delete(path);
            var selected = ProjectsReadRouteSelection.Create(options);
            selected.Mode.ShouldBe("Unavailable");
            selected.RefusalReason.ShouldBe(reason);
            selected.RefusalReason!.ShouldNotContain(directory);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static bool Activate(JsonObject evidence)
    {
        using JsonDocument parsed = JsonDocument.Parse(evidence.ToJsonString());
        return ProjectReadCutoverGate.CanActivate(parsed.RootElement, Revision, Contract);
    }

    private static JsonObject Evidence() => new()
    {
        ["normalizationRevision"] = ProjectReadCutoverGate.NormalizationRevision, ["sourceRevision"] = Revision,
        ["contractHash"] = Contract, ["contractParity"] = true, ["identityCompatibility"] = true,
        ["cases"] = new JsonArray(ProjectReadCutoverGate.Operations.SelectMany(operation => ProjectReadCutoverGate.Scenarios.Select(scenario => (JsonNode)new JsonObject
        {
            ["operation"] = operation, ["scenario"] = scenario, ["executed"] = true, ["passed"] = true, ["skipped"] = false,
            ["canonicalOutput"] = true, ["independentKeys"] = true, ["exactPositions"] = true,
            ["cursorScope"] = true, ["isolation"] = true, ["supportedOnlyAssertions"] = true, ["noDomainWrites"] = true, ["nativeXmlHash"] = Contract,
        })).ToArray()),
    };
}
