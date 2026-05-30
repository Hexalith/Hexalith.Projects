import type { ApiRequestFixtureParams, EnhancedApiPromise } from '@seontechnologies/playwright-utils/api-request';
import { mutationHeaders, queryHeaders, type MutationHeaderOptions, type AuthHeaderOptions } from './correlation.js';
import type { CreateProjectInput } from '../factories/project-factory.js';

/**
 * Thin typed client over the Hexalith.Projects REST API (AR-15 OpenAPI spine).
 *
 * Wraps playwright-utils `apiRequest` (passed in from the fixture) so tests stay
 * declarative. Mutations are command-async: they return `202 AcceptedCommand` and do
 * NOT guarantee read-after-write — callers must converge via `waitForProject` (see
 * readiness.ts). Reads carry freshness/trust state.
 *
 * NOTE: paths follow the planned v1 spine; adjust to the generated OpenAPI client once
 * `Contracts/openapi/hexalith.projects.v1.yaml` (Story 1.3) is authoritative.
 */

/** The playwright-utils `apiRequest` *fixture* signature (no `request` — injected by the fixture). */
export type ApiRequest = <T = unknown>(params: ApiRequestFixtureParams) => EnhancedApiPromise<T>;

export type ProjectLifecycle = 'active' | 'archived';

export interface ProjectSummary {
  projectId: string;
  tenantId: string;
  name: string;
  lifecycle: ProjectLifecycle;
  updatedAt: string;
}

export interface ProjectDetail extends ProjectSummary {
  description?: string;
  /** Trust/freshness of the read (AR-16). */
  freshness?: string;
}

export type ResolutionResult = 'NoMatch' | 'SingleCandidate' | 'MultipleCandidates';
export type ResolutionReasonCode = 'ProjectFolderMatched' | 'FileReferenceMatched';

export interface ResolutionCandidate {
  projectId: string;
  displayName?: string;
  lifecycle: ProjectLifecycle;
  score: number;
  rank: number;
  reasonCodes: ResolutionReasonCode[];
}

export interface ResolutionExclusion {
  projectId?: string;
  referenceKind?: string;
  referenceId?: string;
  referenceState?: string;
  reasonCode?: ResolutionReasonCode;
  diagnostic?: string;
}

export interface ProjectResolution {
  result: ResolutionResult;
  candidates: ResolutionCandidate[];
  excluded: ResolutionExclusion[];
}

export interface AcceptedCommand {
  /** Server-assigned aggregate id for the created/affected project. */
  projectId: string;
  /** Correlation id echoed back for tracing. */
  correlationId?: string;
}

export interface ResolveProjectFromAttachmentsInput {
  folderIds?: readonly string[];
  fileIds?: readonly string[];
  includeArchived?: boolean;
}

export interface QueryRequestOptions extends AuthHeaderOptions {
  freshness?: string;
  extraHeaders?: Record<string, string>;
}

/** POST /api/v1/projects → 202 AcceptedCommand (FR-1). */
export async function createProject(
  apiRequest: ApiRequest,
  tenantId: string,
  input: CreateProjectInput,
  headerOptions: MutationHeaderOptions,
): Promise<{ status: number; body: AcceptedCommand }> {
  const { status, body } = await apiRequest<AcceptedCommand>({
    method: 'POST',
    path: '/api/v1/projects',
    headers: { ...mutationHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
    body: input,
  });
  return { status, body };
}

/** GET /api/v1/projects/{id} (FR-2). Returns 404 for unauthorized == nonexistent (safe-denial). */
export async function getProject(
  apiRequest: ApiRequest,
  tenantId: string,
  projectId: string,
  headerOptions: AuthHeaderOptions,
): Promise<{ status: number; body: ProjectDetail }> {
  const { status, body } = await apiRequest<ProjectDetail>({
    method: 'GET',
    path: `/api/v1/projects/${projectId}`,
    headers: { ...queryHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
    // 404 is an expected safe-denial outcome, not a transport failure — don't retry it.
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** GET /api/v1/projects?lifecycle=… (FR-5). Tenant-scoped + authorization-filtered. */
export async function listProjects(
  apiRequest: ApiRequest,
  tenantId: string,
  headerOptions: AuthHeaderOptions,
  lifecycle?: ProjectLifecycle,
): Promise<{ status: number; body: ProjectSummary[] }> {
  const { status, body } = await apiRequest<ProjectSummary[]>({
    method: 'GET',
    path: '/api/v1/projects',
    params: lifecycle ? { lifecycle } : undefined,
    headers: { ...queryHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
  });
  return { status, body };
}

/** GET /api/v1/projects/resolution/from-attachments (FR-13). */
export async function resolveProjectFromAttachments(
  apiRequest: ApiRequest,
  tenantId: string,
  input: ResolveProjectFromAttachmentsInput,
  headerOptions: QueryRequestOptions,
): Promise<{ status: number; body: ProjectResolution }> {
  const params = new URLSearchParams();
  input.folderIds?.forEach((id) => params.append('folderId', id));
  input.fileIds?.forEach((id) => params.append('fileId', id));
  if (input.includeArchived !== undefined) {
    params.set('includeArchived', String(input.includeArchived));
  }

  const query = params.toString();
  const headers = {
    ...queryHeaders(headerOptions),
    ...(headerOptions.freshness ? { 'X-Hexalith-Freshness': headerOptions.freshness } : {}),
    ...headerOptions.extraHeaders,
    'X-Hexalith-Tenant-Id': tenantId,
  };
  const { status, body } = await apiRequest<ProjectResolution>({
    method: 'GET',
    path: `/api/v1/projects/resolution/from-attachments${query ? `?${query}` : ''}`,
    headers,
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** POST /api/v1/projects/{id}/archive → 202 (FR-4). */
export async function archiveProject(
  apiRequest: ApiRequest,
  tenantId: string,
  projectId: string,
  headerOptions: MutationHeaderOptions,
): Promise<{ status: number }> {
  const { status } = await apiRequest({
    method: 'POST',
    path: `/api/v1/projects/${projectId}/archive`,
    headers: { ...mutationHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
  });
  return { status };
}
