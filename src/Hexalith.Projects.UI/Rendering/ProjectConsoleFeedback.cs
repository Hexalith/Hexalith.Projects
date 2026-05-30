// <copyright file="ProjectConsoleFeedback.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.UI.Rendering;

using System.Globalization;
using System.Text;

/// <summary>
/// Safe, data-only operation/query feedback for Projects console views.
/// </summary>
/// <param name="Category">The stable feedback category.</param>
/// <param name="SafeReasonCode">The safe reason code.</param>
/// <param name="Message">The visible safe message.</param>
/// <param name="CorrelationId">The safe correlation identifier when available.</param>
public sealed record ProjectConsoleFeedback(
    string Category,
    string SafeReasonCode,
    string Message,
    string? CorrelationId = null)
{
    /// <summary>Success category.</summary>
    public const string SuccessCategory = "success";

    /// <summary>Warning category.</summary>
    public const string WarningCategory = "warning";

    /// <summary>Error category.</summary>
    public const string ErrorCategory = "error";

    /// <summary>Fail-closed category.</summary>
    public const string FailClosedCategory = "fail-closed";

    /// <summary>Loading category.</summary>
    public const string LoadingCategory = "loading";

    /// <summary>Creates success feedback.</summary>
    public static ProjectConsoleFeedback Success(string safeReasonCode, string? correlationId = null)
        => new(SuccessCategory, SafeCode(safeReasonCode), "Operation completed.", correlationId);

    /// <summary>Creates warning feedback.</summary>
    public static ProjectConsoleFeedback Warning(string safeReasonCode, string? correlationId = null)
        => new(WarningCategory, SafeCode(safeReasonCode), "The operation completed with a warning.", correlationId);

    /// <summary>Creates error feedback.</summary>
    public static ProjectConsoleFeedback Error(string safeReasonCode, string? correlationId = null)
        => new(ErrorCategory, SafeCode(safeReasonCode), "The operation could not be completed.", correlationId);

    /// <summary>Creates fail-closed feedback.</summary>
    public static ProjectConsoleFeedback FailClosed(string safeReasonCode, string? correlationId = null)
        => new(FailClosedCategory, SafeCode(safeReasonCode), "The request failed closed.", correlationId);

    /// <summary>Creates loading feedback.</summary>
    public static ProjectConsoleFeedback Loading(string safeReasonCode)
        => new(LoadingCategory, SafeCode(safeReasonCode), "Loading.");

    private static string SafeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        string code = value.Trim().ToLower(CultureInfo.InvariantCulture);
        var builder = new StringBuilder(code.Length);
        foreach (char ch in code)
        {
            builder.Append(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-' or '.' ? ch : '_');
        }

        code = builder.ToString();
        return IsForbidden(code) ? "unsafe_reason_code" : code;
    }

    private static bool IsForbidden(string value)
        => value.Contains("secret", StringComparison.Ordinal)
            || value.Contains("token", StringComparison.Ordinal)
            || value.Contains("transcript", StringComparison.Ordinal)
            || value.Contains("prompt", StringComparison.Ordinal)
            || value.Contains("payload", StringComparison.Ordinal)
            || value.Contains("body", StringComparison.Ordinal)
            || value.Contains("private_key", StringComparison.Ordinal)
            || value.Contains("authorization", StringComparison.Ordinal);
}
