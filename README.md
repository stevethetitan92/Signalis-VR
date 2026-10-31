# SIGNALIS VR

Experimental Windows PC VR mod for SIGNALIS. New tester candidate: **0.7.166 — separate launchers and automatic diagnostics**. The two connections are independent; you only need the one you use.

## Download and install

[Open the latest launcher release](https://github.com/stevethetitan92/Signalis-VR/releases/tag/v0.7.166-launchers.1).

1. Download **SIGNALIS-VR-0.7.166-Folder-Full-v6.zip** and choose **Extract All**.
2. In Steam, right-click **SIGNALIS → Manage → Browse local files**.
3. Copy **everything inside `Mod Files`** into that game folder. Keep the two outer launchers beside `Mod Files`. The `Mods` folder and launchers should be beside `SIGNALIS.exe`.
4. Open Steam, connect your headset, and double-click **one** launcher:

| Your connection | Open this file | What you need |
| --- | --- | --- |
| SteamVR | `Launch SteamVR.cmd` | SteamVR and your usual headset connection. **No Virtual Desktop required.** |
| Virtual Desktop | `Launch VirtualDesktop.cmd` | Virtual Desktop on the headset and Streamer on the PC. **No SteamVR required.** |

Close the game before copying files and keep a working backup. The first mod-loader start can take longer.

**Already installed the mod?** Use **SIGNALIS-VR-0.7.166-Folder-Update-v6.zip** instead. Back up `Mods/SignalisVrTracking.dll`, then copy the contents of the update Mod Files folder into the game folder. Your saves and settings stay in place. Do not overwrite your existing BepInEx configuration with the full package when updating.

GitHub's automatic **Source code** ZIP/TAR downloads are not the install package.

## If the launcher cannot find the game

The launcher searches your Steam libraries automatically, including other drives. If it needs help, its command window asks for the game path:

**Steam → SIGNALIS → Manage → Browse local files → right-click SIGNALIS.exe → Copy as path.** Paste it into the command window and press Enter.

If that path also fails, the launcher automatically checks discovery, its own files, required mod files, native VR bridge/provider DLLs and your selected connection software. It saves a report in **Logs** beside the launcher and tells you what it found. A uniquely verified game location can be recovered; otherwise you can paste another path or cancel.

You can also run **Diagnose Launcher.cmd**. The original Diagnose Launcher.cmd does not install/repair files or initialize VR. The outer launchers add the repair preflight described below.

## Included and verified

The full package includes MelonLoader 0.5.7 x64, the existing BepInEx folder, `SignalisVrTracking.dll` 0.7.166, required `FPv2_reworked.dll` and `CameraPerspectiveSolid.dll`, the matching native bridge, both separately selected providers, instructions, licenses, source archives and a 0.7.163 rollback DLL.

SteamVR retains the original OpenVR path. Virtual Desktop uses **VDXR through OpenComposite**; this is not a rewrite of the whole renderer as native OpenXR. Launcher selection is per game process and does not change system-wide runtime registry settings.

**Offline verification:** 114 regression checks, 18 launcher checks, 16 diagnostic checks, all three command wrappers, deterministic compilation, resource/dependency checks and archive checksums passed. The native bridge passed its simulated 2,000-frame test.

The earlier 0.7.161 fresh install was user-confirmed working. [Earlier release/fallback](https://github.com/stevethetitan92/Signalis-VR/releases/tag/v0.7.161-test.1). Unfinished 0.7.164/0.7.165 performance work and paused Blender changes are not integrated.

## Source and status

[0.7.166 code-review snapshot](https://github.com/stevethetitan92/Signalis-VR/releases/download/v0.7.166-launchers.1/SIGNALIS-VR-code-review-0.7.166.zip) · [Launcher source](launcher/0.7.166) · [Detailed status](STATUS-2026-10-31.md) · [Existing issues](CURRENT_ISSUES.md)

The review archive omits embedded assets/data, proprietary assemblies and binaries; it is not a standalone build/install package. Historical source archives and the root `src` tree remain preserved. Use the versioned review snapshot for current logic.

Passage door/ladder artwork remains hidden while actual interactions, approved item markers, maps, enemies/burning, computers, controls and native reflection behavior are preserved.

## Report a problem

[Open a bug report](https://github.com/stevethetitan92/Signalis-VR/issues/new?template=bug_report.yml). Include your mod version, launcher name, headset, what happened, the diagnostic report from **Logs**, and `MelonLoader/Latest.log`. Review personal paths in logs before posting.

## License and contributions

Project-owned code is GPL-3.0-only; see [LICENSE](LICENSE). Third-party code retains its own notices. This does not license SIGNALIS, game assets or proprietary assemblies. See [third-party notices](THIRD_PARTY_NOTICES.md) and [contributing](CONTRIBUTING.md). The mod remains free; optional support does not unlock exclusive features.

## Assets Used

- **Signalis Mega Pack** - [mod.io](https://mod.io/g/bonelab/m/signalis-mega-pack#description)
- **HD Signalis Weapon Pack** - [mod.io](https://mod.io/g/bonelab/m/hd-signalis-weapon-pack#description)
- **Signalis-ified M92 Beretta** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-m92-beretta-52c5b4bc8021449885a47075150e17f2)
- **Signalis Thermite Flare** - [Sketchfab](https://sketchfab.com/3d-models/signalis-thermite-flare-879c71c3dabd4b1f873e5f4706e70498)
- **Signalis-ified MP5 Swordfish "The Xiphias"** - [Sketchfab](https://sketchfab.com/3d-models/signalis-ified-mp5-swordfish-the-xiphias-1b4c4426802d4dd9b823429e53b01756)


## Folder layout and health/repair update (v6)

The v6 ZIP opens to exactly **Launch SteamVR.cmd**, **Launch VirtualDesktop.cmd**, and **Mod Files**. Copy the CONTENTS of Mod Files beside SIGNALIS.exe once; do not place the Mod Files folder itself inside the game. Keep the extracted bundle to use the outer launchers and repair copies. First-time installation remains manual.

The outer launchers check installed packaged DLLs, launcher files, native bridge loading and the selected VR software. Missing/mismatched installed DLLs can be restored from verified bundled copies, after checksum-verified save and original-file backups. Failed multi-file repairs attempt rollback. Errors and attempted fixes are printed and saved under Mod Files/Logs; backups are under Mod Files/Backups. Close the game first. Settings, saves, unknown mods and original game binaries are not automatically replaced.

Medicine, graphics and save log errors are reported as evidence, not proof of the cause. Save error evidence triggers backup; no unknown save-format edits occur. A selected earlier backup can be restored explicitly with -RestoreSaveBackup and a RESTORE prompt, after backing up current saves. This may lose later progress; Steam Cloud may resync saves. Original-game corruption requires Steam Verify integrity.

-DiagnoseOnly checks/repairs without launching; -NoRepair reports only; -ValidateOnly never repairs or launches. Read Mod Files/INSTALL-FIRST.txt. The inner legacy launchers and original Diagnose Launcher.cmd do not perform this new repair preflight.

30 additional offline checks passed, including actual CMD wrappers, save backup/restore, corrupted sources, failed-repair rollback, linked-path rejection and running-game protection. Runtime DLLs/assets are unchanged. Legacy v2 assets remain as fallback. Choose the v6 folder ZIPs for this layout.
