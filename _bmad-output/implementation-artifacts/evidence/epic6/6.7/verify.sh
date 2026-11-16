#!/usr/bin/env bash
set -u
project_root=$(pwd)
evidence="$project_root/_bmad-output/implementation-artifacts/evidence/epic6/6.7"
# Existing source-inspection tests locate the repository by walking up from the assembly.
# Keep build outputs private while exposing only the existing checkout's inputs at that ancestor.
for entry in "$project_root"/* "$project_root"/.[!.]*; do
    [ -e "$entry" ] || continue
    if [ ! -e "$TMPDIR/$(basename "$entry")" ]; then ln -s "$entry" "$TMPDIR/$(basename "$entry")"; fi
done
export STORY67_AUTHENTICATED_OBSERVATIONS="$evidence/authenticated-observations.json"
overall=0
suffixes=("$@")
if [ "${#suffixes[@]}" -eq 0 ]; then suffixes=('' '.Contracts' '.Client' '.Server' '.UI'); fi
for suffix in "${suffixes[@]}"; do
    name="Hexalith.Projects${suffix}.Tests"
    label=${suffix#.}
    label=${label:-Domain}
    label="$label${STORY67_ATTEMPT:-}"
    printf '%s\n' "Building $name"
    dotnet build "tests/$name/$name.csproj" --configuration Debug -m:1 --disable-build-servers --artifacts-path "$TMPDIR/artifacts" > "$evidence/$label-build.log" 2>&1
    build_result=$?
    if [ "$build_result" -eq 0 ]; then
        native_arguments=(-xml "$evidence/$label.xml")
        if [ -n "${STORY67_TEST_CLASS:-}" ]; then native_arguments+=(-class "$STORY67_TEST_CLASS"); fi
        dotnet "$TMPDIR/artifacts/bin/$name/debug/$name.dll" "${native_arguments[@]}" > "$evidence/$label-tests.log" 2>&1
        test_result=$?
        tail -n 6 "$evidence/$label-tests.log"
        if [ "$test_result" -ne 0 ]; then overall=1; fi
    else
        tail -n 18 "$evidence/$label-build.log"
        overall=1
    fi
done
exit "$overall"
