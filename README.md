# SIGNALIS VR

**0.7.167 — TESTER BUILD**

## Download and play

[Download the all-in-one package](https://github.com/stevethetitan92/Signalis-VR/releases/download/v0.7.167-launchers.1/SIGNALIS-VR-0.7.167-All-In-One-v13.zip) — one download for new installations and updates.

1. Download the ZIP and choose **Extract All**.
2. Keep **Launch SteamVR.cmd**, **Launch VirtualDesktop.cmd**, and **Mod Files** together.
3. Close SIGNALIS, open Steam and connect your headset. Open **one** launcher:
   - **Launch VirtualDesktop.cmd** — Virtual Desktop and its PC Streamer, using VDXR. SteamVR is not required.
   - **Launch SteamVR.cmd** — SteamVR. Virtual Desktop is not required.

## Automatic setup, updates and repairs

Choose your preferred VR connection and let the launcher handle the setup. It automatically locates SIGNALIS, checks the selected Virtual Desktop/VDXR or SteamVR runtime configuration and VR bridge, and checks GitHub for mod updates. When an update is available, the launcher downloads it, verifies its checksum and bundled files, and installs the required changes before starting the game. Built-in diagnostics identify failures, attempt supported repairs, and display a clear summary of what was repaired and anything that still needs attention.

**No manual mod-file copying is needed.** The launcher searches Steam libraries, nearby installation folders and connected drives automatically. Multiple installations appear as numbered choices. If discovery and diagnostics cannot locate the game, paste the path to **SIGNALIS.exe** when prompted: **Steam → SIGNALIS → Manage → Browse local files → right-click SIGNALIS.exe → Copy as path**.

Runtime checks validate configuration and components; they do not confirm a live headset or wireless connection. The SteamVR path uses SteamVR/OpenVR; the Virtual Desktop path uses VDXR. Steam, SIGNALIS and your selected connection software must already be installed. First-time mod-loader assembly generation may download dependencies.

The package includes the mod, required companion mods, MelonLoader, BepInEx, both VR providers, embedded assets, instructions, licenses and source archives. Existing configuration is preserved; missing defaults are added. Saves and replaced files are backed up before changes, with rollback attempted if installation or repair fails. Unknown extra mods and original game data are not deleted.

The command window lists **REPAIRED**, **REMAINING FAILURES**, and **UPDATE WARNINGS**, and prints exact log and backup paths. Reports and backups are under the active package’s **Mod Files/Logs** and **Mod Files/Backups**. Original game-file corruption requires **Steam → Properties → Installed Files → Verify integrity**. Save contents are not silently rewritten; selected backups can be restored explicitly after backing up current saves.

An intact local package remains usable if the update service is unavailable. Downloads are cached under `%LOCALAPPDATA%/SignalisVR/Packages`. Keep the extracted folder as your launcher; the command window prints when it switches to a downloaded package.

[Troubleshooting and fixes](TROUBLESHOOTING.md) — launcher errors, downloads, VR connections, logs and save recovery.

Optional flags: `-DiagnoseOnly` checks/updates/repairs without starting the game; `-NoRepair` preserves installed files; `-NoDownload` skips online checks and uses the intact local package; `-ValidateOnly` performs offline checks without installation. Advanced options are in **Mod Files/INSTALL-FIRST.txt**.

## Source and verification

Current launcher and runtime source archives are bundled under **Mod Files/Sources**. Historical repository source remains preserved. GitHub’s automatic Source code ZIP/TAR links are for developers, not installation.

Revision 13 passed 70 discovery scenarios in both supported PowerShell versions, 500 launcher scenarios, 35 setup/health/layout checks, updater recovery checks, and runtime regression suites. Native GPU checks covered rendering-state restoration and frame lifecycle. Automated checks are separate from live headset appearance and frame-rate measurements. See the [release notes](https://github.com/stevethetitan92/Signalis-VR/releases/tag/v0.7.167-launchers.1) for changes and the package checksum.

## Report a problem

[Report an issue](https://github.com/stevethetitan92/Signalis-VR/issues/new?template=bug_report.yml). Include the launcher name, headset, connection software, mod version, what happened, the active package’s diagnostic report and `MelonLoader/Latest.log`. Review personal paths before posting.

## License and contributions

Project-owned code is GPL-3.0-only; see [LICENSE](LICENSE). Third-party code retains its own notices. This does not license SIGNALIS, game assets or proprietary assemblies. See [third-party notices](THIRD_PARTY_NOTICES.md) and [contributing](CONTRIBUTING.md). The mod remains free; optional support does not unlock exclusive features.

## Assets Used

- **Signalis Mega Pack** - [mod.io](https://mod.io/g/bonelab/m/signalis-mega-pack#description)
- **HD Signalis Weapon Pack** - [mod.io](https://mod.io/g/bonelab/m/hd-signalis-weapon-pack#description)
- **Signalis-ified M92 Beretta** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-m92-beretta-52c5b4bc8021449885a47075150e17f2)
- **Signalis Thermite Flare** - [Sketchfab](https://sketchfab.com/3d-models/signalis-thermite-flare-879c71c3dabd4b1f873e5f4706e70498)
- **Signalis-ified MP5 Swordfish "The Xiphias"** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-mp5-swordfish-the-xiphias-1b4c4426802d4dd9b823429e53b01756)
