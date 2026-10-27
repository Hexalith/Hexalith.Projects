# Minimal-module platform compatibility

This CI-only compatibility suite compiles and runs the frozen Story 11.2 fixture against
the external package compatibility selection in `RegisteredCommand.targets` (EventStore
3.128.0, matching the selected Builds catalog). It runs the original six tests and
adds one registered-command assertion through the public in-process domain router.
The assertion resolves the keyed processor, checks the serialized creation event,
replays it through the public in-process router in a fresh service scope, and checks
the successful replay sequence and serialized state. It then supplies a detached JSON
state snapshot to a second registered command in another scope and requires no emitted
event. That no-op observes rehydration of the `Created` flag through the SDK; it does not
observe `Title` after rehydration or establish persistence. The serialized title is
checked at the replay output boundary.

The external `RegisteredCommand.targets` import adds the assertion only to the fixture
test project. With `MinimalModuleCompatibilityMode=package`, its initial target replaces
only external EventStore project edges with exact package references. Internal fixture
project edges stay intact. A separate read-only MSBuild catalog evaluation compares
that deliberately qualified selection with the direct pinned Builds catalog and refuses
drift before restore/build; it never refreshes the successor locks. The compatibility
job initializes only that direct Builds checkout, keeping all EventStore edges as packages.
`MinimalModuleCompatibilityUseSuccessorLocks=true` selects
`locks/package/<project>.packages.lock.json` for the four fixture projects. Omitting
these opt-ins retains the original fixture graph and original lock selection.

All 17 measured fixture files, including four original locks, remain byte-for-byte
unchanged. Historical authoring-cost records and receipts remain unchanged. This
compatibility build is a separate current package graph and is not a regenerated
measured authoring sample. Keep the fixture outside the root solution so its isolated
xUnit and package configuration applies.

The `ci / minimal-module platform compatibility` job restores packages only, builds
Release and executes all seven tests without launching the compiled AppHost. It needs
the pinned .NET SDK, direct Builds catalog and NuGet access; no EventStore source checkout, Docker daemon,
Dapr CLI, sidecar, or external state store is needed. It deliberately does not inherit
the product's producer mapping imports.

The original source graph's locked restore reproduced NU1004 for the changed EventStore
ServiceDefaults/Aspire dependencies on 2026-10-27. The successor locks were deliberately
generated once with the explicit package overlay and `--force-evaluate`, then verified
with `--locked-mode`. CI always uses locked mode and never regenerates them. A future
dependency change must fail until the external compatibility selection and successor
locks are deliberately qualified again. All original six fixture assertions and the
registered-command assertion remain required by `verify_results.py`.

Reproduce with the exact restore/build/test commands in `.github/workflows/ci.yml`.
Pass the same absolute `CustomAfterMicrosoftCommonTargets` path and both explicit
compatibility properties to restore and build. Run the built test DLL directly, then
run `verify_results.py` on its XML. Local source debugging uses Debug and the original
source graph; its historical locks are frozen and may refuse current dependencies.
An umbrella's ancestor `.editorconfig` can add diagnostics absent in a standalone
Conversations checkout; qualification evidence records that failure separately and
verifies the identical fixture bytes in the standalone CI layout without suppressions.
