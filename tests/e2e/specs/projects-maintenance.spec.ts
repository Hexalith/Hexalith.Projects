import { test as base } from '@playwright/test';
import { test, liveAppHostTest, expect } from '../support/merged-fixtures.js';
import { ProjectDetailPage } from '../support/page-objects/project-detail.page.js';

const FORBIDDEN_MAINTENANCE_MARKERS = [
  'Idempotency-Key',
  'idempotencyKey',
  'command body',
  'proposal body',
  'raw ProblemDetails',
  'candidateScore',
  'candidateRank',
  'rejectedCandidateId',
  'workspace/path',
  'file content',
  'memory payload',
  'transcript',
  'BEGIN PRIVATE KEY',
  'Authorization: Bearer',
  'secret token',
];

/**
 * F5 critical journey — audit-first maintenance with dry-run (UX-DR17 / UX-DR21 / UX-DR25).
 *
 * Live-gated behind `E2E_LIVE_APPHOST=1`. Demonstrates the
 * five-state command lifecycle (Idle → Submitting → Acknowledged(202) → Syncing → Confirmed)
 * and the dry-run-before-execute confirmation contract, driven through the web console with
 * network-first interception and deterministic readiness (no sleeps).
 */
test.describe('Projects maintenance (Story 5.9)', () => {
  liveAppHostTest('dry-run an archive shows the expected audit event before execution (UX-DR17/25)', async ({ page, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    const detail = new ProjectDetailPage(page);
    await page.getByTestId('project-detail-tab-actions').click();
    await detail.maintenanceActionSelect.selectOption('archive');
    await page.getByTestId('maintenance-action-dry-run-run').click();

    // Confirmation surfaces current→proposed state + expected audit event, no execution yet.
    await expect(detail.maintenanceProposedState).toContainText('archived');
    await expect(detail.maintenanceAuditEvent).toContainText('project.archived');
    await expect(detail.maintenanceSubmit).toBeDisabled();
  });

  liveAppHostTest('confirmed archive flows through the five-state lifecycle to Confirmed (UX-DR21)', async ({ page, interceptNetworkCall, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    const detail = new ProjectDetailPage(page);
    const archiveCall = interceptNetworkCall({ url: `**/api/v1/projects/${seededProject.projectId}/archive` });
    await page.getByTestId('project-detail-tab-actions').click();
    await detail.maintenanceActionSelect.selectOption('archive');
    await page.getByTestId('maintenance-action-dry-run-run').click();
    await detail.maintenanceConfirm.check();
    await detail.maintenanceSubmit.click();

    const { status } = await archiveCall;
    expect(status).toBe(202); // Acknowledged

    // Syncing → Confirmed: the UI re-queries after the nudge; assert the terminal state
    // via the visible badge rather than a fixed wait.
    await expect(page.getByTestId('project-lifecycle-badge')).toHaveText(/archived/i);
  });

  liveAppHostTest('restore preview and confirmation preserve metadata-only lifecycle semantics', async ({ page, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    const detail = new ProjectDetailPage(page);
    await page.getByTestId('project-detail-tab-actions').click();
    await detail.maintenanceActionSelect.selectOption('restore');
    await detail.maintenanceDryRunRun.click();

    await expect(detail.maintenanceCurrentState).toContainText(/archived|active/i);
    await expect(detail.maintenanceProposedState).toContainText('active');
    await expect(detail.maintenanceAuditEvent).toContainText('project.restored');
    await expect(detail.maintenanceSubmit).toBeDisabled();
  });

  liveAppHostTest('relink and unlink previews preserve sibling resources and block unsafe file relink inputs', async ({
    page,
    seededProject,
  }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    const detail = new ProjectDetailPage(page);
    await page.getByTestId('project-detail-tab-actions').click();

    await detail.maintenanceActionSelect.selectOption('relink');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceWarning).toContainText(/transient|sibling ACL|workspace|path/i);
    await expect(detail.maintenanceSubmit).toBeDisabled();

    await detail.maintenanceActionSelect.selectOption('unlink');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceWarning).toContainText(/association|not deleted|sibling resources/i);
    await expect(detail.maintenanceSubmit).toBeDisabled();
  });

  liveAppHostTest('re-evaluate reloads diagnostics without persisting traces or candidate scores', async ({ page, seededProject }) => {
    await page.goto(`/projects/${seededProject.projectId}`);

    const detail = new ProjectDetailPage(page);
    await page.getByTestId('project-detail-tab-actions').click();
    await detail.maintenanceActionSelect.selectOption('reevaluate');
    await detail.maintenanceDryRunRun.click();

    await expect(detail.maintenanceProposedState).toContainText('recomputed diagnostics');
    await expect(detail.maintenanceWarning).toContainText(/does not persist|candidate scores/i);
    await expect(detail.maintenanceAuditEvent).toContainText('none (read-only recompute)');
    await expect(detail.maintenanceSubmit).toBeDisabled();
  });
});

base.describe('Projects maintenance selector contract (no app required)', () => {
  base('maintenance panel exposes Story 5.9 stable selectors and safe text', async ({ page }) => {
    await page.setContent(maintenancePanelFixture({ lifecycle: 'active', referenceKind: 'memory', referenceId: 'memory-001' }));

    const detail = new ProjectDetailPage(page);
    await expect(detail.maintenancePanel).toBeVisible();
    await expect(detail.maintenancePanel.getByRole('heading', { name: 'Maintenance actions' })).toBeVisible();
    await expect(detail.maintenanceActionSelect).toHaveValue('archive');
    await expect(page.getByTestId('maintenance-action-archive')).toHaveText('Archive');
    await expect(page.getByTestId('maintenance-action-restore')).toHaveText('Restore');
    await expect(page.getByTestId('maintenance-action-relink')).toHaveText('Relink');
    await expect(page.getByTestId('maintenance-action-unlink')).toHaveText('Unlink');
    await expect(page.getByTestId('maintenance-action-reevaluate')).toHaveText('Re-evaluate');
    await expect(detail.maintenanceActionState).toContainText('Preview');
    await expect(detail.maintenanceCurrentState).toContainText('active');
    await expect(detail.maintenanceProposedState).toContainText('archived');
    await expect(detail.maintenanceAuditEvent).toContainText('project.archived');

    const panelText = await detail.maintenancePanel.innerText();
    for (const marker of FORBIDDEN_MAINTENANCE_MARKERS) {
      expect(panelText).not.toContain(marker);
    }
  });

  base('dry-run and confirmation gate archive execution until explicit evidence exists', async ({ page }) => {
    await page.setContent(maintenancePanelFixture({ lifecycle: 'active', referenceKind: 'memory', referenceId: 'memory-001' }));

    const detail = new ProjectDetailPage(page);
    await expect(detail.maintenanceSubmit).toBeDisabled();

    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceActionState).toContainText('DryRunPassed');
    await expect(detail.maintenanceDryRun).toContainText('dry-run passed; explicit confirmation required');
    await expect(detail.maintenanceAuditEvent).toContainText('project.archived');
    await expect(detail.maintenanceSubmit).toBeDisabled();

    await detail.maintenanceConfirm.check();
    await expect(detail.maintenanceActionState).toContainText('ConfirmationRequired');
    await expect(detail.maintenanceSubmit).toBeEnabled();

    await detail.maintenanceSubmit.click();
    await expect(detail.maintenanceActionState).toContainText('Succeeded');
    await expect(detail.maintenanceFeedback).toContainText('confirmed');
    await expect(detail.maintenanceDryRun).toContainText('confirmed audit audit-001');
  });

  base('blocked dry-runs show safe reason codes without enabling submit', async ({ page }) => {
    const detail = new ProjectDetailPage(page);

    await page.setContent(maintenancePanelFixture({ lifecycle: 'active' }));
    await detail.maintenanceActionSelect.selectOption('restore');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceActionState).toContainText('DryRunBlocked');
    await expect(detail.maintenanceDryRun).toContainText('restore requires an archived Project');
    await expect(detail.maintenanceFeedback).toContainText('invalid_lifecycle');
    await expect(detail.maintenanceSubmit).toBeDisabled();

    await detail.maintenanceActionSelect.selectOption('relink');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceActionState).toContainText('DryRunBlocked');
    await expect(detail.maintenanceDryRun).toContainText('reference target is required');
    await expect(detail.maintenanceFeedback).toContainText('invalid_reference');
    await expect(detail.maintenanceSubmit).toBeDisabled();

    await page.setContent(maintenancePanelFixture({ lifecycle: 'active', referenceKind: 'file', referenceId: 'file-001' }));
    await detail.maintenanceActionSelect.selectOption('relink');
    await detail.maintenanceReferenceId.fill('file-001');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceActionState).toContainText('DryRunBlocked');
    await expect(detail.maintenanceDryRun).toContainText('transient workspace/path validation');
    await expect(detail.maintenanceFeedback).toContainText('transient_validation_required');
    await expect(detail.maintenanceSubmit).toBeDisabled();
  });

  base('restore, unlink, and re-evaluate previews expose distinct audit and safety semantics', async ({ page }) => {
    await page.setContent(maintenancePanelFixture({ lifecycle: 'archived', referenceKind: 'memory', referenceId: 'memory-001' }));

    const detail = new ProjectDetailPage(page);
    await detail.maintenanceActionSelect.selectOption('restore');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceActionState).toContainText('DryRunPassed');
    await expect(detail.maintenanceCurrentState).toContainText('archived');
    await expect(detail.maintenanceProposedState).toContainText('active');
    await expect(detail.maintenanceAuditEvent).toContainText('project.restored');
    await expect(detail.maintenanceWarning).toContainText('does not relink references');

    await detail.maintenanceActionSelect.selectOption('unlink');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceProposedState).toContainText('excluded');
    await expect(detail.maintenanceAuditEvent).toContainText('memory.unlinked');
    await expect(detail.maintenanceWarning).toContainText('sibling resources are not deleted');

    await detail.maintenanceActionSelect.selectOption('reevaluate');
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceProposedState).toContainText('recomputed diagnostics');
    await expect(detail.maintenanceAuditEvent).toContainText('none (read-only recompute)');
    await expect(detail.maintenanceWarning).toContainText('does not persist resolution traces or candidate scores');
  });

  base('maintenance panel remains operable in a mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.setContent(maintenancePanelFixture({ lifecycle: 'active', referenceKind: 'folder', referenceId: 'folder-001' }));

    const detail = new ProjectDetailPage(page);
    await expect(detail.maintenancePanel).toBeVisible();
    await expect(detail.maintenanceActionSelect).toBeInViewport();
    await expect(detail.maintenanceCurrentState).toBeInViewport();
    await detail.maintenanceDryRunRun.click();
    await expect(detail.maintenanceConfirm).toBeInViewport();
    await expect(detail.maintenanceSubmit).toBeInViewport();
  });
});

interface MaintenancePanelFixtureOptions {
  lifecycle: 'active' | 'archived';
  referenceKind?: 'conversation' | 'file' | 'folder' | 'memory';
  referenceId?: string;
}

function maintenancePanelFixture(options: MaintenancePanelFixtureOptions): string {
  const referenceKind = options.referenceKind ?? '';
  const referenceId = options.referenceId ?? '';

  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Maintenance action selector contract</title>
    <style>
      body { margin: 0; font-family: Arial, sans-serif; }
      main { max-width: 920px; margin: 0 auto; padding: 16px; }
      section { border: 1px solid #8a8f98; border-radius: 6px; padding: 16px; }
      dl { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 8px; }
      dt { font-weight: 700; }
      dd { margin: 0; overflow-wrap: anywhere; }
      label, button, input, select { font: inherit; }
      button, input, select { min-height: 36px; }
    </style>
    <script>
      var context = { lifecycle: '${options.lifecycle}', referenceKind: '${referenceKind}', referenceId: '${referenceId}' };
      function text(id, value) {
        document.querySelector('[data-testid="' + id + '"]').textContent = value;
      }
      function selectedAction() {
        return document.querySelector('[data-testid="maintenance-action-select"]').value;
      }
      function targetKind() {
        return document.querySelector('[data-testid="maintenance-action-reference-kind"]').value;
      }
      function targetId() {
        return document.querySelector('[data-testid="maintenance-action-reference-id"]').value.trim();
      }
      function hasReference() {
        return targetKind() && targetId();
      }
      function expectedAudit(action) {
        if (action === 'archive') return 'project.archived';
        if (action === 'restore') return 'project.restored';
        if (action === 'reevaluate') return 'none (read-only recompute)';
        if (action === 'relink' && targetKind() === 'folder') return 'project.folder_set';
        if (action === 'relink' && targetKind() === 'file') return 'file_reference.linked';
        if (action === 'relink' && targetKind() === 'memory') return 'memory.linked';
        if (action === 'unlink' && targetKind() === 'conversation') return 'conversation assignment (Conversations-owned)';
        if (action === 'unlink' && targetKind() === 'file') return 'file_reference.unlinked';
        if (action === 'unlink' && targetKind() === 'memory') return 'memory.unlinked';
        return action === 'relink' ? 'reference.linked' : 'reference.unlinked';
      }
      function proposedState(action) {
        if (action === 'archive') return 'archived';
        if (action === 'restore') return 'active';
        if (action === 'reevaluate') return 'recomputed diagnostics';
        if (action === 'relink') return hasReference() ? 'included' : 'blocked';
        if (action === 'unlink') return hasReference() ? 'excluded' : 'blocked';
        return 'unknown';
      }
      function currentState(action) {
        if (action === 'archive' || action === 'restore') return context.lifecycle;
        if (action === 'reevaluate') return 'trusted';
        return hasReference() ? 'included' : 'none';
      }
      function warningText(action) {
        if (action === 'archive') return 'References stay associated and remain audit-visible; no sibling resources are changed.';
        if (action === 'restore') return 'Restore changes only Project lifecycle metadata and does not relink references.';
        if (action === 'relink') return 'Relink requires sibling ACL validation; transient identifiers stay outside the rendered panel.';
        if (action === 'unlink') return 'Unlink removes only the Project association; sibling resources are not deleted.';
        return 'Re-evaluate reloads diagnostics and does not persist resolution traces or candidate scores.';
      }
      function canSubmit() {
        return document.querySelector('[data-testid="maintenance-action-confirm"]').checked
          && document.querySelector('[data-testid="maintenance-action-state"]').textContent === 'ConfirmationRequired';
      }
      function refreshSubmit() {
        document.querySelector('[data-testid="maintenance-action-submit"]').disabled = !canSubmit();
        document.querySelector('[data-testid="maintenance-action-confirm"]').disabled =
          !['DryRunPassed', 'ConfirmationRequired'].includes(document.querySelector('[data-testid="maintenance-action-state"]').textContent);
      }
      function syncReferenceControls(action) {
        const controls = document.querySelector('[data-testid="maintenance-reference-controls"]');
        const referenceKind = document.querySelector('[data-testid="maintenance-action-reference-kind"]');
        const referenceId = document.querySelector('[data-testid="maintenance-action-reference-id"]');
        controls.hidden = action !== 'relink' && action !== 'unlink';
        referenceKind.value = context.referenceKind || 'folder';
        referenceId.value = action === 'unlink' ? context.referenceId : '';
      }
      function resetPreview() {
        const action = selectedAction();
        syncReferenceControls(action);
        invalidatePreview(action, action === 'archive' ? 'Preview' : 'DryRunRequired');
      }
      function fieldChanged() {
        invalidatePreview(selectedAction(), 'DryRunRequired');
      }
      function invalidatePreview(action, state) {
        document.querySelector('[data-testid="maintenance-action-confirm"]').checked = false;
        text('maintenance-action-state', state);
        text('maintenance-action-current-state', currentState(action));
        text('maintenance-action-proposed-state', proposedState(action));
        text('maintenance-action-warning', warningText(action));
        text('maintenance-action-dry-run', 'Preview not yet run');
        text('maintenance-action-feedback', 'idle');
        text('maintenance-action-audit-event', expectedAudit(action));
        refreshSubmit();
      }
      function runDryRun() {
        const action = selectedAction();
        if (action === 'restore' && context.lifecycle !== 'archived') {
          text('maintenance-action-state', 'DryRunBlocked');
          text('maintenance-action-dry-run', 'blocked: restore requires an archived Project');
          text('maintenance-action-feedback', 'invalid_lifecycle');
          refreshSubmit();
          return;
        }
        if (action === 'archive' && context.lifecycle === 'archived') {
          text('maintenance-action-state', 'DryRunBlocked');
          text('maintenance-action-dry-run', 'blocked: project is already archived');
          text('maintenance-action-feedback', 'invalid_lifecycle');
          refreshSubmit();
          return;
        }
        if ((action === 'relink' || action === 'unlink') && !hasReference()) {
          text('maintenance-action-state', 'DryRunBlocked');
          text('maintenance-action-dry-run', 'blocked: reference target is required');
          text('maintenance-action-feedback', 'invalid_reference');
          refreshSubmit();
          return;
        }
        if (action === 'relink' && targetKind() === 'conversation') {
          text('maintenance-action-state', 'DryRunBlocked');
          text('maintenance-action-dry-run', 'blocked: conversation relink remains Conversations-owned');
          text('maintenance-action-feedback', 'unsupported_reference_kind');
          refreshSubmit();
          return;
        }
        if (action === 'relink' && targetKind() === 'file') {
          text('maintenance-action-state', 'DryRunBlocked');
          text('maintenance-action-dry-run', 'blocked: file relink requires transient workspace/path validation outside this payload-safe panel');
          text('maintenance-action-feedback', 'transient_validation_required');
          refreshSubmit();
          return;
        }
        text('maintenance-action-state', 'DryRunPassed');
        text('maintenance-action-dry-run', 'dry-run passed; explicit confirmation required');
        text('maintenance-action-feedback', 'dry_run_passed');
        refreshSubmit();
      }
      function confirmChanged() {
        const confirmed = document.querySelector('[data-testid="maintenance-action-confirm"]').checked;
        if (confirmed && document.querySelector('[data-testid="maintenance-action-state"]').textContent === 'DryRunPassed') {
          text('maintenance-action-state', 'ConfirmationRequired');
        } else if (!confirmed && document.querySelector('[data-testid="maintenance-action-state"]').textContent === 'ConfirmationRequired') {
          text('maintenance-action-state', 'DryRunPassed');
        }
        refreshSubmit();
      }
      function submitAction() {
        if (!canSubmit()) return;
        text('maintenance-action-state', 'Executing');
        text('maintenance-action-feedback', 'submitting');
        window.setTimeout(() => {
          text('maintenance-action-state', 'Succeeded');
          text('maintenance-action-feedback', 'confirmed');
          text('maintenance-action-dry-run', 'confirmed audit audit-001');
          refreshSubmit();
        }, 0);
      }
    </script>
  </head>
  <body onload="resetPreview()">
    <main>
      <section data-testid="maintenance-action-panel" aria-labelledby="maintenance-action-panel-heading">
        <h1 id="maintenance-action-panel-heading">Maintenance actions</h1>
        <label>
          Action
          <select data-testid="maintenance-action-select" onchange="resetPreview()">
            <option data-testid="maintenance-action-archive" value="archive">Archive</option>
            <option data-testid="maintenance-action-restore" value="restore">Restore</option>
            <option data-testid="maintenance-action-relink" value="relink">Relink</option>
            <option data-testid="maintenance-action-unlink" value="unlink">Unlink</option>
            <option data-testid="maintenance-action-reevaluate" value="reevaluate">Re-evaluate</option>
          </select>
        </label>
        <span data-testid="maintenance-reference-controls" hidden>
          <label>
            Reference type
            <select data-testid="maintenance-action-reference-kind" onchange="fieldChanged()">
              <option value="folder">Folder</option>
              <option value="file">File</option>
              <option value="memory">Memory</option>
              <option value="conversation">Conversation</option>
            </select>
          </label>
          <label>
            Reference ID
            <input data-testid="maintenance-action-reference-id" onchange="fieldChanged()" />
          </label>
        </span>
        <dl>
          <div><dt>Panel state</dt><dd data-testid="maintenance-action-state">Preview</dd></div>
          <div><dt>Command lifecycle</dt><dd>Acknowledged(202)</dd></div>
          <div><dt>Tenant scope</dt><dd>server-derived tenant</dd></div>
          <div><dt>Project ID</dt><dd><code>project-001</code></dd></div>
          <div><dt>Target</dt><dd>${referenceKind ? `${referenceKind}:${referenceId}` : 'project:project-001'}</dd></div>
          <div><dt>Current state</dt><dd data-testid="maintenance-action-current-state"></dd></div>
          <div><dt>Proposed state</dt><dd data-testid="maintenance-action-proposed-state"></dd></div>
          <div><dt>Expected audit</dt><dd data-testid="maintenance-action-audit-event"></dd></div>
        </dl>
        <p data-testid="maintenance-action-warning"></p>
        <p data-testid="maintenance-action-dry-run"></p>
        <p>Payload-bearing data is excluded from UI, command evidence, audit rows, exports, and parity descriptors.</p>
        <label>
          <input data-testid="maintenance-action-confirm" type="checkbox" onchange="confirmChanged()" disabled />
          Confirm selected action
        </label>
        <button type="button" data-testid="maintenance-action-dry-run-run" onclick="runDryRun()">Preview / dry-run</button>
        <button type="button" data-testid="maintenance-action-submit" onclick="submitAction()" disabled>Submit</button>
        <p data-testid="maintenance-action-feedback">idle</p>
      </section>
    </main>
  </body>
</html>`;
}
