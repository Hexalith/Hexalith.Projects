## Git Submodules

- Initialize only submodules declared in the root `.gitmodules`, never initialize nested submodules.
- Never initialize or update nested submodules recursively unless the user explicitly asks for nested submodules.
- For repositories with submodules, initialize/update only root-declared submodules by default.
- Avoid `git submodule update --init --recursive` and similar recursive submodule commands unless nested submodule initialization is explicitly requested.
