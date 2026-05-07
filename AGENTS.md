## Git Submodules

- Never initialize or update nested submodules recursively unless the user explicitly asks for nested submodules.
- For repositories with submodules, initialize/update only root-level submodules by default.
- Avoid `git submodule update --init --recursive` and similar recursive submodule commands unless nested submodule initialization is explicitly requested.
