## Shared Hexalith LLM Instructions

Before starting any work in this repository, read and follow
[`Hexalith.AI.Tools\hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md).

## Git Submodules

- Initialize only submodules declared in the root `.gitmodules`, never initialize nested submodules.
- Never initialize or update nested submodules recursively unless the user explicitly asks for nested submodules.
- For repositories with submodules, initialize/update only root-declared submodules by default.
- Avoid `git submodule update --init --recursive` and similar recursive submodule commands unless nested submodule initialization is explicitly requested.
- Apply this rule for all Codex work sessions in this repository.
- **Do not read bmad folders inside submodules.** Each root submodule can run its own independent bmad / story-automator orchestration (own marker, sprint-status, settings). Keep bmad reads scoped to the umbrella root's `_bmad/`, `_bmad-output/`, and `.claude`/`.codex` — never read `references/Hexalith.*/_bmad`, `references/Hexalith.*/_bmad-output`, `references/Hexalith.*/.codex`, or `references/Hexalith.*/.claude/skills/bmad-*` unless explicitly asked to work inside that submodule's bmad.
