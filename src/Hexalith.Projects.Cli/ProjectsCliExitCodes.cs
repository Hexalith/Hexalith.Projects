// <copyright file="ProjectsCliExitCodes.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Cli;

/// <summary>Stable semantic exit codes for Projects operator commands.</summary>
public static class ProjectsCliExitCodes
{
    /// <summary>Command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>Command-line usage or parse error.</summary>
    public const int Usage = 2;

    /// <summary>Safe validation/domain rejection.</summary>
    public const int Validation = 3;

    /// <summary>Safe denial or not found.</summary>
    public const int DenialOrNotFound = 4;

    /// <summary>Retryable unavailable service/read model.</summary>
    public const int Unavailable = 5;

    /// <summary>Unexpected sanitized failure.</summary>
    public const int Unexpected = 10;
}
