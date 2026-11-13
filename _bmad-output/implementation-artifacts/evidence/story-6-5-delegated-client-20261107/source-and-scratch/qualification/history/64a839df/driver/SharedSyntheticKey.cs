namespace Story65.DelegatedClientProof;

using System.Security.Cryptography;

internal static class SharedSyntheticKey
{
    private static readonly RSA Key = RSA.Create(2048);
    internal static readonly string Private = Convert.ToBase64String(Key.ExportPkcs8PrivateKey());
    internal static readonly string Public = Convert.ToBase64String(Key.ExportSubjectPublicKeyInfo());
    internal static void Prepare() { _ = Private.Length; }
}
