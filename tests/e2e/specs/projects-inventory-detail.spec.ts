import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { getProject, listProjects } from '../support/helpers/projects-api-client.js';
import { openProjectDetailSection, ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

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
 * These run only when the authenticated Projects AppHost/UI lane is explicitly enabled.
 * The default no-AppHost lane never resolves their live fixtures.
 */
test.describe('Project inventory and detail views (Story 5.4)', () => {
  liveAppHostTest('lists metadata-only project inventory rows with eventual freshness and no tenantId on the wire', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    seededProject,
  }) => {
    const { status, body } = await listProjects(
      apiRequest,
      tenantContext.tenantId,
      {
        authToken,
        correlationId: requestIdentity('inventory-list').correlationId,
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

  liveAppHostTest('rejects inventory query idempotency and non-eventual freshness without echoing row metadata', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    seededProject,
  }) => {
    const idempotencyRejected = await listProjects(apiRequest, tenantContext.tenantId, {
      authToken,
      correlationId: requestIdentity('inventory-list-idempotency').correlationId,
      extraHeaders: { 'Idempotency-Key': requestIdentity('inventory-list-idempotency').idempotencyKey },
    });
    expect(idempotencyRejected.status).toBe(400);
    expect(JSON.stringify(idempotencyRejected.body)).not.toContain(seededProject.projectId);
    expect(JSON.stringify(idempotencyRejected.body)).not.toContain(seededProject.name);

    const freshnessRejected = await listProjects(apiRequest, tenantContext.tenantId, {
      authToken,
      correlationId: requestIdentity('inventory-list-freshness').correlationId,
      freshness: 'strong',
    });
    expect(freshnessRejected.status).toBe(400);
    expect(JSON.stringify(freshnessRejected.body)).not.toContain(seededProject.projectId);
    expect(JSON.stringify(freshnessRejected.body)).not.toContain(seededProject.name);
  });

  liveAppHostTest('loads project detail through query semantics and safe failure mapping', async ({
    apiRequest,
    authToken,
    requestIdentity,
    tenantContext,
    seededProject,
  }) => {
    const detail = await getProject(apiRequest, tenantContext.tenantId, seededProject.projectId, {
      authToken,
      correlationId: requestIdentity('inventory-detail').correlationId,
      freshness: 'eventually_consistent',
    });

    expect(detail.status).toBe(200);
    expect(detail.body.projectId).toBe(seededProject.projectId);
    expect(detail.body.lifecycleState).toBe('active');

    const idempotencyRejected = await getProject(apiRequest, tenantContext.tenantId, seededProject.projectId, {
      authToken,
      correlationId: requestIdentity('inventory-detail-idempotency').correlationId,
      extraHeaders: { 'Idempotency-Key': requestIdentity('inventory-detail-idempotency').idempotencyKey },
    });
    expect(idempotencyRejected.status).toBe(400);
    expect(JSON.stringify(idempotencyRejected.body)).not.toContain(seededProject.name);

    const deniedOrMissing = await getProject(apiRequest, tenantContext.tenantId, 'not/a/canonical/project-id', {
      authToken,
      correlationId: requestIdentity('inventory-detail-safe-denial').correlationId,
    });
    expect(deniedOrMissing.status).toBe(404);
    expect(JSON.stringify(deniedOrMissing.body)).not.toContain(seededProject.projectId);
  });

  liveAppHostTest('renders inventory filters, warnings filters, and row-to-detail navigation selectors', async ({
    page,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.gotoInventory();

    await expect(detail.inventoryLifecycleFilter).toBeVisible();
    await expect(detail.inventoryUpdatedFilter).toBeVisible();
    await expect(detail.inventoryWarningFilter).toBeVisible();
    await expect(detail.inventoryReasonCodeFilter).toBeVisible();
    await expect(detail.inventoryReferenceTypeFilter).toBeVisible();
    await expect(detail.inventoryRows.filter({ hasText: seededProject.projectId })).toBeVisible();

    await page.getByTestId('project-inventory-row-link').filter({ hasText: seededProject.projectId }).click();
    await expect(detail.inspector).toBeVisible();
    await expect(detail.metadataSection).toContainText(seededProject.projectId);
  });

  liveAppHostTest('renders read-only detail sections without future-story payload surfaces', async ({
    page,
    seededProject,
  }) => {
    const detail = new ProjectDetailPage(page);
    await detail.goto(seededProject.projectId);

    await expect(detail.diagnosticHeader).toBeVisible();
    await expect(detail.inspector).toBeVisible();
    await expect(detail.metadataSection).toContainText(seededProject.projectId);

    await openProjectDetailSection(page, 'setup');
    await expect(detail.setupSection).toBeVisible();
    await openProjectDetailSection(page, 'references');
    await expect(detail.referencesSection).toBeVisible();
    await openProjectDetailSection(page, 'resolution');
    await expect(detail.resolutionTraceWorkbench).toBeVisible();
    await expect(detail.resolutionTraceFeedback).toContainText('No trace has been run yet');
    await openProjectDetailSection(page, 'audit');
    await expect(detail.auditSection).toBeVisible();
    await openProjectDetailSection(page, 'actions');
    await expect(detail.maintenancePanel).toBeVisible();
    await expect(detail.maintenancePanel.getByRole('heading', { name: 'Maintenance actions' })).toBeVisible();

    const bodyText = await page.locator('body').innerText();
    for (const marker of FORBIDDEN_INVENTORY_MARKERS) {
      expect(bodyText).not.toContain(marker);
    }
  });
});
