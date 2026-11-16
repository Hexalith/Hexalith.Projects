#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Text.Json;
using Hexalith.Projects.Client;
using Shouldly;

/// <summary>Exercises immutable production routing with retained native receipts and real legacy transport.</summary>
public sealed partial class SupportedProjectGatewayHttpTests
{
    private static JsonElement? LoadRealReceiptRoutingProof(string? matrixOutput)
    {
        if (string.IsNullOrWhiteSpace(matrixOutput)) return null;
        string configurationPath = Path.ChangeExtension(matrixOutput, ".routing-proof.json");
        if (!File.Exists(configurationPath)) return null;
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(configurationPath));
        JsonElement configuration = document.RootElement.Clone();
        ProjectsReadRouteSelection selected = RealReceiptSelection(configuration, "Supported", "evidencePath", "evidenceHash");
        selected.Mode.ShouldBe("Supported", "The real retained native XML and all 77 executed cells must qualify.");
        RealReceiptSelection(configuration, "Supported", "regressionPath", "regressionHash").Mode.ShouldBe("Unavailable");
        selected.Mode.ShouldBe("Supported", "A request regression cannot mutate an existing startup selection or trigger fallback.");
        RealReceiptSelection(configuration, "LegacyRollback", "evidencePath", "evidenceHash").Mode.ShouldBe("LegacyRollback");
        RealReceiptSelection(configuration, "LegacyRollback", "missingLegacyPath", "missingLegacyHash").Mode.ShouldBe("Unavailable");
        return configuration;
    }

    private static ProjectsReadRouteSelection RealReceiptSelection(JsonElement configuration, string mode, string pathField, string hashField)
        => ProjectsReadRouteSelection.Create(new ProjectsReadRoutingOptions
        {
            Mode = mode,
            EvidencePath = configuration.GetProperty(pathField).GetString(),
            EvidenceHash = configuration.GetProperty(hashField).GetString(),
            SourceRevision = configuration.GetProperty("sourceRevision").GetString(),
            ContractHash = configuration.GetProperty("contractHash").GetString(),
        });
}
#endif
