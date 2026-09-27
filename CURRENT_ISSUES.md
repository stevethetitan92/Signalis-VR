# Current issues — September 27, 2026

Latest user report; these are open issues, not completed fixes.

- **Black bars in the first cutscenes.** Floating bars and the small central panel remain in the early ship shot. 0.4.83 removed later central edge notches in reviewed video frames, but did not fix the entire sequence. 0.4.84 extends bar handling to the earlier capture path; headset verification is pending.
- **Stereo is lost upon entering the snow scene.** Snow traversal and the red shaft remain on a flat display. First-person/stereo coverage for this scene is unresolved.
- **Function buttons do not work at the red bottom of the stairs.** User reports function-key controls unavailable there. Prior code inspection identifies PEN_Hole as explicitly excluded by the original first-person mod and current recovery, but this does not independently explain every function-button failure.
- **Function buttons work again in the bathroom.** User reports being able to re-enable first person and stereo there and proceed. This is a user-observed manual recovery point, not confirmation of automatic recovery.
- **Many interactable objects are missing their markers.** User report; affected objects/locations still need inventory and investigation. Earlier starting-area marker success does not establish coverage throughout the game. Door and ladder markers remain intentionally hidden; object markers must be preserved/restored.

## Evidence and build status

0.4.83 is the last hash-verified installed build from the reviewed session. Its video and runtime log show partial bar improvement and successful UI bar discovery. 0.4.84 compiles and passes existing recovery/capture guard harnesses; installation and headset outcome have not been confirmed. Those checks do not establish graphics stability.

0.4.84 DLL SHA256: `C186DD0DAEB848691A0D3F8F5E7FB111E7E7A39D0921D842B3AB406169A54FF3`.

Other open work: small cinematic/logo presentation, startup SteamVR dashboard, keyboard prompts, head-tracking tilt/wobble, and weapon-control validation. No stable release.
