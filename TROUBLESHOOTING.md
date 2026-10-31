# SIGNALIS VR — Troubleshooting and fixes

Start with the section that matches the error in your launcher window. Use the [all-in-one download](https://github.com/stevethetitan92/Signalis-VR/releases/tag/v0.7.166-launchers.1) for both first-time setup and updates.

## Start here

1. Close SIGNALIS before installing, updating, repairing or restoring saves.
2. Extract the entire ZIP with **Extract All**. Do not run it from inside the ZIP.
3. Keep these together:

   ```text
   Launch SteamVR.cmd
   Launch VirtualDesktop.cmd
   Mod Files/
   ```

4. Open Steam and connect your headset. Run **one outer launcher**, using the connection you want. The launchers inside `Mod Files` are legacy helpers; use the two at the top level.
5. Read the first error, the subsequent repair results, and the final summary. An earlier `[FAIL]` followed by `[FIXED]` describes a problem that was repaired. Remaining blocking errors prevent startup.

The launcher can download a current package and install or repair its bundled mod files. It preserves existing settings, adds missing defaults, and backs up saves and replaced files. It does not install SIGNALIS, Steam, SteamVR or Virtual Desktop for you.

## Run diagnostics without opening the game

Open the extracted launcher folder in File Explorer. Click the address bar, type `cmd`, and press Enter. Then enter one of these commands.

**Check, install/update or repair, then stop:**

```bat
"Launch SteamVR.cmd" -DiagnoseOnly
```

For Virtual Desktop, replace the filename with `"Launch VirtualDesktop.cmd"` in any example below.

**Report problems using the local package, without downloading or changing installed files:**

```bat
"Launch SteamVR.cmd" -DiagnoseOnly -NoRepair -NoDownload
```

Reports and diagnostic logs can still be written. This command does not start the game.

**Validate paths and dependencies offline:**

```bat
"Launch SteamVR.cmd" -ValidateOnly -GamePath "D:\SteamLibrary\steamapps\common\SIGNALIS"
```

Replace the example path with your actual game folder. `-ValidateOnly` does not download, install or launch. Health reports can still be created.

## Game cannot be found / wrong path

1. In Steam, right-click **SIGNALIS → Manage → Browse local files**.
2. Find **SIGNALIS.exe**, right-click it and select **Copy as path**. If necessary, use **Show more options**.
3. Paste that path into the launcher's prompt and press Enter. A game folder path also works.

Do not paste a ZIP, shortcut, Steam URL, `SIGNALIS_Data` folder or extracted mod folder. If the drive is disconnected, reconnect it first. If several installations are found, specify the one you want:

```bat
"Launch SteamVR.cmd" -DiagnoseOnly -GamePath "D:\SteamLibrary\steamapps\common\SIGNALIS"
```

If `SIGNALIS.exe` is missing from Steam's game folder, follow **Original game files are missing or damaged** below.

## Missing Mod Files / bootstrap script / integrity error

**Examples:** `Missing Mod Files\Bundle-Launch.ps1`, `Mod Files integrity check failed`, or missing `Bundle-Download.ps1`.

1. Download the all-in-one ZIP again from this repository's release page.
2. Extract it into a **new folder**. Keep the old folder and its backups until the new one works.
3. Open an outer launcher from the new folder. Do not mix files from different downloads.

Automatic recovery needs the bootstrap scripts to be present. If those are missing, re-extract the package. Do not download individual DLLs from unrelated sites.

## Download failed / checksum mismatch / GitHub unavailable

- **Intact local package:** the launcher can continue using it when the release service is unavailable. To deliberately skip the online check, use:

  ```bat
  "Launch SteamVR.cmd" -NoDownload
  ```

- **Damaged local package:** it must recover a verified download or stop. Download and extract a fresh ZIP if automatic recovery fails.
- **Size or SHA-256 mismatch:** do not force the downloaded files to run. Retry from the release page and extract a new copy. The launcher rejects incomplete or mismatched packages.
- **GitHub rate limit or connection error:** wait and retry, or use `-NoDownload` with an intact local package. This flag cannot bypass damaged files.

Verified downloads are cached under `%LOCALAPPDATA%\SignalisVR\Packages`. A newer package may run from there; the command window prints its location. Do not delete a cache folder while a launcher is using it. You normally do not need to clear the cache.

## Missing or mismatched mod/loader files

1. Close SIGNALIS.
2. Run the outer launcher normally, or use `-DiagnoseOnly` to stop after setup.
3. Let it verify its source files, back up affected files and saves, and finish the repairs.
4. Check the final summary for unresolved errors.

Different checksums can mean another mod version, not necessarily damaged data. The launcher installs the version in its verified active package. It retains the replaced bytes in its backup folder. Existing configuration is preserved; missing defaults are added. Extra mods are not automatically removed.

If a repair fails, inspect the rollback messages. Do not assume rollback succeeded if the window reports a rollback error. Keep the original-file backup and report the full error.

## Access denied / backup failed / game already running

- Close SIGNALIS before retrying. Do not run both launchers at once.
- Extract the download somewhere you can write, such as a folder under Documents or Downloads. The active package needs space for logs, downloads and backups.
- Check available space and whether the game/library drive is connected and writable.
- Review Windows Security's protection history if files disappeared or were quarantined. Do not disable security protection or restore an unfamiliar file merely to silence an error.
- The launcher rejects linked/reparse paths for repair. Use an ordinary extracted folder and provide the actual game folder path instead of a junction or symbolic link.

A failed backup stops the repair. Keep the error text; do not delete saves or backups to get past it.

## Original game files are missing or damaged

1. Close SIGNALIS.
2. In Steam, right-click **SIGNALIS → Properties → Installed Files → Verify integrity of game files**.
3. Let Steam finish, then run the outer launcher again so it can recheck the mod.

The launcher checks the presence of key original files, but Steam performs original-game integrity verification. The mod package does not contain replacement game executables or game data. Steam verification does not remove every extra mod file, so it is not the same as uninstalling the mod.

## SteamVR launcher problems

- Use **Launch SteamVR.cmd**. Virtual Desktop is not required.
- Install/open SteamVR and confirm your headset connects successfully there.
- If discovery fails, find SteamVR's installation folder through Steam and specify it:

  ```bat
  "Launch SteamVR.cmd" -DiagnoseOnly -SteamVRPath "C:\Program Files (x86)\Steam\steamapps\common\SteamVR"
  ```

  Change that example to your real SteamVR folder. It should contain `bin\win64\vrstartup.exe`.

- If the launcher reports that SteamVR did not start in time, open SteamVR yourself, connect the headset, and retry.
- Missing native DLL dependencies or exports: let the launcher restore matching bundled files first. If the DLL still cannot load, include the exact Windows error number and diagnostic report in your issue.

## Virtual Desktop / VDXR launcher problems

- Use **Launch VirtualDesktop.cmd**. SteamVR is not required for this path.
- Install/open Virtual Desktop Streamer on the PC and connect the headset through Virtual Desktop.
- If Streamer discovery fails, specify its real installation folder:

  ```bat
  "Launch VirtualDesktop.cmd" -DiagnoseOnly -VirtualDesktopPath "C:\Program Files\Virtual Desktop Streamer"
  ```

- If `OpenXR\virtualdesktop-openxr.json` or the runtime DLL is missing, repair/update Virtual Desktop Streamer from its official source. Mod-file repair cannot supply the Streamer's runtime installation.
- Close SIGNALIS before switching between SteamVR and Virtual Desktop launchers. Their runtime selection applies to the game process started by that launcher.

## Game opens, but the headset image or controls are wrong

1. Confirm you used the outer launcher for your actual connection.
2. Run `-DiagnoseOnly` and resolve remaining launcher/provider errors.
3. Compare the headset view with the desktop view. Record which one is wrong, whether it affects one or both eyes, and what happened immediately before it started.
4. For the existing VR controls: **F10** retries VR, **F7** recenters, and **F12** stops VR. Avoid changing several settings at once while identifying the cause.

For stutter or shaking, report whether it occurs while standing still, moving, turning, aiming or entering a room. Include the headset refresh rate and connection method. A file-health pass does not identify every rendering or timing problem.

## Medicine, inventory, weapons or other gameplay problems

Run diagnostics so missing/mismatched files are addressed first. Then record the exact item or weapon, health/ammo state, whether it was carried or equipped, the action used, and what you expected versus what happened.

The launcher can highlight medicine/graphics/save-related errors from recent log lines. Those lines can belong to an earlier session and are evidence, not automatic proof of the cause. Replacing files does not guarantee that every gameplay symptom is fixed. Do not delete saves as a general gameplay fix.

## Save problems and restoring a backup

The launcher backs up the game's `SIGNALIS_Data\save` files before replacing installed mod files, and can preserve saves when save-related log errors appear. A checksum-verified backup proves the copied bytes match; it does not prove that the game can load that snapshot.

To restore a **specific earlier launcher backup**:

1. Close SIGNALIS. Find the printed backup path in the active package's `Mod Files\Backups` folder.
2. Select a backup folder containing both **Save-Backup.json** and **Saves**. Keep the backup intact.
3. From the outer launcher folder's command window, run:

   ```bat
   "Launch SteamVR.cmd" -DiagnoseOnly -RestoreSaveBackup "C:\path\to\Mod Files\Backups\SELECTED-BACKUP"
   ```

4. Read the prompt. Restoring older files can lose progress made after that snapshot. Type **RESTORE** only when you want that selected snapshot.

The launcher verifies that the backup matches the game location and its recorded hashes, backs up current saves, then restores the selected files without deleting other slots. Steam Cloud can resync saves. A mismatched or damaged backup is blocked. There is no guessed rewrite of the game's save format.

## Find the reports and ask for help

Use the exact paths printed by the launcher, especially when an update is running from the cache:

| File/folder | Where to look |
| --- | --- |
| Health report | Active package → `Mod Files\Logs\Health-*.json` |
| Bridge/discovery diagnostic | Active package → `Mod Files\Logs\SIGNALIS-diagnostic-*.txt` |
| Launch transcript | Active package → `Mod Files\Logs\SIGNALIS-launch-*.log` |
| Mod/game startup log | Installed game → `MelonLoader\Latest.log` |
| Original-file/save backups | Active package → `Mod Files\Backups` |

Keep the reports from the same attempt and copy logs before another launch replaces them. Review usernames and personal paths before posting publicly.

[Report a problem](https://github.com/stevethetitan92/Signalis-VR/issues/new?template=bug_report.yml) with the package version, launcher used, headset, connection, exact error, steps to reproduce, and relevant logs or a short video. [Check existing issues](https://github.com/stevethetitan92/Signalis-VR/issues) for the same symptom.
