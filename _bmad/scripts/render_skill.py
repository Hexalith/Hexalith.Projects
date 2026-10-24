#!/usr/bin/env python3
# /// script
# requires-python = ">=3.11"
# ///
"""Render a skill's Markdown sources into an immutable project snapshot.

Publication and reuse require POSIX no-follow directory descriptors (Linux/macOS).
Unsupported platforms halt before publication. Project-root ancestors are trusted;
entries beneath that root are opened without following links. Render ancestors
must remain exclusively owned during the operation: no API can prohibit another
process with the same permissions from relocating an opened directory. Detected
movement halts rendering; handles never redirect writes into a link's target.
Paths can also be modified after rendering has returned.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import stat
import sys
from contextlib import ExitStack, contextmanager
from pathlib import Path
from typing import Any

# Installed scripts are consumer files, not a location for interpreter caches.
sys.dont_write_bytecode = True

from config_utils import ConfigError, load_central_config, load_customization, load_toml


class RenderError(ValueError):
    """Raised when rendering cannot safely publish a snapshot."""


_CONFIG_TOKEN = re.compile(r"\{\{config\.([A-Za-z0-9_.-]+)\}\}")
_SHORT_CONFIG_TOKEN = re.compile(r"\{\{\.([A-Za-z0-9_]+)\}\}")
_CUSTOM_TOKEN = re.compile(r"\{workflow\.([A-Za-z0-9_.-]+)\}")
_SNAPSHOT_TOKEN = re.compile(r"\[\[bmad-snapshot:([A-Za-z0-9_./-]+\.md)\]\]")
_DESCRIPTOR_IO = (
    hasattr(os, "O_NOFOLLOW") and hasattr(os, "O_DIRECTORY")
    and {os.open, os.mkdir, os.rename, os.stat, os.unlink, os.rmdir} <= os.supports_dir_fd
    and os.listdir in os.supports_fd
)


def _hash_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def _canonical_json(value: Any) -> bytes:
    return json.dumps(
        value, ensure_ascii=False, sort_keys=True, separators=(",", ":")
    ).encode("utf-8")


def _root_hash(project_root: Path) -> str:
    return _hash_bytes(str(project_root).encode("utf-8"))[:12]


def _project_slug(project_root: Path) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", project_root.name.lower()).strip("-") or "project"
    return slug[:80].rstrip("-") or "project"


def _generation_hash(identity: dict[str, Any]) -> str:
    return _hash_bytes(_canonical_json(identity))[:20]


def _lookup(data: dict[str, Any], dotted_path: str, label: str) -> Any:
    current: Any = data
    for part in dotted_path.split("."):
        if not isinstance(current, dict) or part not in current:
            raise RenderError(f"missing {label} `{dotted_path}`")
        current = current[part]
    return current


def _require_string(value: Any, label: str, *, allow_empty: bool = False) -> str:
    if not isinstance(value, str):
        raise RenderError(f"{label} must be a string, got {type(value).__name__}")
    if not allow_empty and not value.strip():
        raise RenderError(f"{label} must not be empty")
    return value


def _require_string_list(value: Any, label: str) -> list[str]:
    if not isinstance(value, list):
        raise RenderError(f"{label} must be a list, got {type(value).__name__}")
    result = []
    for index, item in enumerate(value):
        result.append(_require_string(item, f"{label}[{index}]"))
    return result


def _require_review_layers(value: Any, label: str) -> list[dict[str, str]]:
    if not isinstance(value, list):
        raise RenderError(f"{label} must be a list of tables")
    result: list[dict[str, str]] = []
    seen: set[str] = set()
    for index, item in enumerate(value):
        item_label = f"{label}[{index}]"
        if not isinstance(item, dict):
            raise RenderError(f"{item_label} must be a table")
        identifier = _require_string(item.get("id"), f"{item_label}.id")
        if identifier in seen:
            raise RenderError(f"duplicate review layer id `{identifier}`")
        seen.add(identifier)
        layer = {
            "id": identifier,
            "name": _require_string(item.get("name", identifier), f"{item_label}.name"),
            "instruction": _require_string(
                item.get("instruction"), f"{item_label}.instruction", allow_empty=True
            ),
        }
        if "when" in item:
            layer["when"] = _require_string(item["when"], f"{item_label}.when")
        result.append(layer)
    return result


def _load_sources(skill_dir: Path) -> dict[str, str]:
    sources: dict[str, str] = {}
    for candidate in sorted(skill_dir.rglob("*.md")):
        if candidate.name == "SKILL.md":
            continue
        name = candidate.relative_to(skill_dir).as_posix()
        path = candidate.resolve(strict=True)
        if not path.is_relative_to(skill_dir):
            raise RenderError(f"render source escapes skill directory: {name}")
        if not path.is_file():
            raise RenderError(f"render source is missing or not a file: {path}")
        try:
            sources[name] = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as error:
            raise RenderError(f"failed to read render source {path}: {error}") from error
    if "workflow.md" not in sources:
        raise RenderError(f"render entry is missing: {skill_dir / 'workflow.md'}")
    return sources


def _resolve_config_value(value: Any, label: str, project_root: Path) -> str:
    text = _require_string(value, label)
    if "{project-root}" not in text:
        return text
    resolved = text.replace("{project-root}", str(project_root))
    if not Path(resolved).is_absolute():
        raise RenderError(f"{label} must resolve to an absolute path: {resolved}")
    return resolved


def _find_config_values(data: Any, key: str, prefix: str = "") -> list[tuple[str, Any]]:
    matches: list[tuple[str, Any]] = []
    if not isinstance(data, dict):
        return matches
    for name, value in data.items():
        path = f"{prefix}.{name}" if prefix else name
        if name == key and not isinstance(value, (dict, list)):
            matches.append((path, value))
        matches.extend(_find_config_values(value, key, path))
    return matches


def _resolve_short_config(
    central: dict[str, Any], key: str, project_root: Path
) -> tuple[str, str]:
    matches = _find_config_values(central, key)
    if not matches:
        raise RenderError(f"missing config value `{key}`")
    if len(matches) > 1:
        paths = ", ".join(path for path, _ in matches)
        raise RenderError(f"ambiguous config value `{key}` found at: {paths}")
    path, value = matches[0]
    return path, _resolve_config_value(value, f"config.{path}", project_root)


def _format_markdown_list(items: list[str]) -> str:
    if not items:
        return "_None._"
    rendered = []
    for item in items:
        lines = item.splitlines() or [""]
        rendered.append("- " + lines[0])
        rendered.extend("  " + line for line in lines[1:])
    return "\n".join(rendered)


def _format_review_layers(layers: list[dict[str, str]]) -> str:
    active = [layer for layer in layers if layer["instruction"].strip()]
    if not active:
        return "No active review layers. HALT with blocking condition `no active review layers`."
    sections = []
    for layer in active:
        section = [f"#### {layer['name']} (`{layer['id']}`)"]
        if layer.get("when"):
            section.extend(["", f"Run only when: {layer['when']}"])
        section.extend(["", layer["instruction"].strip()])
        sections.append("\n".join(section))
    return "\n\n".join(sections)


def _resolve_customization_value(value: Any, default: Any, label: str) -> tuple[Any, str]:
    if isinstance(default, str):
        allow_empty = not default.strip() or label == "customization.workflow.open_spec"
        resolved = _require_string(value, label, allow_empty=allow_empty)
        return resolved, resolved
    if isinstance(default, list):
        if default and all(isinstance(item, dict) for item in default):
            resolved = _require_review_layers(value, label)
            return resolved, _format_review_layers(resolved)
        resolved = _require_string_list(value, label)
        return resolved, _format_markdown_list(resolved)
    raise RenderError(f"{label} has unsupported default type {type(default).__name__}")


def _resolve_replacements(
    sources: dict[str, str],
    central: dict[str, Any],
    customization: dict[str, Any],
    defaults: dict[str, Any] | None,
    project_root: Path,
) -> tuple[dict[str, str], dict[str, Any]]:
    replacements: dict[str, str] = {}
    input_values: dict[str, Any] = {}
    for content in sources.values():
        for match in _SHORT_CONFIG_TOKEN.finditer(content):
            token, key = match.group(0), match.group(1)
            path, resolved = _resolve_short_config(central, key, project_root)
            source = f"config.{path}"
            replacements[token] = resolved
            input_values[source] = resolved
        for match in _CONFIG_TOKEN.finditer(content):
            token, path = match.group(0), match.group(1)
            source = f"config.{path}"
            resolved = _resolve_config_value(
                _lookup(central, path, "config value"), source, project_root
            )
            replacements[token] = resolved
            input_values[source] = resolved
        for match in _CUSTOM_TOKEN.finditer(content):
            if defaults is None:
                raise RenderError("customization tokens require customize.toml")
            token, relative_path = match.group(0), match.group(1)
            path = f"workflow.{relative_path}"
            source = f"customization.{path}"
            resolved, rendered = _resolve_customization_value(
                _lookup(customization, path, "customization value"),
                _lookup(defaults, path, "customization default"),
                source,
            )
            replacements[token] = rendered
            input_values[source] = resolved
    return replacements, input_values


def _render_sources(
    sources: dict[str, str], replacements: dict[str, str], destination: Path
) -> dict[str, str]:
    """Resolve only tokens authored in installed sources in one opaque pass."""
    # Workflow customization may reference installed skill files; bind those
    # references to the immutable generation before inserting the prose.
    replacements = {
        token: value.replace("{skill-root}", str(destination))
        if token.startswith("{workflow.")
        else value
        for token, value in replacements.items()
    }
    source_names = set(sources)
    patterns = [
        *(re.escape(token) for token in sorted(replacements, key=len, reverse=True)),
        _SNAPSHOT_TOKEN.pattern,
    ]
    token_pattern = re.compile("|".join(patterns))

    def replace(match: re.Match[str]) -> str:
        token = match.group(0)
        if token in replacements:
            return replacements[token]
        snapshot = _SNAPSHOT_TOKEN.fullmatch(token)
        if snapshot is None:
            raise RenderError(f"unsupported render token: {token}")
        target = snapshot.group(1)
        if target not in source_names:
            raise RenderError(f"snapshot reference targets undeclared source: {target}")
        return str(destination / target)

    rendered: dict[str, str] = {}
    for name, content in sources.items():
        # Inserted paths and customization prose are never scanned as source tokens.
        rendered[name] = token_pattern.sub(replace, content)
    return rendered


def _same_entry(parent: int, name: str, opened: os.stat_result) -> bool:
    """Check that a held handle is still bound by its original directory entry."""
    current = os.stat(name, dir_fd=parent, follow_symlinks=False)
    return (current.st_dev, current.st_ino, current.st_mode) == (
        opened.st_dev, opened.st_ino, opened.st_mode
    )


def _file_identity(info: os.stat_result) -> tuple[int, ...]:
    return (info.st_dev, info.st_ino, info.st_mode, info.st_size, info.st_mtime_ns, info.st_ctime_ns)


@contextmanager
def _open_directory(parent: int, name: str, label: str):
    try:
        descriptor = os.open(name, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=parent)
    except OSError as error:
        raise RenderError(label) from error
    try:
        opened = os.fstat(descriptor)
        if not _same_entry(parent, name, opened):
            raise RenderError(f"directory changed during rendering: {name}")
        yield descriptor
        if not _same_entry(parent, name, opened):
            raise RenderError(f"directory changed during rendering: {name}")
    finally:
        os.close(descriptor)


def _snapshot_files(directory: int, display: Path) -> tuple[dict[str, bytes], dict[str, os.stat_result]]:
    """Read only regular files relative to held, no-follow directory handles."""
    files: dict[str, bytes] = {}
    identities: dict[str, os.stat_result] = {}
    names = os.listdir(directory)
    for name in names:
        path = display / name
        info = os.stat(name, dir_fd=directory, follow_symlinks=False)
        identities[name] = info
        label = f"generation contains linked or nonregular entry: {path}"
        if stat.S_ISDIR(info.st_mode):
            with _open_directory(directory, name, label) as child:
                if _file_identity(os.fstat(child)) != _file_identity(info):
                    raise RenderError(f"generation changed during verification: {path}")
                child_files, child_identities = _snapshot_files(child, path)
                files.update({f"{name}/{key}": value for key, value in child_files.items()})
                identities.update({f"{name}/{key}": value for key, value in child_identities.items()})
        elif stat.S_ISREG(info.st_mode):
            # NONBLOCK prevents a concurrent FIFO substitution from hanging open().
            descriptor = os.open(name, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=directory)
            with os.fdopen(descriptor, "rb") as source:
                opened = os.fstat(source.fileno())
                if not stat.S_ISREG(opened.st_mode) or _file_identity(info) != _file_identity(opened):
                    raise RenderError(label)
                files[name] = source.read()
                if _file_identity(os.fstat(source.fileno())) != _file_identity(opened):
                    raise RenderError(f"generation changed during verification: {path}")
        else:
            raise RenderError(label)
    return files, identities


def _verify_identities(directory: int, display: Path, identities: dict[str, os.stat_result]) -> None:
    """Recheck the read tree after hashing, using only one handle per tree level."""
    direct = {name: info for name, info in identities.items() if "/" not in name}
    if set(os.listdir(directory)) != set(direct):
        raise RenderError(f"generation changed during verification: {display}")
    for name, expected in direct.items():
        current = os.stat(name, dir_fd=directory, follow_symlinks=False)
        if _file_identity(current) != _file_identity(expected):
            raise RenderError(f"generation changed during verification: {display / name}")
        if stat.S_ISDIR(expected.st_mode):
            with _open_directory(directory, name, "generation directory was replaced") as child:
                if _file_identity(os.fstat(child)) != _file_identity(expected):
                    raise RenderError(f"generation changed during verification: {display / name}")
                nested = {key[len(name) + 1:]: value for key, value in identities.items() if key.startswith(name + "/")}
                _verify_identities(child, display / name, nested)


def _verify_existing(destination: Path, manifest: dict[str, Any], parent: int) -> None:
    with _open_directory(parent, destination.name,
                         f"generation is not an unlinked directory: {destination}") as directory:
        files, identities = _snapshot_files(directory, destination)
        try:
            existing = json.loads(files["manifest.json"].decode("utf-8"))
        except (KeyError, UnicodeError, json.JSONDecodeError) as error:
            raise RenderError(f"corrupt existing generation {destination}: {error}") from error
        if existing != manifest:
            raise RenderError(f"generation collision or corruption at {destination}")
        expected_files = set(manifest["outputs"]) | {"manifest.json"}
        if set(files) != expected_files:
            raise RenderError(f"generation contains unexpected or missing files: {destination}")
        for name, expected_hash in manifest["outputs"].items():
            if _hash_bytes(files[name]) != expected_hash:
                raise RenderError(f"generation output hash mismatch: {destination / name}")
        _verify_identities(directory, destination, identities)


def _clear_owned_directory(directory: int) -> None:
    """Remove staging contents through its held handle, never its mutable path."""
    for name in os.listdir(directory):
        info = os.stat(name, dir_fd=directory, follow_symlinks=False)
        if stat.S_ISDIR(info.st_mode):
            with _open_directory(directory, name, "staging child was replaced") as child:
                if _file_identity(os.fstat(child)) != _file_identity(info):
                    raise RenderError("staging child was replaced")
                _clear_owned_directory(child)
            os.rmdir(name, dir_fd=directory)
        else:
            os.unlink(name, dir_fd=directory)


def _publish(destination: Path, outputs: dict[str, bytes], manifest: dict[str, Any], *, trusted_root: Path) -> None:
    if not _DESCRIPTOR_IO:
        raise RenderError("safe rendering requires no-follow directory descriptor support")
    relative = destination.relative_to(trusted_root)
    if any(part in (".", "..") for part in relative.parts) or len(relative.parts) < 2:
        raise RenderError("generation must be beneath the trusted project root")
    with ExitStack() as handles:
        root = os.open(trusted_root, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
        handles.callback(os.close, root)
        parent = root
        ancestors: list[tuple[int, str, os.stat_result]] = []
        for name in relative.parts[:-1]:
            for ancestor_parent, ancestor_name, identity in ancestors:
                if not _same_entry(ancestor_parent, ancestor_name, identity):
                    raise RenderError("render ancestor changed during rendering")
            try:
                os.mkdir(name, dir_fd=parent)
            except FileExistsError:
                pass
            child = handles.enter_context(_open_directory(
                parent, name, f"render ancestor is not an unlinked directory: {name}"
            ))
            ancestors.append((parent, name, os.fstat(child)))
            parent = child

        def check_ancestors():
            for ancestor_parent, ancestor_name, identity in ancestors:
                if not _same_entry(ancestor_parent, ancestor_name, identity):
                    raise RenderError("render ancestor changed during rendering")

        check_ancestors()
        try:
            os.stat(destination.name, dir_fd=parent, follow_symlinks=False)
        except FileNotFoundError:
            pass
        else:
            _verify_existing(destination, manifest, parent)
            return
        # Staging stays relative to the pinned parent even if an ancestor moves.
        staging_name = ".staging-" + os.urandom(16).hex()
        check_ancestors()
        os.mkdir(staging_name, mode=0o700, dir_fd=parent)
        staging = os.open(staging_name, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=parent)
        staging_identity = os.fstat(staging)
        published = False
        try:
            for name, content in {**outputs, "manifest.json": (
                json.dumps(manifest, ensure_ascii=False, indent=2, sort_keys=True).encode("utf-8") + b"\n"
            )}.items():
                check_ancestors()
                with ExitStack() as directories:
                    target = staging
                    parts = Path(name).parts
                    if Path(name).is_absolute() or any(part in (".", "..") for part in parts):
                        raise RenderError(f"unsafe output name: {name}")
                    for part in parts[:-1]:
                        try:
                            os.mkdir(part, dir_fd=target)
                        except FileExistsError:
                            pass
                        target = directories.enter_context(_open_directory(target, part, "unsafe staging directory"))
                    descriptor = os.open(parts[-1], os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW,
                                         0o600, dir_fd=target)
                    with os.fdopen(descriptor, "wb") as output:
                        output.write(content)
            _verify_existing(destination.with_name(staging_name), manifest, parent)
            check_ancestors()
            if not _same_entry(parent, staging_name, staging_identity):
                raise RenderError("staging directory was replaced")
            try:
                os.rename(staging_name, destination.name, src_dir_fd=parent, dst_dir_fd=parent)
                published = True
                if not _same_entry(parent, destination.name, staging_identity):
                    raise RenderError("published generation differs from the owned staging directory")
            except OSError as error:
                try:
                    os.stat(destination.name, dir_fd=parent, follow_symlinks=False)
                except FileNotFoundError:
                    raise error
            _verify_existing(destination, manifest, parent)
            check_ancestors()
        finally:
            try:
                if not published:
                    # Even if its name is replaced, this handle still owns only
                    # the original staging tree. Never recurse by pathname.
                    try:
                        _clear_owned_directory(staging)
                        if _same_entry(parent, staging_name, staging_identity):
                            os.rmdir(staging_name, dir_fd=parent)
                    except (OSError, RenderError):
                        pass
            finally:
                os.close(staging)


def render(project_root: Path, skill_dir: Path) -> Path:
    project_root = project_root.resolve(strict=True)
    skill_dir = skill_dir.resolve(strict=True)
    if not (project_root / "_bmad").is_dir():
        raise RenderError(f"project root does not contain _bmad/: {project_root}")

    sources = _load_sources(skill_dir)
    central = load_central_config(project_root)
    has_customization = any(
        _CUSTOM_TOKEN.search(content) for content in sources.values()
    )
    defaults = (
        load_toml(skill_dir / "customize.toml", required=True)
        if has_customization
        else None
    )
    customization = (
        load_customization(project_root, skill_dir) if has_customization else {}
    )
    replacements, input_values = _resolve_replacements(
        sources, central, customization, defaults, project_root
    )
    source_hashes = {
        name: _hash_bytes(content.encode("utf-8")) for name, content in sources.items()
    }
    root_hash = _root_hash(project_root)
    slug = _project_slug(project_root)
    renderer_hash = _hash_bytes(Path(__file__).read_bytes())
    identity = {
        "project_root": str(project_root),
        "renderer_sha256": renderer_hash,
        "resolved_values": input_values,
        "source_sha256": source_hashes,
    }
    generation_hash = _generation_hash(identity)
    destination = (
        project_root
        / "_bmad"
        / "render"
        / skill_dir.name
        / f"{slug}-{root_hash}"
        / generation_hash
    )
    rendered = _render_sources(sources, replacements, destination)
    outputs = {name: content.encode("utf-8") for name, content in rendered.items()}
    output_hashes = {name: _hash_bytes(content) for name, content in outputs.items()}
    manifest = {
        "schema_version": 1,
        "skill": skill_dir.name,
        "project_root": str(project_root),
        "project_slug": slug,
        "root_hash": root_hash,
        "generation_hash": generation_hash,
        "inputs": identity,
        "outputs": output_hashes,
    }
    _publish(destination, outputs, manifest, trusted_root=project_root)
    return destination / "workflow.md"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--skill", required=True)
    args = parser.parse_args()
    reconfigure = getattr(sys.stdout, "reconfigure", None)
    if reconfigure is not None:
        reconfigure(encoding="utf-8")
    try:
        entry = render(Path(args.project_root), Path(args.skill))
    except (ConfigError, RenderError, OSError, UnicodeError, ValueError) as error:
        sys.stdout.write(f"HALT: {error}\n")
        return 1
    sys.stdout.write(f"read and follow {entry}\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
