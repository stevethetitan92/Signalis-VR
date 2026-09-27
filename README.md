# Signalis VR

Experimental SIGNALIS VR mod. No stable release.

## Latest status — September 27, 2026

**[Current issues](CURRENT_ISSUES.md):** black bars remain in the first cutscenes; stereo is lost entering snow; function buttons fail at the red bottom of the stairs but work again in the bathroom, allowing first person/stereo to be re-enabled; many interactable object markers are missing. These are user-reported issues, not resolved features. 0.4.84 is a local bar-fix candidate awaiting headset verification. Source archives below currently extend through 0.4.82.

## Historical checkpoint: 0.4.82 — September 27, 2026

The no-door/ladder-marker variant preserves object highlights and interactions. The user verified available Quest inputs, including the previously failing keypad/terminal areas, on the 0.4.79 input baseline. F1 first person was subsequently confirmed working. Right-stick turning remains smooth and horizontal only. Weapon aim/fire/reload still require testing with a weapon.

Cutscene continuity in 0.4.80 allowed the user to progress through the airlock/EVA sequence and reach the next area with controls working. First person and stereo were lost during snow traversal and afterward; cinematic bars and small presentation elements remained.

0.4.81 adds guarded recovery in normal gameplay areas and temporary cinematic-bar hiding. The latest recording still shows short central black bars and flat snow traversal. Its clip ends before the next normal area, so automatic recovery there remains unverified. 0.4.82 corrects an exact camera-name mismatch in the bar matcher and logs matches. It compiles and passes recovery/capture harness checks; headset verification is pending. The user reports copying 0.4.82.

- [Current roadmap](ROADMAP.md)
- [Detailed 0.4.82 findings and checks](STATUS-0482.md)
- [Separate variant development log](SignalisVrNoMarkersDevelopmentLog.md)

## Source checkpoints

[September 27 archive](SignalisVR-source-checkpoints-2026-09-27.zip) preserves source snapshots 0.4.76–0.4.82, build/test scripts, review notes and binding licenses. Start with SignalisVrSceneRecovery0482 for the current experimental candidate; SignalisVrPuzzleInput0479 is the verified input baseline. Earlier candidates can contain known regressions. [September 26 archive](SignalisVR-source-checkpoints-2026-09-26.zip) remains available through 0.4.75.

The archives contain source, not game assemblies or game assets. Building requires local references described in the scripts. The older src tree is historical, not the latest candidate. See [third-party notices](THIRD_PARTY_NOTICES.md).

0.4.82 DLL SHA256: `51C815A2197DCC9EDBB5E4C78586B83DE028749C686C05BD3487CE8F20D352A5`.

## Remaining issues

Snow first person/stereo, normal-area recovery verification, cinematic bar verification, small rose-engine/logo presentation, recurring startup SteamVR dashboard, keyboard prompts, and head-tracking tilt/wobble remain open. Automated checks do not establish headset or graphics stability. No stable release or automatic updates; no automatic installation or game launch. See the GPLv3 license below.


## License and contributions

Copyright (C) 2026 stevethetitan92 and contributors.

The project-owned mod code, including project-owned code in the source checkpoint archives, is licensed under GNU General Public License version 3 (GPL-3.0-only). See [LICENSE](LICENSE). Third-party code retains its own notices and licenses; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and bundled vendor notices. This does not license SIGNALIS itself, its assets, or proprietary game assemblies.

The mod will remain free to download; Patreon support is optional and does not unlock exclusive mod features. Contributions, bug reports, documentation and headset testing are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before starting. This is experimental source, not a stable public binary release.
