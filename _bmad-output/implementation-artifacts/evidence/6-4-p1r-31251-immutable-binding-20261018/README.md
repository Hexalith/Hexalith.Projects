# 3.125.1 tooling/source binding verification

This is focused tooling evidence. Published qualification and owner selection remain pending.

The implementation agent ran six complete unittest discovery suites with pre-review sources: preparation 43, executor 155, published qualification 65, isolation 31, isolation hardening 27, snapshot registration 2: **323 passed**. Their complete outputs remain in the session tool transcripts. The exact command pattern was `python3 -m unittest discover -s tools/tests -p NAME`, with each corresponding `test_p1r_*.py` filename.

The primary agent retained named verification output for **18 focused matrix/legacy tests** and **27 optimized isolation-hardening tests**. Exact commands, exit codes and logs are in [commands.json](commands.json). These outputs directly prove executed coverage for:

| Approved matrix case | Retained coverage |
| --- | --- |
| Clean separate tooling/source identities | qualification, executor and packet logs: distinct inventories, both CLI paths, packet creation/validation |
| Wrong pins, dirty/staged/added inputs | qualification and executor logs: every repository role, ignored code/configuration, pre-operational refusal |
| Changed or redirected release source | qualification/executor logs: source pin/path substitutions and evaluated source-reference checks |
| Missing or replaced release tag | qualification log: tag substitution cases |
| Substituted Builds or imported catalog | qualification/executor logs: Builds roles, catalog and effective-version checks |
| Removed/swapped runtime binding after rehashing | packet/isolation logs: packet, lane, finalizer and importer refusals |
| Historical guard and unknown successors | legacy-binding, executor and packet logs: unchanged equality guard and bounded successor admission |

The isolation logs also prove post-start drift remains a failure while repeated owned cleanup succeeds. Generated bin/obj and invocation evidence copies have positive controls.

The required Aspire startup check ran after the initial tooling helper edit. Command: `aspire start --non-interactive --apphost src/Hexalith.EventStore.AppHost/Hexalith.EventStore.AppHost.csproj`. Exit 2; build succeeded with zero warnings/errors, then AppHost exited 134 because the nested Tenants and Tenants.Api projects were absent. No nested submodules were initialized. Original logs: `/home/administrator/.aspire/logs/cli_20261018T171337_087626da.log` and `/home/administrator/.aspire/logs/cli_20261018T171337960_detach-child_8ed23fb281674f768fc89f99214103df.log`. The sequencing miss is recorded here; this was not a pre-edit startup baseline.

Primary diff audit: runtime source/deploy files, build/catalog inputs, root and owner HEADs, gitlinks, frozen intent, Story 6.4, sprint state and P1R acceptance retain their baseline content/identities. No production qualification or final tooling selection was performed.

## Final verification after review fixes

The primary agent ran all six suites again with verbose output after the review patches: preparation **46**, executor **157**, published qualification **67**, isolation **32**, isolation hardening **27**, snapshot registration **3**: **332 passed, zero skips**. Optimized Python isolation hardening also passed **27** tests. [Exact commands, counts, exit codes and timings](final/commands.json); complete native logs are beside that file. [Final changed-source hashes](final/sources-sha256.json) identify the reviewed files. These final outputs supersede the earlier verification observations for the completed change.

All sixteen individual review findings were recorded, grouped into thirteen patch causes and addressed. Final tests cover hidden index flags, canonical newline handling, native modules, embedded repositories, known connected source closure, actual executor output roots, exact MSBuild commands and allowed imports, post-restore reevaluation, runtime workspace containment, observation-byte stability, emitted tooling pins, real MSBuild evaluation without restore, and distinct PostgreSQL templates. No findings were deferred.

The AppHost startup limitation above remains. Final review and tests create no owner selection, published qualification or downstream acceptance. Changes remain uncommitted under the approved prohibition on Git-ref updates.
