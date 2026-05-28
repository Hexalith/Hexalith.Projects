import { test, expect } from '../support/merged-fixtures.js';
import { mutationHeaders, queryHeaders } from '../support/helpers/correlation.js';

/**
 * F5 critical journey — optional File Reference link/unlink (Story 2.5; FR-9 / FR-11; AR-11 Folders ACL).
 *
 * `test.fixme` until the Hexalith.Projects API + AppHost expose the file-reference routes through a
 * running topology (mirrors the lifecycle/resolution specs, which are also scaffolded). The bodies are
 * pattern-complete and document the load-bearing E2E disciplines this slice must honour:
 *   - command-async (202 AcceptedCommand), no read-after-write — converge on the read model (no sleeps);
 *   - File References are OPTIONAL and supplement Project Context — they never satisfy or replace the
 *     single Project Folder (AC3);
 *   - unlink removes only the Project↔file association — it never deletes/mutates the file in Folders (AC4);
 *   - Folders-owned denial/redaction/sensitivity collapses to an indistinguishable safe denial that never
 *     leaks raw paths, file contents, or upstream authorization details (AC5 / NoPayloadLeakage).
 *
 * Folders addresses a file by (folderId, workspaceId, workspace-relative path); those addressing fields
 * are transient (endpoint → Folders ACL only) and are never stored — Projects keeps only the opaque
 * fileReferenceId, owning folderId, and safe display metadata.
 */
interface ReferenceSummary {
  referenceKind: 'conversation' | 'folder' | 'file' | 'memory';
  referenceId: string;
  state: string;
}

interface ProjectReferences {
  projectId: string;
  references: ReferenceSummary[];
}

const FOLDER_ID = 'folder_01HZ9K8YQ3W6V2N4R7T5P0X1AC';
const WORKSPACE_ID = 'workspace_01HZ9K8YQ3W6V2N4R7T5P0X1AD';
const FILE_REFERENCE_ID = 'file_01HZ9K8YQ3W6V2N4R7T5P0X1F1';

const linkBody = (projectId: string, fileReferenceId = FILE_REFERENCE_ID) => ({
  requestSchemaVersion: 'v1',
  operation: 'link',
  projectId,
  fileReferenceId,
  folderId: FOLDER_ID,
  workspaceId: WORKSPACE_ID,
  filePath: 'docs/contract.pdf',
  fileMetadata: { displayName: 'contract.pdf' },
});

const fileReferences = (refs: ReferenceSummary[]) => refs.filter((r) => r.referenceKind === 'file');
const folderReferences = (refs: ReferenceSummary[]) => refs.filter((r) => r.referenceKind === 'folder');

test.describe('Projects file references (link / unlink)', () => {
  test.fixme('links an authorized file reference (202) and surfaces it as referenceKind=file (FR-9 / AC1,2,7)', async ({ apiRequest, authToken, recurse, tenantContext, seededProject }) => {
    const { status } = await apiRequest({
      method: 'POST',
      path: `/api/v1/projects/${seededProject.projectId}/files/${FILE_REFERENCE_ID}/link`,
      headers: { ...mutationHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: linkBody(seededProject.projectId),
    });
    expect(status).toBe(202); // AcceptedCommand — command-async

    // No read-after-write: poll the read model until the file reference is indexed (no sleeps).
    await recurse(
      () =>
        apiRequest<ProjectReferences>({
          method: 'GET',
          path: `/api/v1/projects/${seededProject.projectId}`,
          headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
        }),
      (res) => fileReferences(res.body.references).some((r) => r.referenceId === FILE_REFERENCE_ID),
      { timeout: 30_000, interval: 1_000, log: 'Waiting for the file reference to appear in the read model' },
    );
  });

  test.fixme('linking a file never satisfies, replaces, or auto-creates the single Project Folder (AC3)', async ({ apiRequest, authToken, recurse, tenantContext, seededProject }) => {
    const before = await apiRequest<ProjectReferences>({
      method: 'GET',
      path: `/api/v1/projects/${seededProject.projectId}`,
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    const folderBefore = folderReferences(before.body.references);

    const { status } = await apiRequest({
      method: 'POST',
      path: `/api/v1/projects/${seededProject.projectId}/files/${FILE_REFERENCE_ID}/link`,
      headers: { ...mutationHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: linkBody(seededProject.projectId),
    });
    expect(status).toBe(202);

    await recurse(
      () =>
        apiRequest<ProjectReferences>({
          method: 'GET',
          path: `/api/v1/projects/${seededProject.projectId}`,
          headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
        }),
      (res) => fileReferences(res.body.references).length > 0,
      { timeout: 30_000, interval: 1_000, log: 'Waiting for the file reference before asserting the folder lane is untouched' },
    );

    const after = await apiRequest<ProjectReferences>({
      method: 'GET',
      path: `/api/v1/projects/${seededProject.projectId}`,
      headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
    });
    // The folder lane is disjoint from the file lane: linking a file must not add/remove a Project Folder.
    expect(folderReferences(after.body.references)).toEqual(folderBefore);
  });

  test.fixme('unlinking a file removes only the association, never the Project Folder row (FR-9 / AC4)', async ({ apiRequest, authToken, recurse, tenantContext, seededProject }) => {
    await apiRequest({
      method: 'POST',
      path: `/api/v1/projects/${seededProject.projectId}/files/${FILE_REFERENCE_ID}/link`,
      headers: { ...mutationHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: linkBody(seededProject.projectId),
    });
    await recurse(
      () =>
        apiRequest<ProjectReferences>({
          method: 'GET',
          path: `/api/v1/projects/${seededProject.projectId}`,
          headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
        }),
      (res) => fileReferences(res.body.references).some((r) => r.referenceId === FILE_REFERENCE_ID),
      { timeout: 30_000, interval: 1_000, log: 'Waiting for the file reference to be linked before unlink' },
    );

    const { status } = await apiRequest({
      method: 'DELETE',
      path: `/api/v1/projects/${seededProject.projectId}/files/${FILE_REFERENCE_ID}`,
      headers: { ...mutationHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: {
        requestSchemaVersion: 'v1',
        operation: 'unlink',
        unlinkIntent: 'removeReference',
        projectId: seededProject.projectId,
        fileReferenceId: FILE_REFERENCE_ID,
      },
    });
    expect(status).toBe(202);

    await recurse(
      () =>
        apiRequest<ProjectReferences>({
          method: 'GET',
          path: `/api/v1/projects/${seededProject.projectId}`,
          headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
        }),
      (res) => !fileReferences(res.body.references).some((r) => r.referenceId === FILE_REFERENCE_ID),
      { timeout: 30_000, interval: 1_000, log: 'Waiting for the file reference to be removed from the read model' },
    );
  });

  test.fixme('denied/redacted Folders evidence fails closed as safe-denial and never leaks path or content (AC5)', async ({ apiRequest, authToken, tenantContext, seededProject }) => {
    const { status, body } = await apiRequest<unknown>({
      method: 'POST',
      path: `/api/v1/projects/${seededProject.projectId}/files/file_forbidden0000000000000000/link`,
      headers: { ...mutationHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
      body: { ...linkBody(seededProject.projectId, 'file_forbidden0000000000000000'), filePath: 'secret/redacted-note.md' },
      // Safe-denial is an expected domain outcome, not a transport failure — don't retry it.
      retryConfig: { maxRetries: 0 },
    });
    // Unauthorized/redacted == nonexistent → safe denial, never 200/202.
    expect(status).toBe(404);
    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain('secret/redacted-note.md');
    expect(serialized).not.toContain('redacted');
  });

  test.fixme('equivalent duplicate link with the same Idempotency-Key replays safely (AC8)', async ({ apiRequest, authToken, recurse, tenantContext, seededProject }) => {
    const idempotencyKey = `idem-file-link-${seededProject.projectId}`;
    const headers = { ...mutationHeaders({ authToken, idempotencyKey }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId };

    const first = await apiRequest({
      method: 'POST',
      path: `/api/v1/projects/${seededProject.projectId}/files/${FILE_REFERENCE_ID}/link`,
      headers,
      body: linkBody(seededProject.projectId),
    });
    const second = await apiRequest({
      method: 'POST',
      path: `/api/v1/projects/${seededProject.projectId}/files/${FILE_REFERENCE_ID}/link`,
      headers,
      body: linkBody(seededProject.projectId),
    });
    expect(first.status).toBe(202);
    expect(second.status).toBe(202);

    // Idempotent replay: the reference is recorded exactly once, never duplicated.
    await recurse(
      () =>
        apiRequest<ProjectReferences>({
          method: 'GET',
          path: `/api/v1/projects/${seededProject.projectId}`,
          headers: { ...queryHeaders({ authToken }), 'X-Hexalith-Tenant-Id': tenantContext.tenantId },
        }),
      (res) => fileReferences(res.body.references).filter((r) => r.referenceId === FILE_REFERENCE_ID).length === 1,
      { timeout: 30_000, interval: 1_000, log: 'Waiting for exactly one file reference after idempotent replay' },
    );
  });
});
