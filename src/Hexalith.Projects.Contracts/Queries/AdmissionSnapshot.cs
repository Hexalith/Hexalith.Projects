// <copyright file="AdmissionSnapshot.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using System;
using System.Collections.Generic;

/// <summary>Shared AD-32 response snapshot governing supported Project read usability.</summary>
/// <param name="ResponseState">The aggregate response state.</param>
/// <param name="AsOf">The persisted read-model observation instant.</param>
/// <param name="ProjectVersion">The authorized Project read-model version, or zero when not disclosable.</param>
/// <param name="Components">Metadata-only component evidence.</param>
/// <param name="RecoveryActions">Bounded recovery actions.</param>
public sealed record AdmissionSnapshot(
    AdmissionResponseState ResponseState,
    DateTimeOffset AsOf,
    long ProjectVersion,
    IReadOnlyList<AdmissionComponent> Components,
    IReadOnlyList<string> RecoveryActions);
