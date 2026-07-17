---
title: 'Prevent assistants from loading submodule skills'
type: 'chore'
created: '2026-07-17'
status: 'done'
review_loop_iteration: 0
baseline_commit: '6a31cf95533624c5d7eb22323d4878ced75112fc'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Codex, GitHub Copilot, and Claude can discover duplicate or repository-specific agent skills inside root-declared submodules under `references/`. Loading those skills while working from the umbrella repository can introduce conflicting workflows and instructions from dependency repositories.

**Approach:** Add the same explicit skill-loading boundary to each root assistant instruction entry point. Assistants may use skills exposed by the root workspace or user environment, but must never discover, load, or execute skills whose location is within the root-level `references/` tree.

## Boundaries & Constraints

**Always:** Keep `AGENTS.md`, `.github/copilot-instructions.md`, and `CLAUDE.md` behaviorally identical. Scope the prohibition specifically to agent skills located under the repository root's `references/` directory. Preserve the existing requirement to read `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` and the existing Git submodule safety rules. Preserve CRLF line endings.

**Ask First:** Any proposal to change skill installation, remove existing root-local skills, edit a submodule, or broaden the restriction beyond agent skills.

**Never:** Modify content inside `references/`; prohibit required instruction, documentation, or source-code reads from submodules; alter assistant-specific configuration beyond the three instruction entry points; touch the unrelated untracked implementation-readiness report.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Root-local or user skill | Skill is exposed outside the root `references/` tree | Assistant may load it under normal skill-selection rules | Existing skill fallback behavior applies |
| Submodule skill | Skill or its assets resolve inside root `references/` | Assistant does not discover, load, or execute it | Continue without that skill; use an allowed alternative if available |
| Required submodule context | Task requires instructions, docs, or source under `references/` | Assistant may read the non-skill content | The skill restriction must not be interpreted as a blanket submodule read ban |

</frozen-after-approval>

## Code Map

- `AGENTS.md` -- Codex and general agent instruction entry point.
- `.github/copilot-instructions.md` -- GitHub Copilot repository instruction entry point.
- `CLAUDE.md` -- Claude repository instruction entry point.

## Tasks & Acceptance

**Execution:**
- [x] `AGENTS.md` -- add a concise agent-skills section forbidding skill discovery/loading/execution from root-level `references/` while preserving permitted non-skill reads.
- [x] `.github/copilot-instructions.md` -- apply the identical skill boundary for GitHub Copilot.
- [x] `CLAUDE.md` -- apply the identical skill boundary for Claude.

**Acceptance Criteria:**
- Given any of the three root assistant instruction entry points, when an assistant evaluates available skills, then it is explicitly instructed not to discover, load, or execute skills located under the repository root's `references/` directory.
- Given a required instruction, documentation, or source file under `references/`, when an assistant needs that non-skill content, then the new rule does not prohibit reading it.
- Given the completed edits, when the three files are compared, then their instruction content remains identical and their CRLF line endings are preserved.

## Spec Change Log

## Verification

**Commands:**
- `set -euo pipefail; for instruction_file in AGENTS.md .github/copilot-instructions.md CLAUDE.md; do rg -Fq 'repository root is the directory containing this instruction file' "$instruction_file"; rg -Fq 'Never discover (register or select as available), load, or execute an agent skill' "$instruction_file"; rg -Fq 'If tooling exposes a skill from' "$instruction_file"; rg -Fq 'Use only skills located outside the root' "$instruction_file"; rg -Fq 'Continue to read non-skill instructions' "$instruction_file"; done` -- expected: fail-fast assertions for the fixed root, blocked submodule skills, exposed-skill fallback, allowed external skills, and permitted non-skill submodule reads all pass.
- `cmp -s AGENTS.md CLAUDE.md && cmp -s AGENTS.md .github/copilot-instructions.md` -- expected: all three instruction files are byte-identical.
- `perl -e 'for my $path (@ARGV) { open my $fh, "<:raw", $path or die "$path: $!"; local $/; my $content = <$fh>; die "$path: non-CRLF newline\n" if $content =~ /(?<!\r)\n|\r(?!\n)/; die "$path: missing final CRLF\n" unless $content =~ /\r\n\z/; }' AGENTS.md .github/copilot-instructions.md CLAUDE.md` -- expected: every newline in each instruction file is CRLF and each file ends with CRLF.
- `git -c core.whitespace=cr-at-eol diff --check -- AGENTS.md .github/copilot-instructions.md CLAUDE.md` -- expected: no whitespace errors while honoring the repository's required CRLF line endings.
- `spec_diff_exit=0; git -c core.whitespace=cr-at-eol diff --no-index --check -- /dev/null _bmad-output/implementation-artifacts/spec-prevent-submodule-skill-loading.md || spec_diff_exit=$?; test "$spec_diff_exit" -eq 1` -- expected: the new specification differs from an empty file without whitespace errors.

## Suggested Review Order

**Skill boundary**

- Defines root identity, skill markers, path checks, fallback, and non-skill carve-out.
  [`AGENTS.md:7`](../../AGENTS.md#L7)

**Cross-assistant parity**

- Mirrors the boundary for GitHub Copilot's repository instruction entry point.
  [`copilot-instructions.md:7`](../../.github/copilot-instructions.md#L7)

- Mirrors the boundary for Claude's repository instruction entry point.
  [`CLAUDE.md:7`](../../CLAUDE.md#L7)

**Verification and follow-up**

- Records fail-fast content, parity, CRLF, and whitespace checks.
  [`spec-prevent-submodule-skill-loading.md:57`](spec-prevent-submodule-skill-loading.md#L57)

- Defers the pre-existing repository-wide CRLF and Git-check conflict.
  [`deferred-work.md:53`](deferred-work.md#L53)
