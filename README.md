# SIGNALIS VR

**0.7.166 — TESTER BUILD**

## Download and play

Use one download for both new installations and updates: **SIGNALIS-VR-0.7.166-All-In-One-v9.zip**.

[Download the all-in-one package](https://github.com/stevethetitan92/Signalis-VR/releases/download/v0.7.166-launchers.1/SIGNALIS-VR-0.7.166-All-In-One-v9.zip)

1. Download the ZIP and choose **Extract All**.
2. Keep **Launch SteamVR.cmd**, **Launch VirtualDesktop.cmd**, and **Mod Files** together.
3. Close SIGNALIS, open Steam, connect your headset, then double-click **one** launcher:
   - **Launch SteamVR.cmd** — use SteamVR. Virtual Desktop is not required.
   - **Launch VirtualDesktop.cmd** — use Virtual Desktop and its PC Streamer. SteamVR is not required.

**No manual mod-file copying is needed.** The launcher finds the game, checks this GitHub repository for the current all-in-one package, downloads it when needed, verifies its checksum and bundled files, then installs, updates or repairs the required mod files before launching.

If asked for the game path: **Steam → SIGNALIS → Manage → Browse local files → right-click SIGNALIS.exe → Copy as path**. Paste the path into the command window and press Enter.

An intact local bundle remains usable if the update service is unavailable. Downloads are cached under `%LOCALAPPDATA%/SignalisVR/Packages`. Keep the extracted folder as your launcher; when it switches to a downloaded package, the command window prints that location.

## Automatic setup and diagnostics

The package includes the mod, required companion mods, MelonLoader, BepInEx, both VR providers, instructions, licenses and source archives. Steam, SIGNALIS and the selected connection software must already be installed. First-time mod-loader assembly generation may download its own dependencies.

Existing configuration is preserved; missing defaults are added. Saves and replaced files are backed up before changes. Backups and reports are in the active package's **Mod Files/Backups** and **Mod Files/Logs**; the launcher prints the exact paths. Unknown extra mods and original game data are not deleted.

Errors and attempted repairs appear in the command window. Original game-file corruption requires **Steam → Properties → Installed Files → Verify integrity**. Medicine, graphics and save errors are reported from log evidence; the launcher does not guess at changes to save contents. Selected save backups can be restored explicitly after backing up current saves.

Optional flags: `-DiagnoseOnly` checks/updates without starting the game; `-NoRepair` preserves installed files; `-NoDownload` uses the intact local bundle without an online check; `-ValidateOnly` performs offline checks. Advanced options are in **Mod Files/INSTALL-FIRST.txt**.

## Source and verification

Launcher source: [launcher/0.7.166/folder-launchers](launcher/0.7.166/folder-launchers). Source archives are bundled under **Mod Files/Sources**, including the current runtime code-review snapshot. Historical repository source remains preserved. GitHub's automatic Source code ZIP/TAR links are for developers, not installation.

35 automatic-setup/health/layout checks and 12 mocked download/recovery checks passed. The existing runtime's regression, dependency and checksum records remain available. Runtime DLLs and assets are unchanged by this launcher update.

## Report a problem

[Report an issue](https://github.com/stevethetitan92/Signalis-VR/issues/new?template=bug_report.yml). Include the launcher name, headset, what happened, the active package's diagnostic report and `MelonLoader/Latest.log`. Review personal paths before posting.

## License and contributions

Project-owned code is GPL-3.0-only; see [LICENSE](LICENSE). Third-party code retains its own notices. This does not license SIGNALIS, game assets or proprietary assemblies. See [third-party notices](THIRD_PARTY_NOTICES.md) and [contributing](CONTRIBUTING.md). The mod remains free; optional support does not unlock exclusive features.

## Assets Used

- **Signalis Mega Pack** - [mod.io](https://mod.io/g/bonelab/m/signalis-mega-pack#description)
- **HD Signalis Weapon Pack** - [mod.io](https://mod.io/g/bonelab/m/hd-signalis-weapon-pack#description)
- **Signalis-ified M92 Beretta** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-m92-beretta-52c5b4bc8021449885a47075150e17f2)
- **Signalis Thermite Flare** - [Sketchfab](https://sketchfab.com/3d-models/signalis-thermite-flare-879c71c3dabd4b1f873e5f4706e70498)
- **Signalis-ified MP5 Swordfish "The Xiphias"** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-mp5-swordfish-the-xiphias-1b4c4426802d4dd9b823429e53b01756)



