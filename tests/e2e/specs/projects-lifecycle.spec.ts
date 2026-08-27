import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { createMinimalProjectInput, createForbiddenSetupInput } from '../support/factories/project-factory.js';
import { archiveProject, createProject, getProject, listProjects } from '../support/helpers/projects-api-client.js';
import { waitForProject } from '../support/helpers/readiness.js';

/**
 * F5 critical journey — workspace lifecycle (FR-1/2/5/19; B1/B5/B6/B7).
 *
 * Live-gated behind `E2E_LIVE_APPHOST=1`. The bodies are
 * pattern-complete and document the load-bearing E2E disciplines: command-async (202),
 * no read-after-write (poll the read model — no sleeps), safe-denial (404 for both),
 * and rendered browser assertions over the server-side UI gateway.
 */
test.describe('Projects lifecycle', () => {
  liveAppHostTest('creates a project (202) and converges in the read model as Active', async ({ apiRequest, authToken, recurse, tenantContext, liveFixtureGraph }) => {
    // Create project with name only (FR-1).
    const input = createMinimalProjectInput({ projectId: liveFixtureGraph.projectId });
    const { status } = await createProject(apiRequest, tenantContext.tenantId, input, { authToken });
    expect(status).toBe(202); // AcceptedCommand — command-async

    // No read-after-write guarantee: poll until the projection converges (TC-3, no sleeps).
    const project = await waitForProject(
      recurse,
      apiRequest,
      tenantContext.tenantId,
      liveFixtureGraph.projectId,
      { authToken },
      { lifecycle: 'active' },
    );
    expect(project.name).toBe(input.name);
    expect((await archiveProject(apiRequest, tenantContext.tenantId, project.projectId, { authToken })).status).toBe(202);
    await waitForProject(recurse, apiRequest, tenantContext.tenantId, project.projectId, { authToken }, { lifecycle: 'archived' });
  });

  liveAppHostTest('rejects forbidden setup naming the field without echoing the value (FR-19)', async ({ apiRequest, authToken, tenantContext, liveFixtureGraph }) => {
    const input = createForbiddenSetupInput({ projectId: liveFixtureGraph.projectId });
    const { status, body } = await createProject(apiRequest, tenantContext.tenantId, input, { authToken });
    // Rejection is a domain outcome (ProblemDetails), not an exception path.
    expect(status).toBeGreaterThanOrEqual(400);
    // NoPayloadLeakage: the rejected secret must never be echoed back.
    expect(JSON.stringify(body)).not.toContain('AKIAFAKEFAKEFAKE12345');
  });

  liveAppHostTest('safe-denial: unauthorized == nonexistent → 404 (AR-16 / R13)', async ({ apiRequest, authToken, tenantContext }) => {
    const { status } = await getProject(apiRequest, tenantContext.tenantId, 'non-existent-or-forbidden-id', { authToken });
    expect(status).toBe(404);
  });

  liveAppHostTest("lists only the requesting tenant's active projects (FR-5)", async ({ apiRequest, authToken, seededProject, tenantContext }) => {
    const { status, body } = await listProjects(apiRequest, tenantContext.tenantId, { authToken }, 'active');
    expect(status).toBe(200);
    expect(body.items.some((p) => p.projectId === seededProject.projectId)).toBe(true);
    expect(body.items.every((p) => p.lifecycleState === 'active')).toBe(true);
    // Tenant-scoped + authorization-filtered (R1), without echoing tenantId on list rows.
    expect(JSON.stringify(body.items)).not.toContain('tenantId');
  });

  liveAppHostTest('opens a project in the console and renders metadata via data-testid', async ({ page, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    await expect(page.getByTestId('project-detail-name')).toHaveText(seededProject.name);
  });
});
