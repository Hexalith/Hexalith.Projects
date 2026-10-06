import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { queryHeaders, mutationHeaders } from '../support/helpers/correlation.js';
import { confirmProjectResolution, resolveProjectFromAttachments } from '../support/helpers/projects-api-client.js';

/**
 * F5 critical journey — resolution → confirm (FR-12/13/14; E1 / R10).
 *
 * Live-only: the two resolution Projects and their folder/file/conversation evidence come from the
 * attempt-scoped fixture graph, and every request identity is derived from the same attempt. The
 * assertions cover binary outcomes, reason codes, safe-denial, query validation, and the
 * never-silently-attach guarantee.
 */
test.describe('Projects resolution', () => {
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
    requestIdentity,
    tenantContext,
    resolutionProjects,
    liveFixtureGraph,
  }) => {
    const { status, body } = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [liveFixtureGraph.folderId] },
      { authToken, correlationId: requestIdentity('resolution-folder').correlationId },
    );

    expect(status).toBe(200);
    expect(body.result).toBe('SingleCandidate');
    expect(body.candidates).toContainEqual(
      expect.objectContaining({
        projectId: resolutionProjects.primary.projectId,
        reasonCodes: expect.arrayContaining(['ProjectFolderMatched']),
      }),
    );
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('file attachment resolves with FileReferenceMatched and does not read raw content (FR-13 / AC1,2)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    resolutionProjects,
    liveFixtureGraph,
  }) => {
    const { status, body } = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { fileIds: [liveFixtureGraph.fileReferenceId] },
      { authToken, correlationId: requestIdentity('resolution-file').correlationId },
    );

    expect(status).toBe(200);
    expect(body.result).toBe('SingleCandidate');
    expect(body.candidates).toContainEqual(
      expect.objectContaining({
        projectId: resolutionProjects.secondary.projectId,
        reasonCodes: expect.arrayContaining(['FileReferenceMatched']),
      }),
    );
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('folder and file attachments can produce multiple candidates and never auto-attach (FR-13 / NFR-9)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    liveFixtureGraph,
    resolutionProjects,
  }) => {
    const { status, body } = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [liveFixtureGraph.folderId], fileIds: [liveFixtureGraph.fileReferenceId] },
      { authToken, correlationId: requestIdentity('resolution-multiple').correlationId },
    );

    expect(status).toBe(200);
    expect(body.result).toBe('MultipleCandidates');
    expect(body.candidates.map((candidate) => candidate.projectId)).toEqual(
      expect.arrayContaining([resolutionProjects.primary.projectId, resolutionProjects.secondary.projectId]),
    );
    expect(JSON.stringify(body)).not.toContain('attached');
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('attachment query rejects Idempotency-Key and strong freshness as validation errors (AC5)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    liveFixtureGraph,
  }) => {
    const idempotencyRejected = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [liveFixtureGraph.folderId] },
      {
        authToken,
        correlationId: requestIdentity('resolution-query-idempotency').correlationId,
        extraHeaders: { 'Idempotency-Key': requestIdentity('resolution-query-idempotency').idempotencyKey },
      },
    );
    expect(idempotencyRejected.status).toBe(400);

    const freshnessRejected = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { folderIds: [liveFixtureGraph.folderId] },
      {
        authToken,
        correlationId: requestIdentity('resolution-query-freshness').correlationId,
        freshness: 'strong',
      },
    );
    expect(freshnessRejected.status).toBe(400);
  });

  liveAppHostTest('missing or malformed attachment identifiers collapse to safe-denial 404 (AC6)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
  }) => {
    const missing = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      {},
      { authToken, correlationId: requestIdentity('resolution-missing').correlationId },
    );
    expect(missing.status).toBe(404);

    const malformed = await resolveProjectFromAttachments(
      apiRequest,
      tenantContext.tenantId,
      { fileIds: ['bad/slash'] },
      { authToken, correlationId: requestIdentity('resolution-malformed').correlationId },
    );
    expect(malformed.status).toBe(404);
  });

  liveAppHostTest('ambiguous resolution returns MultipleCandidates and never silently attaches (E1 / R10)', async ({ apiRequest, authToken, tenantContext, liveFixtureGraph, resolutionProjects }) => {
    const { status, body } = await apiRequest<{ result: string; candidates: unknown[] }>({
      method: 'GET',
      path: '/api/v1/projects/resolution/from-conversation',
      params: { conversationId: liveFixtureGraph.ambiguousConversationId },
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    expect(status).toBe(200);
    expect(body.result).toBe('MultipleCandidates');
    // NFR-9: ambiguity asks for confirmation; nothing is attached automatically.
    expect(JSON.stringify(body)).not.toContain('attached');
    expect(body.candidates.length).toBeGreaterThan(1);
    expect(JSON.stringify(body.candidates)).toContain(resolutionProjects.primary.projectId);
    expect(JSON.stringify(body.candidates)).toContain(resolutionProjects.secondary.projectId);
  });

  liveAppHostTest('confirming a candidate accepts only explicit MultipleCandidates evidence (FR-14 / AC2,3,4)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    resolutionProjects,
    liveFixtureGraph,
  }) => {
    const { status, body } = await confirmProjectResolution(
      apiRequest,
      tenantContext.tenantId,
      {
        projectId: resolutionProjects.primary.projectId,
        conversationId: liveFixtureGraph.ambiguousConversationId,
        candidateProjectIds: [resolutionProjects.primary.projectId, resolutionProjects.secondary.projectId],
      },
      { authToken, ...requestIdentity('resolution-confirm') },
    );

    expect(status).toBe(202);
    expect(body.correlationId).toBeTruthy();
    assertNoResolutionPayloadLeakage(JSON.stringify(body), tenantContext.tenantId);
  });

  liveAppHostTest('confirmation mutation requires Idempotency-Key and rejects non-ambiguous evidence (FR-14 / AC3,7)', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    resolutionProjects,
    liveFixtureGraph,
  }) => {
    const path = `/api/v1/projects/${resolutionProjects.primary.projectId}/conversations/${liveFixtureGraph.ambiguousConversationId}/resolution/confirm`;
    const body = {
      requestSchemaVersion: 'v1',
      operation: 'confirm',
      projectId: resolutionProjects.primary.projectId,
      conversationId: liveFixtureGraph.ambiguousConversationId,
      resolutionResult: 'MultipleCandidates',
      confirmed: true,
      candidateProjectIds: [resolutionProjects.primary.projectId, resolutionProjects.secondary.projectId],
    };

    const missingIdempotency = await apiRequest({
      method: 'POST',
      path,
      headers: { ...queryHeaders({ authToken, correlationId: requestIdentity('resolution-confirm-missing-idempotency').correlationId }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body,
      retryConfig: { maxRetries: 0 },
    });
    expect(missingIdempotency.status).toBe(400);

    const notAmbiguous = await apiRequest({
      method: 'POST',
      path,
      headers: {
        ...mutationHeaders({ authToken, ...requestIdentity('resolution-confirm-single-candidate') }),
        'X-Hexalith-Tenant-Id': tenantContext.tenantId,
      },
      body: { ...body, resolutionResult: 'SingleCandidate' },
      retryConfig: { maxRetries: 0 },
    });
    expect(notAmbiguous.status).toBe(400);
  });

  liveAppHostTest('archived projects are excluded from resolution unless explicitly requested (E1)', async ({ apiRequest, authToken, tenantContext, liveFixtureGraph }) => {
    const { body } = await apiRequest<{ candidates: Array<{ lifecycle: string }> }>({
      method: 'GET',
      path: '/api/v1/projects/resolution/from-conversation',
      params: { conversationId: liveFixtureGraph.conversationId, includeArchived: false },
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    expect(body.candidates.every((c) => c.lifecycle !== 'archived')).toBe(true);
  });
});
