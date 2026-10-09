# Signalis VR

Experimental Windows PC VR mod for SIGNALIS. Latest documented candidate: **0.7.161**, October 9, 2026. No stable release yet.

The tester reports that the complete **fresh install worked**. Earlier user observations confirmed HE flaregun switching, hands/grips/models, the Nowhere minimap, and computer functionality. These reports do not establish universal graphics stability or smooth movement.

See [current status and installation requirements](STATUS-2026-10-09.md), [remaining issues](CURRENT_ISSUES.md), and [roadmap](ROADMAP.md).

## Download and install

[Download the full 0.7.161 fresh-install prerelease](https://github.com/stevethetitan92/Signalis-VR/releases/tag/v0.7.161-test.1). Choose **SignalisVR-0.7.161-FreshInstall-BepInEx.zip** under Assets; the automatic Source code downloads are not install packages. Close SIGNALIS, preserve a working backup, and extract the ZIP contents beside SIGNALIS.exe. Read the release notes and included INSTALL-FIRST.txt.

## Required setup

Use a legitimate Windows 64-bit SIGNALIS installation, SteamVR, and your headset connection software. The tested package uses **MelonLoader 0.5.7 x64** and requires all three mods:

- `Mods/SignalisVrTracking.dll` (0.7.161)
- `Mods/FPv2_reworked.dll`
- `Mods/CameraPerspectiveSolid.dll`

Also required: the matching `openvr_api.dll` and `SignalisVrRenderBridge.dll` in `UserLibs/SignalisVrTracking/`. The tested fresh-install package includes the requested BepInEx folder; its separate plugin activation is not established by the startup report. Preserve a working fallback and close the game before manually replacing files. No automatic installation or stable updates.

## Current source for review

[SignalisVR-code-review-0.7.161.zip](SignalisVR-code-review-0.7.161.zip) contains current mod logic and native bridge source. It excludes embedded model/texture/audio/artwork payloads, game binaries, proprietary assemblies, personal settings, saves and raw runtime logs. **It is a code-review snapshot, not a complete build or install package.** Its omitted-file list and original source manifest describe the missing parts.

Historical source archives, status files and the root `src` tree remain available. The root `src` tree is historical; use the versioned review snapshot for current logic. Do not assume older checkpoints have current behavior.

## Variant behavior and validation

Passage door/ladder artwork and labels are hidden on desktop and in VR; actual interactions and approved object markers remain. Preserve books, dialogue, puzzles, inventory, controls and native rendering behavior.

0.7.161 passed 110 automated regression checks and two matching deterministic builds. Automated checks do not establish headset appearance, reflections or performance. Computer disk-drive view/crosshair changes, movement stutter, enemy graphics and other open issues need real game/headset checks.

## License and contributions

Project-owned code is GPL-3.0-only; see [LICENSE](LICENSE). Third-party code retains its own notices. This does not license SIGNALIS, game assets or proprietary assemblies. See [third-party notices](THIRD_PARTY_NOTICES.md) and [contributing](CONTRIBUTING.md). The mod remains free; optional support does not unlock exclusive features.

## Assets Used

- **Signalis Mega Pack** - [mod.io](https://mod.io/g/bonelab/m/signalis-mega-pack#description)
- **HD Signalis Weapon Pack** - [mod.io](https://mod.io/g/bonelab/m/hd-signalis-weapon-pack#description)
- **Signalis-ified M92 Beretta** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-m92-beretta-52c5b4bc8021449885a47075150e17f2)
- **Signalis Thermite Flare** - [Sketchfab](https://sketchfab.com/3d-models/signalis-thermite-flare-879c71c3dabd4b1f873e5f4706e70498)
- **Signalis-ified MP5 Swordfish "The Xiphias"** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-mp5-swordfish-the-xiphias-1b4c4426802d4dd9b823429e53b01756)
