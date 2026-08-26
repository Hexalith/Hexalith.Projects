import { createHash } from 'node:crypto';

const MAX_IDENTIFIER_LENGTH = 96;

export interface LiveFixtureDimensions {
  runId: string;
  workerIndex: number;
  retry: number;
  repeatEachIndex: number;
  scenario: string;
}

export interface LiveFixtureIdentities extends LiveFixtureDimensions {
  graphId: string;
  projectId: string;
  secondaryProjectId: string;
  proposalProjectId: string;
  proposalRetryProjectId: string;
  conversationId: string;
  ambiguousConversationId: string;
  existingConversationId: string;
  folderId: string;
  secondaryFolderId: string;
  proposalFolderId: string;
  workspaceId: string;
  fileReferenceId: string;
  secondaryFileReferenceId: string;
  proposalFileReferenceId: string;
  deniedFileReferenceId: string;
  memoryReferenceId: string;
  correlationId: string;
  taskId: string;
  idempotencyKey: string;
}

/**
 * Builds deterministic, bounded, URL-safe identities for one logical Playwright attempt.
 * Every live entity and request identity is rooted in all isolation dimensions.
 */
export function createLiveFixtureIdentities(dimensions: LiveFixtureDimensions): LiveFixtureIdentities {
  validateDimensions(dimensions);
  const root = [
    dimensions.runId,
    dimensions.workerIndex,
    dimensions.retry,
    dimensions.repeatEachIndex,
    dimensions.scenario,
  ].join('|');
  const id = (kind: string): string => boundedId(kind, `${root}|${kind}`);

  return {
    ...dimensions,
    graphId: id('graph'),
    projectId: id('project'),
    secondaryProjectId: id('project-secondary'),
    proposalProjectId: id('project-proposal'),
    proposalRetryProjectId: id('project-proposal-retry'),
    conversationId: id('conversation'),
    ambiguousConversationId: id('conversation-ambiguous'),
    existingConversationId: id('conversation-existing'),
    folderId: id('folder'),
    secondaryFolderId: id('folder-secondary'),
    proposalFolderId: id('folder-proposal'),
    workspaceId: id('workspace'),
    fileReferenceId: id('file'),
    secondaryFileReferenceId: id('file-secondary'),
    proposalFileReferenceId: id('file-proposal'),
    deniedFileReferenceId: id('file-denied'),
    memoryReferenceId: id('memory'),
    correlationId: id('correlation'),
    taskId: id('task'),
    idempotencyKey: id('idempotency'),
  };
}

/** Rejects accidentally reused attempt dimensions before parallel live work starts. */
export function assertDisjointLiveFixtureDimensions(dimensions: readonly LiveFixtureDimensions[]): void {
  const seen = new Set<string>();
  for (const item of dimensions) {
    validateDimensions(item);
    const key = `${item.runId}|${item.workerIndex}|${item.retry}|${item.repeatEachIndex}|${item.scenario}`;
    if (seen.has(key)) {
      throw new Error(`[live-fixture-identities] duplicate fixture dimensions: ${key}`);
    }
    seen.add(key);
  }
}

function boundedId(kind: string, source: string): string {
  const safeKind = kind.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
  const digest = createHash('sha256').update(source, 'utf8').digest('hex').slice(0, 40);
  return `${safeKind}-${digest}`.slice(0, MAX_IDENTIFIER_LENGTH);
}

function validateDimensions(dimensions: LiveFixtureDimensions): void {
  if (!dimensions.runId.trim() || !dimensions.scenario.trim()) {
    throw new Error('[live-fixture-identities] runId and scenario are required.');
  }
  for (const [name, value] of [
    ['workerIndex', dimensions.workerIndex],
    ['retry', dimensions.retry],
    ['repeatEachIndex', dimensions.repeatEachIndex],
  ] as const) {
    if (!Number.isSafeInteger(value) || value < 0) {
      throw new Error(`[live-fixture-identities] ${name} must be a non-negative safe integer.`);
    }
  }
}
