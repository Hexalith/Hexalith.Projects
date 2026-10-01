# Superseded G-6 attempt 10

Superseded [packet](packet.json), normalized-LF SHA-256 `865fa27083b6ef16907b2735116899a68b3a898c82911a93f8b0263fb1771adf`. This retained attempt is rejected and supplies no acceptance or prerequisite usability.

All 22 commands passed. Candidate validation rejected ownedProcessesStopped=false: the old cleanup loop did not verify disappearance after stopping an owned group. A subsequent independent check found no remaining members of the recorded groups; the captured receipt remains unchanged.

Captured packet fields: status `pending`, technicalValidity `true`, usableAsPrerequisite `false`. Those original bytes and claims are preserved. Actual command outcomes remain in [command-record.json](command-record.json): 22 recorded commands, 0 nonzero outcomes. Runner errors remain in [attempts.json](attempts.json).

[Cleanup](cleanup.json): 4 exact owned container identities and 4 removals; shared snapshots match `true`. Original cleanup flags: `{"fixtureScratchRemoved": true, "ownedProcessesStopped": false, "scratchRemoved": true}`. No old receipt is rewritten.
