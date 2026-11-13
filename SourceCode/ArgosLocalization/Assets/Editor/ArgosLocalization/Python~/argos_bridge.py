#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Argos Offline Machine Translation Bridge for Unity Localization Tables.
Reads JSON from stdin:  {"source_locale": "en", "target_locale": "es", "texts": [...]}
Writes JSON to stdout:  {"ok": true, "target_locale": "es", "translations": [...]}

Anything Argos (or its dependencies) prints while working is routed to stderr so
stdout carries exactly one JSON document for the Unity side to parse.
"""

import sys
import io
import json
import traceback


def normalize_code(code: str) -> str:
    """Map Unity locale codes like 'es-ES', 'ja-JP', 'hi-IN' to the ISO 639-1 codes used by Argos."""
    if not code:
        return "en"
    return code.strip().replace("_", "-").split("-")[0].lower()


def ensure_language_pair(from_code: str, to_code: str) -> None:
    import argostranslate.package

    installed = argostranslate.package.get_installed_packages()
    if any(p.from_code == from_code and p.to_code == to_code for p in installed):
        return

    # Auto-install the model on first use (needs network once; offline afterwards)
    argostranslate.package.update_package_index()
    available = argostranslate.package.get_available_packages()
    pkg = next((p for p in available if p.from_code == from_code and p.to_code == to_code), None)
    if pkg is None:
        raise RuntimeError(f"No Argos Translate package found for '{from_code}' -> '{to_code}'.")
    argostranslate.package.install_from_path(pkg.download())


def main() -> int:
    # Force UTF-8 across stdin/stdout on Windows & macOS
    sys.stdin = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8")
    real_stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", write_through=True)
    sys.stdout = sys.stderr  # keep stdout clean for the JSON reply

    def reply(obj) -> None:
        real_stdout.write(json.dumps(obj, ensure_ascii=False))
        real_stdout.flush()

    try:
        raw_input = sys.stdin.read()
        if not raw_input.strip():
            raise ValueError("Empty JSON input received on stdin.")

        payload = json.loads(raw_input)
        src_code = normalize_code(payload.get("source_locale", "en"))
        tgt_code = normalize_code(payload.get("target_locale", "es"))
        texts = payload.get("texts", [])

        if not isinstance(texts, list):
            raise ValueError("'texts' field must be a JSON array of strings.")

        if src_code == tgt_code or len(texts) == 0:
            reply({
                "ok": True,
                "source_locale": src_code,
                "target_locale": tgt_code,
                "translations": [str(t) for t in texts],
            })
            return 0

        import argostranslate.translate

        ensure_language_pair(src_code, tgt_code)

        installed_languages = argostranslate.translate.get_installed_languages()
        from_lang = next((l for l in installed_languages if l.code == src_code), None)
        to_lang = next((l for l in installed_languages if l.code == tgt_code), None)

        if from_lang is None or to_lang is None:
            raise RuntimeError(f"Language model not loaded for {src_code} -> {tgt_code}.")

        translator = from_lang.get_translation(to_lang)
        if translator is None:
            raise RuntimeError(f"Translator object unavailable for {src_code} -> {tgt_code}.")

        translations = []
        for item in texts:
            text_str = "" if item is None else str(item)
            translations.append(text_str if not text_str.strip() else translator.translate(text_str))

        reply({
            "ok": True,
            "source_locale": src_code,
            "target_locale": tgt_code,
            "translations": translations,
        })
        return 0

    except Exception as exc:
        reply({
            "ok": False,
            "error": str(exc),
            "traceback": traceback.format_exc(),
        })
        return 1


if __name__ == "__main__":
    sys.exit(main())
