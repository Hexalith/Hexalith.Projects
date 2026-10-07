import { faker } from '@faker-js/faker';

/**
 * Project create-input factory (metadata only — FR-1 / FR-19).
 *
 * The ONLY required user input is the project name; description and durable setup are
 * optional. NEVER include forbidden sibling-owned content (transcripts, file contents,
 * memory bodies, secrets, raw tokens, unrestricted paths) — those are rejected by setup
 * validation and asserted absent by the NoPayloadLeakage harness (NFR-2 / FS-1 / FS-2).
 */

export interface CreateProjectInput {
  /** Optional caller-owned deterministic identity for live-fixture isolation. */
  projectId?: string;
  /** Required: the project name. */
  name: string;
  /** Optional human-readable description. */
  description?: string;
  /** Optional durable setup metadata. */
  setupMetadata?: string;
}

export const createProjectInput = (overrides: Partial<CreateProjectInput> = {}): CreateProjectInput => ({
  name: metadataSafe(`${faker.commerce.productAdjective()} ${faker.commerce.department()} Project ${faker.string.alphanumeric(6)}`),
  // Free-text faker phrases (catch-phrases such as "content-based", lorem words) can contain the
  // payload markers that metadata-only specs assert absent; hex suffixes cannot spell any of them.
  description: `Live E2E description ${faker.string.hexadecimal({ length: 12, casing: 'lower', prefix: '' })}`,
  setupMetadata: `Live E2E setup ${faker.string.hexadecimal({ length: 12, casing: 'lower', prefix: '' })}`,
  ...overrides,
});

/**
 * Keeps generated positive metadata inside the Projects safe-metadata profile. Faker phrases such as
 * "24/7" contain path separators, which the command validator rightly rejects as unrestricted paths;
 * an unsanitized phrase makes roughly one seeded Project in eighty fail at random.
 */
export function metadataSafe(text: string): string {
  return text.replace(/[\\/:]+/g, ' ').replace(/\.{2,}/g, '.').replace(/\s+/g, ' ').trim();
}

/** A minimal create input exercising the "name is the only required field" path (FR-1). */
export const createMinimalProjectInput = (overrides: Partial<CreateProjectInput> = {}): CreateProjectInput => ({
  name: `Minimal Project ${faker.string.alphanumeric(6)}`,
  ...overrides,
});

/**
 * An INVALID setup payload for FR-19 negative tests: a raw secret + an unrestricted
 * local path. The aggregate must reject these and name the offending field WITHOUT
 * echoing its value. Used only to assert rejection — never as a positive fixture.
 */
export const createForbiddenSetupInput = (overrides: Partial<CreateProjectInput> = {}): CreateProjectInput => ({
  name: `Rejected Project ${faker.string.alphanumeric(6)}`,
  // Deliberately forbidden content — must be rejected, never persisted or logged.
  setupMetadata: 'AWS_SECRET_ACCESS_KEY=AKIAFAKEFAKEFAKE12345 and path C:\\Users\\admin\\secrets.txt',
  ...overrides,
});
