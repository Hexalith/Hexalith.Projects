# External review prompt: adversarial general

Invoke the `bmad-review-adversarial-general` skill on the complete workspace
diff for P3.

Baseline commit: `d4a69ad9a640294e849444a60d7ddfbd0468f91a`

Review the tracked diff with:

```bash
git diff d4a69ad9a640294e849444a60d7ddfbd0468f91a
```

Also inspect these untracked files, which are part of the change:

- `docs/runbooks/projects-production-identity-contract.md`
- `src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationOptions.cs`
- `src/Hexalith.Projects.Server/Authentication/ProjectsAuthenticationServiceCollectionExtensions.cs`
- `src/Hexalith.Projects.Server/Authentication/ValidateProjectsAuthenticationOptions.cs`
- `tests/Hexalith.Projects.Server.Tests/Authentication/ProjectsAuthenticationContractTests.cs`

Treat the modified `references/Hexalith.FrontComposer` pointer as pre-existing
workspace state unless the diff proves that this story caused it. Report only
actionable findings, with file paths and severity rationale.
