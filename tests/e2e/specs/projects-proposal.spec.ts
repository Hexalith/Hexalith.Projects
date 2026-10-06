import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { queryHeaders } from '../support/helpers/correlation.js';
import type { LiveFixtureGraph } from '../support/helpers/live-fixtures-api-client.js';
import {
  confirmNewProjectProposal,
  proposeNewProject,
  type ConfirmNewProjectProposalInput,
  type ProjectCreationProposalInput,
} from '../support/helpers/projects-api-client.js';

/**
 * F5 critical journey — NoMatch proposal preview → explicit confirm (FR-15 / Story 4.5).
 *
 * Live-only: conversation/folder/file metadata comes from the attempt-scoped sibling graph and every
 * Project/request identity is derived from it. Created Projects are archived to convergence in
 * reverse order by `liveCleanup`, which preserves the primary failure.
 */
test.describe('Projects new-project proposal', () => {
  function safeFailureSummary(body: unknown): string {
    const problem = body as { category?: unknown; details?: { rejectedField?: unknown } };
    return `category=${String(problem?.category ?? 'none')}, rejectedField=${String(problem?.details?.rejectedField ?? 'none')}, body=${JSON.stringify(body)}`;
  }

  function proposalRequest(graph: LiveFixtureGraph, overrides: Partial<ProjectCreationProposalInput> = {}): ProjectCreationProposalInput {
    return {
      requestSchemaVersion: 'v1',
      conversationId: graph.conversationId,
      folderId: graph.proposalFolderId,
      fileReferenceIds: [graph.proposalFileReferenceId],
      suggestedName: 'synthetic-project-alpha',
      description: 'synthetic metadata description',
      setupMetadata: 'synthetic-setup-reference',
      ...overrides,
    };
  }

  function confirmRequest(graph: LiveFixtureGraph, overrides: Partial<ConfirmNewProjectProposalInput> = {}): ConfirmNewProjectProposalInput {
    return {
      requestSchemaVersion: 'v1',
      operation: 'confirmNewProjectProposal',
      resolutionResult: 'NoMatch',
      confirmed: true,
      projectId: graph.proposalProjectId,
      conversationId: graph.conversationId,
      projectMetadata: {
        displayName: 'synthetic-project-alpha',
        metadataClass: 'tenant_sensitive',
      },
      description: 'synthetic metadata description',
      setupMetadata: 'synthetic-setup-reference',
      folder: {
        folderId: graph.proposalFolderId,
        folderMetadata: {
          displayName: 'synthetic-project-alpha',
        },
      },
      fileReferences: [
        {
          fileReferenceId: graph.proposalFileReferenceId,
          folderId: graph.proposalFolderId,
          workspaceId: graph.workspaceId,
          filePath: graph.filePath,
          fileMetadata: {
            displayName: 'synthetic-note',
          },
        },
      ],
      fileReferenceIds: [graph.proposalFileReferenceId],
      ...overrides,
    };
  }

  function assertNoProposalPayloadLeakage(serialized: string, tenantId: string, graph: LiveFixtureGraph): void {
    expect(serialized).not.toContain('tenantId');
    expect(serialized).not.toContain(tenantId);
    expect(serialized).not.toContain('transcript');
    expect(serialized).not.toContain('prompt');
    expect(serialized).not.toContain('memory body');
    expect(serialized).not.toContain('secret');
    expect(serialized).not.toContain('raw token');
    expect(serialized).not.toContain(graph.workspaceId);
    expect(serialized).not.toContain(graph.filePath);
  }

  liveAppHostTest('previews a NoMatch proposal without creating or leaking sibling payload data (AC1,3,8)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    liveFixtureGraph,
  }) => {
    const { status, body } = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(liveFixtureGraph),
      {
        authToken,
        correlationId: requestIdentity('proposal-preview').correlationId,
        freshness: 'eventually_consistent',
      },
    );

    expect(status).toBe(200);
    expect(body).toMatchObject({
      resolutionResult: 'NoMatch',
      suggestedName: 'synthetic-project-alpha',
      conversationId: liveFixtureGraph.conversationId,
      folderId: liveFixtureGraph.proposalFolderId,
      freshness: 'eventually_consistent',
    });
    expect(body.fileReferenceIds).toEqual([liveFixtureGraph.proposalFileReferenceId]);
    expect(body.warnings).toEqual([]);
    assertNoProposalPayloadLeakage(JSON.stringify(body), tenantContext.tenantId, liveFixtureGraph);
  });

  liveAppHostTest('rejects preview idempotency, strong freshness, duplicate references, and unsafe metadata (AC3,8)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    liveFixtureGraph,
  }) => {
    const idempotencyRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(liveFixtureGraph),
      {
        authToken,
        correlationId: requestIdentity('proposal-preview-idempotency').correlationId,
        extraHeaders: { 'Idempotency-Key': requestIdentity('proposal-preview-idempotency').idempotencyKey },
      },
    );
    expect(idempotencyRejected.status).toBe(400);

    const freshnessRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(liveFixtureGraph),
      {
        authToken,
        correlationId: requestIdentity('proposal-preview-freshness').correlationId,
        freshness: 'strong',
      },
    );
    expect(freshnessRejected.status).toBe(400);

    const duplicateReferenceRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(liveFixtureGraph, { fileReferenceIds: [liveFixtureGraph.proposalFileReferenceId, liveFixtureGraph.proposalFileReferenceId] }),
      { authToken, correlationId: requestIdentity('proposal-preview-duplicate-reference').correlationId },
    );
    expect(duplicateReferenceRejected.status).toBe(400);

    const unsafeMetadataRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(liveFixtureGraph, { setupMetadata: 'secret raw token' }),
      { authToken, correlationId: requestIdentity('proposal-preview-unsafe-metadata').correlationId },
    );
    expect(unsafeMetadataRejected.status).toBe(400);
    assertNoProposalPayloadLeakage(JSON.stringify(unsafeMetadataRejected.body), tenantContext.tenantId, liveFixtureGraph);
  });

  liveAppHostTest('returns a safe conflict when an existing Project now qualifies instead of proposing creation (AC1,3)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    seededProject,
    liveFixtureGraph,
  }) => {
    const { status, body } = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(liveFixtureGraph, { conversationId: liveFixtureGraph.existingConversationId }),
      { authToken, correlationId: requestIdentity('proposal-existing-match').correlationId },
    );

    expect(status).toBe(400);
    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain(seededProject.projectId);
    assertNoProposalPayloadLeakage(serialized, tenantContext.tenantId, liveFixtureGraph);
  });

  liveAppHostTest('confirms a NoMatch proposal through command-async create, conversation assignment, folder, and file links (AC2,4,5,7)', async ({
    authToken,
    liveCleanup,
    request,
    requestIdentity,
    tenantContext,
    liveFixtureGraph,
  }) => {
    const { status, body } = await confirmNewProjectProposal(
      request,
      tenantContext.tenantId,
      confirmRequest(liveFixtureGraph),
      { authToken, ...requestIdentity('proposal-confirm') },
    );
    if (status === 202) liveCleanup.trackProject(liveFixtureGraph.proposalProjectId, 'proposal');

    expect(status, safeFailureSummary(body)).toBe(202);
    expect(body.correlationId).toBeTruthy();
    assertNoProposalPayloadLeakage(JSON.stringify(body), tenantContext.tenantId, liveFixtureGraph);
  });

  liveAppHostTest('same root idempotency key with a different confirm body returns conflict without duplicate writes (AC7)', async ({
    authToken,
    liveCleanup,
    request,
    requestIdentity,
    tenantContext,
    liveFixtureGraph,
  }) => {
    // Both requests share one attempt-scoped idempotency root; correlation/task identities differ.
    const { idempotencyKey } = requestIdentity('proposal-retry');
    const first = await confirmNewProjectProposal(
      request,
      tenantContext.tenantId,
      confirmRequest(liveFixtureGraph, { projectId: liveFixtureGraph.proposalRetryProjectId }),
      { authToken, ...requestIdentity('proposal-retry-first'), idempotencyKey },
    );
    if (first.status === 202) liveCleanup.trackProject(liveFixtureGraph.proposalRetryProjectId, 'proposal-retry');
    expect(first.status, safeFailureSummary(first.body)).toBe(202);

    const conflict = await confirmNewProjectProposal(
      request,
      tenantContext.tenantId,
      confirmRequest(liveFixtureGraph, {
        projectId: liveFixtureGraph.proposalRetryProjectId,
        projectMetadata: {
          displayName: 'synthetic-project-beta',
          metadataClass: 'tenant_sensitive',
        },
      }),
      { authToken, ...requestIdentity('proposal-retry-conflict'), idempotencyKey },
    );

    expect(conflict.status).toBe(409);
    assertNoProposalPayloadLeakage(JSON.stringify(conflict.body), tenantContext.tenantId, liveFixtureGraph);
  });

  liveAppHostTest('confirm validation fails closed for missing idempotency and mismatched file evidence (AC4,6,8)', async ({
    apiRequest,
    authToken,
    request,
    requestIdentity,
    tenantContext,
    liveFixtureGraph,
  }) => {
    const missingIdempotency = await apiRequest({
      method: 'POST',
      path: '/api/v1/projects/proposals/confirm',
      headers: {
        ...queryHeaders({ authToken, correlationId: requestIdentity('proposal-confirm-missing-idempotency').correlationId }),
        'X-Hexalith-Tenant-Id': tenantContext.tenantId,
      },
      body: confirmRequest(liveFixtureGraph),
      retryConfig: { maxRetries: 0 },
    });
    expect(missingIdempotency.status).toBe(400);

    const mismatchedFileEvidence = await confirmNewProjectProposal(
      request,
      tenantContext.tenantId,
      confirmRequest(liveFixtureGraph, { fileReferenceIds: [liveFixtureGraph.secondaryFileReferenceId] }),
      { authToken, ...requestIdentity('proposal-confirm-file-evidence') },
    );
    expect(mismatchedFileEvidence.status).toBe(400);
    assertNoProposalPayloadLeakage(JSON.stringify(mismatchedFileEvidence.body), tenantContext.tenantId, liveFixtureGraph);
  });
});
