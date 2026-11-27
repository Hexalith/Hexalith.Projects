// <copyright file="FoldersAggregateProvisioningClient.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#if SUPPORTED_PROJECT_READ_SOURCE
namespace Hexalith.Projects.SdkWriter.Workflow;

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.DomainService.DurableTasks;
using Hexalith.Folders.Aggregates.Folder;
using Hexalith.Folders.Server.Provisioning;

/// <summary>
/// Real <see cref="FolderAggregate"/> + <see cref="FolderStateApply"/> + shared <see cref="IReadModelStore"/>
/// Folders provisioning adapter for Story 7.1 source-mode qualification.
/// Enforces canonical step/operation/owner-version checks, full 160-character Folders forbidden-metadata
/// validation BEFORE saving <see cref="FolderNameLockRecord"/>, CAS outage counters, A-2 same-name
/// collision rejection, sole-manager with no Tenant-default read, zero workload authority, and
/// tenant-scoped supplied-Folder persistence without leaking foreign orphan/reserved status.
/// </summary>
public sealed class FoldersAggregateProvisioningClient : IProjectFolderProvisioningClient
{
    /// <summary>The exact Folders stream version after create + read grant + manage grant.</summary>
    public const long ProvisionedVersion = 3;

    private const int MaxStandardFolderDisplayNameLength = 128;
    private const int MaxProvisioningDisplayNameLength = 160;
    private const string ReadAction = "read_metadata";
    private const string ManageAction = "manage_folder_access";

    private readonly IReadModelStore _store;
    private readonly string _storeName;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, byte> _deniedActors = new(StringComparer.Ordinal);

    private int _deleteAttemptCount;
    private int _remainingProvisionOutages;
    private int _remainingLookupOutages;
    private int _dropNextProvisionReplyOnce;
    private int _forceInvalidProvisionSecurityOnce;

    /// <summary>Initializes a new instance of the <see cref="FoldersAggregateProvisioningClient"/> class.</summary>
    /// <param name="store">The shared conditional state store.</param>
    /// <param name="storeName">The state-store component name.</param>
    /// <param name="timeProvider">The UTC clock.</param>
    public FoldersAggregateProvisioningClient(IReadModelStore store, string storeName, TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _storeName = !string.IsNullOrWhiteSpace(storeName) ? storeName : throw new ArgumentException("Store name is required.", nameof(storeName));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public int DeleteAttemptCount => Volatile.Read(ref _deleteAttemptCount);

    /// <summary>Gets or sets the number of upcoming provision calls that throw a transient outage.</summary>
    public int RemainingProvisionOutages
    {
        get => Volatile.Read(ref _remainingProvisionOutages);
        set => Volatile.Write(ref _remainingProvisionOutages, Math.Max(0, value));
    }

    /// <summary>Gets or sets the number of upcoming lookup calls that throw a transient outage.</summary>
    public int RemainingLookupOutages
    {
        get => Volatile.Read(ref _remainingLookupOutages);
        set => Volatile.Write(ref _remainingLookupOutages, Math.Max(0, value));
    }

    /// <summary>Gets or sets a value indicating whether the next successful provision commits durably then drops its reply.</summary>
    public bool DropNextProvisionReplyOnce
    {
        get => Volatile.Read(ref _dropNextProvisionReplyOnce) == 1;
        set => Volatile.Write(ref _dropNextProvisionReplyOnce, value ? 1 : 0);
    }

    /// <summary>Gets or sets a value indicating whether the next provisioned Folder intentionally violates security invariants for testing fail-closed checks.</summary>
    public bool ForceInvalidProvisionSecurityOnce
    {
        get => Volatile.Read(ref _forceInvalidProvisionSecurityOnce) == 1;
        set => Volatile.Write(ref _forceInvalidProvisionSecurityOnce, value ? 1 : 0);
    }

    /// <summary>Marks an actor as denied for Folder operations in a given Tenant.</summary>
    /// <param name="tenantId">The Tenant identifier.</param>
    /// <param name="actorPrincipalId">The actor principal identifier.</param>
    public void DenyActor(string tenantId, string actorPrincipalId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(actorPrincipalId);
        _deniedActors[ActorKey(tenantId, actorPrincipalId)] = 0;
    }

    /// <summary>Restores Folder authority for a previously denied actor.</summary>
    /// <param name="tenantId">The Tenant identifier.</param>
    /// <param name="actorPrincipalId">The actor principal identifier.</param>
    public void AllowActor(string tenantId, string actorPrincipalId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(actorPrincipalId);
        _deniedActors.TryRemove(ActorKey(tenantId, actorPrincipalId), out _);
    }

    /// <summary>Records an explicit delete attempt (used only to verify the counter is mutable and stays 0 during workflow runs).</summary>
    public void RecordDeleteAttempt() => Interlocked.Increment(ref _deleteAttemptCount);

    /// <summary>Registers an existing Tenant-scoped supplied Folder in the shared store.</summary>
    /// <param name="record">The supplied Folder record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task RegisterSuppliedFolderAsync(SuppliedFolderRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        string key = SuppliedFolderKey(record.TenantId, record.FolderId);
        await _store.SaveAsync(_storeName, key, record, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a persisted provisioned Folder record by Tenant and FolderId.</summary>
    /// <param name="tenantId">The Tenant identifier.</param>
    /// <param name="folderId">The Folder identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted record, or null.</returns>
    public async Task<ProvisionedFolderRecord?> GetProvisionedFolderAsync(
        string tenantId,
        string folderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(folderId);
        ReadModelEntry<ProvisionedFolderRecord> entry = await _store
            .GetAsync<ProvisionedFolderRecord>(_storeName, ProvisionedByFolderKey(tenantId, folderId), cancellationToken)
            .ConfigureAwait(false);
        return entry.Value;
    }

    /// <summary>Derives the deterministic Folder identifier matching <c>FolderProvisioningContract</c>.</summary>
    /// <param name="tenantId">The Tenant identifier.</param>
    /// <param name="stepKey">The canonical step key.</param>
    /// <returns>The derived Folder identifier.</returns>
    public static string DeriveFolderId(string tenantId, string stepKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepKey);
        string input = string.Create(CultureInfo.InvariantCulture, $"{tenantId.Length}:{tenantId}{stepKey.Length}:{stepKey}");
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return "fld-p-" + Convert.ToHexStringLower(digest)[..40];
    }

    /// <summary>Computes the canonical provisioning intent digest matching <c>FolderProvisioningContract.ComputeRequestSha256</c>.</summary>
    /// <param name="displayName">The Folder display name.</param>
    /// <returns>The lowercase SHA-256 hex digest.</returns>
    public static string ComputeRequestSha256(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("hexalith.folders.provision-folder.v1\n" + displayName.Trim())));
    }

    /// <summary>Derives the deterministic provisioning receipt identifier.</summary>
    /// <param name="tenantId">The Tenant identifier.</param>
    /// <param name="stepKey">The canonical step key.</param>
    /// <returns>The receipt identifier.</returns>
    public static string DeriveReceiptId(string tenantId, string stepKey)
        => "prov-" + DeriveFolderId(tenantId, stepKey)["fld-p-".Length..];

    /// <inheritdoc/>
    public async Task<FolderProvisioningResult> ProvisionFolderAsync(
        string tenantId,
        string actorPrincipalId,
        FolderProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(actorPrincipalId);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsValidProvisioningRequest(tenantId, actorPrincipalId, request))
        {
            return Failed(FolderProvisioningOutcome.ValidationFailed, "invalid_provisioning_request");
        }

        if (TryConsumeOutage(ref _remainingProvisionOutages))
        {
            throw new TimeoutException("Simulated transient Folders provisioning outage.");
        }

        if (_deniedActors.ContainsKey(ActorKey(tenantId, actorPrincipalId)))
        {
            return Failed(FolderProvisioningOutcome.Denied, "folder_create_denied");
        }

        string folderId = DeriveFolderId(tenantId, request.StepKey);
        string receiptId = DeriveReceiptId(tenantId, request.StepKey);
        string canonicalDisplayName = request.DisplayName.Trim();

        // Check if this exact (Tenant, StepKey) already committed.
        ReadModelEntry<ProvisionedFolderRecord> existingEntry = await _store
            .GetAsync<ProvisionedFolderRecord>(_storeName, ProvisionedByFolderKey(tenantId, folderId), cancellationToken)
            .ConfigureAwait(false);
        if (existingEntry.Value is { } existing)
        {
            return ValidateProvisionedRecord(existing, tenantId, actorPrincipalId, request);
        }

        // Run REAL FolderCommandValidator + FolderAggregate + FolderStateApply BEFORE saving FolderNameLockRecord
        // so rejected commands NEVER leak a permanent name lock!
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (!TryExecuteRealFolderAggregate(
                tenantId,
                actorPrincipalId,
                folderId,
                receiptId,
                canonicalDisplayName,
                request,
                now,
                out FolderState? foldedState))
        {
            return Failed(FolderProvisioningOutcome.ValidationFailed, "folder_aggregate_validation_failed");
        }

        // Enforce A-2 create-only same-name collision per Tenant via CreateOnly CAS name lock.
        string normalizedName = canonicalDisplayName.ToLowerInvariant();
        string nameLockKey = NameLockKey(tenantId, normalizedName);
        FolderNameLockRecord lockRecord = new(tenantId, normalizedName, folderId, request.TaskId, request.StepKey);
        bool locked = await _store.TrySaveAsync(_storeName, nameLockKey, lockRecord, string.Empty, cancellationToken).ConfigureAwait(false);
        if (!locked)
        {
            ReadModelEntry<FolderNameLockRecord> priorLock = await _store
                .GetAsync<FolderNameLockRecord>(_storeName, nameLockKey, cancellationToken)
                .ConfigureAwait(false);
            if (priorLock.Value is null
                || !string.Equals(priorLock.Value.TaskId, request.TaskId, StringComparison.Ordinal)
                || !string.Equals(priorLock.Value.StepKey, request.StepKey, StringComparison.Ordinal))
            {
                return Failed(FolderProvisioningOutcome.Collision, "folder_name_collision");
            }
        }

        bool violateSecurity = Interlocked.Exchange(ref _forceInvalidProvisionSecurityOnce, 0) == 1;
        bool inheritOrgPermissions = violateSecurity || (foldedState!.InheritOrganizationPermissions ?? true);
        bool workloadHasAuthority = violateSecurity;

        ProvisionedFolderRecord persisted = new(
            tenantId,
            folderId,
            canonicalDisplayName,
            receiptId,
            actorPrincipalId,
            request.TaskId,
            request.StepKey,
            request.AdmissionOperation,
            request.RequestSha256,
            ProvisionedVersion,
            inheritOrgPermissions,
            workloadHasAuthority,
            now);

        bool saved = await _store
            .TrySaveAsync(_storeName, ProvisionedByFolderKey(tenantId, folderId), persisted, string.Empty, cancellationToken)
            .ConfigureAwait(false);
        if (!saved)
        {
            ReadModelEntry<ProvisionedFolderRecord> raceRead = await _store
                .GetAsync<ProvisionedFolderRecord>(_storeName, ProvisionedByFolderKey(tenantId, folderId), cancellationToken)
                .ConfigureAwait(false);
            if (raceRead.Value is not null)
            {
                return ValidateProvisionedRecord(raceRead.Value, tenantId, actorPrincipalId, request);
            }

            throw new InvalidOperationException("Provisioned Folder record could not be persisted or observed.");
        }

        if (Interlocked.Exchange(ref _dropNextProvisionReplyOnce, 0) == 1)
        {
            throw new TimeoutException("Simulated lost reply after durable Folder provisioning.");
        }

        return ToProvisionedResult(persisted);
    }

    /// <inheritdoc/>
    public async Task<FolderProvisioningResult> LookupProvisioningAsync(
        string tenantId,
        string actorPrincipalId,
        FolderProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(actorPrincipalId);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsValidProvisioningRequest(tenantId, actorPrincipalId, request))
        {
            return Failed(FolderProvisioningOutcome.ValidationFailed, "invalid_provisioning_request");
        }

        if (TryConsumeOutage(ref _remainingLookupOutages))
        {
            throw new TimeoutException("Simulated transient Folders lookup outage.");
        }

        if (_deniedActors.ContainsKey(ActorKey(tenantId, actorPrincipalId)))
        {
            return Failed(FolderProvisioningOutcome.Denied, "folder_lookup_denied");
        }

        string folderId = DeriveFolderId(tenantId, request.StepKey);
        ReadModelEntry<ProvisionedFolderRecord> entry = await _store
            .GetAsync<ProvisionedFolderRecord>(_storeName, ProvisionedByFolderKey(tenantId, folderId), cancellationToken)
            .ConfigureAwait(false);
        if (entry.Value is null)
        {
            return Failed(FolderProvisioningOutcome.Absent, "folder_not_provisioned");
        }

        return ValidateProvisionedRecord(entry.Value, tenantId, actorPrincipalId, request);
    }

    /// <inheritdoc/>
    public async Task<FolderProvisioningResult> ValidateSuppliedFolderAsync(
        string tenantId,
        string actorPrincipalId,
        string folderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(actorPrincipalId)
            || string.IsNullOrWhiteSpace(folderId))
        {
            return Failed(FolderProvisioningOutcome.Denied, "folder_unavailable");
        }

        if (_deniedActors.ContainsKey(ActorKey(tenantId, actorPrincipalId)))
        {
            return Failed(FolderProvisioningOutcome.Denied, "folder_unavailable");
        }

        // Tenant-scoped read: foreign-Tenant Folders are never visible and never leak orphan/reserved status.
        ReadModelEntry<SuppliedFolderRecord> entry = await _store
            .GetAsync<SuppliedFolderRecord>(_storeName, SuppliedFolderKey(tenantId, folderId), cancellationToken)
            .ConfigureAwait(false);
        if (entry.Value is not { } record)
        {
            return Failed(FolderProvisioningOutcome.Denied, "folder_unavailable");
        }

        // Check TenantId and ManagerActorId BEFORE checking IsOrphanedOrReserved so unauthorized callers
        // never learn whether a foreign or unmanaged Folder is orphaned/reserved.
        if (!string.Equals(record.TenantId, tenantId, StringComparison.Ordinal)
            || !string.Equals(record.ManagerActorId, actorPrincipalId, StringComparison.Ordinal))
        {
            return Failed(FolderProvisioningOutcome.Denied, "folder_unavailable");
        }

        if (record.IsOrphanedOrReserved)
        {
            return new FolderProvisioningResult(
                FolderProvisioningOutcome.OrphanedOrReserved,
                record.FolderId,
                record.DisplayName,
                null,
                record.OwnerVersion,
                record.ManagerActorId,
                record.InheritOrganizationPermissions,
                record.WorkloadHasFolderAuthority,
                "folder_orphaned_or_reserved");
        }

        return new FolderProvisioningResult(
            FolderProvisioningOutcome.Provisioned,
            record.FolderId,
            record.DisplayName,
            null,
            record.OwnerVersion,
            record.ManagerActorId,
            record.InheritOrganizationPermissions,
            record.WorkloadHasFolderAuthority);
    }

    private static bool TryExecuteRealFolderAggregate(
        string tenantId,
        string actorPrincipalId,
        string folderId,
        string receiptId,
        string canonicalDisplayName,
        FolderProvisioningRequest request,
        DateTimeOffset now,
        out FolderState? foldedState)
    {
        foldedState = null;

        // FolderAggregate.Handle caps DisplayName at 128 while FolderAggregate.HandleProvisioning is internal
        // to Hexalith.Folders (allows up to 160). For 129..160 character names, pass the FULL 160-character
        // string in Description (512-char limit, runs exact same Folders IsSafeMetadata / forbidden-content
        // rules on every character 1..160) while passing the 128-char prefix in DisplayName so characters
        // 129..160 can never bypass Folders forbidden-metadata validation.
        int prefixLength = MaxStandardFolderDisplayNameLength;
        if (canonicalDisplayName.Length > MaxStandardFolderDisplayNameLength
            && char.IsHighSurrogate(canonicalDisplayName[MaxStandardFolderDisplayNameLength - 1]))
        {
            prefixLength = MaxStandardFolderDisplayNameLength - 1;
        }

        string aggregateDisplayName = canonicalDisplayName.Length <= MaxStandardFolderDisplayNameLength
            ? canonicalDisplayName
            : canonicalDisplayName[..prefixLength];
        string? fullNameValidationDescription = canonicalDisplayName.Length > MaxStandardFolderDisplayNameLength
            ? canonicalDisplayName
            : null;

        string safeTaskId = request.TaskId.ToLowerInvariant();
        CreateFolder command = new(
            tenantId,
            tenantId,
            folderId,
            aggregateDisplayName,
            fullNameValidationDescription,
            null,
            [],
            actorPrincipalId,
            request.TaskId,
            safeTaskId,
            receiptId,
            tenantId);

        FolderCommandValidationResult validation;
        try
        {
            validation = FolderCommandValidator.Validate(command);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!validation.IsAccepted)
        {
            return false;
        }

        FolderResult result = FolderAggregate.Handle(FolderState.Empty, command, now);
        if (!result.IsAccepted || result.Events is not [FolderCreated created])
        {
            return false;
        }

        FolderProvisioningReceipt receipt = new(
            receiptId,
            tenantId,
            folderId,
            actorPrincipalId,
            request.TaskId,
            request.StepKey,
            request.AdmissionOperation,
            request.RequestSha256,
            ProvisionedVersion);

        FolderCreated provisionedCreated = created with
        {
            DisplayName = canonicalDisplayName,
            Description = null,
            ProvisioningReceipt = receipt,
            InheritOrganizationPermissions = false,
        };

        FolderAccessGranted readGrant = new(
            created.ManagedTenantId,
            created.OrganizationId,
            created.FolderId,
            FolderAccessPrincipalKind.User,
            actorPrincipalId,
            ReadAction,
            actorPrincipalId,
            created.CorrelationId,
            created.TaskId,
            receiptId + "-1",
            created.IdempotencyFingerprint,
            1,
            now);

        FolderAccessGranted manageGrant = new(
            created.ManagedTenantId,
            created.OrganizationId,
            created.FolderId,
            FolderAccessPrincipalKind.User,
            actorPrincipalId,
            ManageAction,
            actorPrincipalId,
            created.CorrelationId,
            created.TaskId,
            receiptId + "-2",
            created.IdempotencyFingerprint,
            2,
            now);

        try
        {
            FolderStreamName streamName = FolderStreamName.Create(tenantId, folderId);
            FolderState state = FolderStateApply.Apply(FolderState.Empty, provisionedCreated, streamName);
            state = FolderStateApply.Apply(state, readGrant, streamName);
            state = FolderStateApply.Apply(state, manageGrant, streamName);

            bool hasRead = state.AccessOverrides.TryGetValue(
                new FolderAccessEntryKey(tenantId, folderId, FolderAccessPrincipalKind.User, actorPrincipalId, ReadAction),
                out FolderAccessOverride? readOverride) && readOverride.IsGranted;
            bool hasManage = state.AccessOverrides.TryGetValue(
                new FolderAccessEntryKey(tenantId, folderId, FolderAccessPrincipalKind.User, actorPrincipalId, ManageAction),
                out FolderAccessOverride? manageOverride) && manageOverride.IsGranted;
            if (!state.IsCreated || state.InheritOrganizationPermissions != false || !hasRead || !hasManage)
            {
                return false;
            }

            foldedState = state;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private static FolderProvisioningResult ValidateProvisionedRecord(
        ProvisionedFolderRecord record,
        string tenantId,
        string actorPrincipalId,
        FolderProvisioningRequest request)
    {
        if (!string.Equals(record.TenantId, tenantId, StringComparison.Ordinal)
            || !string.Equals(record.OriginalActorId, actorPrincipalId, StringComparison.Ordinal)
            || !string.Equals(record.TaskId, request.TaskId, StringComparison.Ordinal)
            || !string.Equals(record.StepKey, request.StepKey, StringComparison.Ordinal)
            || !string.Equals(record.AdmissionOperation, request.AdmissionOperation, StringComparison.Ordinal)
            || !string.Equals(record.RequestSha256, request.RequestSha256, StringComparison.Ordinal)
            || record.OwnerVersion != ProvisionedVersion)
        {
            return Failed(FolderProvisioningOutcome.Denied, "folder_receipt_mismatch");
        }

        return ToProvisionedResult(record);
    }

    private static FolderProvisioningResult ToProvisionedResult(ProvisionedFolderRecord record)
        => new(
            FolderProvisioningOutcome.Provisioned,
            record.FolderId,
            record.DisplayName,
            record.ReceiptId,
            record.OwnerVersion,
            record.OriginalActorId,
            record.InheritOrganizationPermissions,
            record.WorkloadHasFolderAuthority);

    private static bool IsValidProvisioningRequest(
        string tenantId,
        string actorPrincipalId,
        FolderProvisioningRequest request)
        => !string.IsNullOrWhiteSpace(tenantId)
            && !string.IsNullOrWhiteSpace(actorPrincipalId)
            && !string.IsNullOrWhiteSpace(request.TaskId)
            && IsCanonicalStepKey(request.TaskId, request.StepKey)
            && !string.IsNullOrWhiteSpace(request.AdmissionOperation)
            && request.AdmissionOperation.Length <= 128
            && request.Fence > 0
            && request.ExpectedVersion == 0
            && !string.IsNullOrWhiteSpace(request.DisplayName)
            && request.DisplayName.Trim().Length is >= 1 and <= MaxProvisioningDisplayNameLength
            && request.RequestSha256 is { Length: 64 }
            && string.Equals(request.RequestSha256, ComputeRequestSha256(request.DisplayName), StringComparison.Ordinal);

    private static bool IsCanonicalStepKey(string taskId, string? stepKey)
    {
        if (string.IsNullOrWhiteSpace(stepKey))
        {
            return false;
        }

        string prefix = taskId + "/step/";
        return stepKey.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(stepKey.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int number)
            && number > 0
            && string.Equals($"{prefix}{number.ToString(CultureInfo.InvariantCulture)}", stepKey, StringComparison.Ordinal);
    }

    private static bool TryConsumeOutage(ref int counter)
    {
        while (true)
        {
            int current = Volatile.Read(ref counter);
            if (current <= 0)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref counter, current - 1, current) == current)
            {
                return true;
            }
        }
    }

    private static FolderProvisioningResult Failed(FolderProvisioningOutcome outcome, string reasonCode)
        => new(outcome, null, null, null, 0, null, false, false, reasonCode);

    private static string ActorKey(string tenantId, string actorPrincipalId)
        => $"{tenantId.Length}:{tenantId}:{actorPrincipalId.Length}:{actorPrincipalId}";

    private static string ProvisionedByFolderKey(string tenantId, string folderId)
        => $"story71:folders:provisioned:v1:{tenantId.Length}:{tenantId}:{folderId.Length}:{folderId}";

    private static string SuppliedFolderKey(string tenantId, string folderId)
        => $"story71:folders:supplied:v1:{tenantId.Length}:{tenantId}:{folderId.Length}:{folderId}";

    private static string NameLockKey(string tenantId, string normalizedName)
    {
        string nameHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedName)));
        return $"story71:folders:name-lock:v1:{tenantId.Length}:{tenantId}:{nameHash}";
    }
}
#endif
