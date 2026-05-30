// <copyright file="ProjectsCliParser.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli;

/// <summary>Structured command parse result for the Projects CLI.</summary>
public sealed record ProjectsCliInvocation(
    string Command,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Options)
{
    /// <summary>Gets the last supplied option value.</summary>
    public string? Option(string name)
        => Options.TryGetValue(name, out IReadOnlyList<string>? values) ? values.LastOrDefault() : null;

    /// <summary>Gets all supplied option values.</summary>
    public IReadOnlyList<string> Values(string name)
        => Options.TryGetValue(name, out IReadOnlyList<string>? values) ? values : [];

    /// <summary>Gets whether a boolean option was supplied.</summary>
    public bool Has(string name) => Options.ContainsKey(name);
}

/// <summary>Minimal structured parser over already-tokenized command-line arguments.</summary>
public static class ProjectsCliParser
{
    private static readonly HashSet<string> ReadCommands = new(StringComparer.Ordinal)
    {
        "list",
        "describe",
        "inspect",
        "trace",
        "trace-resolution",
        "validate",
        "validate-references",
        "audit",
        "warnings",
        "dashboard",
    };

    private static readonly HashSet<string> MutationCommands = new(StringComparer.Ordinal)
    {
        "dry-run",
        "preview",
        "archive",
        "restore",
        "relink",
        "unlink",
        "reevaluate",
    };

    /// <summary>Parses the Projects CLI command shape.</summary>
    public static bool TryParse(IReadOnlyList<string> args, out ProjectsCliInvocation invocation, out string error)
    {
        ArgumentNullException.ThrowIfNull(args);

        invocation = new ProjectsCliInvocation(string.Empty, new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));
        error = string.Empty;
        if (args.Count == 0)
        {
            error = "usage_error";
            return false;
        }

        int index = string.Equals(args[0], "projects", StringComparison.Ordinal) ? 1 : 0;
        if (index >= args.Count)
        {
            error = "command_required";
            return false;
        }

        string command = args[index++];
        if (string.Equals(command, "diagnostic", StringComparison.Ordinal))
        {
            if (index >= args.Count || !string.Equals(args[index], "export", StringComparison.Ordinal))
            {
                error = "unsupported_diagnostic_command";
                return false;
            }

            command = "diagnostic export";
            index++;
        }

        if (!ReadCommands.Contains(command) && !MutationCommands.Contains(command) && command is not "diagnostic export")
        {
            error = "unsupported_command";
            return false;
        }

        var values = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        while (index < args.Count)
        {
            string token = args[index++];
            if (!token.StartsWith("--", StringComparison.Ordinal) || token.Length <= 2)
            {
                error = "unexpected_argument";
                return false;
            }

            string name = token[2..];
            bool flag = name is "confirm" or "include-archived";
            string value = "true";
            if (!flag)
            {
                if (index >= args.Count || args[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error = "option_value_required";
                    return false;
                }

                value = args[index++];
            }

            if (!values.TryGetValue(name, out List<string>? list))
            {
                list = [];
                values[name] = list;
            }

            list.Add(value);
        }

        invocation = new ProjectsCliInvocation(
            NormalizeAlias(command),
            values.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value,
                StringComparer.Ordinal));
        return true;
    }

    private static string NormalizeAlias(string command)
        => command switch
        {
            "trace-resolution" => "trace",
            "validate-references" => "validate",
            _ => command,
        };
}
