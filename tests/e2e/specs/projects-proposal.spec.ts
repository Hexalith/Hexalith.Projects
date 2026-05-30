import { test, expect } from '../support/merged-fixtures.js';
import { queryHeaders } from '../support/helpers/correlation.js';
import {
  confirmNewProjectProposal,
  proposeNewProject,
  type ConfirmNewProjectProposalInput,
  type ProjectCreationProposalInput,
} from '../support/helpers/projects-api-client.js';

/**
 * F5 critical journey — NoMatch proposal preview → explicit confirm (FR-15 / Story 4.5).
 *
 * `test.fixme` until the AppHost exposes seeded conversation/folder/file ACL fixtures for
 * the real cross-module API. These tests lock the Playwright API shape for preview-only
 * inference, explicit command-async confirmation, idempotent recovery, safe-denial, and
 * no-payload-leakage assertions.
 */
test.describe('Projects new-project proposal', () => {
  const conversationId = 'conversation_proposal_e2e_001';
  const folderId = 'folder_proposal_e2e_001';
  const fileReferenceId = 'file_proposal_e2e_001';
  const workspaceId = 'workspace_proposal_e2e_001';
  const filePath = 'docs/synthetic-note.md';

  function proposalRequest(overrides: Partial<ProjectCreationProposalInput> = {}): ProjectCreationProposalInput {
    return {
      requestSchemaVersion: 'v1',
      conversationId,
      folderId,
      fileReferenceIds: [fileReferenceId],
      suggestedName: 'synthetic-project-alpha',
      description: 'synthetic metadata description',
      setupMetadata: 'synthetic-setup-reference',
      ...overrides,
    };
  }

  function confirmRequest(overrides: Partial<ConfirmNewProjectProposalInput> = {}): ConfirmNewProjectProposalInput {
    return {
      requestSchemaVersion: 'v1',
      operation: 'confirmNewProjectProposal',
      resolutionResult: 'NoMatch',
      confirmed: true,
      projectId: 'project_proposal_e2e_001',
      conversationId,
      projectMetadata: {
        displayName: 'synthetic-project-alpha',
        metadataClass: 'tenant_sensitive',
      },
      description: 'synthetic metadata description',
      setupMetadata: 'synthetic-setup-reference',
      folder: {
        folderId,
        folderMetadata: {
          displayName: 'synthetic-project-alpha',
        },
      },
      fileReferences: [
        {
          fileReferenceId,
          folderId,
          workspaceId,
          filePath,
          fileMetadata: {
            displayName: 'synthetic-note',
          },
        },
      ],
      fileReferenceIds: [fileReferenceId],
      ...overrides,
    };
  }

  function assertNoProposalPayloadLeakage(serialized: string, tenantId: string): void {
    expect(serialized).not.toContain('tenantId');
    expect(serialized).not.toContain(tenantId);
    expect(serialized).not.toContain('transcript');
    expect(serialized).not.toContain('prompt');
    expect(serialized).not.toContain('memory body');
    expect(serialized).not.toContain('secret');
    expect(serialized).not.toContain('raw token');
    expect(serialized).not.toContain(workspaceId);
    expect(serialized).not.toContain(filePath);
  }

  test.fixme('previews a NoMatch proposal without creating or leaking sibling payload data (AC1,3,8)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const { status, body } = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(),
      {
        authToken,
        correlationId: 'corr-proposal-preview',
        freshness: 'eventually_consistent',
      },
    );

    expect(status).toBe(200);
    expect(body).toMatchObject({
      resolutionResult: 'NoMatch',
      suggestedName: 'synthetic-project-alpha',
      conversationId,
      folderId,
      freshness: 'eventually_consistent',
    });
    expect(body.fileReferenceIds).toEqual([fileReferenceId]);
    expect(body.warnings).toEqual([]);
    assertNoProposalPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  test.fixme('rejects preview idempotency, strong freshness, duplicate references, and unsafe metadata (AC3,8)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const idempotencyRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(),
      {
        authToken,
        correlationId: 'corr-proposal-idempotency',
        extraHeaders: { 'Idempotency-Key': 'query-idempotency-is-invalid' },
      },
    );
    expect(idempotencyRejected.status).toBe(400);

    const freshnessRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest(),
      {
        authToken,
        correlationId: 'corr-proposal-freshness',
        freshness: 'strong',
      },
    );
    expect(freshnessRejected.status).toBe(400);

    const duplicateReferenceRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest({ fileReferenceIds: [fileReferenceId, fileReferenceId] }),
      { authToken, correlationId: 'corr-proposal-duplicate-reference' },
    );
    expect(duplicateReferenceRejected.status).toBe(400);

    const unsafeMetadataRejected = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest({ setupMetadata: 'secret raw token' }),
      { authToken, correlationId: 'corr-proposal-unsafe-metadata' },
    );
    expect(unsafeMetadataRejected.status).toBe(400);
    assertNoProposalPayloadLeakage(JSON.stringify(unsafeMetadataRejected.body), tenantContext.tenantId);
  });

  test.fixme('returns a safe conflict when an existing Project now qualifies instead of proposing creation (AC1,3)', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const { status, body } = await proposeNewProject(
      apiRequest,
      tenantContext.tenantId,
      proposalRequest({ conversationId: 'conversation_proposal_existing_match_001' }),
      { authToken, correlationId: 'corr-proposal-existing-match' },
    );

    expect(status).toBe(400);
    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain(seededProject.projectId);
    assertNoProposalPayloadLeakage(serialized, tenantContext.tenantId);
  });

  test.fixme('confirms a NoMatch proposal through command-async create, conversation assignment, folder, and file links (AC2,4,5,7)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const { status, body } = await confirmNewProjectProposal(
      apiRequest,
      tenantContext.tenantId,
      confirmRequest(),
      {
        authToken,
        correlationId: 'corr-proposal-confirm',
        taskId: 'task-proposal-confirm',
        idempotencyKey: 'idem-proposal-confirm',
      },
    );

    expect(status).toBe(202);
    expect(body.correlationId).toBeTruthy();
    assertNoProposalPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  test.fixme('same root idempotency key with a different confirm body returns conflict without duplicate writes (AC7)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const first = await confirmNewProjectProposal(
      apiRequest,
      tenantContext.tenantId,
      confirmRequest({ projectId: 'project_proposal_e2e_idem_001' }),
      {
        authToken,
        correlationId: 'corr-proposal-idem-first',
        taskId: 'task-proposal-idem-first',
        idempotencyKey: 'idem-proposal-retry',
      },
    );
    expect(first.status).toBe(202);

    const conflict = await confirmNewProjectProposal(
      apiRequest,
      tenantContext.tenantId,
      confirmRequest({
        projectId: 'project_proposal_e2e_idem_001',
        projectMetadata: {
          displayName: 'synthetic-project-beta',
          metadataClass: 'tenant_sensitive',
        },
      }),
      {
        authToken,
        correlationId: 'corr-proposal-idem-conflict',
        taskId: 'task-proposal-idem-conflict',
        idempotencyKey: 'idem-proposal-retry',
      },
    );

    expect(conflict.status).toBe(409);
    assertNoProposalPayloadLeakage(JSON.stringify(conflict.body), tenantContext.tenantId);
  });

  test.fixme('confirm validation fails closed for missing idempotency and mismatched file evidence (AC4,6,8)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const missingIdempotency = await apiRequest({
      method: 'POST',
      path: '/api/v1/projects/proposals/confirm',
      headers: {
        ...queryHeaders({ authToken, correlationId: 'corr-proposal-missing-idem' }),
        'X-Hexalith-Tenant-Id': tenantContext.tenantId,
      },
      body: confirmRequest(),
      retryConfig: { maxRetries: 0 },
    });
    expect(missingIdempotency.status).toBe(400);

    const mismatchedFileEvidence = await confirmNewProjectProposal(
      apiRequest,
      tenantContext.tenantId,
      confirmRequest({ fileReferenceIds: ['file_proposal_e2e_999'] }),
      {
        authToken,
        correlationId: 'corr-proposal-file-evidence',
        taskId: 'task-proposal-file-evidence',
        idempotencyKey: 'idem-proposal-file-evidence',
      },
    );
    expect(mismatchedFileEvidence.status).toBe(400);
    assertNoProposalPayloadLeakage(JSON.stringify(mismatchedFileEvidence.body), tenantContext.tenantId);
  });
});
