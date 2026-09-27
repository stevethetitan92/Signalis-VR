# 0481 recording review / 0482 candidate
Recording: VirtualDesktop.Android-20260927-152644-0.mp4 (121.55 seconds).
Reviewed extracted frames: every 3 seconds over entire clip; every second over first 50 seconds. Not continuous playback/audio review.
- 0-8s EVA departure, displayed on flat screen.
- Approximately 12-33s short floating black horizontal bars over snow/ship picture.
- Approximately 33-35s small central white panel between bars.
- 36-46s separate short central notches at top/bottom of larger picture.
- 48-90s snow traversal stays flat; 90-105s hole approach; 111-119s red shaft. No later normal area in clip.
- Headset system menu visible during recording. No Waiting panel identified in sampled frames; brief events cannot be excluded.
- Rose-engine logo not positively identified in these samples; scaling remains unresolved.
Runtime evidence copied to work/cutscene0481/runtime.log. PEN_Hole entered; no LOV_Reeducation scene or SCENE RECOVERY message. This does not test next-normal-area recovery.

0482 changes relative to 0481: SceneRecovery.cs accepts literal camera name Diag/Effects Camera as well as Effects Camera for exact Bars/TopBar or BottomBar renderer ancestry; logs each matched renderer once. SignalisVrTracking.cs version 0.4.82. All other production source files identical. Existing temporary hide/restore mechanism retained.
Reason: runtime Camera diagnostics show literal Diag/Effects Camera; 0481 matcher accepted only Effects Camera. This is a verified code mismatch, not proof that it accounts for every cinematic artifact.
Automated: compilation, scene eligibility harness, menu capture/focus guard harness PASS. No headset/graphics stability claim. Snow stereo and logo sizing NOT fixed.
DLL SHA256: 51C815A2197DCC9EDBB5E4C78586B83DE028749C686C05BD3487CE8F20D352A5.
Installed baseline verified 0481: C8CBAF3554892DA4551635D20D80FCA287D5C6C2D53BA23F83592C0D3019E0E9. Missing local 0481 DLL restored from this verified read-only installed copy. No automatic install or game launch.
Manual test: close game, COPY candidate DLL into Mods, relaunch manually and repeat departure. Check whether short central bars/notches disappear. Continue through next normal area without hotkeys to test inherited recovery separately.
