// <copyright file="ProjectCreationCallerClass.cs" company="Hexalith">
// Copyright (c) Hexalith. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Projects.SdkWriter.Admission;

/// <summary>Caller persona classes evaluated by Story 7.1 creation admission.</summary>
public enum ProjectCreationCallerClass
{
    /// <summary>An authorized Project User (direct or via delegated Chatbot).</summary>
    ProjectUser = 1,

    /// <summary>An authorized Service/Workflow Caller (create-only with no Folder supplied).</summary>
    ServiceWorkflowCaller = 2,

    /// <summary>A Tenant Operator — never a creation persona (always Denied).</summary>
    TenantOperator = 3,

    /// <summary>A Tenant Project Administrator — not a creation persona (always Denied).</summary>
    TenantProjectAdministrator = 4,

    /// <summary>A Projects or sibling workload identity — grants no actor authority (always Denied).</summary>
    WorkloadIdentity = 5,
}
