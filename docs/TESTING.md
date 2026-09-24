# Diagnostic testing

Function keys separate risky operations so a failure can be associated with a stage. The twenty-second waits are observation windows, not a technical requirement that VR must always wait twenty seconds. An eventual normal startup should automate validated initialization.

## Preserved 0.4.2 procedure

1. Start Virtual Desktop, connect the headset, and start SteamVR.
2. Start SIGNALIS and load the same tested room.
3. Press F1 to enable the camera mod's first-person setup used for testing.
4. Press F6, then observe for twenty seconds.
5. Press F9, then observe for twenty seconds.
6. Press F10 once to start the bounded stereo test. If the dashboard opens, select Resume Game. Its time counts toward the test.
7. Walk and rotate your head for up to five minutes. Avoid puzzles, doors, and menus for this baseline test.
8. At five minutes submission stops intentionally. Waiting/black in the headset alone is not evidence of a crash. Record whether the desktop still responds.

This procedure applies to the preserved diagnostic build, not all future versions. Do not repeatedly toggle keys in an attempt to recover an unknown state.

## Record each run

Record version and installed hashes, start/end times, headset/runtime/GPU versions, exact key order, first stereo frame, dashboard timing, centering, visible distortion, movement, desktop responsiveness, and whether the run ended by timeout, normal exit, hang, or crash. Preserve logs before the next launch overwrites them.

## Stability gate still outstanding

- Three cold-start sessions of at least thirty minutes each on the corrected path.
- Repeat enable/disable, tracking loss/recovery, recentering, and clean shutdown.
- Separate checks for scene transitions, inventory, menus, save/load, and puzzles; unsupported features clearly disabled or documented.
- No new native crash, indefinite Waiting, lost movement, or persistent view distortion.
- Confirm portable installation on another path and rollback from a candidate to the baseline.

Compilation, stage logic tests, and simulated compositor tests provide local checks only. They do not substitute for headset testing.
