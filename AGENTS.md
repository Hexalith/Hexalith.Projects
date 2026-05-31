## Git Submodules

- Initialize only submodules at the root of the repository, never initialize nested submodules.
- Never initialize or update nested submodules recursively unless the user explicitly asks for nested submodules.
- For repositories with submodules, initialize/update only root-level submodules by default.
- Avoid `git submodule update --init --recursive` and similar recursive submodule commands unless nested submodule initialization is explicitly requested.
- Apply this rule for all Codex work sessions in this repository.
- **Do not read bmad folders inside submodules.** Each root submodule can run its own independent bmad / story-automator orchestration (own marker, sprint-status, settings). Keep bmad reads scoped to the umbrella root's `_bmad/`, `_bmad-output/`, and `.claude`/`.codex` — never read `Hexalith.*/_bmad`, `Hexalith.*/_bmad-output`, or a submodule's `.codex` / `.claude/skills/bmad-*` unless explicitly asked to work inside that submodule's bmad.
