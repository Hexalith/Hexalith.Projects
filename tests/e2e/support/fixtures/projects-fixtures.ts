import type { ApiRequest, ProjectDetail } from '../helpers/projects-api-client.js';
import {
  archiveProject,
  createProject,
  getProject,
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
import { createLiveRequestIdentity } from '../factories/live-fixture-identities.js';
import type { FixtureCleanupAttempt, LiveFixtureGraph } from '../helpers/live-fixtures-api-client.js';
import type { CleanupLedger, CleanupStep } from './cleanup-evidence.js';

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
  /** The attempt graph; every Project and request identity is derived from it. */
  graph: Pick<LiveFixtureGraph, 'graphId'>;
}

const ARCHIVE_CONVERGENCE_TIMEOUT = 30_000;

/**
 * Create an active project with a caller-owned ProjectId and wait for read-model convergence
 * (command-async, no sleeps). The archive-to-convergence cleanup is registered in `ledger` as soon as
 * creation is accepted, so a convergence failure still archives the Project in reverse order.
 */
export async function seedActiveProject(
  deps: SeedProjectDeps,
  ledger: CleanupLedger,
  role: string,
  input: CreateProjectInput,
): Promise<ProjectDetail> {
  const { apiRequest, authToken, recurse, tenantContext } = deps;
  const projectId = input.projectId?.trim();
  if (!projectId) {
    throw new Error('[projects-fixtures] live project seed requires a caller-owned projectId.');
  }
  const retryableCreationStatuses = new Set([404, 502, 503, 504]);
  const creation = await recurse(
    () => createProject(apiRequest, tenantContext.tenantId, input, requestOptions(deps, `${role}:create`)),
    (response) => response.status === 202 || !retryableCreationStatuses.has(response.status),
    {
      timeout: 30_000,
      interval: 1_000,
      log: `Waiting for ${role} creation to be accepted`,
      error: `${role} creation was not accepted within 30000ms.`,
    },
  );
  if (creation.status !== 202) {
    throw new Error(
      `[projects-fixtures] ${role} seed was not accepted (status ${creation.status}); verify the token tenant's projected access.`,
    );
  }

  ledger.register(projectArchiveStep(deps, projectId, role));
  return waitForProject(recurse, apiRequest, tenantContext.tenantId, projectId, { authToken }, { lifecycle: 'active' });
}

/**
 * Registers archive-to-convergence cleanup for a Project created outside the seed helpers (for
 * example by a proposal confirmation). The step first waits for the Project to become observable,
 * because an archive racing an accepted-but-unprojected creation would leave it Active.
 */
export function projectArchiveStep(deps: SeedProjectDeps, projectId: string, role: string): CleanupStep {
  return {
    role,
    run: async (): Promise<FixtureCleanupAttempt[]> => {
      const tenantId = deps.tenantContext.tenantId;
      let observed: Awaited<ReturnType<typeof getProject>>;
      try {
        observed = await deps.recurse(
          () => getProject(deps.apiRequest, tenantId, projectId, { authToken: deps.authToken }),
          (response) => response.status === 200,
          { timeout: ARCHIVE_CONVERGENCE_TIMEOUT, interval: 1_000, log: `Waiting for ${role} before archive cleanup` },
        );
      } catch {
        return [{ role, statusCode: 404, succeeded: false }];
      }
      if (observed.body.lifecycleState === 'archived') {
        return [{ role, statusCode: 200, succeeded: true }];
      }

      const archive = await archiveProject(deps.apiRequest, tenantId, projectId, requestOptions(deps, `${role}:archive`));
      try {
        await deps.recurse(
          () => getProject(deps.apiRequest, tenantId, projectId, { authToken: deps.authToken }),
          (response) => response.status === 200 && response.body.lifecycleState === 'archived',
          { timeout: ARCHIVE_CONVERGENCE_TIMEOUT, interval: 1_000, log: `Waiting for ${role} archive convergence` },
        );
      } catch {
        return [{ role, statusCode: archive.status, succeeded: false }];
      }
      return [{ role, statusCode: archive.status, succeeded: true }];
    },
  };
}

/** Seeds one Project with the full profile-owned metadata graph through supported APIs only. */
export async function seedReferencedProject(
  deps: SeedProjectDeps,
  ledger: CleanupLedger,
  graph: LiveFixtureGraph,
): Promise<ProjectDetail> {
  const project = await seedActiveProject(
    deps,
    ledger,
    'projects:referenced',
    createProjectInput({ projectId: graph.projectId, name: `Fixture conversation ${graph.scenario}` }),
  );

  expectAccepted(
    'folder seed',
    await setProjectFolder(
      deps.apiRequest,
      deps.tenantContext.tenantId,
      { projectId: project.projectId, folderId: graph.folderId, displayName: `Fixture folder ${graph.scenario}` },
      requestOptions(deps, 'projects:referenced:folder'),
    ),
  );
  expectAccepted(
    'file seed',
    await linkProjectFileReference(
      deps.apiRequest,
      deps.tenantContext.tenantId,
      {
        projectId: project.projectId,
        fileReferenceId: graph.fileReferenceId,
        folderId: graph.folderId,
        workspaceId: graph.workspaceId,
        filePath: graph.filePath,
        displayName: 'contract.pdf',
      },
      requestOptions(deps, 'projects:referenced:file'),
    ),
  );
  expectAccepted(
    'memory seed',
    await linkProjectMemory(
      deps.apiRequest,
      deps.tenantContext.tenantId,
      {
        projectId: project.projectId,
        memoryReferenceId: graph.memoryReferenceId,
        displayName: `Fixture memory ${graph.scenario}`,
      },
      requestOptions(deps, 'projects:referenced:memory'),
    ),
  );

  await deps.recurse(
    () => getProjectOperatorDiagnostics(
      deps.apiRequest,
      deps.tenantContext.tenantId,
      project.projectId,
      { authToken: deps.authToken, freshness: 'eventually_consistent' },
    ),
    ({ status, body }) => status === 200
      && [graph.folderId, graph.fileReferenceId, graph.memoryReferenceId, graph.existingConversationId]
        .every((id) => body.references.some((reference) => reference.referenceId === id)),
    { timeout: 30_000, interval: 1_000, log: 'Waiting for the referenced Project graph to converge' },
  );
  return project;
}

/** Seeds two real Projects whose folder/file evidence yields deterministic single and multiple matches. */
export async function seedResolutionProjects(
  deps: SeedProjectDeps,
  ledger: CleanupLedger,
  graph: LiveFixtureGraph,
): Promise<ResolutionProjects> {
  const displayName = `Fixture conversation ${graph.scenario}`;
  const primary = await seedActiveProject(
    deps,
    ledger,
    'projects:resolution-primary',
    createProjectInput({ projectId: graph.projectId, name: displayName }),
  );
  const secondary = await seedActiveProject(
    deps,
    ledger,
    'projects:resolution-secondary',
    createProjectInput({ projectId: graph.secondaryProjectId, name: displayName }),
  );

  expectAccepted(
    'resolution folder seed',
    await setProjectFolder(
      deps.apiRequest,
      deps.tenantContext.tenantId,
      { projectId: primary.projectId, folderId: graph.folderId, displayName: `Fixture folder ${graph.scenario}` },
      requestOptions(deps, 'projects:resolution-primary:folder'),
    ),
  );
  expectAccepted(
    'resolution file seed',
    await linkProjectFileReference(
      deps.apiRequest,
      deps.tenantContext.tenantId,
      {
        projectId: secondary.projectId,
        fileReferenceId: graph.fileReferenceId,
        folderId: graph.folderId,
        workspaceId: graph.workspaceId,
        filePath: graph.filePath,
        displayName: 'contract.pdf',
      },
      requestOptions(deps, 'projects:resolution-secondary:file'),
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
      && [primary.projectId, secondary.projectId]
        .every((id) => body.candidates.some((candidate) => candidate.projectId === id)),
    { timeout: 30_000, interval: 1_000, log: 'Waiting for deterministic resolution evidence to converge' },
  );
  return { primary, secondary };
}

/** Operation-scoped request identities derived from the attempt graph; never shared across attempts. */
export function requestOptions(deps: Pick<SeedProjectDeps, 'authToken' | 'graph'>, operation: string) {
  return { authToken: deps.authToken, ...createLiveRequestIdentity(deps.graph, operation) };
}

function expectAccepted(operation: string, response: { status: number }): void {
  if (response.status !== 202) {
    throw new Error(`[projects-fixtures] ${operation} was not accepted (status ${response.status}).`);
  }
}
