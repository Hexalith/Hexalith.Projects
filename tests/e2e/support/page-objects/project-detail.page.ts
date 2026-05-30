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
  readonly warningsDashboard: Locator;
  readonly dashboardTiles: Locator;
  readonly warningsQueue: Locator;
  readonly warningRows: Locator;
  readonly warningState: Locator;
  readonly warningReason: Locator;
  readonly warningReference: Locator;
  readonly warningFreshness: Locator;
  readonly warningSafeAction: Locator;
  readonly warningStateFilter: Locator;
  readonly warningReasonFilter: Locator;
  readonly warningReferenceTypeFilter: Locator;
  readonly warningLifecycleFilter: Locator;
  readonly warningEmpty: Locator;
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
  readonly resolutionTraceWorkbench: Locator;
  readonly resolutionTraceMode: Locator;
  readonly resolutionTraceConversationId: Locator;
  readonly resolutionTraceFolderId: Locator;
  readonly resolutionTraceFileId: Locator;
  readonly resolutionTraceIncludeArchived: Locator;
  readonly resolutionTraceRun: Locator;
  readonly resolutionTraceOutcome: Locator;
  readonly resolutionTraceInputSummary: Locator;
  readonly resolutionTraceCandidates: Locator;
  readonly resolutionTraceCandidateComparison: Locator;
  readonly resolutionTraceReasons: Locator;
  readonly resolutionTraceExclusions: Locator;
  readonly resolutionTraceFeedback: Locator;
  readonly auditSection: Locator;
  readonly auditTimeline: Locator;
  readonly auditTimelineEntries: Locator;
  readonly auditTimelineOperation: Locator;
  readonly auditTimelineStateDelta: Locator;
  readonly auditTimelineReference: Locator;
  readonly auditTimelineActor: Locator;
  readonly auditTimelineCorrelationId: Locator;
  readonly auditTimelineTaskId: Locator;
  readonly auditTimelineEventId: Locator;
  readonly auditTimelineCopy: Locator;
  readonly auditTimelineFeedback: Locator;
  readonly safeDiagnosticExport: Locator;
  readonly safeDiagnosticExportPreview: Locator;
  readonly safeDiagnosticExportGuarantee: Locator;
  readonly safeDiagnosticExportCopy: Locator;
  readonly safeDiagnosticExportDownload: Locator;
  readonly safeDiagnosticExportFeedback: Locator;
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
    this.inventoryWarningFilter = page.getByTestId('project-warning-filter-state');
    this.inventoryReasonCodeFilter = page.getByTestId('project-warning-filter-reason');
    this.inventoryReferenceTypeFilter = page.getByTestId('project-warning-filter-reference-type');
    this.inventoryEmpty = page.getByTestId('project-inventory-empty');
    this.warningsDashboard = page.getByTestId('project-warnings-dashboard');
    this.dashboardTiles = page.getByTestId('project-dashboard-tile');
    this.warningsQueue = page.getByTestId('project-warnings-queue');
    this.warningRows = page.getByTestId('project-warning-row');
    this.warningState = page.getByTestId('project-warning-state');
    this.warningReason = page.getByTestId('project-warning-reason');
    this.warningReference = page.getByTestId('project-warning-reference');
    this.warningFreshness = page.getByTestId('project-warning-freshness');
    this.warningSafeAction = page.getByTestId('project-warning-safe-action');
    this.warningStateFilter = page.getByTestId('project-warning-filter-state');
    this.warningReasonFilter = page.getByTestId('project-warning-filter-reason');
    this.warningReferenceTypeFilter = page.getByTestId('project-warning-filter-reference-type');
    this.warningLifecycleFilter = page.getByTestId('project-warning-filter-lifecycle');
    this.warningEmpty = page.getByTestId('project-warning-empty');
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
    this.resolutionTraceWorkbench = page.getByTestId('project-resolution-trace-workbench');
    this.resolutionTraceMode = page.getByTestId('project-resolution-trace-mode');
    this.resolutionTraceConversationId = page.getByTestId('project-resolution-trace-conversation-id');
    this.resolutionTraceFolderId = page.getByTestId('project-resolution-trace-folder-id');
    this.resolutionTraceFileId = page.getByTestId('project-resolution-trace-file-id');
    this.resolutionTraceIncludeArchived = page.getByTestId('project-resolution-trace-include-archived');
    this.resolutionTraceRun = page.getByTestId('project-resolution-trace-run');
    this.resolutionTraceOutcome = page.getByTestId('project-resolution-trace-outcome');
    this.resolutionTraceInputSummary = page.getByTestId('project-resolution-trace-input-summary');
    this.resolutionTraceCandidates = page.getByTestId('project-resolution-trace-candidate');
    this.resolutionTraceCandidateComparison = page.getByTestId('project-resolution-trace-candidate-comparison');
    this.resolutionTraceReasons = page.getByTestId('project-resolution-trace-reason');
    this.resolutionTraceExclusions = page.getByTestId('project-resolution-trace-exclusion');
    this.resolutionTraceFeedback = page.getByTestId('project-resolution-trace-feedback');
    this.auditSection = page.getByTestId('project-detail-section-audit');
    this.auditTimeline = page.getByTestId('audit-timeline');
    this.auditTimelineEntries = page.getByTestId('audit-timeline-entry');
    this.auditTimelineOperation = page.getByTestId('audit-timeline-operation');
    this.auditTimelineStateDelta = page.getByTestId('audit-timeline-state-delta');
    this.auditTimelineReference = page.getByTestId('audit-timeline-reference');
    this.auditTimelineActor = page.getByTestId('audit-timeline-actor');
    this.auditTimelineCorrelationId = page.getByTestId('audit-timeline-correlation-id');
    this.auditTimelineTaskId = page.getByTestId('audit-timeline-task-id');
    this.auditTimelineEventId = page.getByTestId('audit-timeline-event-id');
    this.auditTimelineCopy = page.getByTestId('audit-timeline-copy');
    this.auditTimelineFeedback = page.getByTestId('audit-timeline-feedback');
    this.safeDiagnosticExport = page.getByTestId('safe-diagnostic-export');
    this.safeDiagnosticExportPreview = page.getByTestId('safe-diagnostic-export-preview');
    this.safeDiagnosticExportGuarantee = page.getByTestId('safe-diagnostic-export-guarantee');
    this.safeDiagnosticExportCopy = page.getByTestId('safe-diagnostic-export-copy');
    this.safeDiagnosticExportDownload = page.getByTestId('safe-diagnostic-export-download');
    this.safeDiagnosticExportFeedback = page.getByTestId('safe-diagnostic-export-feedback');
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
    await this.warningsDashboard.or(this.inventoryGrid).or(this.inventoryEmpty).or(this.feedbackRegion).waitFor({ state: 'visible' });
  }

  async gotoWarnings(): Promise<void> {
    await this.page.goto('/projects/warnings');
    await this.warningsDashboard.or(this.warningEmpty).or(this.feedbackRegion).waitFor({ state: 'visible' });
  }
}
