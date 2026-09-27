# SIGNALIS VR no-door/ladder-marker variant development log

## 2026-09-26 — 0448-NM1 / 0.4.49 candidate built, not installed

This workspace is separate from the original project at
`C:/Users/rjrem/Documents/Codex/2026-09-22/i-n`.
The original development log and applicable original AGENTS.md were consulted.
The explicit separate-workspace instruction supersedes synchronization to the
original shared log. This log must not replace either original log.

Source baseline: `outputs/SignalisVrDoorApproach0448` in the original project.
Baseline managed and installed DLL SHA256:
`2B28CC194B15A456D641923457954785E64CF5AAA88D81B38C96772355FE3095`.
Original source files and both baseline DLLs were hash-checked after this build
and remain unchanged. Original project remains the fallback.

### Evidence and implementation

Original code adjusts door/ladder sprites during each eye render and restores
their enabled state, artwork and transforms afterward. Merely skipping those
adjustments would leave the desktop/game artwork visible.

Installed Unity wrapper metadata confirms Renderer.forceRenderingOff and
Camera.onPreCull. Game metadata exposes DoorInteractionPopup.block/bar and
Ladder.block/bar as SpriteRenderer references. Existing 0448 evidence identifies
door icon/Lock Indicator children beneath DoorInteractionPopup and ladder icon/
Lock Indicator children beneath Ladder Icon. This is inspected metadata and
historical logging, not a runtime observation of this candidate.

New MarkerSuppression.cs registers a rooted pre-cull callback independent of VR
activation. It discovers even inactive door/ladder controllers once per rendered
frame, targets their explicit block/bar references plus door popup and Ladder
Icon artwork descendants, and forces their renderers off before every camera.
It does not hide physical door/ladder geometry or disable controller components,
colliders or GameObjects. All rendering cameras share the veto, including desktop,
manual VR eyes and the screen-capture path. It reasserts the veto for each camera.
Scene load resets discovery. FaceMarkersForVr also invokes it explicitly.

Only new suppression calls and identity text were added to SignalisVrTracking.cs.
Existing INSPECT 0.2 scale, (0,5,3) anchor and fixed orientation are unchanged.
PuzzleScreen.cs, DiagnosticFlow.cs, HeadingMath.cs, HotkeyEdge.cs,
NativeLibraries.cs and vendor files remain byte-identical to 0448.
Native render bridge and OpenVR binaries were not changed or rebuilt.

### Automated checks

- Built against installed SIGNALIS/MelonLoader/Unity references: PASS.
- Existing framework-reference warning CS1701 remains; no compilation errors.
- 14 software suppression checks: PASS. Include inactive popups, nested labels,
  renamed ladder marker references, physical geometry and object-prompt exclusion,
  original enabled/active/controller state, independent camera enforcement,
  new-frame discovery, scene reset and preservation of existing camera callbacks.
  These run the production suppression code against test doubles, not Unity.
- Existing 17 camera recovery checks, long inventory/old timeout boundaries,
  diagnostic stage/continuous submission/cancellation checks: PASS.
- Existing 75 heading combinations: PASS.
- Reviewed source diff and verified protected/source/installed hashes: PASS.

Candidate DLL SHA256:
`20C3CF32715930F6F474E6C46EB1FD0E8579E886697511954458334BA6CDD8F7`.
Sources, tests, build log and package are in
`SignalisVrNoDoorLadderMarkers0448-NM1/` beside this log.

### Unverified / next checkpoint

Not installed, no game launched and no headset test. Camera callback runtime
behavior, all-room marker coverage, added discovery cost, actual interactions,
books/dialogue/puzzles/inventory transitions and graphics stability need live
verification. Discovery occurs once per frame; a popup created after that scan
will be discovered the following frame. Historical graphics crashes are not
claimed fixed by this artwork-only change or by software tests.

## 2026-09-26 — interactable-object highlighting requested

User now wants all interactable objects highlighted. Continue in this separate
workspace, preserving the completed no-marker candidate as a fallback.
Asked whether highlighting should be nearby outline/glow, always-visible
outline/glow, or small markers, and whether doors/ladders should be excluded.
Preference response pending at this entry.

Read-only metadata inspection identifies Interaction (with type, pointOfInterest,
inRange and triggered), InteractionItem, ItemPickup and interactionPopup.inter.
The enum includes generic, none, use, inspect, take, open and move.
Native outline-related components also exist; their presence alone does not
establish stereo compatibility or reliable object-to-renderer mapping.
No highlighting implementation or game coverage is claimed yet.

## 2026-09-26 — reference clarified, headset-only object brackets 0.4.50

User supplied `SIGNALIS 2026.09.26 - 15.24.39.03.mp4` as the desired style,
confirmed **objects only; keep doors and ladders unmarked**, and supplied
`VirtualDesktop.Android-20260926-152748.jpg` reporting a missing blue-book highlight.
The video was decoded locally and frames inspected: white corner-bracket artwork
with occasional red edging/flicker. The screenshot shows a blue book without
visible brackets in that frame, displayed on a virtual screen. The screenshot
alone does not establish which rendering/activation gate caused the absence.

User then clarified: **"we just need to make sure it shows up in the headset"**.
This supersedes the broader proposed geometry-based highlight approach. That
unbuilt experiment is retained only under work/; it is not linked into a DLL.
Final scope is existing game object bracket visibility during VR eye rendering,
with original desktop object presentation and approved INSPECT retained.

### Read-only investigation

Decoded game assets identify `interaction` and `interactionScaleable` as the
white corner artwork; `Interaction_inspect` is the separate text label.
Static scene level8 contains both FieldGuide interactions, each with an
interactionPopup and an `interaction` SpriteRenderer. Both also have BookScreen
and a 3D Book mesh. Existing field-guide prompts are therefore present in the
assets. This does not prove the screenshot's runtime visibility cause.
Evidence: `SignalisVrObjectHighlights0450/evidence/book-assets.log` and
`reference-style.jpg`. The provided JPG was visible inline, but copying from its
given OneDrive path failed because that path did not exist; no saved-copy claim.

### Final candidate implementation

Separate folder `SignalisVrObjectHighlights0450`, MelonLoader version 0.4.50.
Based on completed no-door/ladder candidate 0448-NM1 (0.4.49), retained intact.

- New `HeadsetObjectHighlights.cs` and `HighlightPolicy.cs` target only the two
  bracket sprite names and their associated interactionPopup/Interaction.
- For nearby active, enabled, same-room objects, force SR.enabled, clear the
  rendering veto, restore full alpha and popup local Y scale during each eye.
  Retain live authored tint when visible; invisible/black samples become white.
  Do not change any interaction state or trigger an action.
- Exclude travel-object hierarchies, combat StompZone, consumed/fading pickups,
  currently open/opening books and other rooms. Range is 30 XY game units and
  under 12 Z units. No requirement on flat-game Interaction.inRange.
- Save and restore color and forceRenderingOff in addition to the baseline
  artwork/enabled/transforms, in the existing eye-render finally block.
- `Interaction_inspect` is not selected; its approved 0.2 scale and fixed anchor
  are unchanged. Desktop object state is restored after every eye.
- Door/ladder suppression remains camera-wide, independent of VR activation.
- No geometry clones, new materials, generated overlays, new image effect,
  native bridge change or VR submission change are included.

### Automated checks and limits

Build against installed game references: PASS, existing CS1701 warnings.
19 bracket selection/state/range cases: PASS. Emitted DLL inspected with Cecil:
all seven renderer/transform restoration setters are present, and restoration is
called from the RenderStereo finally handler. 14 suppression software checks,
17 recovery checks plus inventory/timeout checks, diagnostic flow and 75 heading
checks: PASS. These are local software/assembly checks, not Unity rendering tests.

Protected functionality sources (PuzzleScreen, DiagnosticFlow, HeadingMath,
HotkeyEdge, NativeLibraries and vendor files) remain byte-identical to 0448.
Original project source hashes and original/installed 0448 DLL hashes still match.
Nothing installed or overwritten outside this workspace. No game launched.

Final DLL SHA256:
`668A9BB9B63E7466B5DD545E8C63AC031322A63AEE23BD90F05C6A9F778D3E6E`.

**Unverified:** actual headset brackets, blue-book result, shape/placement,
all-room coverage, runtime camera callbacks, performance and graphics stability.
This candidate reuses existing popup objects; it does not create missing prompts
or activate inactive GameObjects. Future missing cases need runtime evidence,
not a claim that all interactable objects have been covered by these checks.
Next checkpoint: user installs candidate after closing the game, enters F10
submission and checks the blue field guide and reference book, then reading/
return behavior, approved INSPECT and absence of usable-door/ladder artwork.

## 2026-09-26 15:43 America/Chicago — 0.4.50 installation verified

User reports done copying. Read-only hash check confirms installed
`E:/SteamLibrary/steamapps/common/SIGNALIS/Mods/SignalisVrTracking.dll` matches
0.4.50 SHA256 `668A9BB9B63E7466B5DD545E8C63AC031322A63AEE23BD90F05C6A9F778D3E6E`.
Mods listing shows one SignalisVrTracking DLL alongside the separate probe.
The package/Mods source DLL is no longer present (possibly moved by the user;
not established). The build-root DLL and ZIP remain the preserved deliverables.
File verification only: no headset result, blue-book visibility, interaction
regression or graphics-stability claim. Next: normal F1/F6/F9/F10 readiness
sequence, then inspect blue-book corner brackets in headset and open/close book.

## 2026-09-26 — 0.4.50 brackets visible on floor; Repair Logic identity corrected

User supplies headset screenshot 154556: blue book on table, white corner
brackets visibly well below it near the floor. Visibility is evidenced for this
frame; placement is incorrect. User clarifies it is the **Repair Logic manual**,
and screenshot154744 shows the title "LSTR Features: Repair Logic Module" in
the book reader. This supersedes earlier field-guide identification. The internal
object name is Field Manual; it must not be presented as the user-facing title.

Saved current log as `SignalisVrBookAnchor0451/evidence/0450-floor-brackets.log`.
It identifies Mess Hall/Chunk/Field Manual/Prompt/PromptSprite at
(933.090,-938.070,0.000), with disabled/zero-alpha/zero-Y original renderer.
0.4.50 override is logged. Earlier read-only asset data places that object's
child Model around (933.110,-943.610,-3.908), before mesh-center adjustment.
Thus simply making the authored popup visible leaves a displaced floor anchor.

## 2026-09-26 — 0.4.51 book-cover anchor candidate, not installed

Copied 0.4.50 sources into separate `SignalisVrBookAnchor0451` folder.
Added BookBrackets.cs; adjusted HeadsetObjectHighlights.cs, lifecycle hooks in
SignalisVrTracking.cs, and build.ps1. Books with exactly one active mesh receive
a temporary independent SpriteRenderer using the original sprite/material/tint.
It is positioned on the upper cover (thinnest local mesh axis, toward world -Z),
0.05 game units beyond the mesh bounds, and sized 115% of its two cover axes.
This avoids the original popup's nonuniform/animated parent transform. Geometry
also supplies the proximity origin. Original popup is hidden during that eye,
then restored; proxy is hidden in RestoreMarkerRotations and destroyed at scene
change. No book mesh, interaction state, material asset or desktop behavior is
edited. Missing/ambiguous book models retain 0.4.50 behavior. This covers books,
not a new placement system for all non-book objects.

INSPECT, marker suppression, puzzles, recovery, native loading and highlight
selection policy are byte-identical to 0.4.50. Build passed with existing CS1701
warnings. 19 policy checks and compiled finally restoration checks, 14 suppression
checks, recovery/inventory/timeout and diagnostic checks passed. Compiled calls
confirm proxy hiding in restoration and cleanup on scene load. These checks do
not render Unity geometry or establish correct headset alignment.

Candidate SHA256:
`1D970BE68B4716F135A7E9BB60C7477CCED1403796F5349284CDF91391F0A6EB`.
Installed DLL still matches 0.4.50; no automatic installation. Historical outputs
and original project retained. Next: close game, manually install candidate,
check Repair Logic manual bracket fit/height/head-turn anchoring and reader return.
Exact shape, clearance, all-book coverage and graphics stability remain unverified.

## 2026-09-26 15:50 America/Chicago — 0.4.51 installation verified

User reports done. Read-only installed DLL hash matches candidate 0.4.51:
`1D970BE68B4716F135A7E9BB60C7477CCED1403796F5349284CDF91391F0A6EB`.
File verification only; Repair Logic manual cover anchoring and headset behavior
remain unverified. Next: usual F1/F6/F9/F10 readiness sequence, inspect brackets
around the manual while turning head, then open/close it and check return.

## 2026-09-26 — 0.4.51 no highlight; 0.4.52 world-bounds candidate

User reports "nothing" after installed0.4.51 test. Record as failed visibility,
not successful anchoring. Saved log in SignalisVrBookBounds0452/evidence/
0451-no-brackets.log. Log identifies0.4.51 and stageC begin15:52:47; object scans
continue around Repair Logic manual (internal Field Manual), but no HEADSET
OBJECT BRACKET message is emitted. This indicates the visibility/selection path
never reaches its success logging; exact rejecting condition was not logged.

Code review:0.4.51 transforms sharedMesh.bounds for proximity/placement. Shared
mesh geometry may be batched and is not a reliable substitute for individual
renderer world bounds. This is a plausible explanation for range rejection, not
confirmed by runtime bounds values in the existing log. Attempted additional
static mesh inspection could not run because the previously installed local
UnityPy import was unavailable in this turn; no new mesh measurements claimed.

Created separate SignalisVrBookBounds0452 from0451. Changed BookBrackets.cs and
HeadsetObjectHighlights.cs to use model MeshRenderer.bounds directly, without
transforming world center. Cover proxy uses fixed worldXY orientation at
bounds.min.z-0.08, 115% XY size. Retains original sprite/material, per-eye enable,
finally hiding/restoration and scene cleanup. Added pre-gate BOOK VISIBILITY log
with active/enabled/reading/consumed/room/travel state, range origin, camera and
world size. No change to rejection policy, approved INSPECT or door/ladder hiding.

Build passed with existing CS1701 warnings. 19 selection/state/range checks and
compiled finally-restoration checks,14 suppression checks, recovery/inventory/
timeout and diagnostic checks passed. Protected suppression, puzzle, recovery,
native-loader and selection-policy source hashes unchanged from0451.
Installed DLL remains0451; nothing installed automatically. Original project and
historical candidates remain preserved.

0452 SHA256:39804B006DDD844EAB24EC3BBA6163FBA2986FFBC0A2A5982C44791FDFCC1CE9.
Candidate only. Actual bounds, marker visibility/placement and headset behavior
remain unverified. Next manual install after closing game, approach Repair Logic
manual in stageC; if absent, remain five seconds for new gate evidence. No graphics
stability claim.

## 2026-09-26 15:58 America/Chicago — 0.4.52 brackets on book, user-confirmed

User reports "its on the book" following Repair Logic manual test. Record as
user-confirmed visibility and on-book placement for this manual. Preserve 0452
placement. This is not confirmation of all objects, precise fit, head-turn
stability, reading/return behavior or graphics stability.

Installed DLL hash matches0452:
39804B006DDD844EAB24EC3BBA6163FBA2986FFBC0A2A5982C44791FDFCC1CE9.
Saved runtime log: SignalisVrBookBounds0452/evidence/0452-repair-logic-on-book.log.
Log confirms Field Manual visible=True, bounds center(933.110,-943.610,-3.908),
worldSize(1.996,1.714,0.160), bookAnchor(933.110,-943.610,-4.068), sameRoom=True,
reading=False, consumed=False. This supports current renderer-bounds placement;
it does not retroactively prove the exact0451 rejection cause.
No new build or installation action. Retain current placement as working baseline.

## 2026-09-26 — Cryogenics keypad target and missing keycard box; 0.4.53

User screenshot155951 points to six-square keypad atop front-left pedestal;
large existing brackets float elsewhere. User screenshot160042 reports no box
around keycard inside open cryopod. Repair Logic manual approval is retained;
these are separate targets, not a correction of the manual report.

Read-only0452 log shows Cryo/Inter/Prompt and Cryo/ItemPickup_BrokenKey/Prompt,
both interactionScaleable, with distinct displaced popup positions. Saved log
and static asset evidence in SignalisVrCryoAnchors0453/evidence. Fresh workspace
asset reader downloaded after previous library remained inaccessible even with
read grant. Inspected Cryo controller and child hierarchy; keycard has one
KeycardModel mesh. PEN_Cryo exposes its actual interaction reference.

Texture six-square panel near pixel(84,210) in256x256 PEN_Cryo atlas mapped by UV
barycentric interpolation to top-surface world(604.009967,-305.243029,-1.958400).
Other UV hits are overlapping side/bottom faces; selected the horizontal top
face consistent with user's marked pedestal. Cryo-local(-3.1,-3.8,-1.6) reproduces
this position within0.001 game unit. LocalZ-1.665 adds surface clearance. This is
asset mapping evidence; exact pictured fit still needs headset validation.

Created0453 from0452. BookBrackets adds a dedicated keypad branch, selected only
when PEN_Cryo.interaction equals the popup's Interaction. Keypad uses mapped local
anchor,1.1x0.7 local footprint with existing115% margin. Keycard path reuses single
mesh renderer-bounds anchoring only for ItemPickup_BrokenKey under PEN_Cryo.
Original game sprite/material retained, independent VR-only proxy, source restored
and proxy hidden after each eye. Book renderer-bounds branch remains unchanged,
including confirmed Repair Logic placement. No gameplay/consumption state writes.

Build passed (existing CS1701 warnings).19 selection/state/range checks, compiled
finally restoration,14 suppression checks, recovery/inventory/timeout and diagnostic
checks passed. Anchor mapping check passed after correcting an initial PowerShell
array-expression error in the check script. Protected INSPECT implementation,
marker suppression, puzzle, diagnostic/recovery, native-loader and policy code
unchanged except version/lifecycle integration already inherited from0452.
Installed DLL remains0452; no automatic installation. Original project preserved.

0453 SHA256:8645EB25034A2AD5D7E772BCBD1D6702C04E84441FCCC4784B88B787D519CAFF.
Next: manually install after closing game; test keypad fit and keycard visibility,
pickup disappearance, actual interactions and continued manual placement. Headset
results and graphics stability are not established by local checks.

2026-09-26 — 0.4.53 manual installation confirmed
User reported "copied". Read-only SHA256 verification of the installed Mods DLL
matches candidate 0.4.53:
8645EB25034A2AD5D7E772BCBD1D6702C04E84441FCCC4784B88B787D519CAFF.
No automatic installation performed. Keypad alignment and keycard box visibility
in the headset remain unverified; next step is the user's runtime check.

2026-09-26 — 0.4.53 headset evidence and 0.4.54 candidate
User screenshot 161720 (before puzzle) shows displaced large keypad brackets.
Latest.log identifies Cryogenics/Chunk/Pivot/Interaction/Prompt/PromptSprite
with bookAnchor=none at 16:17:18, unlike the PEN_Cryo controller selected from
16:17:41 onward. Screenshot 161803 after puzzle shows brackets on keypad.
User explicitly confirms keycard box is perfect; preserve its placement.
Full read-only runtime log saved in 0454/evidence/runtime-0453-before-after.log.

Created SignalisVrCryoAnchors0454. BookBrackets.cs extends keypad selection to
exact Cryogenics/Chunk/Pivot/Interaction hierarchy and unique sibling PEN_Cryo.
Reuses existing anchor, dimensions and proxy lifecycle. SignalisVrTracking.cs
version updated to 0.4.54. HeadsetObjectHighlights, MarkerSuppression, PuzzleScreen,
HighlightPolicy and NativeLibraries hashes verified unchanged from 0453.
Build passed (existing warnings);19 policy checks, compiled finally restoration,
and14 suppression checks passed. New pre-puzzle placement remains headset unverified.
No automatic installation; installed game remains 0.4.53.
0.4.54 SHA256: 15DF10A844A71F2A194FA837357CD21DE589DD860D27F6FDBA1039EF2B41BD05

2026-09-26 — 0.4.54 manual installation verified
User reported copied. Installed Mods/SignalisVrTracking.dll SHA256 matches 0.4.54:
15DF10A844A71F2A194FA837357CD21DE589DD860D27F6FDBA1039EF2B41BD05.
No automatic installation. Pre-puzzle keypad placement awaits headset validation.
User preference: always provide both candidate DLL and SIGNALIS Mods folder links
when delivering builds. Keycard box remains user-confirmed perfect in 0.4.53.

2026-09-26 — 0.4.55 keypad enlargement and paper fit
User screenshot 162432 shows keypad aligned in 0454 and requests slightly larger.
User screenshot 162541 requests tighter paper box. Runtime 0454 log identifies
Corrupted Note in Personell with model=none because its children include multiple
book meshes. Asset inspection identifies the paper as Model (6), SpriteRenderer
55751, DET_letter 16x24 pixels at10ppu, local scale0.8, Z rotation~-17 degrees.
Other nearby children contain books, tape, and scattered paper decoration.
Evidence: note-assets.log and runtime-0454-note.log in 0455/evidence.

Created 0455 from0454. BookBrackets.cs grows only keypad width/height by15%.
Adds narrow Corrupted Note/Personell/Model (6)/DET_letter selection and sprite
bounds/transform fit with3% margin and0.04 world-Z surface clearance. Keeps keycard
and existing book geometry behavior. HeadsetObjectHighlights.cs diagnostics now
handle the sprite-backed entry. SignalisVrTracking.cs version0.4.55.
Build passed with existing warnings;19 policy checks and compiled finally
restoration passed. Protected suppression, puzzle, policy and native-loader source
hashes match0454. No automatic installation. New visual fit awaits headset check.
SHA256: 08EB9CA0B866D0C5619F9A1A4B12C68DA28BB9D890433C93DD07A9C30CCB09F0

2026-09-26 — 0.4.55 manual installation verified
User reported done. Installed DLL SHA256 matches candidate: 08EB9CA0B866D0C5619F9A1A4B12C68DA28BB9D890433C93DD07A9C30CCB09F0.
No automatic installation. Larger keypad box and fitted Corrupted Note box await headset validation.

2026-09-26 — 0.4.55 headset confirmation
User reports "the boxes work" following the requested check of the enlarged
keypad box and fitted Corrupted Note box. Record both adjustments as user-confirmed
working in the headset. Keycard fit was previously confirmed perfect.
This confirms these tested markers, not exhaustive coverage of all interactable
objects or general graphics stability. No further changes made.

2026-09-26 — 0.4.56 dialogue overlay candidate
User video VirtualDesktop.Android-20260926-163502-0.mp4 shows bunk dialogue on a
floating full game screen and return to stereo after closing. User reports same
on item pickup. Sampled 17 frames in work/issue-frames; log0455 confirms dialogue
visible=True with gameplayCameraEnabled=True triggers CAMERA RECOVERY and live
menu capture. This is intentional old routing, not evidence of desktop OS capture.
Saved read-only runtime log in0456/evidence/runtime-0455-dialogue.log.

Created SignalisVrDialogue0456 from0455. SignalisVrTracking.cs removes dialogue-only
recovery pause, detects item-view box too, calls overlay after world eye render,
and updates version. New DialogueOverlay.cs temporarily renders live DialogueCanvas
as WorldSpace on isolated layer31 with depth-only eye pass, retaining stereo room.
Saves/restores16 canvas/rect/layer/camera/target settings in finally. Does not alter
UI controller state or inputs. build.ps1 adds overlay and UnityEngine.UIModule.
BookBrackets, HeadsetObjectHighlights, MarkerSuppression, PuzzleScreen,
DiagnosticFlow and NativeLibraries hashes unchanged from0455.

Build passed with existing warnings. New compiled overlay restoration check passed;
19 policy checks, compiled marker restoration,17 recovery checks plus long-inventory
cases and diagnostic checks passed. No headset validation yet. Actual disabled-camera
fallback remains; if pickup disables gameplay camera it can still use capture.
No automatic installation. User must test bunk dialogue, pickup messages/choices,
close/resume, inventory and puzzle fallback. SHA256: 5862B52AC574C93B5A46AE6562C5934F5A6D9D3E055F0D0FA3F2598ACEA7BBC1

2026-09-26 — 0.4.56 manual installation verified
User reported copied. Installed DLL SHA256 matches candidate: 5862B52AC574C93B5A46AE6562C5934F5A6D9D3E055F0D0FA3F2598ACEA7BBC1.
No automatic installation. Dialogue/pickup overlay and transitions await headset validation.

2026-09-26 — 0.4.56 headset confirmation
User reports "it worked" following the requested bunk dialogue and item pickup
check. Record the dialogue/pickup overlay fix as user-confirmed working in the
headset. No separate exhaustive inventory, puzzle, choice or stability validation
was reported. No further changes made.

2026-09-26 — 0.4.56 starting-area validation baseline
User explicitly confirms, for the starting area:
- Stereo works.
- Inventory works.
- Text boxes work.
- Puzzles work.
- All interactable items have boxes.
Record 0.4.56 as the user-validated starting-area baseline. Scope is the starting
area only; do not infer coverage of later areas or exhaustive graphics stability.
No changes made. Preserve this build as a fallback for subsequent development.

2026-09-26 — 0.4.57 positional tracking candidate
User requests leaning/moving head should move stereo viewpoint before proceeding
to cutscene/next area. User also requests step-by-step installation/test instructions.
Preserve0.4.56 as confirmed starting-area baseline.

Created SignalisVrHeadPosition0457. SignalisVrTracking.cs reads position alongside
rotation from prepared render-thread pose (and local pose for F9), captures neutral
head position with F7/start/F10 recenter, maps position delta through rig/base heading
and inverse neutral at5 units/metre, and uses stereoPosition for eye origins.
Translation does not depend on current headset gaze. DialogueOverlay.cs uses that
same stereoPosition for its plane. Adds throttled position diagnostics and version.
No game-camera/character position writes; no collision/reach changes.
BookBrackets, HeadsetObjectHighlights, MarkerSuppression, PuzzleScreen,
DiagnosticFlow and NativeLibraries hashes unchanged from0456.

Build passed. New test-position.ps1 compiles extracted production translation
expression with numerics adapters:8 checks for recenter/metric axes/yaw/game up;
source integration checks for both pose paths and eye origin.75 heading cases,
19 policy checks, compiled marker/overlay restoration,17 recovery cases plus
long-suspension checks passed. An initial attempt to save the test via nested shell
here-strings failed; saved it with apply_patch and reran successfully.
Headset behavior remains unverified. No automatic installation.
SHA256: 3D75E52ABE6E0D0D6333BE20D40402BF8C716F0D7A09AEED75A199DDF94E98F8

2026-09-26 — 0.4.57 manual installation verified
User reported copied. Installed DLL SHA256 matches candidate: 3D75E52ABE6E0D0D6333BE20D40402BF8C716F0D7A09AEED75A199DDF94E98F8.
No automatic installation. Positional headset tracking awaits user validation;0.4.56 remains the confirmed starting-area fallback.

2026-09-26 — 0.4.57 positional tracking issue under investigation
User reports room appears to move instead of viewpoint; supplied headset recording
VirtualDesktop.Android-20260926-165838-0.mp4. Installed0457 hash verified unchanged.
Sampled54 frames in work/position-frames. Read-only log saved to
work/position-investigation/runtime-0457.log. Log confirms nonzero tracked head
translation reaches computed eye origin; video alone does not establish physical
movement direction or whether sign/scale/latency/rig mapping is responsible.
Asked for stick-still, forward-facing lean toward nearby object: closer/farther/
barely moves. No code change or new build made while diagnosis is unresolved.
0.4.56 remains confirmed baseline;0457 is not accepted as working positional tracking.

2026-09-26 — 0.4.58 level tracking axes candidate
User clarifies forward lean moves objects closer but room slides/wobbles; needs
room to feel static. Do not reverse translation. Logs show tilted FP rig (e.g.
278.4deg pitch versus270deg level). Read-only FPv2 IL confirms mouse-look rotations
applied to its camera rig.0457 directly composes full rig pitch/roll into physical
tracking space, tilting physical-up away from room-up. This is a coordinate defect;
its responsibility for the full perceived wobble remains a hypothesis.

Created SignalisVrStableTracking0458 from0457. Only runtime source change is
SignalisVrTracking.cs: LevelTrackingRig projects heading onto game XY floor with
negativeZ up, used for BOTH rotation and translation; version0458. Maintains metre
scale/sign, synchronized pose, F7 references and source camera position. No smoothing.
DialogueOverlay, BookBrackets, HeadsetObjectHighlights, MarkerSuppression, PuzzleScreen,
DiagnosticFlow, NativeLibraries hashes unchanged. Read-only0457 log in evidence.

Build passed. Updated test-position extracts production mapping and leveling method:
8 positional cases and75 tilted-rig combinations validate floor-aligned up/forward
and shared rotation/translation axes.75 heading cases, compiled dialogue restoration,
17 recovery checks plus long-suspension checks pass. Headset symptom fix unverified.
No automatic installation. SHA256: B110B0BD9E6CF3F2F16D2988F7D8EDED348340BB5C32B455B58784AE7B889D1B

2026-09-26 — 0.4.58 still tilts; diagnosis reopened
User reports room still tilts when looking around. Installed hash B110B0BD9E6CF3F2F16D2988F7D8EDED348340BB5C32B455B58784AE7B889D1B and runtime header confirm0458.
Saved runtime log to work/position-investigation/runtime-0458.log.
Correction to prior interpretation: Euler pitch284.6/yaw90/roll270 does NOT by
itself imply a tilted rig floor. Quaternion reconstruction of that logged rig
has up=(0,0,-1); the Euler pitch component encodes heading in the rotated game
coordinate system. Thus0458's leveling is not established as addressing the cause,
and the earlier tilt diagnosis was overstated. Do not claim it fixes the symptom.
Asked user whether upright left/right head turns cause sideways horizon tilt,
room lag/sliding, or both. No further build issued pending clearer diagnosis.

2026-09-26 — 0.4.59 horizon comparison candidate
User clarifies upright slow left/right head turns tip horizon sideways. Raw pose
logs during turns include roll around-10 to+8 degrees; cannot infer whether this
is actual physical roll or another tracking-space issue from this evidence alone.
Created SignalisVrHorizonTest0459. SignalisVrTracking.cs adds default-on horizon
stabilization at RenderStereo entry and edge-triggered global F8 toggle. Uses
LookRotation(viewForward,roomUp) to remove view roll while preserving gaze and
translation. Returns original pose within vertical singularity threshold0.995.
Explicit tradeoff communicated: intentional sideways head roll is also suppressed;
this is a diagnostic comparison, NOT an established physically correct tracking fix.
No native, gameplay, UI or marker source changes. Version0.4.59.

Build passed. test-position now tests extracted production stabilizer:75 combinations
preserve forward and remove sideways horizon tilt, vertical fallback checked;8
positional checks pass. Compiled UI restoration and17 recovery checks plus long
suspension pass. No headset validation. No automatic installation.
SHA256: A28AC06C84F443A6B082C6D6AFA49670387AF18AED23E985317B8EF91D427318

2026-09-26 — partial0459 result;0460 diagnostic build saved; work deferred
User confirms improvement after second F8 press (stabilization ON), with a little
remaining tilt in BOTH left/right and up/down gaze. Installed0459 hash verified.
User asks finish current work then return another time. No further runtime testing
requested and no automation scheduled.

Finished SignalisVrHorizonDiagnostics0460 from0459. Runtime change limited to
SignalisVrTracking.cs: version0460 and read-only ObserveRenderedHorizon calls before
and after each eye render at0.5s sample interval. Logs eye-space room-up, rollDeg,
eye/source-camera positions and projection cross terms. Handles diagnostic errors
without stopping rendering. No change to existing stabilization behavior.
Saved0459 runtime evidence in0460/evidence/runtime-0459-partial-improvement.log.
Build passed with existing warnings;8 positional cases,75 tilted-rig/horizon cases,
vertical fallback and compiled UI-restoration checks pass. No headset validation.
SHA256: D129EA4DBB74EE06960E942CA280C2114DAEC12CD6052236B1B9C0DFAA772FAA
NOT installed; installed0459 retained.0.4.56 remains confirmed starting-area baseline.
Resume here: inspect actual-eye roll before/after callbacks in0460 before further
speculative corrections; give user numbered startup/test steps and both DLL/Mods links.

2026-09-26 - 0.4.61 Quest buttons candidate, NOT installed
User requests Quest sticks/buttons first, tracked controller features later.
Explicit requested mappings: right grip aim, right trigger fire, B reload.
Native game controller support preserved through session-only Rewired custom
controller; abandoned SendInput draft preserved under work, not built.
Based on confirmed0456, NOT unfinished headtracking0459/0460. User informed.
Changed: SignalisVrTracking.cs version0461, controller setup/turn update and
Stop release; build.ps1 adds Rewired_Core and new controller source trees.
New QuestControllers.cs and QuestInputPolicy.cs implement local OpenVR Touch
state reading, native Rewired action maps for all players including system,
neutral re-arm, focus/dashboard/disconnection release, and FPv2 yaw adapter.
Right B shares native Reload and Cancel for context-dependent menu behavior.
Local installed Touch legacy mapping saved in evidence. Action names extracted
from installed game's Rewired InputManager data, work/rewired-strings.txt.
No game/saved bindings modified. No tracked hands or motion aiming yet.
Build passed (existing framework-version warnings). test-quest:15 policy and
4 compiled integration checks pass. Dialogue16 settings restoration, recovery17
plus long suspension, highlight19 plus compiled restoration, suppression14 pass.
Hardware/native custom-controller creation, routing, menu behavior and FP turning
remain unverified. Runtime QUEST logs added. Packaged DLL+README ZIP preserved.
SHA256:474AC659C5459680E3BFBB61E827B23909E4EF8FF44C8B2E524EAF8ADF9DBEF3
Installed0459 hash still A28AC06C84F443A6B082C6D6AFA49670387AF18AED23E985317B8EF91D427318.

2026-09-26 - User copied0461 without backing up installed0459.
Installed0461 verified SHA256474AC659C5459680E3BFBB61E827B23909E4EF8FF44C8B2E524EAF8ADF9DBEF3.
Prior0459 DLL absent from output folder; preserved0459 sources remain intact.
Rebuilt fallback0459 successfully from those sources and archived Fallback0459.zip.
Rebuilt DLL SHA256E6B73AB94C9201C5A648AE67038100994680AB4CA5405AA27CCBC0852EF35648.
This is a rebuilt fallback, not a byte-identical recovered original binary.
No installed game files changed. Controller headset test still pending.

2026-09-26 - 0.4.62 Quest main-menu input candidate
User reports main-menu controls do not work.0461 required system connection
and active stereo/CanSubmit for all controller polling; runtime0461 log confirms
version loaded with no QUEST setup/poll messages. Saved log under0462/evidence.
Changed QuestControllers.cs: automatic input-only background OpenVR connection
with5s retries, separate native readiness (no stereo gating), scene promotion
before F6, preserve focus/dashboard gates and gameplay-only turning guard.
Background client bypasses scene IsInputAvailable (it does not own scene focus);
foreground game/dashboard/device checks still required. Background Touch states
must be tested in user's runtime. Scene polling retains IsInputAvailable check.
SignalisVrTracking.cs:0462, automatic connection update, F6 promotion, persistent
F12 controller stop; scene Stop only neutralizes, does not permanently disable.
QuestInputPolicy.Ready extracted; test-quest now20 policy/9 compiled integration
checks. Build, controller tests, diagnostic gates and dialogue restoration pass.
No headset/native routing confirmation; this is a test candidate, NOT installed.
Other visuals and bindings unchanged. DLL/README ZIP saved.
SHA256:879F13C23AA7937D12C8ABC676373C359D3116626D81A2B3588F99C3334F4A40

2026-09-26 - Follow-up:0461 buttons AND sticks also fail after stereo.
User reports game closed. Fresh runtime log provides definitive setup error:
[17:48:06.859] Quest setup failed: Native action not found: Interact.
Earlier no-QUEST observation was before user's F6. Menu gating is real but not
the only issue.0462 was not delivered/installed; saved as intermediate candidate.
Corrected extraction: regex minimum4 chars omitted the actual action name Use;
Interact is its descriptive label. Raw serialized bytes show len3 Use, type1,
len8 Interact. Other mapped names inspected including Sprint (description Run).

0463 from0462 corrects Use, preflights all17 required actions before controller
creation, logs resolved IDs, adds separate native UI category map for menus.
Carries automatic background connection/menu readiness and F6 promotion from0462.
Changed QuestControllers.cs, version SignalisVrTracking.cs, test-quest, README.
20 policy/11 compiled checks pass; diagnostic stage and dialogue restoration
checks pass. Build passes with framework warnings. Hardware routing unverified.
No auto-install. User asked to keep game closed for copying this candidate.
SHA256:912CFBE154175D8A5E2CB0B99542F0E1542485A1B9CAF5E96640FE0FCE57D397
DLL and README ZIP preserved. Installed0461 remains until user's manual copy.

2026-09-26 -0463 runtime feedback;0464 first-person input adapter
User: both sticks fail in first person; right trigger switches to keyboard.
Runtime0463 saved: native maps succeeded System/Elster, FP turning=True,
Quest armed/focused, left stick movement e.g.0.9622334 is arriving. No DIAG A/B/C
in captured0463 log. Video175538 inspected via4sec contact sheet: desktop
first-person view in SteamVR Home, controller pointer visible. Old yaw adapter
required active stereo, so it never ran here. FPv2 IL confirms Unity axes and
A/D key checks for its physics strafe. Trigger mode switch may involve desktop
pointer mouse events or native custom-controller classification; exact source
not proven. Avoid claiming VirtualDesktop configuration is established cause.

0464 adds QuestFirstPerson.cs scoped managed FPv2 Harmony transpilers for GetAxis,
GetKey and AddForce, preserving original update/gates/physics. Analog-scaled
strafe uses existing forces; native horizontal neutralized only during active FP.
Forward uses native Move Vertical. Right stick look mapped independent of stereo,
90deg/sec normalized against FP mouse sensitivity; left grip sprint key adapter.
Recent Quest activity sets native InputControl.usingController plus OnGUI state's
Controler value to prevent same-frame keyboard detection undoing mode.2sec idle
window permits keyboard/mouse return; no settings overwrite. Scoped FP mouse look
ignored while Quest owns input. Availability guards focus/dashboard/stop/disconnect.
Removed old direct yaw updater; all visual/headtracking sources unchanged except
version0464 and mode update call. Build adds Harmony/Physics refs and adapter tree.
20 policy/11 integration +16 turn math + FP call layout/8 adapter checks pass;
diagnostic and dialogue checks pass. Build passed (framework warnings).
Hardware behavior and native Harmony installation UNVERIFIED. No auto-install.
SHA256:9FFB9B3A276E24A6FD69AE5A618554EFE2D8E6408C029EF2EC2E0A3F13BB13F4
DLL/README ZIP preserved, previous0463 zip fallback retained.

2026-09-26 -0464 buttons unresponsive: runtime diagnosis
Installed0464 hash verified. FP Harmony patches installed successfully:
OnUpdate axes4/keys1, x_movement keys2/forces2, patched=True.
Poll log repeatedly focus=True vrInput=True left=True right=False armed=False.
Occasionally both false. This build requires both controller reads to succeed
before arming, so unavailable right state disables ALL controls. No setup error.
Cannot distinguish sleeping/disconnected/right-role or legacy state failure from
current boolean log. Ask user to wake both and verify right controller in SteamVR
before issuing another speculative build. Runtime log preserved in0464/evidence.

2026-09-26 -0464 controls eventually work; map trigger still selects keyboard
User estimates10minutes unresponsive then controls work; map right trigger still
changes input mode. Latest log shows both hands armed and nonzero sticks/buttons
by18:10. Cause of initial missing right-controller state remains unestablished.
Saved runtime0464-delayed-working.log. Do not claim handshake delay fixed.

0465: QuestHandInputPolicy uses independent hand neutral/reconnect gates instead
of requiring both hands. Left/rightArmed log added. Missing hand can't block other.
QuestFirstPerson mode ownership persists after Quest activity while available,
released by physical keyboard OnGUI keydown or availability loss. Removed2s timer.
Replaced OnGUI correction postfix with guarded prefix preventing mouse detector
side effects; guarded native GetInputState/isMouseKeyboard/isControlerInput
prefixes agree with controller mode. Existing first-person adapters unchanged.
Version0465/build adds IMGUIModule reference. Test-hands9 passes, controller20+
compiled11 passes, turn16/FP layout/8 adapter passes, dialogue restoration passes.
Build success; runtime map fix/hand availability timing UNVERIFIED. No install.
SHA256:E5E58A218450A6B9CF0C56208384DA0849D05F79C66AAAAB950C86C6E7C39A7F
DLL/README ZIP preserved. User gets both links and focused numbered map test.

2026-09-26 -0465 controls also act on SteamVR Home; stop/start input reported
Latest log remains DIAG stage0/localFrames0: automatic Background connection,
not promoted to Scene via F6. Home remains active under this input-only design.
That allows shared legacy input with Home; prior before-stereo testing exposed
this design flaw. Log also shows dashboard=True/inputAvailable=False by18:19:50,
which intentionally disables Quest polling; states had succeeded moments before.
No new build issued. Next focused test uses existing F6/F9/F10 scene/stereo path,
close dashboard and neutral rearm to distinguish background/Home conflict from
remaining native input problems. Do not claim full stability based on log alone.
Automatic main-menu/background design needs reassessment after this test.

2026-09-26 -0465 stereo report and explicit Home ownership requirement
User: no left forward/back, janky strafe, X/Y/A fail, right up/down fails, yaw
sluggish. Runtime evidence saved in0466. Source confirms stereo ignores FP local
pitch via Heading(originalRotation); do not claim right pitch is supported yet.
Native input action loss needs route evidence; raw Quest values alone insufficient.
User explicitly requires SteamVR Home not receive controls even before stereo.

0466 changes EnsureQuestConnection to automatic VRApplication_Scene, removes
Background state/bypass and promotion Shutdown. F6 remains explicit diagnostic
stage acknowledgement before F9/F10 rendering; no automatic eye submission.
SteamVR may show waiting screen before stereo, disclosed in README/user message.
Added read-only4Hz change-based QUEST ROUTE showing scene-focus PID, game PID,
source, raw CustomController, controller enabled/counts, per-player Default/UI
map presence/enabled/count, native Move Vertical/Use/Inventory/Pause/Confirm.
No further movement/turn speed changes; input failures remain open pending trace.
20policy/11integration and16turn/layout/8adapter checks pass. Build success.
Hardware ownership/input unverified. No automatic install;0465 fallback saved.
SHA256:D9F73DCA8CD5DB621794627A568BF5D2B8C40ABEF8F0B62493F202FCCE49D51D

2026-09-26 -0466 menu controls fail
New trace shows dashboard=True/inputAvailable=False during reported test, with
brief dashboard-close intervals arming both hands. Controller enabled True,
4axes/10buttons, Default maps17 enabled. UI map missing (category lookup issue
or absent category) is not yet established causal. Source buttons zero in trace.
Scene-focus PID remains0 even after Scene initialization; startup connection
alone is not proof of active scene ownership. Earlier ownership claim overstated.
Before further changes clarify whether main menu remains visible after dashboard
closes: current code does not present pre-game menu directly to headset. Need
actual menu presentation if user otherwise must use SteamVR desktop dashboard.
Do not bypass dashboard input guard (would reintroduce simultaneous UI control).

2026-09-26 - user clarifies current failures INSIDE stereo;0467 action bridge
Read fresh0466 trace: sceneFocusPid=gamePid40748, armedTrue; forward1.00 at raw
custom controller but native Player forward0.00; A customTrue but Use/ConfirmFalse.
This conclusively localizes loss after custom controller state, before player
native actions. Exact internal map-cache cause not established. UI map missing
alone is not proof. Prior dashboard observation did not describe this stereo test.

0467 new QuestActions.cs guarded native Rewired Player query postfixes for int/
string GetAxis/GetAxisRaw/GetButton/GetButtonDown/GetButtonUp. Known configured
player IDs only, original input retained, strongest axis wins to avoid doubling,
button OR and frame-stable down/up edges. No keyboard injection. Native mapping
stage bypassed for Quest; existing maps retained. QuestButtonEdges pure helper,
InstallQuestActionBridge after FP installation, poll sampling/Stop reset.
Stereo pitch new questStereoPitch accumulated from rightY only during stereo,
clamped60deg, composed into baseRotation and reset on Stop. RightX speed and
FP strafe force not retuned; reported sluggish/janky feel remains open pending
functional controls test. Other UI, object, headtracking experimental sources
unchanged. Native detour runtime success is unverified, not claimed fixed.
Build passed;15edge/action+2compiled,20policy+11compiled,16turn/layout+7adapter,
9hand, dialogue restoration checks pass. Test adjusted to allow stereo pitch
composition while preserving non-stereo input eligibility checks.
SHA256:56C38705E744D931B24C79A14E35557F2204491455582D4CC63EFBFA726E0FE2
No install; DLL/README ZIP retained. User gets both links and focused stereo test.

2026-09-26 -0467 feedback / autonomous investigation /0468 candidate
User reports only right-stick up/down improved; forward/back, buttons, strafe/yaw
remain unsatisfactory. User asked for substantial independent controls work while
away. No claim of complete controls success from0467's patched diagnostic reads.

Prepared isolated workspace game copy and native Rewired mapping probe. Startup
exited before Unity/probe: normal exit53, debugger exit84, also fails without
MelonLoader. No native mapping runtime result obtained. Cause unresolved.
USER THEN INSTRUCTED: STOP LAUNCHING THE GAME. All launch attempts ceased immediately.
No further game launches after that instruction; code/static checks only.
Installed game/mod files never changed. Original0467 installed hash preserved.

Read installed Rewired native binary via matching Cpp2IL metadata and Iced
(nonexecuting disassembly), public Rewired docs, installed FPv2 IL and old traces.
ActionElementMap constructor already enables binding; disabled-default hypothesis
not supported. Native CreateElementMap bakes element/adds lists; AddMap follows
attachment and map-enabler path. Reattach-completed-map is a hypothesis-driven
candidate, NOT an established fix/root cause. Saved disassembly in0468/evidence.

Concrete FP finding: OnLateUpdate assigns Quaternion.y to player.fAngle, while
other FP code uses degree thresholds. Also AddForce applied once per render frame.
0468 corrects heading to world Euler degrees for Quest strafe only, and scales
Quest analog strafe force by deltaTime/fixedDeltaTime with50ms cap. Keyboard
behavior remains original. 42 math/invalid-input cases confirm rate compensation;
actual physics/game feel remains unverified. Yaw speed remains90deg/sec.

0468 sources in outputs/SignalisVrQuestNative0468:
QuestControllers:6axes (UI4/5 separate from movement0/1), explicit normalized axis
calibration/zero extra deadzone and type GUID, completed-map commit, skip nonexistent
UI map category in trace, actual player.input diagnostic. Removed getter bridge.
QuestNativeMap replaces QuestActions: completed clone attached via native AddMap,
verify assigned controller/map owner/element type/index/enabled; retain template
for bounded missing-map/assignment recovery. Do not reenable intact maps game has
disabled; do not alter keyboard/physical controller maps. Failure logs and releases.
QuestFirstPerson: guarded LateUpdate setter adapter, analog frame-rate force fix.
QuestInputPolicy: pure StrafeScale helper. Main tracking changes version only.
Right-stick stereo pitch preserved. Approved UI/highlight/native support source
hashes unchanged versus0467; evidence/preserved-baseline.txt records comparison.

Build passed against installed references (existing CS1701 framework warnings).
Checks passed:20controller/11compiled,16turn/7FP integration/layout,9independent
hands,15button-layout/helper/3compiled,42strafe/12compiled-layout/noOSinjection,
16-setting dialogue restoration. These are software checks ONLY.
No headset/gameplay runtime validation. Main-menu display and exclusive Home focus
remain unresolved/unverified: Scene init alone previously had focusPID0.
No claim all controls fixed. README gives manual numbered test/rollback steps.
Rollback0467.zip captures installed DLL. No install performed.
0468 DLL SHA256:0E089415A1855840500A92F46F5175DB2751137BEAB06D7AA4FE13BA7A3378E9

2026-09-26 -0468 manual menu test
User copied0468; reports nothing happens at main menu. Installed DLL hash matches.
All34 Default bindings across System/Elster validated enabled with correct element
indices/ownership. FP OnUpdate/x_movement/OnLateUpdate patches installed. During
menu test log shows dashboard=True,inputAvailable=False,sceneFocusPid=0,armedFalse,
zero raw Quest samples; this test does not evaluate completed-map input routing.
Main-menu ownership/presentation remains unresolved, not declared fixed. Preserve
menu-test0468.log. No game launch/restart performed by assistant. Next manual step:
load starting-area save using keyboard to isolate gameplay/stereo input separately.

2026-09-26 -0468 stereo forward/back failure confirmed
User: forward/back does not work. Saved stereo-test0468.log. At19:50:00.337:
sceneFocusPid=gamePid61344, dashboardFalse,inputAvailableTrue, armedTrue;
sourceMoveY=.99/customForward=.99 but both native Player MoveVertical0.
At19:50:10.443 raw/customA True but native Use/ConfirmFalse. All installed bindings
previously validated correct indices/enabled/ownership. Completed-map reattachment
DID NOT fix routing. Do not repeat that hypothesis as established cause.
Diagnostic defect: Vector2 concatenation prints type name, not coordinates; future
code must explicitly format input.x/input.y. No actual player vector evidence yet.
Next discriminating manual test: keyboard W/S in the same active FP scene, to
confirm native movement still functions under0468 before changing another route.
No game launch/restart or installed-file modification by assistant.

2026-09-26 -0469 native input candidate after WASD confirmation
User confirms WASD works in0468. Static native inspection finds CharacterAction
directly calls integer Rewired native getters and AlternatePlayerController
consumes its Move vector. Earlier managed postfixes were not native runtime proof.
Created outputs/SignalisVrQuestNative0469. New QuestNativeReads resolves MethodInfo
entry points, checks all five module-relative addresses before hooks, roots
delegates/trampolines, preserves originals and game suspension/processing.
Native Update observation counts getter calls inside that Update and resulting
Move/Use. No player action/transform writes. Focus/dashboard/neutral/F12 retained.
Changed QuestControllers (install/snapshot/Vector2 diagnostic), QuestInputPolicy
(strongest-source axis merge), build.ps1 and version0.4.69. New73-case native-read
test and updated old test wording. FP and approved visuals hash-identical to0468.
Build passes existingCS1701 warnings. Checks pass:73new routing/edges/ABI/compiled,
20policy+11compiled,16turn+7FP,9hands,15button+3compiled,42strafe+12compiled,
16overlay settings. NO native runtime/headset proof. Main-menu focus/Home ownership
unresolved. Simultaneous physical/Quest buttons may independently produce edges.
0469 SHA256 B00E65A80C6E24E2BB7DF91FB119A173845F546156C6C249E0CF4C9F0565602F
Installed0468 unchanged:0E089415A1855840500A92F46F5175DB2751137BEAB06D7AA4FE13BA7A3378E9.
Rollback0468.zip saved installed DLL. Evidence contains build/tests, native
disassembly, baseline hashes, game/candidate hashes. No automatic install/launch.
User asked about Steam Input; explained configurable SteamVR bindings still need
game integration. User closed SIGNALIS manually, ready to copy candidate.

2026-09-26 -0469 crash /0470 removal of custom registration
User reports game crashed moving right in stereo. Installed0469 hash confirmed.
Saved Melon log, crash Player.log, minidump, Windows event to0469/evidence.
Windows c0000005 Mono+2fdc28. Unity log repeated Rewired update out-of-range and
GetLastActiveController -> CharacterAction.Update exceptions, then sync/UI errors.
Native hooks ran idle; getter counters stopped upon stick activity. No movement
success established. Exact crash root cause unresolved; custom registration suspect.
Do not retest0469. No assistant game launch/restart or installed-file changes.
Created0470: no custom controllers/maps/assignment/values/Rewired event callback;
managed OnUpdate samples input. Removed CharacterAction.Update detour to preserve
native exception flow. Four native integer getter hooks retained, original reads,
focus/arming/stop guards retained. Trace observes actual action/player vectors.
Changed QuestControllers,QuestNativeReads,build,version,test expectations; removed
QuestNativeMap from0470. Approved visuals/QuestFirstPerson hash-identical to0469.
Build passes existingCS1701 warnings; checks80new,20policy+11compiled,16turn+7FP,
9hands,15button+3compiled,42strafe+8compiled,16overlay. Software checks only.
No native/headset validation; main-menu/Home ownership unresolved. Candidate0470
not installed. Rollback0468 preserved. Next test movement BEFORE stereo.
0470 SHA256652FA3BAA671601FFA33718E8286B57037B64EE4166B7244FE9363A598E0FF04.

2026-09-26 -0470 pre-stereo test
User copied0470, loaded room, reports no forward movement before stereo.
Log shows stage0,dashboardTrue,inputAvailableFalse,vrInputFalse,armedFalse,
zero sampled input, even while desktop game focusTrue. This test does not
establish native routing failure: SteamVR is withholding controller input.
Saved pre-stereo-test.log. Next step close dashboard and check availability;
no repeat movement test or new build yet. No assistant launch/install.

2026-09-26 -0470 dashboard closed /partial controls reported
User reports lateral movement and looking left/right/up/down work; headset image
shows SteamVR Next Up (stereo remains stage0). Log confirms dashboardFalse and
inputAvailableTrue since20:17:11; raw forward/back, strafe, look and A now received.
Native counters stop at492axis/2706buttons upon activity; gameInput/actionMove stay0.
Thus custom removal alone has not established native forward/button routing.
User-observed lateral/look success uses existing FP adapter; do not dismiss it
as SteamVR Home movement based solely on headset screenshot. Need distinguish
SIGNALIS desktop observation from headset waiting screen. No crash reported0470.
Saved dashboard-closed-test.log. No launch/install performed.

2026-09-26 -0470 forward failure confirmed by user
User confirms only lateral movement, no forward/back. Also reports both SIGNALIS
and SteamVR environment respond. Treat shared control as unresolved; do not call
pre-stereo isolation complete. Raw input arrives, native getter counters stopped.
Need current Unity Player.log to discriminate exceptions from input suspension.
Exact-file read permission was requested/granted for current turn, but Get-Content
and shared-read FileStream both still receive Access Denied. No ACL changes or
privilege escalation attempted. Ask user to copy log into user-diagnostics.
Saved current mod log as forward-failure.log. No new speculative build, no launch,
no installation. Next work depends on detailed runtime exception evidence.

2026-09-26 -0471 confirmed shared native getter bug
User copied Unity log to0470/user-diagnostics/Player.log (31,711,857bytes).
Same collection count/empty stack errors persist without custom registration;
withdraw custom registration as sufficient explanation. Static inspection finds
InputControl.GetInputState native RVA299B70 (mov eax,[rcx+18h];ret) shared by121
methods including List<T>.Count,Stack<T>.Count,Rewired.UpdateLoopDataSet<T>.Count.
Our QuestInputStatePrefix returns1 and skips original after first Quest activity,
so patch can override all unrelated shared getters. This is a concrete bug,
matching error pattern and native consumption stopping after stick movement.
Exact eventual Mono crash causality/runtime recovery still unverified.
0471 removes that patch and prefix. Other7input hook targets each unique in
inspected metadata. Existing InputControl.OnGUI sets actual m_State; getter reads
it naturally. Changes QuestFirstPerson,version,banner,new shared-getter regression.
Native reads/policy/visuals preserved. Source audit/evidence saved0471/evidence.
Build and all prior checks pass plus collision regression. No runtime test,
launch/install by assistant. Full user-controlled restart required. SteamVR
simultaneous control/pre-stereo display unresolved.0471 candidate ready.
SHA256 EE6FF2CDB5468BCF3EAF374C476CABB69D005FCB4258F5631B22FDF6133A1321.
Installed0470 unchanged. Historical0468 rollback also has unsafe getter patch;
retained as history, not recommended as safe controls recovery.

2026-09-26 -0471 user reports desktop controls working
After left controller menu button (closing dashboard), user reports movement,
looking, opening menu and inventory all work. Log stage0/dashboardFalse/inputTrue,
continuous native getter counts beyond100000; sourceForward1 ->nativeForward1
and gameInput.y.50 whileplaying. Zero on release. Native input no longer stalls
at first activity. This supports restored movement routing, not just raw polling.
CharacterAction diagnostic still0 likely selects inactive object; do not use that
observation to contradict actual playerinput/user report. Saved working log.
No crash reported this test; not long-term stability proof. Aim/fire/reload not
individually verified, stereo and SteamVR exclusivity remain pending.
Next manual step enable stereo, then isolate forward/back before wider controls.
No assistant launch/install.

2026-09-26 -0471 stereo forward/back verified by user
User followed F6, then F9 (stage2 eye rendering confirmed), then F10 and confirms
stereo appears in headset. User explicitly confirms forward AND backward left-stick
movement work in headset. Saved stereo-movement-working.log. This is headset
movement proof for0471, not all controls/long-term stability proof. Next check
right-stick look in stereo, followed by menu/inventory and gameplay buttons.
No code changes, installs or assistant launches during these manual tests.

2026-09-26 -0471 all movement/look directions verified in stereo
User explicitly confirms movement forward/back/left/right and looking up/down/
left/right in headset. These eight directions are user-verified on0471.
Menu/inventory worked before stereo; headset menu/inventory check next.
Aim/fire/reload, long-term stability and SteamVR input exclusivity remain unverified.

2026-09-26 -0471 headset inventory/menu verified
User clarifies X opens inventory and Y opens menu separately; not both on one
press. Together with all movement/look directions, these now work in stereo by
user observation. Interaction A next; aim/fire/reload and SteamVR exclusivity
remain unverified. No code/build/install changes.

2026-09-26 -0471 headset interaction/book verified
User confirms A opens repair manual and has gone through the book multiple times.
Record repeated book interaction in headset as user-verified. Movement/look all
axes, X inventory, Y menu previously verified. Back/cancel B, sprint, aim/fire/
reload still need individual checks; no weapon availability assumed in starting
area. SteamVR exclusivity and longer-session stability remain unresolved.

2026-09-26 -0471 B/back verified in headset
User confirms right-controller B closes the book and returns to the room. This verifies back/cancel, not weapon reload. Next check left-grip sprint; aim/fire/reload and SteamVR exclusivity remain pending.

2026-09-26 -0471 left-grip sprint toggle verified
User confirms left grip toggles walking/running: press once to run, release remains running, press again to walk. Record actual toggle behavior; do not claim hold-to-sprint or change without preference. Aim/fire/reload remain unverified; weapon availability not assumed. SteamVR exclusivity remains unresolved.

2026-09-26 -0471 right-stick video reviewed
Reattached VirtualDesktop.Android-20260926-204141-0.mp4 now accessible;9.29s. Reviewed extracted sequence at4fps across full duration, saved contact sheets/video metadata in0471/evidence. SIGNALIS view rotates/pitches; Quest system app overlay visible at beginning/end. No SteamVR Home environment visible during gameplay. This cannot establish background Home input exclusivity. Do not claim full exclusivity based on video. Weapons unavailable; aim/fire/reload deferred.

2026-09-26 -0472 candidate, game closed by user
User requests complete input coverage before tuning left/right stick performance.
Prepared live pre-stereo menu scene and scene-PID input gate; stick response unchanged.
Changed QuestMenuScene(new), SignalisVrTracking, PuzzleScreen, QuestControllers,
QuestFirstPerson, build.ps1; new test-menu-scene. Preserved native input bridge,
policy, markers and shared-getter fix. See0472/INVESTIGATION.md and evidence.
Build +13 existing suites +new menu suite pass. Inherited portability check lacks
UserLibs payload in incremental package and failed; unchanged loader not revalidated.
DLL SHA256 4064AC16FFB79977797CF1BF674D394934BDB809D80B83C17D0E2F8E5247B529.
Candidate ZIP FBBF3EE2FFD22D89BA8BC8C277ED50E4307550C67E9DC8F40F6F140563C51914.
Installed remains0471 EE6FF2CDB5468BCF3EAF374C476CABB69D005FCB4258F5631B22FDF6133A1321.
Rollback0471.zip saved. No assistant game launch/install. Menu rendering, main-menu
inputs, SteamVR focus exclusivity, stereo handoff and graphics stability UNVERIFIED.
User0471 right stick only turns; no dashboard reported. Weapons still unavailable.
Next: user copies0472, then one headset test at a time. Stick tuning deferred.

2026-09-26 -0472 startup failure observed;0473 scene-load fix
User headset screenshot shows Waiting at startup. Saved0472 Latest.log under
0473/evidence/0472-startup-stopped.log: live menu starts, both eye textures accepted,
142 completed frames then OnSceneWasLoaded Stop disables menuScene at20:54:34.
Repeated later scene loads also Stop. Scene focus PID matched game PID. Dashboard
initially open, later closed; input polling armed after closing. Rendering acceptance
is log evidence only, not proof of correct visuals. User game exited20:55:18.
0473 preserves persistent live menu scene on scene load while releasing stale
Quest input and clearing gameplay camera/diagnostic stage. Other scene-load cleanup
unchanged; immersive scene-load Stop remains. Version0.4.73. Changed source only
SignalisVrTracking.cs; test-menu-scene.ps1 extended. Native reads, mappings, stick
response and graphics resources unchanged. Build and seven focused suites pass:
menu scene, shared getter, native reads, quest policy, actions, recovery, diagnostic.
Headset startup/menu navigation and graphics stability still unverified.
DLL SHA256 273CC95FD481DB0934675D77633F2BE3A1CF3E67F196BF8D7334011717757C84.
No assistant launch or install. User had copied0472.0471 rollback preserved in0473.
Next manual test: copy0473 while game closed, relaunch to main menu only.

2026-09-26 -0473 headset main menu confirmed
After user copied0473 and launched manually without F6/F9/F10, user reports main menu visible in headset. This verifies startup screen appearance for this test; menu navigation/buttons, stereo handoff and sustained graphics stability remain unverified. Next isolated test: left-stick up/down menu selection. No code changes, installation or assistant launch. Stick tuning deferred until input coverage complete.

2026-09-26 -0473 user headset input checks
User confirms main-menu navigation/input works, Settings B returns, Continue A loads room, F6/F9/F10 reaches full stereo, X opens inventory, B closes inventory, Y opens pause menu, A opens repair manual after leaving pause, and left-stick page turning works both directions. Settings still displays keyboard icons despite controller operation: unresolved prompt-display issue. No weapon aim/fire/reload verification yet; stick tuning remains deferred. These are user observations, not automated tests or proof of sustained graphics stability. No code changes or assistant installation/launch. Next isolated check: B closes manual.

2026-09-26 -0474 right-stick horizontal-only candidate
User confirms0473 B closes manual and left-grip sprint toggles correctly. No weapon
available; aim/fire/reload remain unverified. User requests removal of right-stick
up/down, explicitly keeps smooth turning. Settings keyboard prompts remain open.
0474 changes QuestInputPolicy.Update lookY to zero; SignalisVrTracking version.
Smooth horizontal speed, left-stick movement/menu navigation, buttons unchanged.
Build and four suites pass: quest policy, firstperson, native reads, action edges.
Direct policy check at right Y -1/0/+1 verifies lookY0, lookX1, left moveY1.
This is automated evidence only; headset behavior remains unverified.
DLL SHA256 3D87316575DD391B46F3D613E57CCE03432B02CE5F79AB0AC9773FADCB52470F.
Installed remains0473 hash273CC95FD481DB0934675D77633F2BE3A1CF3E67F196BF8D7334011717757C84.
0473 workspace DLL was missing; restored from hash-verified installed0473 as
historical evidence and saved0474/Rollback0473.zip. No game launch/auto-install.

2026-09-26 -0474 title screen missing input;0475 candidate
User screenshot Press Any Key title screen: no Quest input works.0474 log shows
A/buttons1 with focus and input available. Native inspection confirms title calls
CharacterAction.AnyInput/AnyInputUp -> ReInput.ControllerHelper.GetAnyButton/Up,
bypassing existing per-action bridge. Evidence saved0475/evidence.
0475 adds unique-address-validated Harmony postfixes retaining physical originals
and OR-ing guarded Quest held/release states; thread/focus/dashboard checks apply.
Changed QuestFirstPerson, SignalisVrTracking version. Six suites +compiled title
hook/address guards pass; build succeeds. Right vertical remains disabled and
smooth horizontal preserved. Runtime/headset title behavior still unverified.
DLL SHA256 2537A162E69B90C67BBEBC6AEFB9C0F9871897155857D04FDB5E6728586AEDD9.
Installed0474 hash3D87316575DD391B46F3D613E57CCE03432B02CE5F79AB0AC9773FADCB52470F.
No assistant launch/install. Rollback0473 preserved. Settings prompts unresolved;
weapons unavailable. Next user closes/copies, tests A press/release on title.

2026-09-26 -0475 user reports all inputs working
After copying0475, user saw SteamVR dashboard at startup, then explicitly confirmed A gets past title screen into main menu. Latest user report: all inputs are working. Record as user-observed available input success, not weapon verification (no weapon available). Right-stick vertical removal was requested and built; latest broad report does not separately confirm level-view check. Smooth horizontal turning retained. Keyboard prompts documented by screenshots in pause and inventory as well as Settings remain unresolved. Dashboard startup behavior observed; Resume closes dashboard based on subsequent progression, game exit not established. No code changes, assistant launch or installation. Aim/fire/reload and sustained graphics stability remain unverified.

2026-09-26 -0475 right-stick behavior explicitly verified
User confirms right-stick up/down no longer tilts view and left/right still turns smoothly.0475 is the user-verified available-input baseline. Weapon aim/fire/reload remain untested until weapon available. Keyboard prompts in Settings/pause/inventory remain unresolved. No code changes, installation or launch.

2026-09-26 -End of day; user priority roadmap pinned
User stops development for day. Ordered priorities saved outputs/PINNED_ROADMAP.md: comfortable sticks; stable head tracking/world without tilt/wobble;3DoF;6DoF;possible hand tracking/motion controls;possible touching/picking objects;first airlock-key/EVA-suit cutscene and next area. This supersedes prompt-icon-first ordering. Later ideas: mod settings menu, colored optional lasers, manual reload, hands-only body visibility and holsters.0475 working input baseline preserved. Chat already pinned in Codex. GitHub sync in progress; original workspace/game remain readonly. No new gameplay code, installation or launch.

2026-09-26 -End-of-day GitHub synchronization blocked
Found existing private repository https://github.com/stevethetitan92/Signalis-VR from readonly original repository config. Local Git cannot clone: missing remote-https helper. Authenticated browser can read repository; add-file controls and filechooser on upload/main fail despite reload and both supported chooser approaches. No GitHub commit/upload verified; do not claim synchronized. Prepared outputs/GitHubEndOfDay20260926 README, ROADMAP, separate variant log and source-only ZIP (27 snapshots, licenses; no game binaries/raw dumps). Chat already pinned, roadmap prominently saved locally. Work paused for day by user. Resume comfortable sticks after finishing pending GitHub sync; no gameplay work begun.

2026-09-26 -GitHub retry succeeded
User requested retry. Fresh authenticated browser upload worked. Verified GitHub main commit80f52835068fd76493b73763e616a4890c2c8907, Archive0.4.75 input checkpoint and pin short-term VR roadmap. Updated README, ROADMAP.md, separate variant development log, and source-only ZIP containing27 historical snapshots through0475. Older src/docs preserved as history with current status linked from README. No stable release or binary upload. Screenshot saved outputs/GitHubEndOfDay20260926/github-commit-confirmed.png. Prior sync-blocked entry is historical; this entry records resolution. Local post-sync receipt is not included in earlier uploaded log. Development remains paused for day; no game launched or installed.

2026-09-26 -0476 offline stick comfort candidate
User authorizes offline stick work after shelving licensing/public visibility.
Created separate0476; production changes QuestInputPolicy circular movement dead
zone with capped magnitude and independent legacy UI axes; softer cubic-blended
smooth turn at small input, same90deg/sec full stick, vertical off; finite guards
and circular neutral arming. SignalisVrTracking version only. No camera/graphics,
native hooks, button or force compensation changes. Added test-stick-comfort;
updated native routing fixture for UI axes. Build+11suites pass. Geometry sweep
10863assertions+2neutral assertions. Test oracle integer-overload issue corrected.
DLL FB97B8A78F4F88C876A4E06478EEA985230855B918D1067370D4244FD5FB32F7.
ZIP EDE367971BB16D4F4C0D509F71E34A53F8ED9D0179300082B0017BA8DEADBF2A.
Rollback0475 saved; installed0475 verified hash2537A162E69B90C67BBEBC6AEFB9C0F9871897155857D04FDB5E6728586AEDD9 unchanged. Restored missing local0475 DLL from that reference to preserve history.
No assistant game launch/install. No headset test; comfort, actual movement speeds,
UI regressions and graphics stability unverified. Different forward/strafe physics
still need user assessment. Candidate ready for next session; not new baseline.
See0476/INVESTIGATION.md for detailed checks, limits and one-step manual sequence.
No GitHub update performed for this candidate. Licensing remains shelved.

2026-09-27 -0476 user movement test: no perceived analog speed change
User reports copied0476, title A/main-menu inputs work, F6/F9/F10 full stereo works. In headset, gentle versus full left-stick forward feels the same speed; gentle versus full sideways also feels the same. Record perceived fixed movement speed in both paths, not proof that sampled axes are fixed. Circular input geometry software tests do not establish physical movement speed.0476 has not met analog movement comfort goal. Need inspect downstream movement consumers/physics before claiming fix. Next isolated user test: gentle versus full right-stick horizontal turn rate. No code changes, launch or installation this turn.

2026-09-27 -0477 cutscene candidate; priority changed by user
User requests first airlock/EVA cutscene now due43percent weekly budget; stick tuning paused. Reports Waiting in headset. Code review finds OnSceneWasLoaded and invalid-camera guard Stop active stereo.0477 changes SignalisVrTracking only(version included): scene loads and invalid camera in active submitted stereo enter existing questMenuScene live game-screen capture, reset camera/recovery/input, retain native graphics resources. F12/error Stop preserved. Manual F6/F9/F10 required to resume immersive stereo in next gameplay area. Build+6focused suites pass, not graphics proof. Saved current log is not a cutscene reproduction. Blocking scene loads can still interrupt Unity frames; no guarantee of eliminating every Waiting interval. User runtime verification pending. DLL78BC6F41DE52BEA3F18101A3C05820C03502C40293199CD9B673275C4E7D9553. Installed0476 FB97B8A78F4F88C876A4E06478EEA985230855B918D1067370D4244FD5FB32F7 untouched; backup saved. No assistant game launch/install.0475 remains verified input baseline.

2026-09-27 -Cutscene screenshots are0476, not0477 verification
Installed DLL hash remains FB97B8A78F4F88C876A4E06478EEA985230855B918D1067370D4244FD5FB32F7 (0476). User screenshots140632/140642 show snowy cutscene behind central white rectangle with black bars, then Waiting. Log records DIAG stopped at14:06:34. Saved full log0477/evidence/0476-cutscene-waiting.log. Do not attribute this to0477 or call new fix tested. White rectangle origin unresolved. Next user closes game and copies0477 before repeating cutscene.

2026-09-27 -0478 six-button keypad candidate
User reports left stick fails in all four directions on six-button keypad.
Offline native inspection finds EventScreen3DCam and ROT_Cursor use separate
CharacterAction.lastActiveController and Move, beyond InputControl mode bridge.
0478 adds scoped QuestPuzzleInput prefix/finalizer supplying left UI axes (stronger
physical axes preserved) and controller branch, restoring fields even on original
exception. No global CharacterAction.Update/shared getter patch. Two unique native
addresses audited and guarded at runtime. Changed QuestFirstPerson installation,
build.ps1, tracking version; new QuestPuzzleInput and harness test. Retains0477
unverified cutscene change. Build+7 suites pass. Shared-getter initial missing
audit fixture failure preserved; regenerated audit then pass. Checks do not prove
native detour/headset/graphics behavior. See0478/INVESTIGATION.md and evidence.
DLL6F6015BE869A5219FBA04BAA253B4881AD8A269A34221FF0C7D6F4F65A219120.
Installed still0476 FB97B8A78F4F88C876A4E06478EEA985230855B918D1067370D4244FD5FB32F7;
Rollback0476.zip saved. No automatic install or game launch. Manual keypad test
pending; cutscene Waiting/white rectangle not yet verified fixed. No GitHub update.

2026-09-27 -0479 inputs first; cutscene deferred by user
User adds screenshots of terminal, keypad and airlock interaction views where
stick fails, and says F1 does not put them in first person. Unknown whether F1
also fails outside interaction views (user: i dont know). No camera fix claimed.
Inspected readonly CameraPerspectiveSolid F1 handling; it moves Main Camera,
not the separate interaction camera. Saved current0476 log includes foreground
focus loss, but cause of user F1 observation is not proven.
Created0479 using0476 rendering baseline plus0478 scoped puzzle fix.0478 retained
as undelivered history;0477 cutscene changes excluded per new priority. Changed
QuestPuzzleInput(new), QuestFirstPerson hook install, build source list, version.
Build+7focused suites pass; native detour/headset/graphics remain unverified.
DLL38E0F431770D41D39540C99FF243D74ADB37201A94178FE4BDDB7FE6064AC74D.
Installed0476 unchanged; no launch/install. Backup retained; both copy links will
be provided. Next test is directions in keypad, then each reported screen and
A/B, ordinary movement and F1 outside interaction view. Do not claim all inputs
complete before user checks. Weapon actions remain pending weapon availability.

2026-09-27 -0479 user confirms interaction-area input
After reporting copy complete and being asked to test keypad left-stick directions,
user reports "input works in those areas" referring to the previously reported
terminal/keypad/airlock interaction views. Record user-observed interaction input
success; no separate A/B or every direction confirmation beyond that broad report.
F1 outside interaction views remains unresolved/unverified. Next manual step:
exit interaction view, focus game, press F1 once during ordinary room gameplay.
No code change, assistant installation or launch. Cutscene work remains deferred.

2026-09-27 -F1 reported ineffective after restart
User reports pressing F1 does nothing. Latest log saved0479/evidence/0479-f1-report.log.
No FPS Mode Enabled message found; last samples show desktop focus=False while
SteamVR scene ownership remains correct, menuScene=True stage0. Focus was True
earlier; timestamp of F1 press unknown. Original CameraPerspectiveSolid uses
Unity Input.GetKeyDown(F1), unlike focus-independent diagnostic hotkeys. Cannot
conclude focus caused reported press. Next controlled check: click game window
then F1 during ordinary gameplay. No source change/install/launch this turn.

2026-09-27 -F1 works with game window focused
User confirms "that worked" after instruction to click inside desktop SIGNALIS
window and press F1. Record user-observed first-person activation with desktop
focus; no F1 code fix needed for this reproduction. Interaction-area input was
previously user-confirmed on0479. Next check A activates a keypad button and B
exits the puzzle. Complete input coverage and weapon actions remain unverified;
cutscene work still deferred. No code change, installation or launch.

2026-09-27 -0479 keypad A activation confirmed
User answers yes to selecting a keypad button with left stick and pressing A to
activate it. User-observed success; B exit is next separate check. No code change,
installation or game launch. Complete input coverage remains unverified.

2026-09-27 -0479 keypad B exit confirmed
User answers yes: B closes keypad and returns to room. Keypad stick navigation,
A activation and B exit now user-observed working. Next check ordinary movement
and smooth horizontal turning after exiting puzzle. No code change/install/launch.
Cutscene remains deferred; full input coverage and weapon actions not established.

2026-09-27 -0479 movement after puzzle confirmed
User answers yes to left-stick movement in all four directions and right-stick
left/right turning after exiting keypad. User-observed transition back to ordinary
controls works. Next regression check X inventory and B close. No source change,
installation or launch; cutscene deferred and weapon actions remain unverified.

2026-09-27 -0479 inventory open/close confirmed
User confirms X opens inventory and B closes it. User-observed regression check
passes after keypad and movement tests. Next check Y pause menu and B back.
No code change, install or launch. Weapon actions and full coverage unverified.

2026-09-27 -0479 pause menu confirmed
User confirms Y opens pause menu and B returns to game. User-observed regression
check passes. Next check left-grip sprint toggle on/off while moving forward.
No code change, installation or launch. Weapon actions remain untested.

2026-09-27 -0479 sprint toggle confirmed
User confirms left-grip tap toggles sprint on and off while moving forward.
Current user-confirmed0479 checks: interaction-area input, keypad navigation/A/B,
movement and horizontal turning after puzzle, X/B inventory, Y/B pause, sprint.
F1 works with game window focused. Next targeted check terminal selection and
activation (distinct from keypad). Weapon actions remain untested; cutscene work
still deferred. No source change, installation or launch.

2026-09-27 -0479 available-input checkpoint accepted by user
User reports "all inputs work" after terminal selection/activation test prompt.
Accept0479 as user-verified available-input checkpoint, following explicit keypad
navigation/A/B, post-puzzle movement/turning, inventory, pause and sprint checks.
F1 works when desktop game window is focused. Broad report does not establish
weapon aim/fire/reload without weapon; those remain pending. Input prerequisite
for returning to deferred cutscene investigation is satisfied for tested area.
No new build, installation or launch. Preserve0479 as fallback for cutscene work.

2026-09-27 -0480 cutscene work resumed on verified0479 inputs
User agrees to resume cutscene investigation. Built0480 from0479 input sources
with0477 scene-load/invalid-camera live-screen handoff in SignalisVrTracking only
(version included). Baseline tracking equality0479-to0476 verified except version.
All input source preserved. Build+8focused suites pass; headset, cutscene rectangle,
Waiting intervals and next-area behavior unverified. Blocking Unity load can still
interrupt frames. No immersive cutscene claim. See0480/INVESTIGATION.md.
DLL AE82EDFF4A61B7EF0928E3A482D162885E129CEB5EEED412EB4BA12174CAA144.
Installed0479 hash38E0F431770D41D39540C99FF243D74ADB37201A94178FE4BDDB7FE6064AC74D
verified unchanged; restored missing workspace0479 DLL from that reference and
saved Rollback0479.zip. No assistant installation or launch. Next manual copy/test.

2026-09-27 -0480 cutscene traversal succeeds; remaining visual/return issues
User reports cutscene reaches next area, controls fine, but first person/stereo
lost in snow and next area; asks adjust/remove bars and enlarge rose-engine card.
Requests thorough video review. Reviewed full239.68s timeline with2401fps samples,
108additional4fps title-region samples, and full-resolution222s frame. Correct
count wording: 240 frames sampled at1fps. No continuous/audio playback claim.
Findings and timestamps saved work/cutscene0480/REVIEW.md with sheets and log.
Flat panel persists throughout; scene log confirms0480 persistent screen handoff
PEN_Hole then LOV_Reeducation. Automatic stereo restoration not implemented in0480.
Full-width short framing varies during cinematics; face close-up also has separate
short CENTRAL black notches top/bottom. No Waiting panel in reviewed samples, not
proof zero transient Waiting. Rose-engine logo not positively isolated; remains
user report. Original F1 handler excludes PEN_Hole, requiring special handling.
No new build/code/install/launch.0480 preserves progression and working input;
first-person restoration, stereo recovery and cinematic layout still outstanding.

2026-09-27 -0481 consolidated findings and first recovery build
User requests consolidate findings and start build. Saved0481/CONSOLIDATED_FINDINGS.md
with visual vs code vs user evidence, limitations and test sequence.0481 adds guarded
standard-area first-person/stereo recovery through original perspective mod's managed
F1 path, retained textures and pose drain; only after normal play stable0.75s. Stop
clears intent. Snow PEN_Hole remains excluded with camera diagnostics; no snow fix
claimed. Temporarily hides only named Effects Camera/Bars/TopBar/BottomBar renderers
during flat capture, restoring afterward/stop/scene change. Logo scaling and baked
movie bars unresolved. EnhancedResolution scaling of bars is code evidence, not
runtime causal proof. UnityPy asset inspection unavailable; no assets modified.
Changed SceneRecovery(new), tracking lifecycle/version, QuestFirstPerson install,
QuestMenuScene finally, build source list; added scene gate harness. Input policy,
controllers/native reads/puzzle mapping/rendering/book/dialogue hash-equal0480.
Build+9suites pass; no native/headset/graphics validation. DLL
C8CBAF3554892DA4551635D20D80FCA287D5C6C2D53BA23F83592C0D3019E0E9.
Installed0480 remains AE82EDFF4A61B7EF0928E3A482D162885E129CEB5EEED412EB4BA12174CAA144;
restored missing workspace0480 from verified installed copy, backup zip preserved.
No assistant launch/install or GitHub update. Partial candidate: standard-area
recovery and bar artwork; snow/rose-engine remain subsequent work.

2026-09-27 -Recurring startup SteamVR dashboard reported
User reports dashboard appears whenever starting game, supplies143059 screenshot
showing SIGNALIS Resume Game/VR Controller Bindings/VR Video Settings/Exit Game.
Screenshot predates0481; do not attribute appearance to new recovery build.
Record recurring startup issue distinct from cutscene Waiting or game pause menu.
Cause not established from screenshot. Next immediate step Resume Game to dismiss
and continue0481 test. No code change, automatic install, dashboard action or launch.

2026-09-27 -0481 video reviewed;0482 narrow cinematic bar matcher correction
Reviewed152644-0 recording with 3s whole-clip and1s first50s extracted frames.
Short central bars persist12-35s, central edge notches36-46s; snow remains flat;
clip reaches red shaft but not next standard area. Runtime PEN_Hole only, no
recovery messages; normal-area recovery therefore unverified. Logo not identified.
Found exact camera-name mismatch: runtime Diag/Effects Camera versus0481 Effects
Camera-only matcher.0482 accepts both exact names and logs matched renderers once.
Changed SceneRecovery.cs and tracking version only; other production code unchanged.
Build, scene-recovery harness, menu-scene harness pass; graphics/headset unverified.
0482 SHA256 51C815A2197DCC9EDBB5E4C78586B83DE028749C686C05BD3487CE8F20D352A5.
Installed0481 hash verified C8CBAF3554892DA4551635D20D80FCA287D5C6C2D53BA23F83592C0D3019E0E9;
restored missing local0481 DLL from verified installed copy. No install/launch.
Evidence work/cutscene0481; report0482/REVIEW.md. Snow stereo/logo remain unresolved.

2026-09-27 -GitHub updated on user request
Published main commit3b20e12fe6fb8de379560e687479643d26944ca1 via authenticated
GitHub browser upload; repository page confirms commit. Updated README, ROADMAP,
separate variant log, STATUS-0482, and source archive covering0476-0482. Archives
contain source/scripts/licenses, no game assemblies/assets or raw runtime dumps.
User reported copying0482; headset verification remains pending. Prior checkpoints
preserved; license/visibility unchanged. No game launch/install. Local upload staging
and confirmation screenshot: work/github-0482. No new build;0482 hash unchanged.

2026-09-27 -0482 headset recording: cinematic bar correction FAILED
User provided VirtualDesktop.Android-20260927-172201-0.mp4 after guided test.
Installed DLL verified0482 SHA25651C815A2197DCC9EDBB5E4C78586B83DE028749C686C05BD3487CE8F20D352A5.
Reviewed one-second extracted frames across114.89s recording (not continuous/audio).
27-47s: short floating horizontal bars persist.48-49s: small central white panel.
50-64s: central top/bottom notches persist (vary during face closeup).65s onward
snow traversal displayed flat; clip reaches red shaft, not next normal area.
No CINEMATIC BAR match messages in copied runtime log. Exact-name correction alone
insufficient; next investigation must identify actual bar component/hierarchy rather
than assume Renderer match. SNOW CAMERA messages show recovery intent active;
normal-area auto recovery still untested by this clip. No Waiting seen in sampled
frames; not proof of zero transient events. No new code/build/install/launch.
Evidence: work/cutscene0482/runtime.log, video-info.txt, timeline01-05.

2026-09-27 -0483 targets actual BlackBars UI references
Offline Assembly-CSharp metadata: BlackBars.barTop/barBottom are RectTransform.
Renderer-only scan misses UI Graphic components.0483 adds loaded-scene BlackBars
reference discovery, per-graphic match logging, temporary enabled-Graphic hiding
and restoration via existing capture finally/stop/scene-change mechanism. No native
hooks/container disable/game assets altered. Actual graphic matches not yet observed.
Changed SceneRecovery.cs and tracking version only; all other production C# hash-equal0482.
Build +scene-recovery +menu-scene harnesses PASS; no graphics/headset validation.
DLL SHA2565331FADCA51FE28CBDBA2832557D21434A9A000323150B3DF5FB1EBE32120EF3.
Candidate outputs/SignalisVrCinematicBars0483; evidence scripts/logs stored there.
0482 baseline retained/restored if missing from verified installed copy. No install
or launch. Snow/logo unresolved; normal-area recovery unverified; GitHub unchanged.

2026-09-27 -Red shaft first-person failure clarified
User172832 screenshot shows red shaft in third person; reports first person does
not work. Latest runtime scene remains PEN_Hole (loaded17:22:51.813), no later scene
load. Original perspective mod OnUpdate explicitly excludes PEN_Hole; current
recovery does too. Thus unresolved snow-scene coverage includes red shaft, not
just outdoor walking.0483 changes bars only and does not address this limitation.
No new build/install/launch. Do not ask repeated F1 tests here; unsupported scene
needs separate camera work. Normal-area recovery test occurs later.

2026-09-27 -0483 headset recording: partial bar improvement
User supplied173308-0.mp4 duration105.43s. Reviewed one-second extracted frames
throughout (not continuous playback/audio). Installed0483 verified SHA256
5331FADCA51FE28CBDBA2832557D21434A9A000323150B3DF5FB1EBE32120EF3.
14-34s short floating central bars persist;35-37s central white panel remains.
38s onward snow arrival lacks prior central edge notches; face closeup still has
full-width cinematic framing. Snow/redshaft remain flat as known. No next normal
area shown. Runtime at17:33:46.399 loadsPEN_Hole; six CINEMATIC BAR UI matches
at17:33:46.577-.579 confirm discovery of three top/bottom pairs. Thus0483 improves
post-scene-load masks but does not fix earlier ship shot. Need inspect pre-handoff
capture path/questMenuScene guard and earlier bars; do not declare whole fix done.
Evidence work/cutscene0483 runtime.log, video-info.txt, timeline01-05. No code/build,
install or launch by assistant; no general stability claim or GitHub update.

2026-09-27 -0484 extends bar handling to early cinematic capture
0483 runtime shows camera-suspended capture before PEN_Hole; prior bar gate
required questMenuScene.0484 permits paused-camera or native cutscene states too,
while immersive requested/active/stereo; outer PuzzleFrames finally restores on
success, skip and error. Changed SceneRecovery.cs, PuzzleScreen.cs and tracking
version. Build +menu-scene +scene-recovery harness PASS; no headset verification.
SHA256 C186DD0DAEB848691A0D3F8F5E7FB111E7E7A39D0921D842B3AB406169A54FF3.
No install/launch;0483 remains installed reference. User reiterates stereo lost
on snow entry; confirmed unresolved with redshaft.0484 is bar-only; next camera
work must handle PEN_Hole rather than claim standard-area recovery covers snow.
Small white panel/logo unresolved. GitHub unchanged.

2026-09-27 -User current-issues report for GitHub
User requests recording: black bars in first cutscenes; stereo lost entering snow;
function buttons fail at red bottom of stairs; function buttons work in bathroom
and allow proceeding in first person/stereo; many interactable objects lack markers.
Bathroom is user-reported manual recovery, NOT proof automatic recovery works.
Missing markers are newly reported beyond prior starting-area coverage; specific
objects not enumerated. Door/ladder suppression remains intentional.0484 headset
result/install not confirmed. Preparing CURRENT_ISSUES, README, ROADMAP and variant
log update on GitHub; no source/build changes, game launch or install.

GitHub publication confirmed: main commit185213a2232feeca7551e3d6019c71a898469211. Updated CURRENT_ISSUES.md, README.md, ROADMAP.md and variant log. Evidence work/github-current-issues/published.png. No new build/install/launch; no visibility/license change.

2026-09-27 - GPLv3 chosen by user
Preparing GPLv3 license and contribution guide on GitHub. Scope: project-owned code, including archived checkpoints; third-party notices retained. README/ROADMAP clarify license and free mod policy. No code/build/hash changes; no install or launch. Repository visibility not yet changed.
