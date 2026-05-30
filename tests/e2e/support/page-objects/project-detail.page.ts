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
  readonly inventoryGrid: Locator;
  readonly inventoryRows: Locator;
  readonly inventoryLifecycleFilter: Locator;
  readonly inventoryUpdatedFilter: Locator;
  readonly inventoryWarningFilter: Locator;
  readonly inventoryReasonCodeFilter: Locator;
  readonly inventoryReferenceTypeFilter: Locator;
  readonly inventoryEmpty: Locator;
  readonly inspector: Locator;
  readonly metadataSection: Locator;
  readonly setupSection: Locator;
  readonly referencesSection: Locator;
  readonly referenceHealthMatrix: Locator;
  readonly referenceHealthRows: Locator;
  readonly referenceKindCells: Locator;
  readonly referenceOwnerCells: Locator;
  readonly referenceStateCells: Locator;
  readonly referenceReasonCells: Locator;
  readonly referenceLastCheckedCells: Locator;
  readonly referenceSafeActionCells: Locator;
  readonly resolutionSection: Locator;
  readonly auditSection: Locator;
  readonly actionsSection: Locator;
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
    this.inventoryGrid = page.getByTestId('project-inventory-grid');
    this.inventoryRows = page.getByTestId('project-inventory-row');
    this.inventoryLifecycleFilter = page.getByTestId('project-inventory-filter-lifecycle');
    this.inventoryUpdatedFilter = page.getByTestId('project-inventory-filter-updated');
    this.inventoryWarningFilter = page.getByTestId('project-inventory-filter-warning');
    this.inventoryReasonCodeFilter = page.getByTestId('project-inventory-filter-reason-code');
    this.inventoryReferenceTypeFilter = page.getByTestId('project-inventory-filter-reference-type');
    this.inventoryEmpty = page.getByTestId('project-inventory-empty');
    this.inspector = page.getByTestId('project-detail-inspector');
    this.metadataSection = page.getByTestId('project-detail-section-metadata');
    this.setupSection = page.getByTestId('project-detail-section-setup');
    this.referencesSection = page.getByTestId('project-detail-section-references');
    this.referenceHealthMatrix = page.getByTestId('project-reference-health-matrix');
    this.referenceHealthRows = page.getByTestId('project-reference-health-row');
    this.referenceKindCells = page.getByTestId('project-reference-kind');
    this.referenceOwnerCells = page.getByTestId('project-reference-owner');
    this.referenceStateCells = page.getByTestId('project-reference-state');
    this.referenceReasonCells = page.getByTestId('project-reference-reason');
    this.referenceLastCheckedCells = page.getByTestId('project-reference-last-checked');
    this.referenceSafeActionCells = page.getByTestId('project-reference-safe-actions');
    this.resolutionSection = page.getByTestId('project-detail-section-resolution');
    this.auditSection = page.getByTestId('project-detail-section-audit');
    this.actionsSection = page.getByTestId('project-detail-section-actions');
    this.archiveAction = page.getByTestId('action-archive');
    this.archiveDryRun = page.getByTestId('action-archive-dry-run');
    this.archiveConfirm = page.getByTestId('action-archive-confirm');
  }

  async goto(projectId: string): Promise<void> {
    await this.page.goto(`/projects/${projectId}`);
    await this.diagnosticHeader.waitFor({ state: 'visible' });
  }

  async gotoInventory(): Promise<void> {
    await this.page.goto('/projects');
    await this.inventoryGrid.or(this.inventoryEmpty).or(this.feedbackRegion).waitFor({ state: 'visible' });
  }
}
