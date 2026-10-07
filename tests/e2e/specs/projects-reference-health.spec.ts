import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import {
  archiveProject,
  getProjectContextExplanation,
  getProjectOperatorDiagnostics,
  listProjectConversations,
} from '../support/helpers/projects-api-client.js';
import { createTrackedProject } from '../support/fixtures/projects-fixtures.js';
import { waitForProject } from '../support/helpers/readiness.js';
import { openProjectDetailSection, ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

const FORBIDDEN_REFERENCE_HEALTH_MARKERS = [
  'tenantId',
  'idempotencyKey',
  'transcript',
  'raw prompt',
  'file content',
  'memory payload',
  'candidate',
  'score',
  'rank',
  'rejected',
  'proposal body',
  'command body',
  'ProblemDetails',
  'BEGIN PRIVATE KEY',
  'Authorization: Bearer',
  'secret token',
];

function expectNoReferencePayloadLeakage(serialized: string, tenantId: string): void {
  if (tenantId) {
    expect(serialized).not.toContain(tenantId);
  }
  for (const marker of FORBIDDEN_REFERENCE_HEALTH_MARKERS) {
    expect(serialized).not.toContain(marker);
  }
}

/**
 * Story 5.5 critical journeys - Reference Inventory & Health View.
 *
 * These run only in the explicit live lane; the linked conversation/folder/file/memory references
 * come from the attempt-scoped `referencedProject` fixture graph, degraded Unauthorized/Stale/
 * Unavailable rows come from the graph's degraded Project, and Archived rows from archiving the
 * referenced Project. Conflict is not producible live: Project read models never store a Conflict
 * reference state (it exists only on rejected commands). The assertions bind the Story 5.5 contract: metadata-only API inputs, shared context-evaluation sources,
 * explicit matrix columns, visible non-color-only states, and read-only safe actions.
 */
test.describe('Project reference health matrix (Story 5.5)', () => {
  liveAppHostTest('loads reference-health source reads with eventual freshness and no payload leakage', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    referencedProject,
  }) => {
    const diagnostics = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      referencedProject.projectId,
      {
        authToken,
        correlationId: requestIdentity('reference-health-operator-diagnostics').correlationId,
        auditLimit: 25,
        freshness: 'eventually_consistent',
      },
    );
    const explanation = await getProjectContextExplanation(
      apiRequest,
      tenantContext.tenantId,
      referencedProject.projectId,
      {
        authToken,
        correlationId: requestIdentity('reference-health-context-explain').correlationId,
        freshness: 'eventually_consistent',
      },
    );
    const conversations = await listProjectConversations(
      apiRequest,
      tenantContext.tenantId,
      referencedProject.projectId,
      {
        authToken,
        correlationId: requestIdentity('reference-health-conversations').correlationId,
        freshness: 'eventually_consistent',
        pageSize: 100,
      },
    );

    expect(diagnostics.status).toBe(200);
    expect(explanation.status).toBe(200);
    expect(conversations.status).toBe(200);
    expect(diagnostics.body.projectId).toBe(referencedProject.projectId);
    expect(explanation.body.context.projectId).toBe(referencedProject.projectId);
    expect(conversations.body.projectId).toBe(referencedProject.projectId);
    expect(Array.isArray(diagnostics.body.references)).toBe(true);
    expect(Array.isArray(explanation.body.evaluations)).toBe(true);
    expect(Array.isArray(conversations.body.items)).toBe(true);

    for (const evaluation of explanation.body.evaluations) {
      expect(['conversation', 'folder', 'file', 'memory']).toContain(evaluation.referenceKind);
      expect(evaluation.resultState).toBeTruthy();
      expect(evaluation.observedAt).toBeTruthy();
    }

    for (const conversation of conversations.body.items) {
      expect(conversation.projectId).toBe(referencedProject.projectId);
      expect(conversation.conversationId).toBeTruthy();
      expect(conversation.trustSignal).toBeTruthy();
    }

    expectNoReferencePayloadLeakage(JSON.stringify(diagnostics.body), tenantContext.tenantId);
    expectNoReferencePayloadLeakage(JSON.stringify(explanation.body), tenantContext.tenantId);
    expectNoReferencePayloadLeakage(JSON.stringify(conversations.body), tenantContext.tenantId);
  });

  liveAppHostTest('rejects reference-health query idempotency and non-eventual freshness safely', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    referencedProject,
  }) => {
    const explanationWithIdempotency = await getProjectContextExplanation(
      apiRequest,
      tenantContext.tenantId,
      referencedProject.projectId,
      {
        authToken,
        correlationId: requestIdentity('reference-health-explain-idempotency').correlationId,
        extraHeaders: { 'Idempotency-Key': requestIdentity('reference-health-explain-idempotency').idempotencyKey },
      },
    );
    expect(explanationWithIdempotency.status).toBe(400);
    expect(JSON.stringify(explanationWithIdempotency.body)).not.toContain(referencedProject.name);

    const conversationsWithIdempotency = await listProjectConversations(
      apiRequest,
      tenantContext.tenantId,
      referencedProject.projectId,
      {
        authToken,
        correlationId: requestIdentity('reference-health-conversations-idempotency').correlationId,
        extraHeaders: { 'Idempotency-Key': requestIdentity('reference-health-conversations-idempotency').idempotencyKey },
      },
    );
    expect(conversationsWithIdempotency.status).toBe(400);
    expect(JSON.stringify(conversationsWithIdempotency.body)).not.toContain(referencedProject.name);

    const explanationWithStrongFreshness = await getProjectContextExplanation(
      apiRequest,
      tenantContext.tenantId,
      referencedProject.projectId,
      {
        authToken,
        correlationId: requestIdentity('reference-health-explain-freshness').correlationId,
        freshness: 'strong',
      },
    );
    expect(explanationWithStrongFreshness.status).toBe(400);

    const conversationsWithStrongFreshness = await listProjectConversations(
      apiRequest,
      tenantContext.tenantId,
      referencedProject.projectId,
      {
        authToken,
        correlationId: requestIdentity('reference-health-conversations-freshness').correlationId,
        freshness: 'strong',
      },
    );
    expect(conversationsWithStrongFreshness.status).toBe(400);
  });

  liveAppHostTest('renders the full Reference Health Matrix with explicit headers and row selectors', async ({
    page,
    referencedProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(referencedProject.projectId);
    await openProjectDetailSection(page, 'references');

    await expect(detail.referencesSection).toBeVisible();
    await expect(detail.referenceHealthMatrix).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Reference type' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Reference ID' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Owner' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Inclusion state' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Health state' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Reason code' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Diagnostic' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Last checked' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Freshness' })).toBeVisible();
    await expect(detail.referenceHealthMatrix.getByRole('columnheader', { name: 'Safe actions' })).toBeVisible();
    await expect(detail.referenceHealthRows.first()).toBeVisible();
    await expect(detail.referenceKindCells.first()).toHaveText(/conversation|folder|file|memory/);
    await expect(detail.referenceOwnerCells.first()).toHaveText(/Conversations|Folders|Projects|Memories/);
    await expect(detail.referenceStateCells.first()).not.toHaveText('');
    await expect(detail.referenceReasonCells.first()).not.toHaveText('');
    await expect(detail.referenceLastCheckedCells.first()).toContainText(/\d{4}-\d{2}-\d{2}/);
  });

  liveAppHostTest('surfaces fixture health states as visible text and keeps safe actions read-only', async ({
    page,
    referencedProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(referencedProject.projectId);
    await openProjectDetailSection(page, 'references');

    await expect(detail.referenceHealthMatrix).toContainText(/conversation|folder|file|memory/);
    await expect(detail.referenceStateCells.first()).not.toHaveText('');
    await expect(detail.referenceReasonCells.first()).not.toHaveText('');
    await expect(detail.referenceSafeActionCells.first()).toContainText(/Inspect|Copy ID|Story 5.9/);

    const inspectAction = detail.referenceSafeActionCells.first().getByRole('button', { name: 'Inspect' });
    const copyAction = detail.referenceSafeActionCells.first().getByRole('button', { name: 'Copy ID' });
    await expect(inspectAction).toHaveAttribute('aria-disabled', 'true');
    await expect(copyAction).toHaveAttribute('aria-disabled', 'true');

    const bodyText = await page.locator('body').innerText();
    expectNoReferencePayloadLeakage(bodyText, '');
  });
  liveAppHostTest('renders degraded sibling trust as Unauthorized, Stale, and Unavailable rows', async ({
    page,
    apiRequest,
    authToken,
    recurse,
    tenantContext,
    liveCleanup,
    liveFixtureGraph,
  }) => {
    const degraded = await createTrackedProject(
      { apiRequest, authToken, recurse, tenantContext, graph: liveFixtureGraph },
      liveCleanup,
      liveFixtureGraph.degradedProjectId,
      'reference-health-degraded',
    );
    const detail = new ProjectDetailPage(page);
    await detail.goto(degraded.projectId);
    await openProjectDetailSection(page, 'references');

    // The degraded Project's three fixture conversations report Forbidden, Stale, and Unavailable trust.
    // Each conversation renders once from the Conversations-backed list and once from context evaluation,
    // so every matching row must carry the degraded state.
    for (const [conversationId, state] of [
      [liveFixtureGraph.forbiddenConversationId, 'Unauthorized'],
      [liveFixtureGraph.staleConversationId, 'Stale'],
      [liveFixtureGraph.unavailableConversationId, 'Unavailable'],
    ] as const) {
      const rows = detail.referenceHealthRows.filter({ hasText: conversationId });
      await expect(rows.first()).toBeVisible();
      const count = await rows.count();
      for (let index = 0; index < count; index++) {
        const row = rows.nth(index);
        await expect(row.getByTestId('project-reference-state')).toContainText(state);
        await expect(row.getByTestId('project-reference-kind')).toHaveText('conversation');
        await expect(row.getByTestId('project-reference-reason')).not.toHaveText('');
        await expect(row.getByRole('button', { name: 'Inspect' })).toHaveAttribute('aria-disabled', 'true');
      }
    }

    expectNoReferencePayloadLeakage(await page.locator('body').innerText(), '');
  });

  liveAppHostTest('renders Archived reference evaluations once the referenced Project is archived', async ({
    page,
    apiRequest,
    authToken,
    recurse,
    requestIdentity,
    tenantContext,
    referencedProject,
    liveFixtureGraph,
  }) => {
    const archived = await archiveProject(apiRequest, tenantContext.tenantId, referencedProject.projectId, {
      authToken,
      ...requestIdentity('reference-health-archive'),
    });
    expect(archived.status).toBe(202);
    await waitForProject(recurse, apiRequest, tenantContext.tenantId, referencedProject.projectId, { authToken }, { lifecycle: 'archived' });

    const detail = new ProjectDetailPage(page);
    await detail.goto(referencedProject.projectId);
    await openProjectDetailSection(page, 'references');

    // An archived Project's folder, file, and memory references are excluded by the lifecycle check.
    for (const referenceId of [liveFixtureGraph.folderId, liveFixtureGraph.fileReferenceId, liveFixtureGraph.memoryReferenceId]) {
      await expect(detail.referenceHealthRows.filter({ hasText: referenceId }).getByTestId('project-reference-state')).toContainText('Archived');
    }
  });
});
