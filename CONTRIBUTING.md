# Contributing to Signalis VR

Fixes, documentation, reproducible bug reports and headset testing are welcome. Start with CURRENT_ISSUES.md and ROADMAP.md. Coordinate substantial changes in an issue; focused fixes can go directly into a pull request.

## Current source

Use `SignalisVR-code-review-0.7.161.zip` for current logic. It deliberately excludes embedded model/texture/audio/artwork payloads, generated asset catalogue/font data and proprietary references, and **cannot build standalone**. The original source manifest and omitted-file list document this scope. The root `src` tree and September source checkpoint archives are historical.

Build work requires a legitimate local game installation and matching loader/game references. Do not redistribute those references. Preserve a working fallback and inspect build scripts before use. No automatic installation is provided.

## Changes and testing

Submit reviewable source or patches, explain the concrete problem and resulting behavior, and identify the starting version. Preserve working controls, puzzles, inventory, books, dialogue and native rendering. Passage door/ladder artwork is intentionally hidden in this variant; actual interactions and approved other markers must remain.

Run affected checks and clearly separate compilation/simulated tests from real game/headset measurements. Include version, scene, headset/runtime, reproduction steps and desktop comparison. Remove private details from logs. Audit map/enemy/camera/world-marker/suppression/reflection lifecycle and costs before a new candidate; compilation alone does not establish graphics stability.

## License

Project-owned code is GPL-3.0-only. Contribute only work you can provide under that license and retain third-party notices. Do not upload game assets, proprietary assemblies, credentials or unrelated personal files. The mod remains free; financial support is optional.
