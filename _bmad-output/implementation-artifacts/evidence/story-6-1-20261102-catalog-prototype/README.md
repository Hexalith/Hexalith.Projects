# Story 6.1 catalog prototype diagnostic — 2026-11-02

Status: **failed; diagnostic only**. This run tested an uncommitted Projects-owned Development route-catalog prototype. The prototype was removed because the approved Story 6.1 scope excludes enrollment. These results do not qualify the current tree or satisfy Story 6.1 acceptance.

## Tested source and command

- Projects HEAD: `96cf96ccafd472871c72aeaf07dd2e7aed729e23`, plus a temporary prototype. [run.json](run.json) records its source-manifest digest as `9cfcfef5b721252229c39ba014157a0c091468d2cabfec0b87ea22e2aee1f5a6`; the prototype source is not retained in this packet.
- EventStore: `a3dbec2f8b6150a3ee33dcbfc2b6bf2fec274625`; Tenants: `4d0d599879767196928e1c265622483a62b457ae`; Folders: `0e976ae267c2bad0f9559f41e87b52e00c3acc2e`. The report records the remaining root-declared revisions.

```bash
MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_PROCESSOR_COUNT=2 python3 tools/qualification/run_supported_project_reads.py --evidence /tmp/story-6-1-bound-tenants-attempt16-20261102 --timeout 900
```

The launcher controls passed **63/63**. The catalog committed on EventStore, Tenants, Projects, and Folders. The native [distributed.xml](distributed.xml) records **0 passed, 1 failed, 0 skipped**: Tenants' bound global-administrator bootstrap was refused with HTTP 500. The sanitized report records Dapr `ERR_DIRECT_INVOKE`, `matrixCompleted=false`, EventStore `/ready` at 503, and complete owned-resource cleanup with preexisting resources preserved. The isolated sidecars did not have mTLS caller identity; a Dapr ACL diagnostic showed an empty caller app ID for Tenants to EventStore. A closed ACL could not authorize that invocation. No persisted list/open matrix ran.

The authenticated Dapr identity topology and route-catalog enrollment belong outside the approved Story 6.1 scope. This packet records a failed diagnostic run; it is not a feature acceptance claim.
