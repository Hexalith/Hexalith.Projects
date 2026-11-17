# Fixed owner delivery implementation

The EventStore proposal helper publishes only
`refs/heads/cicd/contracts-prerequisite-eventstore`. Its push uses the exact
empty expected-ref lease `--force-with-lease=refs/heads/cicd/contracts-prerequisite-eventstore:`.
This conditional create-only operation atomically requires the ref to be
absent. An existing ref is rejected even when it points to an ancestor of the
candidate. The helper never constructs a nonempty lease or a force overwrite.
The exact reviewed main, complete tree, direct parent, token scope and owner
source guards remain required.

Actions rerun initializers do not restore prior checkpoints. On
`GITHUB_RUN_ATTEMPT > 1`, a new branch creation or PR POST requires a preserved
checkpoint bound to the same run, owner source, operation, fixed ref and plan,
originating from the first attempt and proving that write was not attempted.
Without that evidence the helper permits read-only observation of the exact
existing branch or draft PR and refuses new writes. A refusal cannot authorize
a later retry. An uncertain PR POST remains an attempted write and is never
automatically repeated.

Qualification and writer receipts, including the fixed comparison URL, are
retained for coordinator inspection. This implementation does not add a
checkpoint restoration workflow or request additional App permissions.
