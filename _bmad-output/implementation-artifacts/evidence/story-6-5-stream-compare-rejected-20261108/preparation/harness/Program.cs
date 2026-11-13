using System.Text.Json;

namespace Story65.ImageMemo.Harness;

/// <summary>Runs only the reviewed isolated parity and computation benchmark in a fresh output directory.</summary>
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static async Task<int> Main(string[] args)
    {
        if (args.Length != 4 || args[0] != "all" || !Path.IsPathFullyQualified(args[1]) || !File.Exists(args[1])
            || !Path.IsPathFullyQualified(args[3]) || Directory.Exists(args[3]) || File.Exists(args[3]))
        {
            Console.WriteLine("closed-input-refusal");
            return 2;
        }
        if (!OperatingSystem.IsLinux())
        {
            Console.WriteLine("linux-required");
            return 2;
        }
        string output = args[3];
        Directory.CreateDirectory(output, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            Save(output, "current-reviewed-input-manifest.json", FinalTransferInputs.Load(args[1], args[2]));
            Save(output, "runtime-facts.json", RuntimeFacts.Validate());
            var allResults = new List<object>();
            foreach (string role in new[] { "folders", "projects" })
            {
                string cohortOutput = Path.Combine(output, role); Directory.CreateDirectory(cohortOutput);
                ImageInventory inventory = ImageInventory.Load(role);
                Save(cohortOutput, "inventory.json", new { inventory.OwnerPath, inventory.OwnerRole,
                    ImageCount = inventory.Images.Length, inventory.Bytes, inventory.ChargedBytes,
                    inventory.ImageSizeEligibleCount, inventory.EntireCohortFitsOriginalBounds,
                    SourceDerivedOwnerDomainDirectReferenceCohortOnly = true,
                    CompleteExecutableOwnersUnionNotClaimed = true, inventory.Files });
                var proofs = new List<object>();
                foreach (string variant in Hosts.Names) { using IMemoHost host = Hosts.Create(variant); proofs.Add(VariantPathProof.Validate(host)); }
                Save(cohortOutput, "selected-variant-path-proofs.json", proofs);
                ParityReport parity = await Parity.RunAsync(inventory.Images, Path.Combine(cohortOutput, "parity-fixtures"));
                Save(cohortOutput, "parity.json", parity); Require.That(parity.AllPassed, "base-parity-before-timing");
                Save(cohortOutput, "actual-production-control-candidate-agreement.json", InventoryAgreement.Validate(inventory));
                Save(cohortOutput, "native-file-parity-and-mutation-costs.json", NativeFileParity.Run(inventory.Images[0], Path.Combine(cohortOutput, "native-file-fixtures")));
                inventory.VerifyUnchanged(); FinalTransferInputs.VerifyUnchanged();
                BatchMeasurement[] rows = BenchmarkDriver.Run(inventory, cohortOutput);
                inventory.VerifyUnchanged(); FinalTransferInputs.VerifyUnchanged();
                Save(cohortOutput, "benchmark-summary.json", BenchmarkSummary.Build(rows));
                allResults.Add(new { Owner = role, Images = inventory.Images.Length, parity.Checks, parity.Passed,
                    RawBatches = rows.Length, DomainWholeOpenReadCompareEofDisposeMeasured = true });
            }
            Save(output, "result.json", new { Completed = true, DiagnosticOnly = true, FeatureAcceptance = false,
                Cohorts = allResults, EnabledNativeObserverReentryQualificationStillRequired = true,
                CandidateBenefitOrProductionApprovalNotClaimed = true, ProductSourcesEdited = false,
                ApplicationHostsConstructed = false });
            Console.WriteLine("closed-benchmark-complete");
            return 0;
        }
        catch (Exception error)
        {
            ParityFailure? failure = error as ParityFailure;
            Save(output, "failure.json", new
            {
                Completed = false,
                DiagnosticOnly = true,
                FeatureAcceptance = false,
                ErrorType = error.GetType().FullName,
                ClosedCheck = failure?.ClosedCheck,
                AssertionFile = failure?.AssertionFile,
                AssertionLine = failure?.AssertionLine,
                ArbitraryExceptionMessageOrPayloadPersisted = false,
            });
            Console.WriteLine("closed-benchmark-failed");
            return 1;
        }
    }

    private static void Save(string directory, string name, object value)
    {
        using FileStream file = new(Path.Combine(directory, name), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(file, value, JsonOptions);
    }
}
