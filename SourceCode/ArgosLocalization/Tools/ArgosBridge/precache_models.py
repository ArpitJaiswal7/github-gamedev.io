#!/usr/bin/env python3
"""One-time bootstrap: download the Argos en -> X language packs so translation works offline afterwards."""

import sys

import argostranslate.package

TARGET_LOCALES = sys.argv[1:] or ["es", "ja", "hi", "fr", "de"]

argostranslate.package.update_package_index()
available = argostranslate.package.get_available_packages()
installed = {(p.from_code, p.to_code) for p in argostranslate.package.get_installed_packages()}

for target in TARGET_LOCALES:
    if ("en", target) in installed:
        print(f"Already installed: en -> {target}")
        continue
    pkg = next((p for p in available if p.from_code == "en" and p.to_code == target), None)
    if pkg is None:
        print(f"WARNING: no Argos package published for en -> {target}")
        continue
    print(f"Installing offline Argos model: en -> {target}...")
    argostranslate.package.install_from_path(pkg.download())

print("Done.")
