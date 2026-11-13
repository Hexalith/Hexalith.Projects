namespace Story65.InvocationTransportProof;
internal static class ResultComparison
{
    internal static void Validate(object[] r)
    {
        Check.Require(r.Length == 22, "Unexpected primitive result size.");
        Check.Require(r[0] is object[] e && e.Length == 5 && e.Take(3).All(x => x is string) && e.Skip(3).All(x => x is bool), "Invalid error DTO.");
        Check.Require(r[2] is string && r[7] is object[][] && r[14] is object[][], "Invalid in-memory trace DTO.");
        foreach (int index in new[] { 1, 8, 9, 10, 11, 13, 15, 16, 18, 19, 20, 21 }) Check.Require(r[index] is int, "Invalid scalar integer.");
        foreach (int index in new[] { 3, 4, 5, 6, 12, 17 }) Check.Require(r[index] is bool, "Invalid scalar boolean.");
        foreach (object[] row in (object[][])r[7]) Check.Require(row.Length == 5 && row[0] is string && row[1] is string && row[2] is int && row[3] is bool && row[4] is bool, "Invalid observation DTO.");
        foreach (object[] row in (object[][])r[14]) Check.Require(row.Length == 2 && row[0] is string && row[1] is int, "Invalid fault DTO.");
    }
    internal static void Equal(object[] left, object[] right)
    {
        Validate(left); Validate(right);
        Check.Require(Same(left, right), "Original/private primitive outcomes differ.");
    }
    private static bool Same(object? left, object? right)
    {
        if (left is object[] a && right is object[] b) return a.Length == b.Length && a.Zip(b).All(p => Same(p.First, p.Second));
        return left?.GetType() == right?.GetType() && Equals(left, right);
    }
}
