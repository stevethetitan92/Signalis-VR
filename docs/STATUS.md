# Verified status — September 23, 2026

Evidence labels distinguish headset observations, log evidence, local checks, and hypotheses. A build passing compilation does not prove that graphics crashes are fixed.

| Area | Result | Evidence and limits |
|---|---|---|
| Stereo startup | Working in bounded tests | User saw stereo in 0.4.0, 0.4.1, and 0.4.2. |
| Walking in stereo | Five-minute pass | User completed the full 0.4.2 same-room test. No puzzles, doors, or menus included. |
| Sustained submission | Five-minute pass | Log recorded 10,789 local rendered frames and sampled left/right errors 0/0. |
| Head rotation | Worked during successful runs | Earlier versions had centering and warping reports; comprehensive tracking verification still needed. |
| Image orientation | Correct in successful gameplay tests | Earlier upside-down images were corrected. Puzzle orientation requires separate validation. |
| Plain function keys | Working | Direct key handling replaced Ctrl combinations. |
| Dashboard after F10 | Unresolved | User consistently reports dashboard opening only after F10. Resume Game permits stereo testing. |
| Waiting at five minutes | Expected diagnostic behavior | 0.4.2 intentionally stops submission after 300 seconds. Desktop may remain responsive. |
| Native graphics crashes | Improved, not proven eliminated | Old builds crashed in the graphics driver. Corrected path passed 10 seconds, two minutes, and five minutes. Longer sessions/restarts unverified. |
| Puzzles | Paused | 0.2.6 automatically returned from puzzles; 0.2.7 displayed a puzzle but later hung. Restored integration in 0.4.3 is untested. |
| Scene transitions | Unsupported in this diagnostic flow | Scene loading stops the test; uninterrupted room changes unverified. |
| Menus, inventory, save/load | Unverified | Not covered by the five-minute baseline. |
| Frame rate | Needs improvement | About 36 locally rendered frames/sec in baseline logs. This is not a measurement of headset display refresh. |
| Motion-controller gameplay | Not established | No claim of tracked hands or Quest controller bindings. |
| Other headsets/runtime combinations | Unverified | Current evidence is specific to the development setup. |
| Automatic updates | Not implemented | No eligible stable release exists yet. |

## Dashboard hypothesis

Local SteamVR evidence identifies SIGNALIS as a legacy Steam application and records a desktop-game overlay. SteamVR includes an automatic game-theater setting. This is a candidate explanation for the F10 interruption, not an established cause. No dashboard fix has been validated.
