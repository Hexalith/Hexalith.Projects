import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import {
  getProjectContextExplanation,
  getProjectOperatorDiagnostics,
  listProjectConversations,
} from '../support/helpers/projects-api-client.js';
import { ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

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
 * These run only in the explicit live lane; linked conversation/folder/file/memory
 * references with health outcomes remain a required fixture prerequisite. The assertions bind
 * the Story 5.5 contract: metadata-only API inputs, shared context-evaluation sources,
 * explicit matrix columns, visible non-color-only states, and read-only safe actions.
 */
test.describe('Project reference health matrix (Story 5.5)', () => {
  liveAppHostTest('loads reference-health source reads with eventual freshness and no payload leakage', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const diagnostics = await getProjectOperatorDiagnostics(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-story-5-5-operator-diagnostics',
        auditLimit: 25,
        freshness: 'eventually_consistent',
      },
    );
    const explanation = await getProjectContextExplanation(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-story-5-5-context-explain',
        freshness: 'eventually_consistent',
      },
    );
    const conversations = await listProjectConversations(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-story-5-5-conversations',
        freshness: 'eventually_consistent',
        pageSize: 100,
      },
    );

    expect(diagnostics.status).toBe(200);
    expect(explanation.status).toBe(200);
    expect(conversations.status).toBe(200);
    expect(diagnostics.body.projectId).toBe(seededProject.projectId);
    expect(explanation.body.context.projectId).toBe(seededProject.projectId);
    expect(conversations.body.projectId).toBe(seededProject.projectId);
    expect(Array.isArray(diagnostics.body.references)).toBe(true);
    expect(Array.isArray(explanation.body.evaluations)).toBe(true);
    expect(Array.isArray(conversations.body.items)).toBe(true);

    for (const evaluation of explanation.body.evaluations) {
      expect(['conversation', 'folder', 'file', 'memory']).toContain(evaluation.referenceKind);
      expect(evaluation.resultState).toBeTruthy();
      expect(evaluation.observedAt).toBeTruthy();
    }

    for (const conversation of conversations.body.items) {
      expect(conversation.projectId).toBe(seededProject.projectId);
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
    tenantContext,
    seededProject,
  }) => {
    const explanationWithIdempotency = await getProjectContextExplanation(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-story-5-5-explain-idempotency',
        extraHeaders: { 'Idempotency-Key': 'context-explain-is-a-query' },
      },
    );
    expect(explanationWithIdempotency.status).toBe(400);
    expect(JSON.stringify(explanationWithIdempotency.body)).not.toContain(seededProject.name);

    const conversationsWithIdempotency = await listProjectConversations(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-story-5-5-conversations-idempotency',
        extraHeaders: { 'Idempotency-Key': 'conversation-list-is-a-query' },
      },
    );
    expect(conversationsWithIdempotency.status).toBe(400);
    expect(JSON.stringify(conversationsWithIdempotency.body)).not.toContain(seededProject.name);

    const explanationWithStrongFreshness = await getProjectContextExplanation(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-story-5-5-explain-freshness',
        freshness: 'strong',
      },
    );
    expect(explanationWithStrongFreshness.status).toBe(400);

    const conversationsWithStrongFreshness = await listProjectConversations(
      apiRequest,
      tenantContext.tenantId,
      seededProject.projectId,
      {
        authToken,
        correlationId: 'corr-story-5-5-conversations-freshness',
        freshness: 'strong',
      },
    );
    expect(conversationsWithStrongFreshness.status).toBe(400);
  });

  liveAppHostTest('renders the full Reference Health Matrix with explicit headers and row selectors', async ({
    page,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);
    await page.getByTestId('project-detail-tab-references').click();

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

  liveAppHostTest('surfaces failure states as visible text and keeps safe actions read-only', async ({
    page,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);
    await page.getByTestId('project-detail-tab-references').click();

    await expect(detail.referenceHealthMatrix).toContainText(/Unauthorized|Stale|Unavailable|Archived|Conflict|Invalid reference/);
    await expect(detail.referenceHealthMatrix).toContainText(
      /ReferenceAuthorization|ReferenceFreshness|referenceUnauthorized|referenceStale|referenceUnavailable|referenceArchived|invalidReference/,
    );
    await expect(detail.referenceSafeActionCells.first()).toContainText(/Inspect|Copy ID|Story 5.9/);

    const inspectAction = detail.referenceSafeActionCells.first().getByRole('button', { name: 'Inspect' });
    const copyAction = detail.referenceSafeActionCells.first().getByRole('button', { name: 'Copy ID' });
    await expect(inspectAction).toHaveAttribute('aria-disabled', 'true');
    await expect(copyAction).toHaveAttribute('aria-disabled', 'true');

    const bodyText = await page.locator('body').innerText();
    expectNoReferencePayloadLeakage(bodyText, '');
  });
});
