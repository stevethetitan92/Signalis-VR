# 0.4.4 portability candidate — NOT A STABLE RELEASE

Based on the five-minute 0.4.2 stereo baseline. Puzzle work remains paused. No installation has been performed.

## Changes

- Locate UserLibs/SignalisVrTracking from the running executable's directory, independent of the working directory or build machine's Steam path.
- Resolve VR native exports from the exact loaded library handles using cached Cdecl delegates. No hardcoded installation path in the managed DLL.
- Verify both native files against the packaged SHA256 values before loading either. Report missing, changed, unloadable files, missing exports, and bridge ABI mismatch.
- Run a dependency check at mod startup without opening a SteamVR scene. F6/F9/F10 and the five-minute diagnostic limit remain.
- Preserve the tested native bridge binary (ABI 400); graphics scheduling and puzzle behavior are unchanged in scope.

## Local validation

Managed compilation passed with existing MelonLoader/framework-reference warnings. A separate 64-bit .NET Framework harness loaded the real libraries from two relocated folders (spaces and Unicode) with its working directory outside the installation. Bridge version export and OpenVR IsRuntimeInstalled delegate calls returned; missing-export diagnostics passed. Missing runtime and corrupted bridge were rejected. Diagnostic stage/timeout tests and 75 heading cases passed. Binary scan found no old E:/SteamLibrary installation path.

The sandbox harness could not locate the user's SteamVR path registry and returned installed=False. This verifies the native call path, not runtime installation or headset connectivity. Actual MelonLoader/Mono startup, stereo, dashboard behavior, and long-session stability remain UNTESTED. Do not classify this as a graphics crash fix.

## Package and rollback

With SIGNALIS closed, a future authorized test may copy the package's Mods and UserLibs folders into the game root. Keep both native files together with the managed DLL. No replacement is needed now without the headset; this candidate is staged only. Preserve saves separately as usual. Roll back using the complete preserved 0.4.2 package; 0.4.3 is an untested puzzle checkpoint.

Native libraries are deliberately retained until process exit. Hash pinning is strict: rebuilding or updating a native library requires reviewing and updating its expected hash. Original code licensing remains undecided; Valve bindings retain their original license.

## Rebuilding / tests

Run build.ps1 -GamePath <your local game installation> for compile-time game references. That path is no longer emitted into the DLL. test-portability.ps1 expects the matching native libraries under package/UserLibs/SignalisVrTracking. LoaderTests is a standalone test executable, not the game. Do not include it in a game installation. test-diagnostic.ps1 and test-heading.ps1 test isolated logic.
