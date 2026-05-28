import { test, expect } from '../support/merged-fixtures.js';
import { createMinimalProjectInput, createForbiddenSetupInput } from '../support/factories/project-factory.js';
import { createProject, getProject, listProjects } from '../support/helpers/projects-api-client.js';
import { waitForProject } from '../support/helpers/readiness.js';

/**
 * F5 critical journey — workspace lifecycle (FR-1/2/5/19; B1/B5/B6/B7).
 *
 * `test.fixme` until the Hexalith.Projects API + AppHost exist (Epic 1). The bodies are
 * pattern-complete and document the load-bearing E2E disciplines: command-async (202),
 * no read-after-write (poll the read model — no sleeps), safe-denial (404 for both),
 * and network-first interception for the web console.
 */
test.describe('Projects lifecycle', () => {
  test.fixme('creates a project (202) and converges in the read model as Active', async ({ apiRequest, authToken, recurse, tenantContext }) => {
    // Create project with name only (FR-1).
    const input = createMinimalProjectInput();
    const { status, body } = await createProject(apiRequest, tenantContext.tenantId, input, { authToken });
    expect(status).toBe(202); // AcceptedCommand — command-async

    // No read-after-write guarantee: poll until the projection converges (TC-3, no sleeps).
    const project = await waitForProject(
      recurse,
      apiRequest,
      tenantContext.tenantId,
      body.projectId,
      { authToken },
      { lifecycle: 'active' },
    );
    expect(project.name).toBe(input.name);
  });

  test.fixme('rejects forbidden setup naming the field without echoing the value (FR-19)', async ({ apiRequest, authToken, tenantContext }) => {
    const input = createForbiddenSetupInput();
    const { status, body } = await createProject(apiRequest, tenantContext.tenantId, input, { authToken });
    // Rejection is a domain outcome (ProblemDetails), not an exception path.
    expect(status).toBeGreaterThanOrEqual(400);
    // NoPayloadLeakage: the rejected secret must never be echoed back.
    expect(JSON.stringify(body)).not.toContain('AKIAFAKEFAKEFAKE12345');
  });

  test.fixme('safe-denial: unauthorized == nonexistent → 404 (AR-16 / R13)', async ({ apiRequest, authToken, tenantContext }) => {
    const { status } = await getProject(apiRequest, tenantContext.tenantId, 'non-existent-or-forbidden-id', { authToken });
    expect(status).toBe(404);
  });

  test.fixme("lists only the requesting tenant's active projects (FR-5)", async ({ apiRequest, authToken, seededProject, tenantContext }) => {
    const { status, body } = await listProjects(apiRequest, tenantContext.tenantId, { authToken }, 'active');
    expect(status).toBe(200);
    expect(body.some((p) => p.projectId === seededProject.projectId)).toBe(true);
    // Tenant-scoped + authorization-filtered (R1).
    expect(body.every((p) => p.tenantId === tenantContext.tenantId)).toBe(true);
  });

  test.fixme('opens a project in the console and renders metadata via data-testid', async ({ page, interceptNetworkCall, seededProject }) => {
    // Network-first: intercept BEFORE navigating (race-free).
    const detailCall = interceptNetworkCall({ url: `**/api/v1/projects/${seededProject.projectId}` });
    await page.goto(`/projects/${seededProject.projectId}`);
    const { status } = await detailCall;
    expect(status).toBe(200);

    await expect(page.getByTestId('project-detail-name')).toHaveText(seededProject.name);
  });
});
