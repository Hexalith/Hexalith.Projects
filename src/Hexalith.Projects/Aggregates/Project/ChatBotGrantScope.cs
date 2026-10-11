using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Projects.Aggregates.Project;

/// <summary>Derives a stable key for an exact project grant scope.</summary>
public static class ChatBotGrantScope
{
    /// <summary>Gets the exact scope key.</summary>
    public static string For(string principalId, string operation, string actorClass, string origin)
    {
        string value = string.Join('\0', principalId, operation, actorClass, origin);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
