// <copyright file="RecordingLogger.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Testing.Context;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

/// <summary>
/// Minimal recording <see cref="ILogger{TCategoryName}"/> used by Story 3.1 tests to assert that the
/// pure inclusion policy emits structured warnings on non-allowlisted reference kinds (AC 5) without
/// pulling in a real logging provider. Pure, deterministic, thread-safe via a lock.
/// </summary>
/// <typeparam name="TCategory">The logger category type (used only for <c>ILogger&lt;T&gt;</c> resolution).</typeparam>
public sealed class RecordingLogger<TCategory> : ILogger<TCategory>
{
    private readonly object _gate = new();
    private readonly List<RecordedLogEntry> _entries = [];

    /// <summary>Gets the list of recorded log entries in insertion order.</summary>
    public IReadOnlyList<RecordedLogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return new ReadOnlyCollection<RecordedLogEntry>([.. _entries]);
            }
        }
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        string message = formatter(state, exception);
        lock (_gate)
        {
            _entries.Add(new RecordedLogEntry(logLevel, eventId, message, exception));
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>
/// One captured <see cref="ILogger"/> entry: the level, event id, formatted message, and optional
/// exception. Used by Story 3.1 tests to assert the policy's structured-warning behavior.
/// </summary>
/// <param name="Level">The log level.</param>
/// <param name="EventId">The event id.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The associated exception, if any.</param>
public sealed record RecordedLogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
