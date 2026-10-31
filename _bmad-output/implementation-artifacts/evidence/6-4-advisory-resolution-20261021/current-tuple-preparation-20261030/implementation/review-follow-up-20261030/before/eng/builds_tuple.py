"""Read the exact producer/publication binding; incomplete preparation never qualifies a tuple."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BINDING = ROOT / "eng/builds-tuple.json"


def load_binding(path=BINDING, require_publication=True):
    binding = json.loads(path.read_bytes())
    if binding.get("schemaVersion") != 1 or binding.get("candidateAcceptance") is not False:
        raise ValueError("Tuple binding must use schema 1 and candidateAcceptance:false.")
    producer = binding["producer"]
    catalog = producer["catalog"]
    if producer["repository"] != "https://github.com/Hexalith/Hexalith.Builds.git":
        raise ValueError("Tuple producer must be the official Builds repository.")
    for value in (producer["sourceRevision"], catalog["blob"]):
        if not isinstance(value, str) or not re.fullmatch(r"[0-9a-f]{40}", value):
            raise ValueError("Producer source and catalog blob require full lowercase Git identities.")
    if catalog["path"] != "Props/Directory.Packages.props" or not isinstance(catalog["sha256"], str) or not re.fullmatch(r"[0-9a-f]{64}", catalog["sha256"]):
        raise ValueError("Producer catalog path and SHA-256 are required.")
    if require_publication:
        publication = binding["publication"]
        required = ("version", "sourceRevision", "releaseTag", "catalogSha256", "archiveSha256", "toolingSha256")
        missing = [key for key in required if not publication.get(key)]
        if missing:
            raise ValueError("Official publication binding is unavailable: " + ", ".join(missing) + ". Preparation is unqualified.")
        version = publication["version"]
        if not isinstance(version, str) or not re.fullmatch(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?", version):
            raise ValueError("Publication requires an exact package semantic version.")
        if publication["sourceRevision"] != producer["sourceRevision"] or publication["releaseTag"] != "v" + version:
            raise ValueError("Publication source/tag differs from the exact producer binding.")
        if publication["catalogSha256"] != catalog["sha256"]:
            raise ValueError("Publication catalog differs from the prepared source catalog.")
        for key in ("archiveSha256", "toolingSha256"):
            if not isinstance(publication[key], str) or not re.fullmatch(r"[0-9a-f]{64}", publication[key]):
                raise ValueError("Publication requires a lowercase SHA-256 for " + key + ".")
    return binding


def validate_owner_pins(binding, root):
    version = binding["publication"]["version"]
    tools = json.loads((root / ".config/dotnet-tools.json").read_bytes())
    floor = json.loads((root / "hexalith-platform.json").read_bytes())
    if tools["tools"]["hexalith.builds.module.cli"]["version"] != version or floor["toolVersionRange"] != "[" + version + ", 5.0.0-0)":
        raise ValueError("Published tuple must match the owner tool pin and range floor.")


def validate_recorded_source(binding, pin, dirty, require_clean=True):
    if pin != binding["producer"]["sourceRevision"] or (require_clean and dirty):
        raise ValueError("CI requires clean recorded Platform source with the exact approved Builds pin.")


def verify_catalog(binding, builds_root):
    """Check committed bytes and the real checkout, allowing Git's configured CRLF conversion."""
    def git(*arguments):
        return subprocess.check_output(["git", *arguments], cwd=builds_root)

    producer = binding["producer"]
    catalog = producer["catalog"]
    if git("rev-parse", "HEAD").decode().strip() != producer["sourceRevision"]:
        raise ValueError("Builds checkout differs from the bound producer revision.")
    if git("rev-parse", "HEAD:" + catalog["path"]).decode().strip() != catalog["blob"]:
        raise ValueError("Recorded catalog blob differs from the producer binding.")
    if hashlib.sha256(git("show", "HEAD:" + catalog["path"])).hexdigest() != catalog["sha256"]:
        raise ValueError("Recorded catalog SHA-256 differs from the producer binding.")
    subprocess.run(["git", "diff", "--exit-code", "HEAD", "--", catalog["path"]], cwd=builds_root, check=True, capture_output=True)
    return builds_root / catalog["path"]


if __name__ == "__main__":
    print(load_binding()["producer"]["sourceRevision"])
