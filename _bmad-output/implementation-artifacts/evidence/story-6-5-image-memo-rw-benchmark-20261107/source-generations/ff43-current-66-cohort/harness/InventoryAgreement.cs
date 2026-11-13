namespace Story65.ImageMemo.Harness;

/// <summary>Requires all variants to agree with independent complete SHA and loaded-MVID identities before timing.</summary>
internal static class InventoryAgreement
{
    internal static object[] Validate(ImageInventory inventory)
    {
        var facts = new List<object>();
        foreach (string variant in Hosts.Names)
        {
            using IMemoHost host = Hosts.Create(variant);
            foreach (string path in new[] { "domain", "technical" })
            {
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    ObservationMeasurement observed = ObservationRunner.Run(host, inventory, path, 0, repeat);
                    facts.Add(new
                    {
                        Variant = variant,
                        Path = path,
                        Repeat = repeat,
                        CompleteIdentityOutputsAgreeWithIndependentExpectedValues = true,
                        Observations = 1,
                        Images = inventory.Images.Length,
                        observed.ReportedMemoHits,
                        observed.FreshResultsWithoutReportedReuse,
                        MemoCount = host.Count,
                        MemoChargedBytes = host.RetainedBytes,
                        inventory.ImageSizeEligibleCount,
                        inventory.EntireCohortFitsOriginalBounds,
                        NonHitsAreNotAutomaticallyCalledMissesOrRejections = true,
                    });
                }
            }
        }
        return facts.ToArray();
    }
}
