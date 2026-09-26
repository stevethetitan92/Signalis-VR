# Latest progress — September 25, 2026 (late session)

This section supersedes the older status below, which is retained as history. These are local experimental builds, not published stable releases. This update documents results; it does not publish binaries, replace repository source, or enable automatic updates.

## Working checkpoint and rollback rule

**0.4.19 is the current working-book rollback baseline.** If a subsequent candidate fails or regresses, return to 0.4.19 before another attempt. Earlier 0.4.16 and 0.4.3 checkpoints remain preserved. This is limited test evidence, not a claim of complete game compatibility.

## Progress since 0.4.12

| Build | Change | Evidence and limits |
|---|---|---|
| 0.4.13 | Inventory screen capture | Inventory became visible; later Waiting was consistent with the retained diagnostic timeout. |
| 0.4.14 | Initial pause candidate | Not installed; superseded by 0.4.16. |
| 0.4.15 | Removed deliberate 15-minute session and 120-second menu cutoffs | User passed a three-minute inventory test and continued beyond fifteen minutes. Logs show about 25 minutes of submission through clean exit; exploration included doors/items/puzzles, so this was not exclusively a starting-room test. Indefinite stability not established. |
| 0.4.16 | Explicit pause-menu detection and capture | User tested repeated Escape presses, settings navigation, mixed inventory/map/pause use and return to movement. A temporary downloaded save allowed successful item inspection. A 50-second headset recording supports item rotation/descriptions and repeated pause display/returns, with no visible Waiting in that clip. Item use/combining remains unverified. |
| 0.4.17 | Book detection using manager screen | Failed: manual appeared on desktop only. Closing it still allowed walking. |
| 0.4.18 | Book-reader diagnostics | Identified the active reader as FieldGuide, while the manager pointed to inactive Memories. FieldGuide open changes to false on closing even though its object remains active. |
| 0.4.19 | Detect active, enabled, open book readers | User confirmed headset book display and ran through the starting manual five times, then confirmed stereo walking/look after closing. Logs corroborate capture activation and recovery. Other documents and dialogue are not covered by this pass. |
| 0.4.20 | Dialogue visibility diagnostics based on 0.4.19 | Prepared locally, compiled and passed inherited recovery/flow checks. Not installed or headset-tested. No capture behavior change. An initial compile failure was corrected in diagnostic references; it never replaced the working installation. |

## Current limitations and next test

- Missing door messages, paper text and other interaction screens still need individual reproduction and diagnosis. The starting-book fix does not prove all text works.
- Puzzle usability and camera disconnection at certain doors remain separate unresolved work.
- Dashboard activation and bending/warping remain unresolved; bending is deferred.
- Normal testing begins with **New Game every launch**, then F1 for first person. There is no regular saved game. The downloaded save was used only for the item test and will not be used going forward.
- Existing setup: Steam SIGNALIS, MelonLoader, Camera Perspective Change, Quest 3S, Virtual Desktop and SteamVR. SSW was reported Disabled.
- Next: test one missing door message with 0.4.20 diagnostics, compare desktop/headset and record open/close states before modifying capture. Preserve the working stereo, inventory, pause and book behavior.
- Automated checks and compilation do not prove graphics stability. Stable releases and automatic updates remain pending acceptance testing.

## Historical status (superseded where noted above)

# Current progress — September 25, 2026

These results supersede the older "Next sequence" below. Builds 0.4.10–0.4.12 are local experimental checkpoints, not published stable releases. This documentation update does not upload their binaries or source, enable automatic updates, or change the main source baseline.

## Verified headset results

Test setup: Steam SIGNALIS, MelonLoader, Camera Perspective Change mod, Meta Quest 3S, Virtual Desktop and SteamVR. F1 is required after loading a save to enter first person. SSW was changed from Automatic to Disabled for recent tests; that is a user-reported setting.

| Build | Change | Observed result and limit |
|---|---|---|
| 0.4.4–0.4.6 | Portability and camera-state diagnostics | Gameplay/first-person movement worked, but temporary gameplay-camera disable events stopped rendering. Diagnostics did not prove the event's cause. |
| 0.4.7 | Passive camera observation | A five-minute stereo run completed. The same camera also disabled and returned during connection-only testing, so submission alone is not required to trigger that transition. |
| 0.4.8 | Fixed parent orientation experiment | Did not improve reported bending/warping; not retained as the movement solution. |
| 0.4.9 | Recenter at stereo start | Recenter executed, but visual bending remained. An initial SSW-disabled comparison stopped before stereo and was inconclusive. |
| 0.4.10 | Retain resources and recover from temporary camera disable; ten-second bound | Two independent five-minute starting-room stereo runs completed at their scheduled limits. These are not one continuous ten-minute test. |
| 0.4.11 | Extend stereo test to fifteen minutes | One continuous fifteen-minute starting-room run completed. Automatic recovery occurred during local rendering. Inventory during stereo later exceeded the ten-second recovery bound and stopped VR; closing inventory after timeout did not restart it. |
| 0.4.12 | Extend camera suspension to 120 seconds; retain fifteen-minute overall limit | Two inventory open/close cycles in one stereo session returned automatically to stereo, with walking confirmed. Logs show pause/resume and continued submissions after each. Inventory itself still displays Waiting in the headset. |

The two recorded 0.4.12 camera interruptions lasted approximately 29.5 and 28.5 seconds. These are log intervals, not exact measurements of the requested 20-second inventory-open action. No extra VR activation was needed after closing inventory.

## What works and what remains

- First-person walking and stereo have passed the bounded tests above.
- Returning from inventory works in two tested cycles on 0.4.12. Keep this as the recovery checkpoint.
- Inventory is NOT yet visible or usable inside VR. Navigation and item use in the headset remain untested.
- The first puzzle and problematic door transitions need separate work. Inventory recovery does not prove support for camera replacement, scene changes, or every door.
- SteamVR dashboard activation still occurs; it is not fixed.
- Objects still bend during head motion with SSW Disabled. Bending is deferred at the user's request, not resolved.
- Waiting at the configured five- or fifteen-minute test deadline is the intentional stop. Earlier Waiting must be checked against logs.
- Compilation and local state/heading tests passed, but they do not establish native graphics stability. No general crash-fix or full-game stability claim is made.

## Current work order

1. Preserve 0.4.12 recovery and earlier checkpoints.
2. Show inventory in the headset; verify navigation, item use, and reliable return to stereo.
3. Make the first puzzle usable in VR and verify exit recovery.
4. Diagnose camera loss at specific doors and verify transitions.
5. Return to bending/warping after those priorities.
6. Publish stable releases and opt-in updates only after documented acceptance testing.

Current code review found older screen-capture code is not active in this candidate. Re-enabling it unchanged is not yet validated; its pose handling must be checked against the current synchronized submission path. No inventory-display build is ready yet.

Report problems through the repository's [Issues](https://github.com/stevethetitan92/Signalis-VR/issues) tab. Include the build, action taken, headset symptom, desktop behavior, and whether it recovered. Keep personal information out of attached logs.

---

## Earlier project history (retained; status below reflects its original date)

# Development progress

This is a sanitized project history. Full local diagnostic logs are retained separately and are not published.

| Version | Change or experiment | Observed outcome |
|---|---|---|
| 0.1.1 | Rotation-only tracking | Head tracking worked. |
| 0.2.0 | Initial stereo submission | Native crash. |
| 0.2.1 | Submit moved to render callback | Crash persisted. |
| 0.2.2 | Retained textures / avoided teardown | Crash persisted. |
| 0.2.3 | Image flip correction | Upright gameplay image observed. |
| 0.2.4 | Tracking/centering changes | Off-center view, warping, and crashes reported. |
| 0.2.5 | Yaw centering, texture ring, GPU completion | Local tests passed; native instability persisted. |
| 0.2.6 | Puzzle exit recovery | User confirmed automatic return after exiting puzzles. Waiting during the puzzle remained. Earlier contrary interpretation was wrong. |
| 0.2.7 | Puzzle screen capture | Puzzle visible; F11 corrected inversion. Later hang after exit. |
| 0.2.8 | Nonblocking status work | Immediate graphics crash still observed. |
| 0.2.9 | Diagnostic stages A/B/C | Dashboard/focus obscured key handling. Stage C changed both pose synchronization and submission; it did not isolate Submit alone. |
| 0.3.0 | Direct Ctrl/function-key handling | Stereo followed by crash. |
| 0.3.1 | Plain function keys | Keys worked; old graphics crash remained. |
| 0.4.0 | Render-thread pose synchronization and removal of premature PostPresentHandoff | Actual headset ten-second test passed. Both corrections changed together; individual contribution not isolated. |
| 0.4.1 | Same graphics path, 120-second test | Full two-minute walking pass. Reported crash clarified as expected Waiting at timeout. |
| 0.4.2 | Same graphics path, 300-second test | Full five-minute walking pass. Preserved as best tested baseline. |
| 0.4.3 | Puzzle capture and recovery restored | Built and checked locally, but not headset-tested. Preserved as requested checkpoint. |

## Next sequence

1. Preserve complete 0.4.2 and 0.4.3 rollback packages and source snapshots.
2. Diagnose F10 dashboard activation with one reversible change at a time.
3. Extend stereo tests without changing the graphics path or adding puzzles.
4. Verify cold restarts, recentering, repeat activation, and graceful exit.
5. Address portability and startup usability, then retest.
6. Reintroduce puzzles as a separate experiment.
7. Publish a stable release only after documented acceptance testing; add opt-in stable updates afterward.

## Portability candidate 0.4.4

An isolated source snapshot and test notes are in [experiments/0.4.4](../experiments/0.4.4/README.md). This candidate removes the build machine's installation path, locates native libraries beside the running executable, and checks their packaged hashes. Relocated native-loading tests (spaces and Unicode), missing/damaged-file checks, and existing diagnostic/heading checks passed locally. The sandbox did not verify SteamVR connectivity. Actual MelonLoader/Mono startup and headset behavior remain untested. Main src remains the 0.4.2 baseline; no stable release or automatic update was published.
