# Story Automator Learnings

## Run: 2026-05-25T21:04:43Z

**Epic:** Project Workspace Foundation
**Stories:** 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9

### Patterns Observed

- Contracts-first sequencing kept later API, client, aggregate, and projection work aligned.
- High-risk invariants became reusable harnesses: no-payload leakage, tenant isolation, projection rebuild/replay, schema tolerance, and trust-state checks.
- The Windows run required replacing tmux orchestration with direct Codex subagents while preserving the same create, dev, review, commit, and retrospective checkpoints.
- Story status was necessary but not sufficient for release readiness; deployment and stakeholder acceptance remain external validations.

### Code Review Insights

- Common issues: fail-closed authorization evidence, safe-denial leakage, generated-client whitespace, idempotency parity, request schema enforcement, projection conformance, durable projection watermarking, and Dapr journal conflict handling.
- Average cycles to clean: 13 review cycles across 9 stories.
- The review loop produced material fixes in security, idempotency, runtime registration, and durable projection behavior.

### Recommendations for Future Runs

- Keep using direct subagent orchestration on Windows; do not depend on tmux.
- Keep `.codex/` local configuration out of story commits.
- Add direct authorization-gate proof for every new Epic 2 read/write surface.
- Record external dependency choices in the relevant story records before implementation.
- Reuse Epic 1 conformance harnesses in Epic 2 story templates.
