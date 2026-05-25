// <copyright file="AuthorizationLayer.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Authorization;

using System.Text.Json.Serialization;

/// <summary>The once-declared ordered authorization layers for Projects operations.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AuthorizationLayer>))]
public enum AuthorizationLayer
{
    JwtValidation,
    EventStoreClaimTransform,
    TenantAccessFreshness,
    ProjectAcl,
    EventStoreValidator,
    DaprDenyByDefaultPolicy,
}
