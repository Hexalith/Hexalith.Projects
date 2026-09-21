#!/usr/bin/env bash
set -euo pipefail

remote_consumer=/tmp/hexalith-p1r-3106-attempt10.1ai9Yh/remote-consumer
remote_cache=/tmp/hexalith-p1r-3106-attempt10.1ai9Yh/caches/remote-packages
bundle=/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3106-20260921-attempt10
manifest="$bundle/eventstore-release-packages-v3.106.0.json"
retained="$bundle/remote-consumer"
inventory="$bundle/remote-package-inventory.tsv"

mkdir -p "$retained/obj"
cp -a "$remote_consumer/RemoteConsumer.csproj" "$retained/RemoteConsumer.csproj"
cp -a "$remote_consumer/NuGet.config" "$retained/NuGet.config"
cp -a "$remote_consumer/obj/." "$retained/obj/"

printf 'package_id\tversion\tcache_directory\tnupkg_sha256\tnuspec_sha256\tmetadata_sha256\tsha512_file_sha256\n' > "$inventory"
while IFS= read -r package_id; do
    normalized_id="$(printf '%s' "$package_id" | tr '[:upper:]' '[:lower:]')"
    package_directory="$remote_cache/$normalized_id/3.106.0"
    nupkg="$package_directory/$normalized_id.3.106.0.nupkg"
    nuspec="$package_directory/$normalized_id.nuspec"
    metadata="$package_directory/.nupkg.metadata"
    sha512_file="$package_directory/$normalized_id.3.106.0.nupkg.sha512"
    test -d "$package_directory"
    test -s "$nupkg"
    test -s "$nuspec"
    test -s "$metadata"
    test -s "$sha512_file"
    printf '%s\t3.106.0\t%s\t%s\t%s\t%s\t%s\n' "$package_id" "${package_directory#"$remote_cache"/}" "$(sha256sum "$nupkg" | cut -d' ' -f1)" "$(sha256sum "$nuspec" | cut -d' ' -f1)" "$(sha256sum "$metadata" | cut -d' ' -f1)" "$(sha256sum "$sha512_file" | cut -d' ' -f1)" >> "$inventory"
done < <(jq -r '.packages[].id' "$manifest")

test "$(jq '.packages | length' "$manifest")" -eq 14
test "$(tail -n +2 "$inventory" | wc -l)" -eq 14
test "$(cut -f1 "$inventory" | tail -n +2 | sort -u | wc -l)" -eq 14
jq -e '.project.restore.sources == {"https://api.nuget.org/v3/index.json": {}}' "$retained/obj/project.assets.json" >/dev/null
jq -e '.project.restore.packagesPath == "/tmp/hexalith-p1r-3106-attempt10.1ai9Yh/caches/remote-packages"' "$retained/obj/project.assets.json" >/dev/null
jq -e '.project.frameworks["net10.0"].downloadDependencies == [{"name":"Hexalith.EventStore.Admin.Cli","version":"[3.106.0, 3.106.0]"}]' "$retained/obj/project.assets.json" >/dev/null
for package_id in $(jq -r '.packages[].id | select(. != "Hexalith.EventStore.Admin.Cli")' "$manifest"); do jq -e --arg key "$package_id/3.106.0" '.libraries[$key] != null' "$retained/obj/project.assets.json" >/dev/null; done
(
    cd "$bundle"
    sha256sum remote-consumer/RemoteConsumer.csproj remote-consumer/NuGet.config remote-consumer/obj/RemoteConsumer.csproj.nuget.dgspec.json remote-consumer/obj/RemoteConsumer.csproj.nuget.g.props remote-consumer/obj/RemoteConsumer.csproj.nuget.g.targets remote-consumer/obj/project.assets.json remote-consumer/obj/project.nuget.cache remote-package-inventory.tsv > remote-consumer-files.sha256
    sha256sum --check remote-consumer-files.sha256
)
