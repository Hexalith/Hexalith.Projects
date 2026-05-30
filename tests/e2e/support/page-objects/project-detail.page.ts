import type { Page, Locator } from '@playwright/test';

/**
 * Optional Page Object for the Project Detail console view (UX-DR7 / UX-DR13).
 *
 * Page objects are optional in this workspace — prefer inline `page.getByTestId(...)` for
 * short journeys. Use a POM only when a view's interactions are reused across several specs.
 * All selectors are role/label-based `data-testid` (UX-DR28) and survive FrontComposer
 * regeneration as long as the generated component keys are preserved.
 */
export class ProjectDetailPage {
  readonly page: Page;
  readonly diagnosticHeader: Locator;
  readonly name: Locator;
  readonly lifecycleBadge: Locator;
  readonly tenantCopy: Locator;
  readonly projectIdCopy: Locator;
  readonly emptyState: Locator;
  readonly feedbackRegion: Locator;
  readonly archiveAction: Locator;
  readonly archiveDryRun: Locator;
  readonly archiveConfirm: Locator;

  constructor(page: Page) {
    this.page = page;
    this.diagnosticHeader = page.getByTestId('project-diagnostic-header');
    this.name = page.getByTestId('project-detail-name');
    this.lifecycleBadge = page.getByTestId('project-lifecycle-badge');
    this.tenantCopy = page.getByTestId('project-copy-tenant-scope');
    this.projectIdCopy = page.getByTestId('project-copy-project-id');
    this.emptyState = page.getByTestId(/^project-empty-/);
    this.feedbackRegion = page.getByTestId(/^project-feedback-/);
    this.archiveAction = page.getByTestId('action-archive');
    this.archiveDryRun = page.getByTestId('action-archive-dry-run');
    this.archiveConfirm = page.getByTestId('action-archive-confirm');
  }

  async goto(projectId: string): Promise<void> {
    await this.page.goto(`/projects/${projectId}`);
    await this.diagnosticHeader.waitFor({ state: 'visible' });
  }
}
