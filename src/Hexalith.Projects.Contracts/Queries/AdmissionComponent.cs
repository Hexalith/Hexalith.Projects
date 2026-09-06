// <copyright file="AdmissionComponent.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Queries;

using Hexalith.Projects.Contracts.Models;

/// <summary>Metadata-only evidence for one AD-32 response component.</summary>
/// <param name="Name">The stable component name.</param>
/// <param name="Included">Whether the component is present.</param>
/// <param name="Freshness">The component freshness.</param>
/// <param name="Reason">A bounded safe reason code.</param>
public sealed record AdmissionComponent(string Name, bool Included, EvidenceFreshnessState Freshness, string Reason);
