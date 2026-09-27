# Pinned short-term roadmap

Updated September 27, 2026. The user reprioritized input completeness, then the airlock/EVA cutscene and progression. Stick comfort and head-tracking work are deferred until this transition works reliably.

## Immediate work

See [current issues](CURRENT_ISSUES.md) for the latest user report.

1. Finish fixing black bars in the first cutscenes; 0.4.84 is awaiting headset verification.
2. Restore first person/stereo through snow entry and the red bottom of the stairs; investigate function buttons failing there.
3. Preserve the bathroom recovery point, where the user reports function buttons work and first person/stereo can be re-enabled. Automatic recovery is not verified.
4. Identify and restore missing markers on interactable objects beyond the previously tested starting area. Keep doors and ladders intentionally unmarked.
5. Preserve working controller input; weapon controls still await a weapon test.

0.4.82 did not remove the bars. 0.4.83 partially improved later masks, while early floating bars remained. 0.4.84 extends the existing fix to the earlier capture path; automated checks pass but no headset result is available. Snow and shaft rendering remain unresolved. The [variant log](SignalisVrNoMarkersDevelopmentLog.md) separates user observations, video/log evidence and automated checks.
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

