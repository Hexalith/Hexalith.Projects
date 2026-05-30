import { test, expect } from '../support/merged-fixtures.js';
import { getProject, listProjects } from '../support/helpers/projects-api-client.js';
import { ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

const FORBIDDEN_INVENTORY_MARKERS = [
  'tenantId',
  'idempotencyKey',
  'transcript',
  'raw prompt',
  'candidate',
  'score',
  'rank',
  'rejected',
  'proposal body',
  'command body',
  'BEGIN PRIVATE KEY',
  'Authorization: Bearer',
  'secret token',
];

/**
 * Story 5.4 critical journeys - project inventory and read-only detail inspector.
 *
 * These remain `test.fixme` while the authenticated Projects AppHost/UI fixture is
 * not runnable by default. The tests are intentionally complete so the AppHost
 * enablement step can unskip them without redesigning the API/UI assertions.
 */
test.describe('Project inventory and detail views (Story 5.4)', () => {
  test.fixme('lists metadata-only project inventory rows with eventual freshness and no tenantId on the wire', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const { status, body } = await listProjects(
      apiRequest,
      tenantContext.tenantId,
      {
        authToken,
        correlationId: 'corr-story-5-4-inventory-list',
        freshness: 'eventually_consistent',
      },
      'active',
    );

    expect(status).toBe(200);
    expect(body.freshness.readConsistency).toBe('eventually_consistent');

    const row = body.items.find((item) => item.projectId === seededProject.projectId);
    expect(row).toBeTruthy();
    expect(row?.name).toBe(seededProject.name);
    expect(row?.lifecycleState).toBe('active');
    expect(row?.freshness.readConsistency).toBe('eventually_consistent');

    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain(tenantContext.tenantId);
    for (const marker of FORBIDDEN_INVENTORY_MARKERS) {
      expect(serialized).not.toContain(marker);
    }
  });

  test.fixme('rejects inventory query idempotency and non-eventual freshness without echoing row metadata', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const idempotencyRejected = await listProjects(apiRequest, tenantContext.tenantId, {
      authToken,
      correlationId: 'corr-story-5-4-inventory-idempotency',
      extraHeaders: { 'Idempotency-Key': 'queries-must-not-carry-idempotency' },
    });
    expect(idempotencyRejected.status).toBe(400);
    expect(JSON.stringify(idempotencyRejected.body)).not.toContain(seededProject.projectId);
    expect(JSON.stringify(idempotencyRejected.body)).not.toContain(seededProject.name);

    const freshnessRejected = await listProjects(apiRequest, tenantContext.tenantId, {
      authToken,
      correlationId: 'corr-story-5-4-inventory-freshness',
      freshness: 'strong',
    });
    expect(freshnessRejected.status).toBe(400);
    expect(JSON.stringify(freshnessRejected.body)).not.toContain(seededProject.projectId);
    expect(JSON.stringify(freshnessRejected.body)).not.toContain(seededProject.name);
  });

  test.fixme('loads project detail through query semantics and safe failure mapping', async ({
    apiRequest,
    authToken,
    tenantContext,
    seededProject,
  }) => {
    const detail = await getProject(apiRequest, tenantContext.tenantId, seededProject.projectId, {
      authToken,
      correlationId: 'corr-story-5-4-detail',
      freshness: 'eventually_consistent',
    });

    expect(detail.status).toBe(200);
    expect(detail.body.projectId).toBe(seededProject.projectId);
    expect(detail.body.lifecycleState).toBe('active');

    const idempotencyRejected = await getProject(apiRequest, tenantContext.tenantId, seededProject.projectId, {
      authToken,
      correlationId: 'corr-story-5-4-detail-idempotency',
      extraHeaders: { 'Idempotency-Key': 'query-detail-must-not-use-idempotency' },
    });
    expect(idempotencyRejected.status).toBe(400);
    expect(JSON.stringify(idempotencyRejected.body)).not.toContain(seededProject.name);

    const deniedOrMissing = await getProject(apiRequest, tenantContext.tenantId, 'not/a/canonical/project-id', {
      authToken,
      correlationId: 'corr-story-5-4-detail-safe-denial',
    });
    expect(deniedOrMissing.status).toBe(404);
    expect(JSON.stringify(deniedOrMissing.body)).not.toContain(seededProject.projectId);
  });

  test.fixme('renders inventory filters, disabled unsupported dimensions, and row-to-detail navigation selectors', async ({
    page,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.gotoInventory();

    await expect(detail.inventoryLifecycleFilter).toBeVisible();
    await expect(detail.inventoryUpdatedFilter).toBeVisible();
    await expect(detail.inventoryWarningFilter).toBeDisabled();
    await expect(detail.inventoryReasonCodeFilter).toBeDisabled();
    await expect(detail.inventoryReferenceTypeFilter).toBeDisabled();
    await expect(detail.inventoryRows.filter({ hasText: seededProject.projectId })).toBeVisible();

    await page.getByTestId('project-inventory-row-link').filter({ hasText: seededProject.projectId }).click();
    await expect(detail.inspector).toBeVisible();
    await expect(detail.metadataSection).toContainText(seededProject.projectId);
  });

  test.fixme('renders read-only detail sections without future-story payload surfaces', async ({
    page,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);

    await expect(detail.diagnosticHeader).toBeVisible();
    await expect(detail.inspector).toBeVisible();
    await expect(detail.metadataSection).toContainText(seededProject.projectId);

    await page.getByTestId('project-detail-tab-setup').click();
    await expect(detail.setupSection).toBeVisible();
    await page.getByTestId('project-detail-tab-references').click();
    await expect(detail.referencesSection).toBeVisible();
    await page.getByTestId('project-detail-tab-resolution').click();
    await expect(detail.resolutionSection).toContainText('Story 5.6');
    await page.getByTestId('project-detail-tab-audit').click();
    await expect(detail.auditSection).toBeVisible();
    await page.getByTestId('project-detail-tab-actions').click();
    await expect(detail.actionsSection).toContainText('Read-only inspector');

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_INVENTORY_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }
  });
});
