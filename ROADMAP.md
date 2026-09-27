# Pinned short-term roadmap

Updated September 27, 2026. The user reprioritized input completeness, then the airlock/EVA cutscene and progression. Stick comfort and head-tracking work are deferred until this transition works reliably.

## Immediate work

1. Preserve the user-verified 0.4.79 input baseline, including keypad and terminal navigation. Weapon controls remain untested without a weapon.
2. Verify 0.4.82 cinematic-bar correction in the headset. User reports copying the candidate; no headset result yet.
3. Verify automatic first-person/stereo recovery after reaching the next normal gameplay area. The latest clip reaches the red shaft, not that area.
4. Implement snow first person/stereo and resolve small logo/presentation issues. Investigate recurring startup SteamVR dashboard.

0.4.80 progressed through the cutscene and next area with controls intact, but lost immersive rendering. 0.4.81 added guarded standard-area recovery; its snow exclusion remains intentional and unresolved. 0.4.82 fixes a bar-renderer camera-name mismatch. Compilation and harness checks pass; runtime graphics behavior remains unverified. See [findings](STATUS-0482.md) and the [variant log](SignalisVrNoMarkersDevelopmentLog.md).

## Retained longer-term order

1. Comfortable stick movement.
2. Better head tracking: eliminate unwanted tilt/wobble and keep the stereo world stationary as the viewpoint moves.
3. 3DoF rotational head tracking.
4. 6DoF rotational and positional head tracking.
5. Possible hand tracking / motion controls; assess feasibility.
6. Possible picking up / touching interactable objects; assess feasibility.
7. Continue validating the first cutscene and subsequent areas.

## Later possibilities

Controller-operated mod menu; optional gun lasers with selectable colors; manual reload with button-reload option; hands-only body visibility; holsters. These remain plans, not completed features.

No automatic game launch or installation. Provide build and Mods-folder links and one test step at a time. License choice remains deferred.
