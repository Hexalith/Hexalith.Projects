"""Compare exact CLR payload bytes after identifying only reproducibility metadata.

Do not normalize method bodies, type/member signatures, attributes, resources,
assembly references, or CLR metadata other than the module MVID GUID stream.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[5]
OUT = Path(__file__).resolve().parent
OLD = ROOT / "_bmad-output/implementation-artifacts/evidence/epic6/6.6/review-pass2/candidate/Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg"
NEW = OUT / "rebuilt-Hexalith.Projects.Contracts.2.0.0-story66.3.nupkg"
DLL = "lib/net11.0/Hexalith.Projects.Contracts.dll"


def require(condition, message):
    if not condition:
        raise ValueError(message)

def analyze(data):
    pe = struct.unpack_from("<I", data, 0x3c)[0]
    require(data[pe:pe+4] == b"PE\0\0", "Missing PE header")
    count = struct.unpack_from("<H", data, pe+6)[0]
    optional_size = struct.unpack_from("<H", data, pe+20)[0]
    optional = pe+24
    magic = struct.unpack_from("<H", data, optional)[0]
    directory = optional + (96 if magic == 0x10b else 112)
    sections = []
    for i in range(count):
        entry = optional+optional_size+40*i
        virtual_size, rva, size, offset = struct.unpack_from("<IIII", data, entry+8)
        sections.append((rva, max(virtual_size,size), offset))
    def raw(rva):
        for begin,size,offset in sections:
            if begin <= rva < begin+size:
                return offset+rva-begin
        raise ValueError("unmapped RVA")
    clr = raw(struct.unpack_from("<I", data, directory+14*8)[0])
    metadata = raw(struct.unpack_from("<I", data, clr+8)[0])
    require(data[metadata:metadata+4] == b"BSJB", "Missing CLR metadata header")
    version_length = struct.unpack_from("<I", data, metadata+12)[0]
    flags = metadata+16+version_length
    stream_count = struct.unpack_from("<H", data, flags+2)[0]
    stream = flags+4
    streams = {}
    for i in range(stream_count):
        offset,size = struct.unpack_from("<II", data, stream)
        end = data.index(b"\0", stream+8)
        name = data[stream+8:end].decode()
        streams[name] = (metadata+offset,size)
        stream = (end+4)&~3
    # The package's #GUID stream contains exactly its sole module MVID.
    guid,size = streams["#GUID"]
    require(size == 16, "The GUID stream must contain only the sole module MVID")
    excluded = [(pe+8,4,"PE deterministic timestamp"), (guid,size,"CLR module MVID")]
    debug_rva,debug_size = struct.unpack_from("<II",data,directory+6*8)
    debug = raw(debug_rva)
    records=[]
    for i in range(debug_size//28):
        entry = debug+28*i
        kind,size,rva,pointer = struct.unpack_from("<IIII",data,entry+12)
        excluded.append((entry+4,4,"PE debug directory timestamp"))
        record={"type":kind,"offset":pointer,"size":size}
        if kind == 2:
            require(data[pointer:pointer+4] == b"RSDS", "Unsupported CodeView record")
            record["pdbPath"] = data[pointer+24:pointer+size].rstrip(b"\0").decode()
            excluded.append((pointer+4,size-4,"CodeView PDB identity, age and source path"))
        elif kind == 19:
            record["algorithm"] = data[pointer:data.index(b"\0",pointer)].decode()
            require(record["algorithm"] == "SHA256", "Unsupported PDB checksum algorithm")
            excluded.append((pointer+7,size-7,"PDB SHA256 checksum"))
        records.append(record)
    normalized = bytearray(data)
    for offset,size,reason in excluded:
        normalized[offset:offset+size] = b"\0"*size
    return {"metadataStreams":streams,"debugRecords":records,"excluded":excluded,
            "normalizedSha256":hashlib.sha256(normalized).hexdigest()}

def compare_archives(retained=OLD, rebuilt=NEW):
    """Compare every payload entry; normalize only parsed metadata in the one Contracts DLL."""
    retained, rebuilt = Path(retained), Path(rebuilt)
    with zipfile.ZipFile(retained) as oldz, zipfile.ZipFile(rebuilt) as newz:
        old_names, new_names = oldz.namelist(), newz.namelist()
        require(len(old_names) == len(set(old_names)), "Duplicate retained ZIP entries")
        require(len(new_names) == len(set(new_names)), "Duplicate rebuilt ZIP entries")
        require(set(old_names) == set(new_names), "ZIP payload entry sets differ")
        require(DLL in old_names, "Missing Contracts DLL")
        non_dll = {}
        for name in sorted(set(old_names) - {DLL}):
            old, new = oldz.read(name), newz.read(name)
            require(old == new, "Non-DLL payload differs: " + name)
            non_dll[name] = hashlib.sha256(old).hexdigest()
        old_analysis, new_analysis = analyze(oldz.read(DLL)), analyze(newz.read(DLL))
    require(old_analysis["metadataStreams"] == new_analysis["metadataStreams"], "CLR metadata stream layouts differ")
    require(old_analysis["excluded"] == new_analysis["excluded"], "Parsed PE/PDB normalization ranges differ")
    require(old_analysis["normalizedSha256"] == new_analysis["normalizedSha256"], "CLR metadata or method payload differs")
    return {"retainedArchiveSha256": hashlib.sha256(retained.read_bytes()).hexdigest(),
        "rebuiltArchiveSha256": hashlib.sha256(rebuilt.read_bytes()).hexdigest(),
        "retained": old_analysis, "rebuilt": new_analysis,
        "identicalNonduplicateZipEntrySets": True, "nonDllPayloadSha256": non_dll,
        "identicalClrMetadataAndMethodBodies": True, "nuspecIdentical": True, "readmeIdentical": True,
        "normalizationBoundary": "Only PE timestamps, sole CLR MVID, CodeView PDB identity/path and PDB checksum. All method IL, CLR signature tables/blobs, assembly references, contract custom attributes and resources remain byte-for-byte unchanged."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--retained", type=Path, default=OLD)
    parser.add_argument("--rebuilt", type=Path, default=NEW)
    parser.add_argument("--output", type=Path, default=OUT / "historical-payload-equivalence.json")
    options = parser.parse_args()
    if options.output.exists():
        raise FileExistsError("Refusing existing qualification receipt: " + str(options.output))
    receipt = compare_archives(options.retained, options.rebuilt)
    with options.output.open("x", encoding="utf-8") as stream:
        stream.write(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
