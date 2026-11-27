// <copyright file="ProjectCreationFaultOptions.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Workflow;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Configurable fault-injection and boundary-pause hooks for Story 7.1 recovery and race tests.</summary>
public sealed class ProjectCreationFaultOptions
{
    private int _dropNextClaimReplyOnce;
    private int _failCommitOnce;
    private int _dropNextCommitReplyOnce;
    private int _failReleaseClaimOnce;
    private int _failProjectionOnce;

    /// <summary>Gets or sets a value indicating whether to pause immediately after task admission before Step 1.</summary>
    public bool PauseBeforeProvisioning { get; set; }

    /// <summary>Gets or sets a value indicating whether to pause immediately after Step 1 Folder receipt persistence.</summary>
    public bool PauseAfterFolderReceipt { get; set; }

    /// <summary>Gets or sets a value indicating whether to pause immediately after Step 2 AD-35 claim acquisition.</summary>
    public bool PauseAfterClaim { get; set; }

    /// <summary>Gets or sets a value indicating whether to pause immediately after Step 3 commit before read-model confirmation.</summary>
    public bool PauseBeforeProjection { get; set; }

    /// <summary>Gets or sets an optional synchronization barrier invoked right before appending the Step 2 claim.</summary>
    public Func<CancellationToken, Task>? BeforeClaimAppendBarrier { get; set; }

    /// <summary>Gets or sets an optional hook invoked right before executing Step 3 commit (e.g. to revoke authority or inject stale claim).</summary>
    public Func<CancellationToken, Task>? BeforePreCommitCheckHook { get; set; }

    /// <summary>Gets or sets a value indicating whether the next Step 2 claim appends durably then throws <see cref="TimeoutException"/>.</summary>
    public bool DropNextClaimReplyOnce
    {
        get => Volatile.Read(ref _dropNextClaimReplyOnce) == 1;
        set => Volatile.Write(ref _dropNextClaimReplyOnce, value ? 1 : 0);
    }

    /// <summary>Gets or sets a value indicating whether the next Step 3 commit fails transiently before appending.</summary>
    public bool FailCommitOnce
    {
        get => Volatile.Read(ref _failCommitOnce) == 1;
        set => Volatile.Write(ref _failCommitOnce, value ? 1 : 0);
    }

    /// <summary>Gets or sets a value indicating whether the next Step 3 commit appends durably then throws <see cref="TimeoutException"/>.</summary>
    public bool DropNextCommitReplyOnce
    {
        get => Volatile.Read(ref _dropNextCommitReplyOnce) == 1;
        set => Volatile.Write(ref _dropNextCommitReplyOnce, value ? 1 : 0);
    }

    /// <summary>Gets or sets a value indicating whether the next AD-35 claim release fails transiently.</summary>
    public bool FailReleaseClaimOnce
    {
        get => Volatile.Read(ref _failReleaseClaimOnce) == 1;
        set => Volatile.Write(ref _failReleaseClaimOnce, value ? 1 : 0);
    }

    /// <summary>Gets or sets a value indicating whether the next projection write fails as unavailable.</summary>
    public bool FailProjectionOnce
    {
        get => Volatile.Read(ref _failProjectionOnce) == 1;
        set => Volatile.Write(ref _failProjectionOnce, value ? 1 : 0);
    }

    /// <summary>Atomically consumes <see cref="DropNextClaimReplyOnce"/>.</summary>
    /// <returns><see langword="true"/> if consumed; otherwise <see langword="false"/>.</returns>
    public bool ConsumeDropNextClaimReplyOnce() => Interlocked.Exchange(ref _dropNextClaimReplyOnce, 0) == 1;

    /// <summary>Atomically consumes <see cref="FailCommitOnce"/>.</summary>
    /// <returns><see langword="true"/> if consumed; otherwise <see langword="false"/>.</returns>
    public bool ConsumeFailCommitOnce() => Interlocked.Exchange(ref _failCommitOnce, 0) == 1;

    /// <summary>Atomically consumes <see cref="DropNextCommitReplyOnce"/>.</summary>
    /// <returns><see langword="true"/> if consumed; otherwise <see langword="false"/>.</returns>
    public bool ConsumeDropNextCommitReplyOnce() => Interlocked.Exchange(ref _dropNextCommitReplyOnce, 0) == 1;

    /// <summary>Atomically consumes <see cref="FailReleaseClaimOnce"/>.</summary>
    /// <returns><see langword="true"/> if consumed; otherwise <see langword="false"/>.</returns>
    public bool ConsumeFailReleaseClaimOnce() => Interlocked.Exchange(ref _failReleaseClaimOnce, 0) == 1;

    /// <summary>Atomically consumes <see cref="FailProjectionOnce"/>.</summary>
    /// <returns><see langword="true"/> if consumed; otherwise <see langword="false"/>.</returns>
    public bool ConsumeFailProjectionOnce() => Interlocked.Exchange(ref _failProjectionOnce, 0) == 1;
}
