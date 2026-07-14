import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { queryHeaders, mutationHeaders } from '../support/helpers/correlation.js';
import { confirmProjectResolution, resolveProjectFromAttachments } from '../support/helpers/projects-api-client.js';

/**
 * F5 critical journey — resolution → confirm (FR-12/13/14; E1 / R10).
 *
 * `test.fixme` until the AppHost exposes seeded resolution fixtures for the real API.
 * The spine-backed routes and exact wire assertions are scaffolded here: binary outcomes,
 * reason codes, safe-denial, query validation, and the never-silently-attach guarantee.
 *
 * Story 4.3's attachment-resolution query is now spine-backed; the explicit live lane
 * still requires seeded folder/file reference fixtures.
 */
test.describe('Projects resolution', () => {
  const folderId = 'folder-001';
  const fileId = 'file-001';

  function assertNoResolutionPayloadLeakage(serialized: string, tenantId: string): void {
    expect(serialized).not.toContain('tenantId');
    expect(serialized).not.toContain(tenantId);
    expect(serialized).not.toContain('workspace');
    expect(serialized).not.toContain('secret');
    expect(serialized).not.toContain('docs/contract.pdf');
  }

  liveAppHostTest('folder attachment resolves to a single candidate without leaking tenant or path data (FR-13 / AC1,7)', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const { status, body } = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [folderId] },
      { authToken, correlationId: 'corr-resolution-folder' },
    );

    expect(status).toBe(200);
    expect(body.result).toBe('SingleCandidate');
    expect(body.candidates).toContainEqual(
      expect.objectContaining({
        projectId: seededProject.projectId,
        reasonCodes: expect.arrayContaining(['ProjectFolderMatched']),
      }),
    );
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('file attachment resolves with FileReferenceMatched and does not read raw content (FR-13 / AC1,2)', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const { status, body } = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { fileIds: [fileId] },
      { authToken, correlationId: 'corr-resolution-file' },
    );

    expect(status).toBe(200);
    expect(body.result).toBe('SingleCandidate');
    expect(body.candidates).toContainEqual(
      expect.objectContaining({
        projectId: seededProject.projectId,
        reasonCodes: expect.arrayContaining(['FileReferenceMatched']),
      }),
    );
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('folder and file attachments can produce multiple candidates and never auto-attach (FR-13 / NFR-9)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const { status, body } = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [folderId], fileIds: [fileId] },
      { authToken, correlationId: 'corr-resolution-multiple' },
    );

    expect(status).toBe(200);
    expect(body.result).toBe('MultipleCandidates');
    expect(body.candidates.length).toBeGreaterThan(1);
    expect(JSON.stringify(body)).not.toContain('attached');
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('attachment query rejects Idempotency-Key and strong freshness as validation errors (AC5)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const idempotencyRejected = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [folderId] },
      {
        authToken,
        correlationId: 'corr-resolution-idempotency',
        extraHeaders: { 'Idempotency-Key': 'query-idempotency-is-invalid' },
      },
    );
    expect(idempotencyRejected.status).toBe(400);

    const freshnessRejected = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [folderId] },
      {
        authToken,
        correlationId: 'corr-resolution-freshness',
        freshness: 'strong',
      },
    );
    expect(freshnessRejected.status).toBe(400);
  });

  liveAppHostTest('missing or malformed attachment identifiers collapse to safe-denial 404 (AC6)', async ({
    apiRequest,
    authToken,
    tenantContext,
  }) => {
    const missing = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      {},
      { authToken, correlationId: 'corr-resolution-missing' },
    );
    expect(missing.status).toBe(404);

    const malformed = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { fileIds: ['bad/slash'] },
      { authToken, correlationId: 'corr-resolution-malformed' },
    );
    expect(malformed.status).toBe(404);
  });

  liveAppHostTest('ambiguous resolution returns MultipleCandidates and never silently attaches (E1 / R10)', async ({ apiRequest, authToken, tenantContext }) => {
    const { status, body } = await apiRequest<{ result: string; candidates: unknown[] }>({
      method: 'GET',
      path: '/api/v1/projects/resolution/from-conversation',
      params: { conversationId: 'conv-ambiguous' },
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    expect(status).toBe(200);
    expect(body.result).toBe('MultipleCandidates');
    // NFR-9: ambiguity asks for confirmation; nothing is attached automatically.
    expect(JSON.stringify(body)).not.toContain('attached');
    expect(body.candidates.length).toBeGreaterThan(1);
  });

  liveAppHostTest('confirming a candidate accepts only explicit MultipleCandidates evidence (FR-14 / AC2,3,4)', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const sourceProjectId = 'project-source-001';
    const { status, body } = await confirmProjectResolution(
      apiRequest,
      tenantContext.tenantId,
      {
        projectId: seededProject.projectId,
        conversationId: 'conv-ambiguous',
        candidateProjectIds: [seededProject.projectId, sourceProjectId],
        sourceProjectId,
      },
      {
        authToken,
        correlationId: 'corr-resolution-confirm',
        taskId: 'task-resolution-confirm',
        idempotencyKey: 'idem-resolution-confirm',
      },
    );

    expect(status).toBe(202);
    expect(body.correlationId).toBeTruthy();
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('confirmation mutation requires Idempotency-Key and rejects non-ambiguous evidence (FR-14 / AC3,7)', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const path = `/api/v1/projects/${seededProject.projectId}/conversations/conv-ambiguous/resolution/confirm`;
    const body = {
      requestSchemaVersion: 'v1',
      operation: 'confirm',
      projectId: seededProject.projectId,
      conversationId: 'conv-ambiguous',
      resolutionResult: 'MultipleCandidates',
      confirmed: true,
      candidateProjectIds: [seededProject.projectId, 'project-source-001'],
    };

    const missingIdempotency = await apiRequest({
      method: 'POST',
      path,
      headers: { ...queryHeaders({ authToken, correlationId: 'corr-confirm-missing-idem' }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body,
      retryConfig: { maxRetries: 0 },
    });
    expect(missingIdempotency.status).toBe(400);

    const notAmbiguous = await apiRequest({
      method: 'POST',
      path,
      headers: {
        ...mutationHeaders({ authToken, correlationId: 'corr-confirm-single', idempotencyKey: 'idem-confirm-single' }),
        'X-Hexalith-Tenant-Id': tenantContext.tenantId,
      },
      body: { ...body, resolutionResult: 'SingleCandidate' },
      retryConfig: { maxRetries: 0 },
    });
    expect(notAmbiguous.status).toBe(400);
  });

  liveAppHostTest('archived projects are excluded from resolution unless explicitly requested (E1)', async ({ apiRequest, authToken, tenantContext }) => {
    const { body } = await apiRequest<{ candidates: Array<{ lifecycle: string }> }>({
      method: 'GET',
      path: '/api/v1/projects/resolution/from-conversation',
      params: { conversationId: 'conv-1', includeArchived: false },
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    expect(body.candidates.every((c) => c.lifecycle !== 'archived')).toBe(true);
  });
});
