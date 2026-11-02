# Verified public release 1.2.3

- Published source: e6eb06a87f1a924c8815306683c6299c41d5c3d4; tag v1.2.3 resolves exactly to it.
- Release workflow: https://github.com/Hexalith/Hexalith.Projects/actions/runs/41380245331 (success).
- Root CI: https://github.com/Hexalith/Hexalith.Projects/actions/runs/41376917554 (all five enabled jobs pass, including G6).
- All required selected-source EventStore and Tenants workflows pass; actual owner source requires six reads, Admin proxy/owner read, refusal, persisted Redis and zero leases.
- Release independently runs and validates fresh G6. Five approved raw G6 JSON records are retained unchanged.
- GitHub release has five assets; all five public NuGet indexes expose 1.2.3; actual signed nupkgs and hashes are retained.
- Strict manifest/version/source validation passes without normalization; all five repository signatures verify; all five isolated real-API consumers compile without project fallback.
- Publication variable is refrozen false and independently read back; production has no required human-review rule.
- Stable SDK is 10.0.403; Projects consumes current released EventStore3.132.0; explicit framework/API compatibility holds and upstream audit findings remain documented.
- All12 root-declared modules are on their pushed main snapshots with clean trees; nested submodules remain uninitialized.
