# 6.1-P1R `3.97.0` stopped qualification attempt

This bundle retains the first results from the 2026-08-24 P1R attempt. It is
failure evidence only. It is not a qualification packet, owner acceptance,
Architecture authority, publication authority, or permission to close P1R or
any downstream gate.

## Completed before the stop

- Builds candidate commit
  `fb05dd84625abdcd1a62d2664e8557379fd631bb` was created locally on
  `fix/p1r-397-candidate` from
  `2f46aaee2ecb0b3f121d50ab8cc58601901046f4`.
- Candidate runner, schema, active fixtures, serialized evidence, package
  qualification assertions, and their coupled evidence hashes were aligned to
  `3.97.0`. The explicit `3.88.0` and `3.70.1` negative manifests were
  preserved with exact `HXM016` expectations; the invalid-profile control
  retained `HXM009`; the deliberate all-`F` evidence hash remained invalid.
- Static parity, the authoritative 49-identity catalog contract, and the full
  284-package/139-family/one-source audit passed.
- The exact candidate commit message passed repository-pinned commitlint before
  and after the commit.

## First non-passing row

`es-001-coordinates` ran from the isolated EventStore worktree at
`94591f3539ce30372db58e5fdd3ba017ea8c07b8` from
`2026-08-24T16:34:36.095461306Z` to
`2026-08-24T16:34:36.633336524Z`. It exited `2` and is `FAIL`.

The evidence wrapper was invoked through an outer double-quoted shell argument.
That outer shell evaluated the two intended inner `$(...)` assertions before
the wrapper changed into the EventStore worktree. The retained command
therefore contains `test -z` and `test  -eq 14`; the latter failed with
`bash: line 1: test: -eq: unary operator expected`. The exact retained failed
log SHA-256 is
`eb0a2e11dc9bbb2bd4e94377b8a8c544c33f9a5dcc588da63e39a3411a8f1a8e`.

Before the malformed assertion stopped the row, the log observed local tag
`v3.97.0` at
`94591f3539ce30372db58e5fdd3ba017ea8c07b8` and emitted the 14-ID release
manifest. Those partial observations do not qualify the coordinate phase. The
retained manifest SHA-256 is
`6b0b70b856839d4117bcd969f6a2de0093c477c109cb79f3f2882b1f05effcae`.

## Required next action

An authorized owner must start a new clean qualification attempt with a
corrected argument boundary that preserves inner command substitutions until
execution inside the recorded EventStore worktree. The new attempt must retain
this failed row, begin again at coordinate capture, and run no later phase
unless that new coordinate gate passes. The rollback commit, EventStore lanes,
remote 14-package restore, Builds qualification, reciprocal rollback, and all
four owner decisions remain pending.
