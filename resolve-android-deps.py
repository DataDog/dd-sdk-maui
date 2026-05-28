#!/usr/bin/env python3
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
"""
resolve-android-deps.py

Parses a Gradle dependency tree, compares it against android-transitive-deps.json,
and auto-updates AndroidMavenLibrary versions in csproj files.

Called by resolve-android-deps.sh — not intended for standalone use.

Arguments:
    $1  Path to android-transitive-deps.json
    $2  Path to bindings/ directory
    $3  "true" or "false" for check-only mode

Environment:
    GRADLE_OUTPUT  Output of Gradle :datadogwrapper:dependencies
"""

import json
import re
import sys
import os

deps_json_path = sys.argv[1]
bindings_dir = sys.argv[2]
check_only = sys.argv[3].lower() == "true"

gradle_output = os.environ.get("GRADLE_OUTPUT", "")

# ── Parse Gradle tree ─────────────────────────────────────────────────────────
# Extract resolved versions. Gradle format:
#   +--- group:artifact:version
#   +--- group:artifact:version -> resolved_version
# We take the resolved version (after ->) when present.

resolved = {}

for line in gradle_output.splitlines():
    m = re.search(r'[\+\\|]---\s+([\w.\-]+:[\w.\-]+):([\w.\-]+)(?:\s+->\s+([\w.\-]+))?', line)
    if m:
        artifact = m.group(1)
        version = m.group(3) if m.group(3) else m.group(2)
        # Skip Datadog's own artifacts — managed by update-native-sdk.sh
        if artifact.startswith("com.datadoghq:"):
            continue
        resolved[artifact] = version

# ── Load current mapping ──────────────────────────────────────────────────────

with open(deps_json_path, "r") as f:
    mapping = json.load(f)

deps = mapping["dependencies"]

# ── Compare ───────────────────────────────────────────────────────────────────

maven_updates = []       # (artifact, old_ver, new_ver)
nuget_warnings = []      # (artifact, nuget_name, old_maven, new_maven, covered)
new_deps = []            # artifacts in Gradle but not in mapping
removed_deps = []        # artifacts in mapping but not in Gradle

def parse_version(v):
    """Parse a version string into a tuple of ints for comparison."""
    parts = []
    for p in re.split(r'[.\-]', v):
        try:
            parts.append(int(p))
        except ValueError:
            parts.append(0)
    return tuple(parts)

mapped_artifacts = set(deps.keys())

for artifact, info in deps.items():
    old_maven = info.get("maven_version")
    if old_maven is None:
        continue

    new_maven = resolved.get(artifact)
    if new_maven is None:
        removed_deps.append(artifact)
        continue

    if old_maven != new_maven:
        maven_updates.append((artifact, old_maven, new_maven))
        nuget_name = info.get("nuget_name")
        if nuget_name and info.get("package_ref_csproj"):
            # Check if the current NuGet package already covers the new Maven version
            covers_maven = info.get("nuget_covers_maven")
            if covers_maven and parse_version(covers_maven) >= parse_version(new_maven):
                covered = True
            else:
                covered = False
            nuget_warnings.append((artifact, nuget_name, old_maven, new_maven, covered))

# Detect new deps from tracked groups
tracked_groups = {
    "com.squareup.okhttp3", "com.squareup.okio", "com.lyft.kronos",
    "com.google.code.gson", "org.jetbrains.kotlin",
    "androidx.annotation", "androidx.collection", "androidx.work"
}

for artifact, version in sorted(resolved.items()):
    if artifact not in mapped_artifacts:
        group = artifact.rsplit(":", 1)[0]
        if group in tracked_groups:
            new_deps.append((artifact, version))

# ── Report ────────────────────────────────────────────────────────────────────

has_changes = bool(maven_updates or nuget_warnings or new_deps or removed_deps)

if not has_changes:
    print("\033[0;32m\u2713\033[0m All transitive dependency versions match the mapping \u2014 no changes needed")
    sys.exit(0)

if maven_updates:
    print("")
    print("\033[0;34mMaven version changes detected:\033[0m")
    for artifact, old_ver, new_ver in maven_updates:
        print(f"  {artifact}: {old_ver} \u2192 {new_ver}")

nuget_covered = [(a, n, om, nm) for a, n, om, nm, c in nuget_warnings if c]
nuget_uncovered = [(a, n, om, nm) for a, n, om, nm, c in nuget_warnings if not c]

if nuget_covered:
    print("")
    print("\033[0;32mNuGet packages already cover the new Maven versions:\033[0m")
    for artifact, nuget_name, old_maven, new_maven in nuget_covered:
        nuget_ver = deps[artifact].get("nuget_version", "?")
        covers = deps[artifact].get("nuget_covers_maven", "?")
        print(f"  \033[0;32m\u2713\033[0m {nuget_name} {nuget_ver} (covers up to Maven {covers})")
        print(f"    Maven dep {artifact} changed: {old_maven} \u2192 {new_maven} \u2014 no NuGet update needed")

if nuget_uncovered:
    print("")
    print("\033[1;33m\u26a0 NuGet PackageReference versions need updating:\033[0m")
    for artifact, nuget_name, old_maven, new_maven in nuget_uncovered:
        nuget_ver = deps[artifact].get("nuget_version", "?")
        covers = deps[artifact].get("nuget_covers_maven", "?")
        print(f"  {nuget_name} (currently {nuget_ver}, covers up to Maven {covers})")
        print(f"    Maven dep {artifact} changed: {old_maven} \u2192 {new_maven}")
        print(f"    \u2192 Find a {nuget_name} release on nuget.org that covers Maven {new_maven}")

if new_deps:
    print("")
    print("\033[1;33m\u26a0 New transitive dependencies not in mapping:\033[0m")
    for artifact, version in new_deps:
        print(f"  {artifact}:{version}")
        print(f"    \u2192 Add to android-transitive-deps.json if it needs a NuGet equivalent")

if removed_deps:
    print("")
    print("\033[1;33m\u26a0 Dependencies in mapping but no longer resolved by Gradle:\033[0m")
    for artifact in removed_deps:
        print(f"  {artifact}")
        print(f"    \u2192 Consider removing from android-transitive-deps.json and csproj files")

if check_only:
    print("")
    print("\033[1;33m\u26a0 Running in --check mode \u2014 no files modified\033[0m")
    if maven_updates or new_deps:
        sys.exit(1)
    sys.exit(0)

# ── Apply changes ─────────────────────────────────────────────────────────────

def update_csproj_version(csproj_dir, include_pattern, old_version, new_version):
    """Update Version attribute for a matching Include in a csproj file."""
    csproj_files = [f for f in os.listdir(csproj_dir) if f.endswith(".csproj")]
    if not csproj_files:
        return False

    csproj_path = os.path.join(csproj_dir, csproj_files[0])
    with open(csproj_path, "r") as f:
        content = f.read()

    escaped_pattern = re.escape(include_pattern)
    escaped_old = re.escape(old_version)
    regex = rf'(Include="{escaped_pattern}"[^>]*Version="){escaped_old}(")'
    new_content, count = re.subn(regex, rf'\g<1>{new_version}\2', content)

    if count > 0:
        with open(csproj_path, "w") as f:
            f.write(new_content)
        return True
    return False

updates_applied = 0

for artifact, old_ver, new_ver in maven_updates:
    info = deps[artifact]

    # Auto-update AndroidMavenLibrary entries (they use Maven versions directly)
    for csproj_name in info.get("maven_library_csproj", []):
        csproj_dir = os.path.join(bindings_dir, csproj_name)
        if update_csproj_version(csproj_dir, artifact, old_ver, new_ver):
            print(f"\033[0;32m\u2713\033[0m Updated AndroidMavenLibrary {artifact} \u2192 {new_ver} in {csproj_name}")
            updates_applied += 1

    # Update the tracked maven_version
    info["maven_version"] = new_ver

# Write updated JSON
with open(deps_json_path, "w") as f:
    json.dump(mapping, f, indent=2)
    f.write("\n")

print("")
print(f"\033[0;32m\u2713\033[0m Updated android-transitive-deps.json with new Maven versions")
if updates_applied > 0:
    print(f"\033[0;32m\u2713\033[0m Applied {updates_applied} AndroidMavenLibrary version update(s) in csproj files")

if nuget_uncovered:
    print("")
    print("\033[1;33mAction required:\033[0m Update the NuGet PackageReference versions listed above")
    print("  in the relevant csproj files, then update nuget_version and nuget_covers_maven")
    print("  in android-transitive-deps.json to match.")
    sys.exit(2)
