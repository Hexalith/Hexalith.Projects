from __future__ import annotations

import importlib.util
import hashlib
import io
import json
import os
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest import mock
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "scripts"
sys.path.insert(0, str(SCRIPTS))

import release_contract  # noqa: E402
import release_state  # noqa: E402


def load_script_module(name: str, filename: str):
    spec = importlib.util.spec_from_file_location(
        name,
        SCRIPTS / filename,
    )
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load release-source verifier")
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


verify_release_source = load_script_module("verify_release_source", "verify-release-source.py")
validate_nuget_packages = load_script_module("validate_nuget_packages", "validate-nuget-packages.py")
pack_release_packages = load_script_module("pack_release_packages", "pack-release-packages.py")
validate_consumer_packages = load_script_module(
    "validate_consumer_packages",
    "validate-consumer-package-references.py",
)


def published_payloads() -> dict[str, bytes]:
    payloads: dict[str, bytes] = {}
    for package_id in release_contract.EXACT_PACKAGE_IDS:
        buffer = io.BytesIO()
        with zipfile.ZipFile(buffer, "w") as archive:
            archive.writestr(f"{package_id}.nuspec", f"<package id='{package_id}' />")
            archive.writestr(f"lib/net10.0/{package_id}.dll", f"assembly:{package_id}")
        payloads[package_id] = buffer.getvalue()
    return payloads


def repository_signed(payload: bytes) -> bytes:
    buffer = io.BytesIO(payload)
    with zipfile.ZipFile(buffer, "a") as archive:
        archive.writestr(".signature.p7s", b"nuget.org repository signature")
    return buffer.getvalue()


def release_assets(version: str, payloads: dict[str, bytes]) -> list[dict[str, object]]:
    assets: list[dict[str, object]] = []
    for name in sorted(release_contract.expected_asset_names(version)):
        package_id = name.removesuffix(f".{version}.nupkg") if name.endswith(".nupkg") else None
        content = payloads[package_id] if package_id in payloads else name.encode()
        assets.append(
            {
                "name": name,
                "state": "uploaded",
                "size": len(content),
                "digest": f"sha256:{hashlib.sha256(content).hexdigest()}",
                "browser_download_url": (
                    f"https://github.com/{release_contract.REPOSITORY}/releases/download/v{version}/{name}"
                ),
            }
        )
    return assets


def publication_request(source_sha: str, payloads: dict[str, bytes]):
    assets = release_assets("1.0.0", payloads)

    def published(method: str, url: str) -> release_state.HttpResponse:
        if "api.nuget.org" in url:
            package_id = next(
                package_id for package_id in release_contract.EXACT_PACKAGE_IDS
                if f"/{package_id.lower()}/" in url
            )
            return release_state.HttpResponse(200, repository_signed(payloads[package_id]))
        if "/git/ref/tags/v1.0.0" in url:
            document = {"object": {"type": "commit", "sha": source_sha}}
        elif "/releases/tags/v1.0.0" in url:
            document = {"tag_name": "v1.0.0", "draft": False, "prerelease": False, "assets": assets}
        elif f"https://github.com/{release_contract.REPOSITORY}/releases/download/v1.0.0/" in url:
            name = url.rsplit("/", maxsplit=1)[-1]
            content = payloads[name.removesuffix(".1.0.0.nupkg")] if name.endswith(".nupkg") else name.encode()
            return release_state.HttpResponse(200, content)
        else:
            raise AssertionError(f"unexpected publication URL: {url}")
        return release_state.HttpResponse(200, json.dumps(document).encode())

    return published


class ReleaseToolingTests(unittest.TestCase):
    def test_package_output_must_stay_below_repository_root(self) -> None:
        with self.assertRaisesRegex(ValueError, "child of the repository root"):
            pack_release_packages.prepare_output_directory(Path("/"))

    def test_exact_manifest_loads_and_unknown_inventory_is_rejected(self) -> None:
        packages = release_contract.load_manifest()
        self.assertEqual(release_contract.EXACT_PACKAGE_IDS, tuple(row.package_id for row in packages))

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for _, project in release_contract.EXACT_PACKAGES:
                path = root / project
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("<Project />\n", encoding="utf-8")
            manifest = root / "tools" / "release-packages.json"
            manifest.parent.mkdir(parents=True)
            rows = [
                {"id": package_id, "project": project}
                for package_id, project in release_contract.EXACT_PACKAGES
            ]
            rows.append({"id": "Hexalith.Conversations.Unknown", "project": rows[0]["project"]})
            manifest.write_text(json.dumps({"packages": rows}), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "canonical four-package sequence"):
                release_contract.load_manifest(manifest, root)

    def test_invalid_archive_boundary_rejects_foreign_assembly(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            package = Path(temporary) / "Hexalith.Conversations.1.0.0.nupkg"
            nuspec = """<?xml version="1.0"?>
<package>
  <metadata>
    <id>Hexalith.Conversations</id>
    <version>1.0.0</version>
    <authors>Hexalith</authors>
    <description>test</description>
    <license type="expression">MIT</license>
    <readme>README.md</readme>
  </metadata>
</package>
"""
            with zipfile.ZipFile(package, "w") as archive:
                archive.writestr("Hexalith.Conversations.nuspec", nuspec)
                archive.writestr("README.md", "test")
                archive.writestr("lib/net10.0/Hexalith.Conversations.dll", b"assembly")
                archive.writestr("runtimes/linux-x64/lib/net10.0/Hexalith.Commons.Http.dll", b"foreign")
            with self.assertRaisesRegex(ValueError, "foreign assemblies"):
                validate_nuget_packages._metadata(package)

    def test_internal_dependencies_follow_candidate_version(self) -> None:
        boundary = {
            "Hexalith.Conversations.Contracts": "1.0.0",
            "Hexalith.EventStore.Client": "3.106.0",
        }
        self.assertEqual(
            {
                "Hexalith.Conversations.Contracts": "0.0.0-ci-test",
                "Hexalith.EventStore.Client": "3.106.0",
            },
            validate_nuget_packages._candidate_dependencies(boundary, "0.0.0-ci-test"),
        )

    def test_symbol_archive_requires_canonical_portable_pdb(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            symbol = Path(temporary) / "Hexalith.Conversations.1.0.0.snupkg"
            with zipfile.ZipFile(symbol, "w") as archive:
                archive.writestr("lib/net10.0/Hexalith.Conversations.pdb", b"not-portable")
            with self.assertRaisesRegex(ValueError, "not a Portable PDB"):
                validate_nuget_packages._validate_symbol_archive(symbol, "Hexalith.Conversations")

    def test_frozen_dispatch_skips_without_network(self) -> None:
        def forbidden_get(_: str):
            self.fail("frozen verification must not query GitHub")

        def forbidden_destinations(_: str, __: str) -> None:
            self.fail("frozen verification must not query destinations")

        enabled = verify_release_source.verify_source(
            "false",
            release_contract.REPOSITORY,
            "refs/heads/main",
            "a" * 40,
            "ci.yml",
            forbidden_get,
            forbidden_destinations,
        )
        self.assertFalse(enabled)

    def test_stale_dispatch_is_rejected(self) -> None:
        def stale_main(_: str):
            return {"object": {"sha": "b" * 40}}

        with self.assertRaisesRegex(ValueError, "STALE_SOURCE"):
            verify_release_source.verify_source(
                "true",
                release_contract.REPOSITORY,
                "refs/heads/main",
                "a" * 40,
                "ci.yml",
                stale_main,
                lambda _version, _repository: None,
            )

    def test_exact_main_and_successful_push_ci_are_required(self) -> None:
        source_sha = "a" * 40
        destinations: list[tuple[str, str]] = []

        def github(url: str):
            if "/git/ref/heads/main" in url:
                return {"object": {"sha": source_sha}}
            return {
                "workflow_runs": [
                    {
                        "id": 101,
                        "run_number": 7,
                        "run_attempt": 1,
                        "head_sha": source_sha,
                        "head_branch": "main",
                        "event": "push",
                        "status": "completed",
                        "conclusion": "success",
                    }
                ]
            }

        enabled = verify_release_source.verify_source(
            "true",
            release_contract.REPOSITORY,
            "refs/heads/main",
            source_sha,
            "ci.yml",
            github,
            lambda version, repository: destinations.append((version, repository)),
        )
        self.assertTrue(enabled)
        self.assertEqual(
            [(release_contract.FIRST_RELEASE_VERSION, release_contract.REPOSITORY)],
            destinations,
        )

        destinations.clear()
        enabled = verify_release_source.verify_source(
            "true",
            release_contract.REPOSITORY,
            "refs/heads/main",
            source_sha,
            "ci.yml",
            github,
            lambda version, repository: destinations.append((version, repository)),
            release_version="1.1.0",
        )
        self.assertTrue(enabled)
        self.assertEqual([("1.1.0", release_contract.REPOSITORY)], destinations)

        destinations.clear()
        enabled = verify_release_source.verify_source(
            "true",
            release_contract.REPOSITORY,
            "refs/heads/main",
            source_sha,
            "ci.yml",
            github,
            lambda version, repository: destinations.append((version, repository)),
            check_destinations=False,
        )
        self.assertTrue(enabled)
        self.assertEqual([], destinations)

    def test_newest_exact_source_ci_attempt_must_succeed(self) -> None:
        source_sha = "a" * 40

        def github(url: str):
            if "/git/ref/heads/main" in url:
                return {"object": {"sha": source_sha}}
            return {
                "workflow_runs": [
                    {
                        "id": 201,
                        "run_number": 8,
                        "run_attempt": 1,
                        "head_sha": source_sha,
                        "head_branch": "main",
                        "event": "push",
                        "status": "completed",
                        "conclusion": "failure",
                    },
                    {
                        "id": 101,
                        "run_number": 7,
                        "run_attempt": 1,
                        "head_sha": source_sha,
                        "head_branch": "main",
                        "event": "push",
                        "status": "completed",
                        "conclusion": "success",
                    },
                ]
            }

        with self.assertRaisesRegex(ValueError, "newest push CI run/attempt"):
            verify_release_source.verify_source(
                "true",
                release_contract.REPOSITORY,
                "refs/heads/main",
                source_sha,
                "ci.yml",
                github,
                lambda _version, _repository: None,
            )

    def test_partial_publication_is_not_treated_as_absent(self) -> None:
        def partial(_method: str, url: str) -> release_state.HttpResponse:
            status = 200 if "hexalith.conversations.contracts" in url else 404
            return release_state.HttpResponse(status)

        with self.assertRaisesRegex(ValueError, "PARTIAL_PUBLICATION"):
            release_state.verify_absent("1.0.0", request=partial)

    def test_occupied_release_is_rejected_before_republication(self) -> None:
        with self.assertRaisesRegex(ValueError, "DESTINATION_OCCUPIED"):
            release_state.verify_absent(
                "1.0.0",
                request=lambda _method, _url: release_state.HttpResponse(200),
            )

    def test_publish_boundary_accepts_only_semantic_releases_exact_tag(self) -> None:
        source_sha = "a" * 40

        def tagged_absent(method: str, url: str) -> release_state.HttpResponse:
            if method == "HEAD" or "/releases/tags/" in url:
                return release_state.HttpResponse(404)
            if "/git/ref/tags/v1.0.0" in url:
                return release_state.HttpResponse(
                    200,
                    json.dumps({"object": {"type": "commit", "sha": source_sha}}).encode(),
                )
            self.fail(f"unexpected publish-boundary URL: {url}")

        release_state.verify_publishable("1.0.0", source_sha, request=tagged_absent)

        with self.assertRaisesRegex(ValueError, "does not resolve to source"):
            release_state.verify_publishable("1.0.0", "b" * 40, request=tagged_absent)

    def test_first_release_present_state_accepts_exact_pairs_and_sha(self) -> None:
        source_sha = "a" * 40
        payloads = published_payloads()
        valid = publication_request(source_sha, payloads)
        downloaded_assets: set[str] = set()

        def observed(method: str, url: str) -> release_state.HttpResponse:
            if "/releases/download/v1.0.0/" in url:
                downloaded_assets.add(url.rsplit("/", maxsplit=1)[-1])
            return valid(method, url)

        release_state.verify_present(
            "1.0.0",
            source_sha,
            request=observed,
            attempts=1,
            delay_seconds=0,
        )
        self.assertEqual(release_contract.expected_asset_names("1.0.0"), downloaded_assets)

    def test_publication_rejects_missing_or_corrupt_asset_downloads(self) -> None:
        source_sha = "a" * 40
        valid = publication_request(source_sha, published_payloads())
        prefix = f"https://github.com/{release_contract.REPOSITORY}/releases/download/v1.0.0/"
        for extension, response, expected in (
            ("snupkg", release_state.HttpResponse(404), "could not be downloaded"),
            ("snupkg", release_state.HttpResponse(200, b"corrupt symbols"), "does not match its API digest"),
            ("nupkg", release_state.HttpResponse(200, b"corrupt package"), "does not match its API digest"),
        ):
            with self.subTest(extension=extension, status=response.status):
                bad_url = f"{prefix}Hexalith.Conversations.1.0.0.{extension}"

                def invalid(method: str, url: str) -> release_state.HttpResponse:
                    return response if url == bad_url else valid(method, url)

                with self.assertRaisesRegex(ValueError, expected):
                    release_state.verify_present("1.0.0", source_sha, request=invalid, attempts=1, delay_seconds=0)

    def test_publication_rejects_different_nuget_package_contents(self) -> None:
        source_sha = "a" * 40
        payloads = published_payloads()
        valid = publication_request(source_sha, payloads)
        buffer = io.BytesIO()
        with zipfile.ZipFile(io.BytesIO(payloads["Hexalith.Conversations"])) as original:
            with zipfile.ZipFile(buffer, "w") as changed:
                for name in original.namelist():
                    changed.writestr(name, b"different assembly" if name.endswith(".dll") else original.read(name))

        def mismatch(method: str, url: str) -> release_state.HttpResponse:
            if "api.nuget.org/v3-flatcontainer/hexalith.conversations/" in url:
                return release_state.HttpResponse(200, repository_signed(buffer.getvalue()))
            return valid(method, url)

        with self.assertRaisesRegex(ValueError, "payload does not match GitHub asset"):
            release_state.verify_present("1.0.0", source_sha, request=mismatch, attempts=1, delay_seconds=0)

    def test_semantic_release_planning_in_ci_preserves_repository_refs(self) -> None:
        with tempfile.TemporaryDirectory(prefix="conversations-planning-test-") as temporary:
            root = Path(temporary)
            configuration = (ROOT / ".releaserc.json").read_text(encoding="utf-8")
            (root / ".releaserc.json").write_text(configuration, encoding="utf-8")
            (root / "node_modules").symlink_to(ROOT / "node_modules", target_is_directory=True)
            environment = {
                **os.environ,
                "GIT_CONFIG_GLOBAL": os.devnull,
                "GIT_CONFIG_NOSYSTEM": "1",
            }

            def git(*arguments: str) -> str:
                result = subprocess.run(
                    ["git", *arguments], cwd=root, env=environment,
                    check=True, capture_output=True, text=True, timeout=30,
                )
                return result.stdout

            git("init", "-b", "main")
            git("add", ".releaserc.json")
            # Validated by the pinned CLI before this temporary fixture creates its commit.
            message = "fix: seed release planning fixture\n"
            validation = subprocess.run(
                [str(ROOT / "node_modules/.bin/commitlint"), "--config", str(ROOT / "commitlint.config.mjs")],
                input=message, capture_output=True, text=True, timeout=30,
            )
            self.assertEqual(0, validation.returncode, validation.stdout + validation.stderr)
            git("-c", "user.name=Release fixture", "-c", "user.email=release-fixture@hexalith.test",
                "commit", "--no-gpg-sign", "-m", message.strip())
            before = git("for-each-ref", "--format=%(refname) %(objectname)")
            environment.update({
                "CI": "true", "GITHUB_ACTIONS": "true", "GITHUB_EVENT_NAME": "push",
                "GITHUB_REF": "refs/heads/main", "GITHUB_REF_NAME": "main",
                "GITHUB_SHA": git("rev-parse", "HEAD").strip(),
                "GITHUB_REPOSITORY": release_contract.REPOSITORY,
                "GITHUB_WORKSPACE": str(root),
            })
            result = subprocess.run(
                ["node", str(SCRIPTS / "verify-semantic-release-plan.mjs"), "1.0.0"],
                cwd=root, env=environment, capture_output=True, text=True, timeout=60,
            )
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("Semantic Release plan is exactly 1.0.0.", result.stdout)
            output_path = root / "github-output.txt"
            environment["GITHUB_OUTPUT"] = str(output_path)
            resolved = subprocess.run(
                ["node", str(SCRIPTS / "verify-semantic-release-plan.mjs")],
                cwd=root, env=environment, capture_output=True, text=True, timeout=60,
            )
            self.assertEqual(0, resolved.returncode, resolved.stdout + resolved.stderr)
            self.assertIn("Semantic Release plan is exactly 1.0.0.", resolved.stdout)
            self.assertEqual("release-version=1.0.0\n", output_path.read_text(encoding="utf-8"))
            self.assertEqual(before, git("for-each-ref", "--format=%(refname) %(objectname)"))

    def test_release_assets_reject_missing_and_unexpected_names(self) -> None:
        payloads = published_payloads()
        assets = release_assets("1.0.0", payloads)
        release = {
            "tag_name": "v1.0.0",
            "draft": False,
            "prerelease": False,
            "assets": assets[:-1],
        }
        with self.assertRaisesRegex(ValueError, "exactly eight assets"):
            release_state._release_asset_digests(release, "1.0.0")

        unexpected = [*assets]
        unexpected[-1] = {
            "name": "Hexalith.Conversations.Unexpected.1.0.0.snupkg",
            "state": "uploaded",
            "size": 1,
            "digest": f"sha256:{'0' * 64}",
            "browser_download_url": (
                f"https://github.com/{release_contract.REPOSITORY}/releases/download/v1.0.0/"
                "Hexalith.Conversations.Unexpected.1.0.0.snupkg"
            ),
        }
        release["assets"] = unexpected
        with self.assertRaisesRegex(ValueError, "unexpected="):
            release_state._release_asset_digests(release, "1.0.0")

    def test_nuget_publication_poll_retries_transient_responses(self) -> None:
        payloads = published_payloads()
        attempts: dict[str, int] = {}

        def transient_then_present(_method: str, url: str) -> release_state.HttpResponse:
            package_id = next(
                package_id
                for package_id in release_contract.EXACT_PACKAGE_IDS
                if f"/{package_id.lower()}/" in url
            )
            attempts[package_id] = attempts.get(package_id, 0) + 1
            if package_id == "Hexalith.Conversations" and attempts[package_id] == 1:
                return release_state.HttpResponse(429)
            return release_state.HttpResponse(200, payloads[package_id])

        digests = release_state._poll_nuget_packages(
            "1.0.0",
            transient_then_present,
            attempts=2,
            delay_seconds=0,
        )
        self.assertEqual(set(release_contract.EXACT_PACKAGE_IDS), set(digests))
        self.assertEqual(2, attempts["Hexalith.Conversations"])

    def test_public_consumer_execution_failure_is_blocking(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            project = root / "PublicConsumer.csproj"
            project.write_text("<Project />\n", encoding="utf-8")
            config = root / "NuGet.Config"
            config.write_text("<configuration />\n", encoding="utf-8")
            failure = subprocess.CalledProcessError(9, ["dotnet", "PublicConsumer.dll"])
            with mock.patch.object(
                validate_consumer_packages,
                "run_dotnet",
                side_effect=[None, None, failure],
            ) as run_dotnet:
                with self.assertRaises(subprocess.CalledProcessError) as raised:
                    validate_consumer_packages.validate_consumer(project, config, {}, False)
            self.assertEqual(9, raised.exception.returncode)
            executed = run_dotnet.call_args_list[2].args[0]
            self.assertEqual(str(root / "bin/Release/net10.0/PublicConsumer.dll"), executed[0])
            self.assertEqual(1, len(executed))

    def test_local_package_source_is_xml_attribute_escaped(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            package_source = root / 'feed & "quoted"'
            config = validate_consumer_packages.write_nuget_config(root / "consumer", package_source)
            document = ElementTree.parse(config)
            source = document.find(".//add[@key='local-conversations']")
            self.assertIsNotNone(source)
            self.assertEqual(str(package_source), source.attrib["value"])

    def test_publication_preflight_routes_verify_and_publish_phases(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            capture = root / "capture.txt"
            python = root / "python3"
            python.write_text(
                "#!/usr/bin/env bash\nprintf '%s\\n' \"$*\" > \"$CAPTURE_PATH\"\n",
                encoding="utf-8",
            )
            python.chmod(0o755)
            environment = {
                **os.environ,
                "PATH": f"{root}{os.pathsep}{os.environ['PATH']}",
                "CAPTURE_PATH": str(capture),
                "HEXALITH_BUILDS_EXECUTION_SHA": release_contract.BUILDS_EXECUTION_SHA,
                "HEXALITH_RELEASE_SOURCE_BRANCH": "main",
                "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW": "ci.yml",
                "HEXALITH_RELEASE_PACKAGE_MANIFEST": "tools/release-packages.json",
                "HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT": "4",
                "HEXALITH_RELEASE_ENVIRONMENT": "production",
                "HEXALITH_RELEASE_REQUIRE_AUTHORITY": "false",
                "GITHUB_REPOSITORY": release_contract.REPOSITORY,
                "GITHUB_REF": "refs/heads/main",
                "GITHUB_SHA": "a" * 40,
            }
            command = ["bash", str(SCRIPTS / "validate-publication-preflight.sh"), "1.1.0"]

            subprocess.run([*command, "verify"], cwd=ROOT, env=environment, check=True)
            verify_arguments = capture.read_text(encoding="utf-8").strip()
            self.assertTrue(verify_arguments.startswith("scripts/verify-release-source.py "))
            self.assertIn("--destination-state absent", verify_arguments)
            self.assertIn("--release-version 1.1.0", verify_arguments)

            subprocess.run([*command, "publish"], cwd=ROOT, env=environment, check=True)
            publish_arguments = capture.read_text(encoding="utf-8").strip()
            self.assertTrue(publish_arguments.startswith("scripts/verify-release-state.py publishable 1.1.0 "))

    def test_unmapped_build_refuses_every_ambient_configuration(self) -> None:
        # Use the real consumer XML with an inert catalog bootstrap so absent nested
        # checkouts cannot hide the consumer's explicit missing-mapping diagnostic.
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for name in ("Directory.Build.props", "Directory.Packages.props", "global.json"):
                (root / name).write_bytes((ROOT / name).read_bytes())
            catalog = root / "references/Hexalith.Builds/Props/Directory.Packages.props"
            catalog.parent.mkdir(parents=True)
            catalog.write_text("<Project />", encoding="utf-8")
            project = root / "Hexalith.Conversations.Contracts.csproj"
            project.write_bytes((ROOT / "src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj").read_bytes())
            for configuration, ci in (("Debug", "false"), ("Release", "false"), ("Debug", "true")):
                result = subprocess.run(
                    ["dotnet", "msbuild", str(project), "-nologo", "-target:PrepareForBuild",
                     f"-p:Configuration={configuration}", f"-p:CI={ci}"],
                    cwd=root,
                    env=validate_consumer_packages.isolated_consumer_environment(os.environ.copy()),
                    check=False, capture_output=True, text=True,
                )
                self.assertNotEqual(0, result.returncode)
                self.assertIn("requires the Platform tool-selected source mapping", result.stdout + result.stderr)
        # Actual source/Debug and package/Release graph evaluations are exercised by
        # PlatformEnrollmentMappingTest.EvaluatedReferencesUseOnlyMappedIdentities.

    def test_pack_inherits_producer_mapping_without_overriding_selection(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary)
            with mock.patch.object(sys, "argv", ["pack-release-packages.py", str(output), "1.2.3"]), \
                 mock.patch.object(pack_release_packages, "prepare_output_directory", return_value=output), \
                 mock.patch.object(pack_release_packages.subprocess, "run") as run:
                self.assertEqual(0, pack_release_packages.main())
            self.assertEqual(4, run.call_count)
            for call in run.call_args_list:
                arguments = call.args[0]
                self.assertIn("-p:Version=1.2.3", arguments)
                self.assertEqual("Release", arguments[arguments.index("--configuration") + 1])
                self.assertFalse(any("UseHexalithProjectReferences" in argument for argument in arguments))
                self.assertNotIn("env", call.kwargs)

    def test_standalone_consumers_remove_parent_mapping_imports(self) -> None:
        parent = {key: "parent-owned" for key in (
            "CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps",
            "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets",
            "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH", "Configuration")}
        parent.update(PATH="/bin", NUGET_PACKAGES="cache", customaftermicrosoftcommonprops="case-insensitive")
        isolated = validate_consumer_packages.isolated_consumer_environment(parent)
        self.assertEqual({"PATH": "/bin", "NUGET_PACKAGES": "cache"}, isolated)
        self.assertEqual("parent-owned", parent["Configuration"])
        with tempfile.TemporaryDirectory() as temporary:
            project = validate_consumer_packages.write_public_consumer(Path(temporary), "1.0.0")
            with mock.patch.object(validate_consumer_packages, "run_dotnet") as run:
                validate_consumer_packages.validate_consumer(project, Path(temporary) / "NuGet.Config", isolated, False)
            self.assertEqual(3, run.call_count)
            self.assertIn("-p:Configuration=Release", run.call_args_list[0].args[0])
            self.assertEqual("Release", run.call_args_list[1].args[0][4])
            for call in run.call_args_list:
                self.assertEqual(isolated, call.args[2])

    def test_workflows_pin_bypass_and_freeze_contract(self) -> None:
        release = (ROOT / ".github" / "workflows" / "release.yml").read_text(encoding="utf-8")
        self.assertEqual(3, release.count(release_contract.BUILDS_EXECUTION_SHA))
        self.assertIn("HEXALITH_RELEASE_REQUIRE_AUTHORITY: 'false'", release)
        self.assertIn("environment: production", release)
        self.assertIn("HEXALITH_RELEASE_PUBLISH_ENABLED", release)
        self.assertIn("if: ${{ needs.verify-source.outputs.publish-enabled == 'true' }}", release)
        self.assertNotIn("inputs.version", release)
        self.assertIn("run: npm run verify:release-plan\n", release)
        self.assertIn('verify:release-plan -- "$RELEASE_VERSION"', release)
        self.assertIn("needs.verify-source.outputs.release-version", release)
        self.assertIn("--destination-state deferred", release)
        self.assertIn("release-version: ${{ steps.plan.outputs.release-version }}", release)
        self.assertIn("NuGet/login@8d196754b4036150537f80ac539e15c2f1028841", release)
        self.assertIn("NUGET_TRUSTED_PUBLISHING_USER: ${{ vars.NUGET_TRUSTED_PUBLISHING_USER }}", release)
        self.assertIn("NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}", release)
        self.assertNotIn("secrets.NUGET_API_KEY", release)
        self.assertNotIn("secrets: inherit", release)

        configuration = json.loads((ROOT / ".releaserc.json").read_text(encoding="utf-8"))
        github_options = configuration["plugins"][-1][1]
        self.assertEqual(["nupkgs/*.nupkg", "nupkgs/*.snupkg"], github_options["assets"])
        self.assertNotIn("--skip-duplicate", json.dumps(configuration))
        exec_options = configuration["plugins"][2][1]
        self.assertIn("validate-nuget-packages.py ./nupkgs ${nextRelease.version}", exec_options["prepareCmd"])
        self.assertTrue(
            exec_options["prepareCmd"].endswith(
                "validate-publication-preflight.sh ${nextRelease.version} verify >&2"
            )
        )
        planner = (SCRIPTS / "verify-semantic-release-plan.mjs").read_text(encoding="utf-8")
        self.assertIn("readFile('.releaserc.json'", planner)
        self.assertIn("['init', '--bare', localRemote]", planner)
        self.assertNotIn("github.com/Hexalith", planner)

    def test_release_solution_and_projects_exclude_external_release_source_edges(self) -> None:
        solution = (ROOT / "Hexalith.Conversations.slnx").read_text(encoding="utf-8")
        self.assertNotIn("references/Hexalith.", solution)

        for project in [*ROOT.glob("src/**/*.csproj"), *ROOT.glob("tests/**/*.csproj")]:
            root = __import__("xml.etree.ElementTree", fromlist=["ElementTree"]).parse(project).getroot()
            for item_group in root.findall("ItemGroup"):
                for reference in item_group.findall("ProjectReference"):
                    include = reference.attrib.get("Include", "")
                    if "references\\Hexalith." in include or "$(Hexalith" in include:
                        self.assertEqual(
                            "'$(UseHexalithProjectReferences)' == 'true'",
                            item_group.attrib.get("Condition"),
                            f"external source edge is not Debug-only: {project}: {include}",
                        )

    def test_ci_and_security_automation_cover_release_boundary(self) -> None:
        ci = (ROOT / ".github" / "workflows" / "ci.yml").read_text(encoding="utf-8")
        tooling_job = ci.split("  tooling:", maxsplit=1)[1]
        windows_job = ci.split("\n  testsupport-windows:\n", maxsplit=1)[1].split("\n  repository:\n", maxsplit=1)[0]
        windows_checkout = windows_job.split("      - name: Check out source\n", maxsplit=1)[1].split("\n      - ", maxsplit=1)[0]
        self.assertLess(windows_job.index("git config --global core.longpaths true"), windows_job.index("      - name: Check out source\n"))
        self.assertIn("uses: actions/checkout@", windows_checkout)
        self.assertIn("          fetch-depth: 0\n", windows_checkout)
        self.assertNotIn("sparse-checkout", windows_checkout)
        self.assertIn("domain-ci.yml@main", ci)
        self.assertIn("run-consumer-validation: true", ci)
        unit_projects = ci.split("      unit-test-projects: |", maxsplit=1)[1].split("      integration-test-projects:", maxsplit=1)[0]
        self.assertIn("tests/Hexalith.Conversations.Client.Tests", [line.strip() for line in unit_projects.splitlines()])
        self.assertIn("npm audit signatures", ci)
        self.assertIn("uses: Hexalith/Hexalith.Builds/Github/initialize-build@main", tooling_job)
        self.assertIn("uses: actions/setup-dotnet@v6.0.0", tooling_job)
        self.assertIn("global-json-file: global.json", tooling_job)
        self.assertIn("npm test", ci)
        self.assertIn("tests/Hexalith.Conversations.Admin.Web.Tests", ci)
        self.assertIn("./tests/install-playwright.ps1 -Configuration Release", ci)
        self.assertIn("name: ci / admin-web", ci)
        self.assertIn("aspire-timeout-minutes: 20", ci)
        self.assertIn("fetch-depth: 0", ci)
        self.assertIn("npx --no-install commitlint --config commitlint.config.mjs --from", ci)
        self.assertIn(
            "npx --no-install commitlint --config commitlint.config.mjs --verbose <<< \"$PR_TITLE\"",
            ci,
        )
        # CI tests the application only; BMAD and story tooling runs in BMAD agent work.
        for bmad_tooling in ("_bmad", "pytest", "uv sync", "uv run"):
            self.assertNotIn(bmad_tooling, ci)

        global_json = json.loads((ROOT / "global.json").read_text(encoding="utf-8"))
        self.assertEqual("Microsoft.Testing.Platform", global_json["test"]["runner"])

        dependabot = (ROOT / ".github" / "dependabot.yml").read_text(encoding="utf-8")
        for ecosystem in ("nuget", "npm", "github-actions"):
            self.assertIn(f'package-ecosystem: "{ecosystem}"', dependabot)
        self.assertTrue((ROOT / ".github" / "workflows" / "codeql.yml").is_file())
        self.assertTrue((ROOT / ".github" / "workflows" / "dependency-review.yml").is_file())


if __name__ == "__main__":
    unittest.main()
