#!/usr/bin/env python3
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache-2.0 License.
# This product includes software developed at Datadog (https://www.datadoghq.com/)
# Copyright 2026 - Present Datadog, Inc.
#
# verify-no-embedded-deps.py
#
# Enforces the one-provider invariant for Android transitive dependencies: a dependency
# that consumers get as a NuGet PackageReference must NOT also have its Java classes
# packaged into any AAR we ship.
#
# Why this matters: an AndroidMavenLibrary entry bakes the Maven jar's classes into our
# own AAR, where they sit outside NuGet's version resolution. A consuming app that pulls
# a different version of the same library (e.g. okio via AndroidX DataStore or Firebase
# Crashlytics) then ships two copies of those classes and d8 fails the build with
# "okio.-Base64 is defined multiple times". The app author cannot fix that from their
# own project — only we can, by not embedding the classes. See issue #72.
#
# The rule is driven by android-transitive-deps.json: every artifact with a nuget_name
# declares the java_packages it owns, and those packages must not appear in our AARs.
#
# Usage:
#   ./verify-no-embedded-deps.py local-packages/*.nupkg

import json
import os
import sys
import tempfile
import zipfile

GREEN = "\033[0;32m"
RED = "\033[0;31m"
YELLOW = "\033[1;33m"
NC = "\033[0m"

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
DEPS_JSON = os.path.join(SCRIPT_DIR, "android-transitive-deps.json")


def owned_packages(deps):
    """([(java_package_prefix, artifact, nuget_name)], [unguarded_artifacts])."""
    owned = []
    unguarded = []
    for artifact, info in deps.items():
        nuget_name = info.get("nuget_name")
        if not nuget_name:
            continue
        pkgs = info.get("java_packages", [])
        if not pkgs:
            # A NuGet-provided artifact with no declared packages would be checked
            # against nothing, silently. Fail loudly instead.
            unguarded.append(artifact)
            continue
        for pkg in pkgs:
            owned.append((pkg.strip("/") + "/", artifact, nuget_name))
    # Longest prefix first so kotlin/reflect is attributed before kotlin.
    owned.sort(key=lambda t: len(t[0]), reverse=True)
    return owned, unguarded


def classes_by_owner(jar_path, owned):
    """{(artifact, nuget_name): count} of classes in this jar owned by a NuGet package."""
    hits = {}
    try:
        with zipfile.ZipFile(jar_path) as jar:
            names = jar.namelist()
    except zipfile.BadZipFile:
        return hits
    for name in names:
        if not name.endswith(".class"):
            continue
        for prefix, artifact, nuget_name in owned:
            if name.startswith(prefix):
                key = (artifact, nuget_name)
                hits[key] = hits.get(key, 0) + 1
                break  # longest-prefix match wins
    return hits


def scan_aar(aar_path, owned, workdir):
    """[(jar_name, artifact, nuget_name, count)] for one AAR's bundled jars."""
    findings = []
    extract_to = tempfile.mkdtemp(dir=workdir)
    try:
        with zipfile.ZipFile(aar_path) as aar:
            jars = [n for n in aar.namelist() if n.endswith(".jar")]
            for jar_name in jars:
                jar_path = aar.extract(jar_name, extract_to)
                for (artifact, nuget_name), count in classes_by_owner(jar_path, owned).items():
                    findings.append((jar_name, artifact, nuget_name, count))
    except zipfile.BadZipFile:
        print(f"  {YELLOW}⚠{NC} Not a readable AAR, skipping: {aar_path}")
    return findings


def main():
    nupkgs = sys.argv[1:]
    if not nupkgs:
        print(f"  {YELLOW}⚠{NC} No .nupkg paths given — nothing to check")
        return 0

    with open(DEPS_JSON) as f:
        deps = json.load(f)["dependencies"]

    owned, unguarded = owned_packages(deps)
    if unguarded:
        for artifact in unguarded:
            print(f"  {RED}✗{NC} {artifact} is NuGet-provided but declares no java_packages")
        print(f"  {YELLOW}→{NC} Add its Java package prefixes to android-transitive-deps.json so this")
        print(f"    check can actually guard it.")
        return 1
    if not owned:
        print(f"  {RED}✗{NC} No java_packages declared in android-transitive-deps.json")
        return 1

    violations = 0
    checked_aars = 0

    with tempfile.TemporaryDirectory() as workdir:
        for nupkg in nupkgs:
            if not os.path.isfile(nupkg):
                print(f"  {YELLOW}⚠{NC} Not found, skipping: {nupkg}")
                continue

            pkg_dir = tempfile.mkdtemp(dir=workdir)
            with zipfile.ZipFile(nupkg) as pkg:
                aars = [n for n in pkg.namelist() if n.endswith(".aar")]
                for aar_name in aars:
                    aar_path = pkg.extract(aar_name, pkg_dir)
                    checked_aars += 1
                    for jar, artifact, nuget_name, count in scan_aar(aar_path, owned, workdir):
                        violations += 1
                        print(
                            f"  {RED}✗{NC} {os.path.basename(nupkg)} → {aar_name} → {jar}"
                        )
                        print(
                            f"      embeds {count} class(es) from {artifact}, "
                            f"which consumers already get via {nuget_name}"
                        )

    if violations:
        print("")
        print(f"  {RED}✗{NC} {violations} embedded-dependency violation(s) across {checked_aars} AAR(s)")
        print(f"  {YELLOW}→{NC} Remove the AndroidMavenLibrary entry for these artifacts and rely on the")
        print(f"    NuGet PackageReference instead, then update android-transitive-deps.json.")
        return 1

    print(f"  {GREEN}✓{NC} No NuGet-provided classes embedded in {checked_aars} shipped AAR(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
