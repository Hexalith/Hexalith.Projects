import type { ApiRequestFixtureParams, EnhancedApiPromise } from '@seontechnologies/playwright-utils/api-request';
import type { APIRequestContext } from '@playwright/test';
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
  name: string;
  lifecycleState: ProjectLifecycle;
  createdAt: string;
  updatedAt: string;
  freshness: ProjectOperatorFreshnessMetadata;
}

export interface ProjectListResponse {
  items: ProjectSummary[];
  freshness: ProjectOperatorFreshnessMetadata;
}

export interface ProjectDetail extends ProjectSummary {
  description?: string;
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

export interface ProjectOperatorFreshnessMetadata {
  readConsistency: 'eventually_consistent';
  observedAt: string;
  projectionWatermark?: string | null;
  stale: boolean;
  trustState: string;
}

export interface ProjectOperatorContextActivation {
  enabled: boolean;
  blockedReasonCode?: string | null;
}

export interface ProjectOperatorReferenceSummary {
  referenceKind: 'conversation' | 'folder' | 'file' | 'memory';
  referenceState: string;
  referenceId?: string | null;
  displayName?: string | null;
  reasonCode?: string | null;
  freshness: ProjectOperatorFreshnessMetadata;
}

export interface ProjectContextReference {
  referenceKind: 'conversation' | 'folder' | 'file' | 'memory';
  referenceId?: string | null;
  displayName?: string | null;
  referenceState: string;
  reasonCode?: string | null;
  observedAt: string;
}

export interface ProjectContextExcludedReference {
  referenceKind: 'conversation' | 'folder' | 'file' | 'memory';
  referenceId?: string | null;
  referenceState: string;
  reasonCode?: string | null;
  failedCheck?: string | null;
  diagnostic?: string | null;
}

export interface ProjectContext {
  projectId: string;
  lifecycle: string;
  projectFolder?: ProjectContextReference | null;
  conversations: ProjectContextReference[];
  fileReferences: ProjectContextReference[];
  memoryReferences: ProjectContextReference[];
  excluded: ProjectContextExcludedReference[];
  assemblyOutcome: string;
  observedAt: string;
  freshness: string;
}

export interface ProjectContextEvaluation {
  referenceKind: 'conversation' | 'folder' | 'file' | 'memory';
  referenceId?: string | null;
  resultState: string;
  failedCheck?: string | null;
  reasonCode?: string | null;
  diagnostic?: string | null;
  observedAt: string;
}

export interface ProjectContextExplanation {
  context: ProjectContext;
  evaluations: ProjectContextEvaluation[];
}

export interface ProjectConversationItem {
  projectId: string;
  conversationId: string;
  lifecycleStatus: string;
  displayLabel?: string | null;
  trustSignal: string;
  projectSafeLabel?: string | null;
  projectSafeStatus?: string | null;
}

export interface ProjectConversationPageMetadata {
  returnedCount: number;
  continuationCursor?: string | null;
}

export interface ProjectConversationsPage {
  projectId: string;
  items: ProjectConversationItem[];
  page: ProjectConversationPageMetadata;
  trustSignal: string;
}

export interface ProjectOperatorAuditTimelineItem {
  auditEventId: string;
  operationType: string;
  occurredAt: string;
  actorPrincipalId: string;
  correlationId: string;
  taskId: string;
  referenceKind?: 'conversation' | 'folder' | 'file' | 'memory' | null;
  referenceId?: string | null;
  previousState?: string | null;
  newState?: string | null;
  reasonCode?: string | null;
  conversationId?: string | null;
  sourceProjectId?: string | null;
  projectionSequence: number;
}

export interface ProjectOperatorDiagnostic {
  projectId: string;
  name: string;
  description?: string | null;
  lifecycleState: ProjectLifecycle;
  createdAt: string;
  updatedAt: string;
  setupMetadata?: string | null;
  projectSetup?: unknown;
  contextActivation: ProjectOperatorContextActivation;
  references: ProjectOperatorReferenceSummary[];
  auditTimeline: ProjectOperatorAuditTimelineItem[];
  freshness: ProjectOperatorFreshnessMetadata;
}

export interface ProjectCreationProposalInput {
  requestSchemaVersion: 'v1';
  conversationId: string;
  folderId?: string;
  fileReferenceIds?: readonly string[];
  suggestedName?: string;
  description?: string;
  setupMetadata?: string;
}

export interface ProjectCreationProposal {
  resolutionResult: 'NoMatch';
  suggestedName: string;
  description?: string;
  setupMetadata?: string;
  conversationId: string;
  folderId?: string;
  fileReferenceIds: string[];
  observedAt: string;
  freshness: 'eventually_consistent';
  warnings: string[];
}

export interface AcceptedCommand {
  /** Optional compatibility echo; live fixtures use the caller-owned request identity. */
  projectId?: string;
  /** Correlation id echoed back for tracing. */
  correlationId?: string;
}

export interface ResolveProjectFromAttachmentsInput {
  folderIds?: readonly string[];
  fileIds?: readonly string[];
  includeArchived?: boolean;
}

export interface ConfirmProjectResolutionInput {
  projectId: string;
  conversationId: string;
  candidateProjectIds: readonly string[];
  sourceProjectId?: string;
}

export interface SetProjectFolderInput {
  projectId: string;
  folderId: string;
  displayName: string;
  replacementConfirmed?: boolean;
}

export interface LinkProjectFileReferenceInput {
  projectId: string;
  fileReferenceId: string;
  folderId: string;
  workspaceId: string;
  filePath: string;
  displayName: string;
}

export interface LinkProjectMemoryInput {
  projectId: string;
  memoryReferenceId: string;
  displayName: string;
}

export interface ProjectMetadataInput {
  displayName: string;
  metadataClass: 'public_metadata' | 'tenant_sensitive' | 'credential_sensitive' | 'secret';
}

export interface ProjectFolderMetadataInput {
  displayName: string;
}

export interface ProjectFileReferenceMetadataInput {
  displayName: string;
}

export interface ConfirmNewProjectProposalFolderInput {
  folderId: string;
  folderMetadata: ProjectFolderMetadataInput;
}

export interface ConfirmNewProjectProposalFileReferenceInput {
  fileReferenceId: string;
  folderId: string;
  workspaceId: string;
  filePath: string;
  fileMetadata: ProjectFileReferenceMetadataInput;
}

export interface ConfirmNewProjectProposalInput {
  requestSchemaVersion: 'v1';
  operation: 'confirmNewProjectProposal';
  resolutionResult: 'NoMatch';
  confirmed: true;
  projectId: string;
  conversationId: string;
  projectMetadata: ProjectMetadataInput;
  description?: string;
  setupMetadata?: string;
  folder?: ConfirmNewProjectProposalFolderInput;
  fileReferences?: readonly ConfirmNewProjectProposalFileReferenceInput[];
  fileReferenceIds: readonly string[];
}

export interface QueryRequestOptions extends AuthHeaderOptions {
  freshness?: string;
  extraHeaders?: Record<string, string>;
}

export interface OperatorDiagnosticOptions extends QueryRequestOptions {
  auditLimit?: number;
}

export interface ProjectConversationListOptions extends QueryRequestOptions {
  pageSize?: number;
  cursor?: string;
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
  headerOptions: QueryRequestOptions,
): Promise<{ status: number; body: ProjectDetail }> {
  const headers = {
    ...queryHeaders(headerOptions),
    ...(headerOptions.freshness ? { 'X-Hexalith-Freshness': headerOptions.freshness } : {}),
    ...headerOptions.extraHeaders,
    'X-Hexalith-Tenant-Id': tenantId,
  };
  const { status, body } = await apiRequest<ProjectDetail>({
    method: 'GET',
    path: `/api/v1/projects/${projectId}`,
    headers,
    // 404 is an expected safe-denial outcome, not a transport failure — don't retry it.
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** GET /api/v1/projects?lifecycle=… (FR-5). Tenant-scoped + authorization-filtered. */
export async function listProjects(
  apiRequest: ApiRequest,
  tenantId: string,
  headerOptions: QueryRequestOptions,
  lifecycle?: ProjectLifecycle,
): Promise<{ status: number; body: ProjectListResponse }> {
  const headers = {
    ...queryHeaders(headerOptions),
    ...(headerOptions.freshness ? { 'X-Hexalith-Freshness': headerOptions.freshness } : {}),
    ...headerOptions.extraHeaders,
    'X-Hexalith-Tenant-Id': tenantId,
  };
  const { status, body } = await apiRequest<ProjectListResponse>({
    method: 'GET',
    path: '/api/v1/projects',
    params: lifecycle ? { lifecycle } : undefined,
    headers,
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** GET /api/v1/projects/{id}/operator-diagnostics (Story 5.2). */
export async function getProjectOperatorDiagnostics(
  apiRequest: ApiRequest,
  tenantId: string,
  projectId: string,
  headerOptions: OperatorDiagnosticOptions,
): Promise<{ status: number; body: ProjectOperatorDiagnostic }> {
  const headers = {
    ...queryHeaders(headerOptions),
    ...(headerOptions.freshness ? { 'X-Hexalith-Freshness': headerOptions.freshness } : {}),
    ...headerOptions.extraHeaders,
    'X-Hexalith-Tenant-Id': tenantId,
  };
  const { status, body } = await apiRequest<ProjectOperatorDiagnostic>({
    method: 'GET',
    path: `/api/v1/projects/${projectId}/operator-diagnostics`,
    params: headerOptions.auditLimit === undefined ? undefined : { auditLimit: headerOptions.auditLimit },
    headers,
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** GET /api/v1/projects/{id}/context/explain (Story 3.3, reused by Story 5.5 health rows). */
export async function getProjectContextExplanation(
  apiRequest: ApiRequest,
  tenantId: string,
  projectId: string,
  headerOptions: QueryRequestOptions,
): Promise<{ status: number; body: ProjectContextExplanation }> {
  const headers = {
    ...queryHeaders(headerOptions),
    ...(headerOptions.freshness ? { 'X-Hexalith-Freshness': headerOptions.freshness } : {}),
    ...headerOptions.extraHeaders,
    'X-Hexalith-Tenant-Id': tenantId,
  };
  const { status, body } = await apiRequest<ProjectContextExplanation>({
    method: 'GET',
    path: `/api/v1/projects/${projectId}/context/explain`,
    headers,
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** GET /api/v1/projects/{id}/conversations (Story 2.1, reused by Story 5.5 health rows). */
export async function listProjectConversations(
  apiRequest: ApiRequest,
  tenantId: string,
  projectId: string,
  headerOptions: ProjectConversationListOptions,
): Promise<{ status: number; body: ProjectConversationsPage }> {
  const headers = {
    ...queryHeaders(headerOptions),
    ...(headerOptions.freshness ? { 'X-Hexalith-Freshness': headerOptions.freshness } : {}),
    ...headerOptions.extraHeaders,
    'X-Hexalith-Tenant-Id': tenantId,
  };
  const { status, body } = await apiRequest<ProjectConversationsPage>({
    method: 'GET',
    path: `/api/v1/projects/${projectId}/conversations`,
    params: {
      ...(headerOptions.pageSize === undefined ? {} : { pageSize: String(headerOptions.pageSize) }),
      ...(headerOptions.cursor === undefined ? {} : { cursor: headerOptions.cursor }),
    },
    headers,
    retryConfig: { maxRetries: 0 },
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

/** POST /api/v1/projects/resolution/new-project-proposal → 200 ProjectCreationProposal only for NoMatch. */
export async function proposeNewProject(
  apiRequest: ApiRequest,
  tenantId: string,
  input: ProjectCreationProposalInput,
  headerOptions: QueryRequestOptions,
): Promise<{ status: number; body: ProjectCreationProposal }> {
  const headers = {
    ...queryHeaders(headerOptions),
    ...(headerOptions.freshness ? { 'X-Hexalith-Freshness': headerOptions.freshness } : {}),
    ...headerOptions.extraHeaders,
    'X-Hexalith-Tenant-Id': tenantId,
  };
  const { status, body } = await apiRequest<ProjectCreationProposal>({
    method: 'POST',
    path: '/api/v1/projects/resolution/new-project-proposal',
    headers,
    body: input,
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** POST /api/v1/projects/{projectId}/conversations/{conversationId}/resolution/confirm → 202 AcceptedCommand (FR-14). */
export async function confirmProjectResolution(
  apiRequest: ApiRequest,
  tenantId: string,
  input: ConfirmProjectResolutionInput,
  headerOptions: MutationHeaderOptions,
): Promise<{ status: number; body: AcceptedCommand }> {
  const { status, body } = await apiRequest<AcceptedCommand>({
    method: 'POST',
    path: `/api/v1/projects/${input.projectId}/conversations/${input.conversationId}/resolution/confirm`,
    headers: { ...mutationHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
    body: {
      requestSchemaVersion: 'v1',
      operation: 'confirm',
      projectId: input.projectId,
      conversationId: input.conversationId,
      resolutionResult: 'MultipleCandidates',
      confirmed: true,
      candidateProjectIds: input.candidateProjectIds,
      ...(input.sourceProjectId ? { sourceProjectId: input.sourceProjectId } : {}),
    },
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** PUT /api/v1/projects/{projectId}/folder -> 202 AcceptedCommand. */
export async function setProjectFolder(
  apiRequest: ApiRequest,
  tenantId: string,
  input: SetProjectFolderInput,
  headerOptions: MutationHeaderOptions,
): Promise<{ status: number; body: AcceptedCommand }> {
  const { status, body } = await apiRequest<AcceptedCommand>({
    method: 'PUT',
    path: `/api/v1/projects/${input.projectId}/folder`,
    headers: { ...mutationHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
    body: {
      requestSchemaVersion: 'v1',
      operation: 'set',
      projectId: input.projectId,
      folderId: input.folderId,
      folderMetadata: { displayName: input.displayName },
      replacementConfirmed: input.replacementConfirmed ?? false,
    },
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** POST /api/v1/projects/{projectId}/files/{fileReferenceId}/link -> 202 AcceptedCommand. */
export async function linkProjectFileReference(
  apiRequest: ApiRequest,
  tenantId: string,
  input: LinkProjectFileReferenceInput,
  headerOptions: MutationHeaderOptions,
): Promise<{ status: number; body: AcceptedCommand }> {
  const { status, body } = await apiRequest<AcceptedCommand>({
    method: 'POST',
    path: `/api/v1/projects/${input.projectId}/files/${input.fileReferenceId}/link`,
    headers: { ...mutationHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
    body: {
      requestSchemaVersion: 'v1',
      operation: 'link',
      projectId: input.projectId,
      fileReferenceId: input.fileReferenceId,
      folderId: input.folderId,
      workspaceId: input.workspaceId,
      filePath: input.filePath,
      fileMetadata: { displayName: input.displayName },
    },
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** POST /api/v1/projects/{projectId}/memories/{memoryReferenceId}/link -> 202 AcceptedCommand. */
export async function linkProjectMemory(
  apiRequest: ApiRequest,
  tenantId: string,
  input: LinkProjectMemoryInput,
  headerOptions: MutationHeaderOptions,
): Promise<{ status: number; body: AcceptedCommand }> {
  const { status, body } = await apiRequest<AcceptedCommand>({
    method: 'POST',
    path: `/api/v1/projects/${input.projectId}/memories/${input.memoryReferenceId}/link`,
    headers: { ...mutationHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
    body: {
      requestSchemaVersion: 'v1',
      operation: 'link',
      projectId: input.projectId,
      memoryReferenceId: input.memoryReferenceId,
      memoryMetadata: { displayName: input.displayName },
    },
    retryConfig: { maxRetries: 0 },
  });
  return { status, body };
}

/** POST /api/v1/projects/proposals/confirm → 202 AcceptedCommand after explicit NoMatch confirmation. */
export async function confirmNewProjectProposal(
  request: APIRequestContext,
  tenantId: string,
  input: ConfirmNewProjectProposalInput,
  headerOptions: MutationHeaderOptions,
): Promise<{ status: number; body: AcceptedCommand }> {
  const apiUrl = process.env.API_URL?.trim();
  if (!apiUrl) throw new Error('[projects-api-client] API_URL is required for proposal confirmation.');
  const response = await request.post(`${apiUrl.replace(/\/$/, '')}/api/v1/projects/proposals/confirm`, {
    headers: { ...mutationHeaders(headerOptions), 'X-Hexalith-Tenant-Id': tenantId },
    data: input,
  });
  const contentType = response.headers()['content-type'] ?? '';
  const body = contentType.includes('json') ? await response.json() : null;
  return { status: response.status(), body: body as AcceptedCommand };
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
    body: {
      archiveIntent: 'archive',
      requestSchemaVersion: 'v1',
    },
  });
  return { status };
}
