import type { ApiRequest, ProjectDetail } from '../helpers/projects-api-client.js';
import { archiveProject, createProject } from '../helpers/projects-api-client.js';
import type { Recurse } from '../helpers/readiness.js';
import { waitForProject } from '../helpers/readiness.js';
import type { TenantContext } from '../factories/tenant-factory.js';
import { createProjectInput, type CreateProjectInput } from '../factories/project-factory.js';

/**
 * Project-domain fixture surface. The implementations live here (logic), the wiring into
 * a Playwright test object lives in `merged-fixtures.ts` (composition) — keeping the
 * dependent-fixture composition explicit.
 */
export interface ProjectFixtures {
  /** A fresh, isolated tenant context per test (drives tenant isolation). */
  tenantContext: TenantContext;
  /** An active project, seeded via API and converged in the read model; archived on teardown. */
  seededProject: ProjectDetail;
}

export interface SeedProjectDeps {
  apiRequest: ApiRequest;
  authToken: string;
  recurse: Recurse;
  tenantContext: TenantContext;
}

/**
 * Create an active project and wait for read-model convergence (command-async, no sleeps).
 * Returns the converged detail plus a `cleanup` that archives it (Projects has no hard delete).
 */
export async function seedActiveProject(
  deps: SeedProjectDeps,
  input?: CreateProjectInput,
): Promise<{ project: ProjectDetail; cleanup: () => Promise<void> }> {
  const { apiRequest, authToken, recurse, tenantContext } = deps;
  const payload = input ?? createProjectInput();

  const { status, body } = await createProject(apiRequest, tenantContext.tenantId, payload, { authToken });
  if (status !== 202 || typeof body?.projectId !== 'string' || !body.projectId.trim()) {
    throw new Error(
      `[projects-fixtures] project seed was not accepted (status ${status}); verify TEST_TENANT_ID and its projected tenant access.`,
    );
  }
  let project: ProjectDetail;
  try {
    project = await waitForProject(
      recurse,
      apiRequest,
      tenantContext.tenantId,
      body.projectId,
      { authToken },
      { lifecycle: 'active' },
    );
  } catch (error) {
    try {
      await archiveProject(apiRequest, tenantContext.tenantId, body.projectId, { authToken });
    } catch {
      // Preserve the convergence error; cleanup is best effort on this failure path.
    }
    throw error;
  }

  const cleanup = async (): Promise<void> => {
    const { status: cleanupStatus } = await archiveProject(
      apiRequest,
      tenantContext.tenantId,
      project.projectId,
      { authToken },
    );
    if (cleanupStatus !== 202) {
      throw new Error(`[projects-fixtures] project cleanup was not accepted (status ${cleanupStatus}).`);
    }
  };

  return { project, cleanup };
}
