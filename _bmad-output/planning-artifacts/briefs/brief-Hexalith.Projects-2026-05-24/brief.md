---
title: "Product Brief: Hexalith.Projects"
status: "draft"
created: "2026-05-24"
updated: "2026-05-24"
---

# Product Brief: Hexalith.Projects

## Executive Summary

Hexalith.Projects is a Hexalith module for managing project data and setup for the Hexalith.Chatbot application. It gives the chatbot a durable project boundary that can group conversations from `Hexalith.Conversations` and folders from `Hexalith.Folders` into a coherent workspace.

The module matters because an AI chat experience quickly stops being useful when every conversation, file, and setup decision lives as an isolated artifact. Users need a project they can return to: a place where related chats, persistent folders, configuration, and operational metadata remain connected across sessions.

[ASSUMPTION] The first version is an internal Hexalith platform module consumed primarily by Hexalith.Chatbot and future Hexalith agent experiences, not a standalone end-user project management product.

## The Problem

Hexalith.Chatbot needs a stable way to organize AI work around projects. Conversations are already handled by `Hexalith.Conversations`, and durable file persistence is handled by `Hexalith.Folders`, but the chatbot still needs a higher-level project model that can tie those capabilities together.

Without a project module, the application risks spreading project setup across chatbot UI state, conversation metadata, folder naming conventions, and ad hoc configuration records. That would make project lifecycle, access control, setup reproducibility, and cross-feature navigation harder to reason about.

The cost of the status quo is operational drift: users and agents may lose the connection between a project, its conversations, its persistent files, and the setup required to continue work safely.

## The Solution

Hexalith.Projects provides the project-level boundary for AI chatbot workspaces. A project owns the durable setup information needed by Hexalith.Chatbot and maintains references to the conversations and folders that belong to that project.

The module should compose existing Hexalith capabilities instead of duplicating them. `Hexalith.Conversations` remains the system of record for conversation lifecycle and governance. `Hexalith.Folders` remains the system of record for persistent file organization and file-related operational safety. Projects coordinates those modules through stable identifiers, associations, and read models that make project work discoverable and resumable.

[ASSUMPTION] The core project experience includes creating a project, configuring project setup, associating conversations, associating one or more folders, reading project summaries, and controlling project availability or archival state.

## What Makes This Different

Hexalith.Projects is not a generic task board, document repository, or chatbot transcript store. Its value is the domain boundary: it gives AI chat work a durable, tenant-aware project context while preserving the ownership of conversations and file persistence in their existing bounded contexts.

The advantage is architectural fit. The module can reuse Hexalith.EventStore, tenant isolation, Dapr-based infrastructure patterns, `Hexalith.Conversations`, and `Hexalith.Folders` rather than inventing a separate persistence or orchestration path.

## Who This Serves

The primary user is a Hexalith.Chatbot user who works with AI across multiple sessions and needs each project to keep its linked conversations, folders, setup, and continuity.

Secondary users are operators and developers of Hexalith.Chatbot who need a clean integration surface for project setup, project-scoped data discovery, and future agent workflow capabilities.

## Success Criteria

- Hexalith.Chatbot can create, open, update, archive, and list projects through a stable Projects API or client surface.
- Each project can reference its related `Hexalith.Conversations` conversations without owning or duplicating conversation persistence.
- Each project can reference its related `Hexalith.Folders` folders without exposing raw file browsing or bypassing folder authorization.
- Tenant isolation is enforced consistently across project reads, writes, conversation links, and folder links.
- Project setup can be reconstructed from durable state rather than ephemeral chatbot UI state.
- Logs, diagnostics, events, and errors remain metadata-only and do not leak file contents, conversation payloads, prompts, secrets, or personal data.

## First Version Scope

In scope:

- Project identity, lifecycle, and metadata needed by Hexalith.Chatbot.
- Project setup data required to initialize or resume a Hexalith.Chatbot project.
- Associations from projects to conversations managed by `Hexalith.Conversations`.
- Associations from projects to folders managed by `Hexalith.Folders`.
- Tenant-aware command and query contracts for project management.
- Read models that let Hexalith.Chatbot list and resume projects efficiently.
- Integration guidance for Chatbot, Conversations, and Folders.

Out of scope:

- Storing chatbot transcript content directly in Projects.
- Storing file contents, diffs, or unrestricted filesystem paths directly in Projects.
- Replacing `Hexalith.Conversations` as the conversation system of record.
- Replacing `Hexalith.Folders` as the file persistence and folder authorization boundary.
- Building a generic project management suite with tasks, milestones, kanban boards, or scheduling.
- Building an independent end-user UI beyond what Hexalith.Chatbot or generated admin surfaces require.

## Implementation Principles

- Use Hexalith.EventStore and the established Dapr infrastructure boundary for durable state and event flow.
- Treat `ProjectId`, `ConversationId`, `FolderId`, `TenantId`, and related stable identifiers as durable references.
- Keep project aggregates deterministic and side-effect free; external module validation and hydration belong at application boundaries or adapters.
- Fail closed when tenant access, project state, conversation association, or folder authorization is missing, stale, or unavailable.
- Preserve metadata-only behavior across events, logs, diagnostics, Problem Details, and audit evidence.
- Keep public contracts additive and serialization-tolerant unless an explicit breaking change is approved.

## Open Questions

- What exact setup data must Hexalith.Chatbot store at the project level versus conversation-level or folder-level state?
- Is a project allowed to have multiple root folders, or should the first version enforce one canonical project folder?
- Can conversations belong to more than one project, or is project membership exclusive?
- What lifecycle states are required beyond active and archived, such as disabled, deleted, initializing, or suspended?
- Which operations require operator/admin tooling versus Chatbot-facing user workflows?
- What audit events are required for linking and unlinking conversations and folders?

## Vision

If successful, Hexalith.Projects becomes the durable workspace backbone for Hexalith AI applications. It lets users and agents return to work with continuity: the right conversations, files, setup, permissions, and operational evidence are already connected.

Over time, this can support richer AI project workflows such as project templates, agent setup profiles, reproducible workspace initialization, project-scoped memory policies, and cross-application project navigation while keeping each bounded context responsible for its own data.
