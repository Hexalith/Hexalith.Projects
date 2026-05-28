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

  const { body } = await createProject(apiRequest, tenantContext.tenantId, payload, { authToken });
  const project = await waitForProject(
    recurse,
    apiRequest,
    tenantContext.tenantId,
    body.projectId,
    { authToken },
    { lifecycle: 'active' },
  );

  const cleanup = async (): Promise<void> => {
    await archiveProject(apiRequest, tenantContext.tenantId, project.projectId, { authToken });
  };

  return { project, cleanup };
}
