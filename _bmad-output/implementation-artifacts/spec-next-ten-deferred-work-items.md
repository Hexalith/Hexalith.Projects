---
title: 'Complete the next ten actionable Projects deferred items'
type: 'chore'
created: '2026-11-24'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete ten actionable entries in ledger order. Verify each result, record its disposition in the ledger, and commit each completed entry separately. Record blockers and obsolete entries without counting them as completed work. Run the full build and test suite before pushing directly to `main`.

</frozen-after-approval>

## Implementation Notes

- The ledger combines numbered entries with unnumbered findings. Process their order within the file and count the original independent obligations. Verification-only entries need evidence against the exact relevant source or runtime.
- Start from Projects `6cd73dca5bb90076e809bf2a9fa28ce96e8c233b` on `main`.

### Partial direct dependency checkout

The disposable probe used the Memories build properties and compiled `RepositoryProjectPaths` from owner revision `667fbf2564ee2a1a30487e24cba20b37eebb76d0` under SDK 11.0.100. The synthetic umbrella declared Memories and EventStore as direct siblings. Inside Memories, a partial direct EventStore checkout had an Aspire project directory and lacked the host project. The sibling had both project files.

`dotnet msbuild Probe.csproj -getProperty:HexalithEventStoreRoot,HexalithEventStoreFromSource -p:Configuration=Debug -p:UseHexalithProjectReferences=true` selected the partial direct root. With the direct Aspire project file absent it left `HexalithEventStoreFromSource` empty. Adding that file changed the property to `true`. In both layouts, executing `RepositoryProjectPaths.ResolveReferencedModuleProjectPath` for the host returned the umbrella sibling. This verifies the selection mismatch. The owner still needs to make the build and runtime choose one coherent checkout.
