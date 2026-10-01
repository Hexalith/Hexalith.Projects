# Current G-6 qualification attempts

The selected current pending failed packet is [attempt-3/packet.json](attempt-3/packet.json), SHA-256 `724939520f2d8850073fd0091ffb7f0ada88e87231bb6ff457063bacb562a6bd`. Its [review packet](attempt-3/README.md) records exact source bindings, all actual outcomes, namespace isolation, unchanged shared resources, cleanup, exclusions and rollback limitations. Qualification remains incomplete and `usableAsPrerequisite=false`.

The original first failed packet and `attempt-2/` are preserved. [attempt-1-actual-outcomes-supplement.json](attempt-1-actual-outcomes-supplement.json) corrects the first producer's lost CTRF totals by binding actual sanitized command logs, without changing the retained packet or claiming acceptance. No packet or acceptance status was promoted.

[Post-capture implementation verification](post-capture-implementation-verification.md) records the root closure correction and final checks separately. The retained failed packet is additionally stale against those later implementation bytes. Its source bindings were not rewritten.
