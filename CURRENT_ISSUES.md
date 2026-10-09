# Current issues — October 9, 2026

Current documented candidate: 0.7.161. User reports the complete fresh install worked. Keep user observations separate from automated validation and unverified headset behavior.

- **Movement/game stutter:** remains open. Desktop movement was reported smoother than headset movement. The movement rewrite was rolled back in 0.7.153. Same-session logs measured frame stalls and expensive rendering preparation; 0.7.159 reduces repeated enemy-room lookups, without proving all stutter fixed.
- **Computer presentation:** user confirmed functionality after 0.7.158. Full computer/floppy-drive view was added in 0.7.160; missing native headset dot/hover icon was addressed in 0.7.161. Both-eye alignment, disk selection/use/cancel, transition behavior and remaining small room-image panel require verification.
- **Enemy textures/graphics:** user reports unresolved appearance problems. Room activation, texture readiness, native materials, animations, death/burn transitions and visibility restoration need actual runtime checks.
- **Map labels and POIs:** user reported inconsistent text sizing and misplaced save TV/storage box. Nowhere minimap previously confirmed working; current cross-scene sizing/alignment/refresh and POI placement still need verification.
- **Brief all-guns aim/fire/reload lockout:** reported; trigger unknown. HE model/hand/grip switching was later user-confirmed working. Do not mark the separate lockout resolved from that report.
- **Autoinjector:** inventory space intentionally left alone per latest user instruction. Verify the carried 10%-health trigger/restoration to 50%, consumption and lethal damage timing separately.
- **Reflections and camera transitions:** both-eye perspective, reflected player/enemies/items, freshness, clipping, flat/stereo recovery and accumulated rendering cost need headset verification. Older snow/cutscene/marker reports remain historical evidence until explicitly retested.

For a useful report, include version, scene, exact steps, headset/runtime, whether desktop differs, and a recording or redacted relevant log. Compilation and simulated checks alone do not establish graphics stability or performance.
