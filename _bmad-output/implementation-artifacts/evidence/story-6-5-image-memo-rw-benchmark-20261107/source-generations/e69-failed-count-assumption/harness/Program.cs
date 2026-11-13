using System.Text.Json;

namespace Story65.ImageMemo.Harness;

/// <summary>Runs only the reviewed isolated parity and computation benchmark in a fresh output directory.</summary>
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static async Task<int> Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "all" || !Path.IsPathFullyQualified(args[1]) || Directory.Exists(args[1]) || File.Exists(args[1]))
        {
            Console.WriteLine("closed-input-refusal");
            return 2;
        }
        if (!OperatingSystem.IsLinux())
        {
            Console.WriteLine("linux-required");
            return 2;
        }
        string output = args[1];
        Directory.CreateDirectory(output, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            Save(output, "runtime-facts.json", RuntimeFacts.Validate());
            ImageInventory inventory = ImageInventory.Load();
            Save(output, "inventory.json", new
            {
                OwnerPath = ImageInventory.OwnerPath,
                ImageCount = inventory.Images.Length,
                inventory.Bytes,
                inventory.ChargedBytes,
                inventory.ImageSizeEligibleCount,
                inventory.EntireCohortFitsOriginalBounds,
                AllImagesRetainedInOriginalOrdinalAssemblyNameOrder = true,
                SourceDerivedOwnerDomainDirectReferenceCohortOnly = true,
                CompleteDomainExecutableOwnersDiscoveryUnionNotClaimed = true,
                InventoryOnlyNoHostDiscoveryOrDomainServiceMethodInvoked = true,
                inventory.Files,
            });
            var proofs = new List<object>();
            foreach (string variant in Hosts.Names)
            {
                using IMemoHost host = Hosts.Create(variant);
                proofs.Add(VariantPathProof.Validate(host));
            }
            Save(output, "selected-variant-path-proofs.json", proofs);
            ParityReport parity = await Parity.RunAsync(inventory.Images, Path.Combine(output, "parity-fixtures"));
            Save(output, "parity.json", parity);
            Require.That(parity.AllPassed, "behavioral-parity-required-before-benchmark");
            Save(output, "oracle-control-candidate-agreement.json", InventoryAgreement.Validate(inventory));
            inventory.VerifyUnchanged();
            BatchMeasurement[] rows = BenchmarkDriver.Run(inventory, output);
            inventory.VerifyUnchanged();
            Save(output, "benchmark-summary.json", BenchmarkSummary.Build(rows));
            Save(output, "result.json", new
            {
                Completed = true,
                DiagnosticOnly = true,
                FeatureAcceptance = false,
                parity.Checks,
                parity.Passed,
                parity.AllPassed,
                All67ExecutableInputsByteAndLoadedIdentityUnchanged = true,
                ActualSdkOracleAndCopiedControlAgreement = true,
                FreshScopePerObservationOneSharedMemoPerVariantSample = true,
                RuntimeMode = "natural14-unoptimized-Debug-ownerServerGC",
                OriginalBoundsPoolsChunkEofPeMvidObservationOrderPreserved = true,
                RawSamples = rows.Length,
                CandidateSpeedupOrLiveCriticalPathBenefitClaimed = false,
                ProductSourcesEdited = false,
                ApplicationHostsConstructed = false,
            });
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
