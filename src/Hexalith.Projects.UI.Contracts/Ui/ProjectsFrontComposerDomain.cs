// <copyright file="ProjectsFrontComposerDomain.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Contracts.Ui;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// FrontComposer bounded-context marker for the Projects operational console.
/// </summary>
[BoundedContext("Projects", DisplayLabel = "Projects")]
public sealed class ProjectsFrontComposerDomain
{
}

