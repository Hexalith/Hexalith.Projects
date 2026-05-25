import { faker } from '@faker-js/faker';

/**
 * Tenant context factory. Drives the canonical `{tenant}:projects:{projectId}` identity
 * (AR-4) and the cross-tenant isolation suite (FS-8): tests that need two distinct tenants
 * call `createTenantContext()` twice.
 *
 * Metadata only — never embed sibling-owned payloads here (NFR-2 / FS-1).
 */
export interface TenantContext {
  /** Stable tenant identifier (the EventStore envelope tenant for project data). */
  tenantId: string;
  /** Display name for assertions/diagnostics — not a security boundary. */
  displayName: string;
}

export const createTenantContext = (overrides: Partial<TenantContext> = {}): TenantContext => ({
  tenantId: `tenant-${faker.string.alphanumeric(10).toLowerCase()}`,
  displayName: faker.company.name(),
  ...overrides,
});

/** Two guaranteed-distinct tenants for cross-tenant negative tests (R1 / FS-8). */
export const createDistinctTenantPair = (): readonly [TenantContext, TenantContext] => {
  const a = createTenantContext();
  let b = createTenantContext();
  while (b.tenantId === a.tenantId) b = createTenantContext();
  return [a, b] as const;
};
