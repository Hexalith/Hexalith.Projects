import type { ApiRequest, ProjectDetail } from '../helpers/projects-api-client.js';
import {
  archiveProject,
  createProject,
  getProjectOperatorDiagnostics,
  linkProjectFileReference,
  linkProjectMemory,
  resolveProjectFromAttachments,
  setProjectFolder,
} from '../helpers/projects-api-client.js';
import type { Recurse } from '../helpers/readiness.js';
import { waitForProject } from '../helpers/readiness.js';
import type { TenantContext } from '../factories/tenant-factory.js';
import { createProjectInput, type CreateProjectInput } from '../factories/project-factory.js';
import type { LiveFixtureGraph } from '../helpers/live-fixtures-api-client.js';

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
  /** A Project whose supported folder/file/memory/conversation metadata graph has converged. */
  referencedProject: ProjectDetail;
  /** Two Projects with disjoint reference matches for deterministic ambiguous resolution. */
  resolutionProjects: ResolutionProjects;
}

export interface ResolutionProjects {
  primary: ProjectDetail;
  secondary: ProjectDetail;
}

export interface SeedProjectDeps {
  apiRequest: ApiRequest;
  authToken: string;
  recurse: Recurse;
  tenantContext: TenantContext;
  requestIdentity?: {
    correlationId: string;
    taskId: string;
    idempotencyKey: string;
  };
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

  if (!payload.projectId?.trim()) {
    throw new Error('[projects-fixtures] live project seed requires a caller-owned projectId.');
  }
  const retryableCreationStatuses = new Set([404, 502, 503, 504]);
  const creation = await recurse(
    () => createProject(
      apiRequest,
      tenantContext.tenantId,
      payload,
      mutationOptions(deps, 'create'),
    ),
    (response) => response.status === 202 || !retryableCreationStatuses.has(response.status),
    {
      timeout: 30_000,
      interval: 1_000,
      log: `Waiting for project ${payload.projectId} creation to be accepted`,
      error: `Project ${payload.projectId} creation was not accepted within 30000ms.`,
    },
  );
  if (creation.status !== 202) {
    throw new Error(
      `[projects-fixtures] project seed was not accepted (status ${creation.status}); verify TEST_TENANT_ID and its projected tenant access.`,
    );
  }
  let project: ProjectDetail;
  try {
    project = await waitForProject(
      recurse,
      apiRequest,
      tenantContext.tenantId,
      payload.projectId,
      { authToken },
      { lifecycle: 'active' },
    );
  } catch (error) {
    try {
      await archiveProject(apiRequest, tenantContext.tenantId, payload.projectId, mutationOptions(deps, 'seed-failure-archive'));
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
      mutationOptions(deps, 'archive'),
    );
    if (cleanupStatus !== 202 && cleanupStatus !== 404) {
      throw new Error(`[projects-fixtures] project cleanup was not accepted (status ${cleanupStatus}).`);
    }
    await waitForProject(
      recurse,
      apiRequest,
      tenantContext.tenantId,
      project.projectId,
      { authToken },
      { lifecycle: 'archived' },
    );
  };

  return { project, cleanup };
}

/** Seeds one Project with the full profile-owned metadata graph through supported APIs only. */
export async function seedReferencedProject(
  deps: SeedProjectDeps,
  graph: LiveFixtureGraph,
): Promise<{ project: ProjectDetail; cleanup: () => Promise<void> }> {
  const seeded = await seedActiveProject(
    deps,
    createProjectInput({
      projectId: graph.projectId,
      name: `Fixture conversation ${graph.scenario}`,
    }),
  );

  try {
    expectAccepted(
      'folder seed',
      await setProjectFolder(
        deps.apiRequest,
        deps.tenantContext.tenantId,
        {
          projectId: seeded.project.projectId,
          folderId: graph.folderId,
          displayName: `Fixture folder ${graph.scenario}`,
        },
        mutationOptions(deps, 'folder'),
      ),
    );
    expectAccepted(
      'file seed',
      await linkProjectFileReference(
        deps.apiRequest,
        deps.tenantContext.tenantId,
        {
          projectId: seeded.project.projectId,
          fileReferenceId: graph.fileReferenceId,
          folderId: graph.folderId,
          workspaceId: graph.workspaceId,
          filePath: graph.filePath,
          displayName: 'contract.pdf',
        },
        mutationOptions(deps, 'file'),
      ),
    );
    expectAccepted(
      'memory seed',
      await linkProjectMemory(
        deps.apiRequest,
        deps.tenantContext.tenantId,
        {
          projectId: seeded.project.projectId,
          memoryReferenceId: graph.memoryReferenceId,
          displayName: `Fixture memory ${graph.scenario}`,
        },
        mutationOptions(deps, 'memory'),
      ),
    );

    await deps.recurse(
      () => getProjectOperatorDiagnostics(
        deps.apiRequest,
        deps.tenantContext.tenantId,
        seeded.project.projectId,
        { authToken: deps.authToken, freshness: 'eventually_consistent' },
      ),
      ({ status, body }) => status === 200
        && [graph.folderId, graph.fileReferenceId, graph.memoryReferenceId, graph.existingConversationId]
          .every((id) => body.references.some((reference) => reference.referenceId === id)),
      {
        timeout: 30_000,
        interval: 1_000,
        log: `Waiting for referenced Project ${seeded.project.projectId} to converge`,
      },
    );
  } catch (error) {
    await cleanupPreservingPrimary(seeded.cleanup, error);
  }

  return seeded;
}

/** Seeds two real Projects whose folder/file evidence yields deterministic single and multiple matches. */
export async function seedResolutionProjects(
  deps: SeedProjectDeps,
  graph: LiveFixtureGraph,
): Promise<{ projects: ResolutionProjects; cleanup: () => Promise<void> }> {
  const displayName = `Fixture conversation ${graph.scenario}`;
  const primary = await seedActiveProject(
    withIdentitySuffix(deps, 'primary'),
    createProjectInput({ projectId: graph.projectId, name: displayName }),
  );
  let secondary: Awaited<ReturnType<typeof seedActiveProject>> | undefined;

  try {
    secondary = await seedActiveProject(
      withIdentitySuffix(deps, 'secondary'),
      createProjectInput({ projectId: graph.secondaryProjectId, name: displayName }),
    );
    expectAccepted(
      'resolution folder seed',
      await setProjectFolder(
        deps.apiRequest,
        deps.tenantContext.tenantId,
        {
          projectId: primary.project.projectId,
          folderId: graph.folderId,
          displayName: `Fixture folder ${graph.scenario}`,
        },
        mutationOptions(deps, 'resolution-folder'),
      ),
    );
    expectAccepted(
      'resolution file seed',
      await linkProjectFileReference(
        deps.apiRequest,
        deps.tenantContext.tenantId,
        {
          projectId: secondary.project.projectId,
          fileReferenceId: graph.fileReferenceId,
          folderId: graph.folderId,
          workspaceId: graph.workspaceId,
          filePath: graph.filePath,
          displayName: 'contract.pdf',
        },
        mutationOptions(deps, 'resolution-file'),
      ),
    );

    await deps.recurse(
      () => resolveProjectFromAttachments(
        deps.apiRequest,
        deps.tenantContext.tenantId,
        { folderIds: [graph.folderId], fileIds: [graph.fileReferenceId] },
        { authToken: deps.authToken, freshness: 'eventually_consistent' },
      ),
      ({ status, body }) => status === 200
        && body.result === 'MultipleCandidates'
        && [primary.project.projectId, secondary!.project.projectId]
          .every((id) => body.candidates.some((candidate) => candidate.projectId === id)),
      {
        timeout: 30_000,
        interval: 1_000,
        log: 'Waiting for deterministic resolution evidence to converge',
      },
    );
  } catch (error) {
    if (secondary) {
      try {
        await secondary.cleanup();
      } catch {
        // Preserve the primary setup failure.
      }
    }
    await cleanupPreservingPrimary(primary.cleanup, error);
  }

  const cleanup = async (): Promise<void> => {
    let secondaryFailure: unknown;
    try {
      await secondary!.cleanup();
    } catch (error) {
      secondaryFailure = error;
    }

    try {
      await primary.cleanup();
    } catch (primaryFailure) {
      throw primaryFailure;
    }

    if (secondaryFailure) throw secondaryFailure;
  };
  return { projects: { primary: primary.project, secondary: secondary!.project }, cleanup };
}

function mutationOptions(deps: SeedProjectDeps, operation: string) {
  return {
    authToken: deps.authToken,
    ...(deps.requestIdentity
      ? {
          correlationId: `${deps.requestIdentity.correlationId}-${operation}`,
          taskId: `${deps.requestIdentity.taskId}-${operation}`,
          idempotencyKey: `${deps.requestIdentity.idempotencyKey}-${operation}`,
        }
      : {}),
  };
}

function withIdentitySuffix(deps: SeedProjectDeps, suffix: string): SeedProjectDeps {
  if (!deps.requestIdentity) return deps;
  return {
    ...deps,
    requestIdentity: {
      correlationId: `${deps.requestIdentity.correlationId}-${suffix}`,
      taskId: `${deps.requestIdentity.taskId}-${suffix}`,
      idempotencyKey: `${deps.requestIdentity.idempotencyKey}-${suffix}`,
    },
  };
}

function expectAccepted(operation: string, response: { status: number }): void {
  if (response.status !== 202) {
    throw new Error(`[projects-fixtures] ${operation} was not accepted (status ${response.status}).`);
  }
}

async function cleanupPreservingPrimary(cleanup: () => Promise<void>, primary: unknown): Promise<never> {
  try {
    await cleanup();
  } catch {
    // Preserve the setup failure; cleanup remains best effort on this path.
  }
  throw primary;
}
