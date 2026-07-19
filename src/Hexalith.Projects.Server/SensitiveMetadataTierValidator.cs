// <copyright file="SensitiveMetadataTierValidator.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Server;

/// <summary>Validates the exact metadata-classification vocabulary declared by the OpenAPI contract.</summary>
internal static class SensitiveMetadataTierValidator
{
    /// <summary>Determines whether a wire value is a supported sensitive metadata tier.</summary>
    /// <param name="value">The metadata-classification wire value.</param>
    /// <returns><see langword="true"/> when the value exactly matches a supported tier; otherwise, <see langword="false"/>.</returns>
    internal static bool IsValid(string? value)
        => value is "public_metadata" or "tenant_sensitive" or "credential_sensitive" or "secret";
}
