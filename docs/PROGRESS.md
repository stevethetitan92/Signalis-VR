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
