// <copyright file="ConversationStartSetupProjector.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.Projections.ConversationStartSetup;

using Hexalith.Projects.Contracts.Models;

/// <summary>
/// Pure projector realizing AR-8's <c>ConversationStartSetupProjection</c> as a server-side projection
/// over the policy-assembled <see cref="ProjectContext"/> (Story 3.1 / Story 3.5 / FR-20).
/// </summary>
/// <remarks>
/// <para>
/// Design Decision (Story 3.5 Dev Notes): the named projection is materialized as a pure projector over
/// <see cref="ProjectContext.Setup"/> (the policy's pass-through of <c>ProjectDetailItem.Setup</c>)
/// rather than a separate event-stream projection over <c>ProjectCreated</c> /
/// <c>ProjectSetupUpdated</c> / <c>ProjectArchived</c> events. A parallel event-stream projection would
/// duplicate <c>ProjectDetailItem.Setup</c> with no new information surfaced and force a two-projection
/// rebuild discipline; the pure-projector form preserves the single source of truth and inherits
/// freshness from the policy. The decision is reversible if a benchmark proves the indirection
/// unacceptable.
/// </para>
/// <para>
/// Purity invariants: single input contract; no <see cref="System.DateTimeOffset.UtcNow"/> /
/// <see cref="System.DateTime.UtcNow"/> / <see cref="System.Diagnostics.Stopwatch"/> /
/// <see cref="System.Environment.TickCount"/> / GUID / random; no infrastructure imports; consumes
/// ONLY <see cref="Hexalith.Projects.Contracts.Models"/> (and transitively
/// <see cref="Hexalith.Projects.Contracts.Ui"/>) types — never <see cref="Hexalith.Projects.Context"/>
/// or <see cref="Hexalith.Projects.Projections.ProjectDetail"/> types.
/// </para>
/// </remarks>
public static class ConversationStartSetupProjector
{
    /// <summary>
    /// Projects the policy-assembled context onto the bounded conversation-start subset.
    /// </summary>
    /// <param name="context">The policy-assembled <see cref="ProjectContext"/> (SOLE input — the projector
    /// never inspects raw <c>ProjectDetailItem.Setup</c> directly).</param>
    /// <returns>The bounded <see cref="ConversationStartSetup"/> wire DTO.</returns>
    public static ConversationStartSetup Project(ProjectContext context)
        => ConversationStartSetup.FromContext(context);
}
