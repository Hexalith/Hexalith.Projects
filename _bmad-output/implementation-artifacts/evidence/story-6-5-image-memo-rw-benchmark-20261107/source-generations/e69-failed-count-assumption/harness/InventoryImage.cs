namespace Story65.ImageMemo.Harness;

/// <summary>Retains public executable-image metadata and its independent complete expected digest.</summary>
internal sealed record InventoryImage(int Ordinal, string FullName, string Path, long Bytes,
    string Sha256, string ModuleVersionId, string ExpectedTechnicalIdentity);
