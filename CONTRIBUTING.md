# Contributing to Signalis VR

Help with fixes, documentation, reproducible bug reports and headset testing is welcome. Start with CURRENT_ISSUES.md and ROADMAP.md. Open an issue before a substantial change so work can be coordinated; small fixes can go directly into a pull request.

## Source and building

The root src directory is historical. The September 27 source archive contains checkpoints 0.4.76 through 0.4.82. Extract a checkpoint into your own working folder; 0.4.79 is the user-verified input baseline and 0.4.82 is a later experimental candidate. Local 0.4.84 work is described in the development log but is not in those archives yet. Do not assume the most recent candidate is stable.

Building requires Windows, a legitimate local SIGNALIS installation with the expected MelonLoader references, and PowerShell with Microsoft.CodeAnalysis.dll and Microsoft.CodeAnalysis.CSharp.dll in PSHOME. Inspect the chosen build.ps1 for its exact references. Run it from PowerShell with your own game path:

```powershell
./build.ps1 -GamePath 'D:/SteamLibrary/steamapps/common/SIGNALIS'
```

The build creates SignalisVrTracking.dll in that checkpoint folder. It does not install it. Keep a backup of your working mod and close the game before manually replacing files. Native bridge source and its tooling are in the historical src tree; this is not yet a one-command complete mod package. Please report missing prerequisites rather than redistributing game assemblies.

## Pull requests and testing

Fork the repository, create a branch, make one focused change and open a pull request. Identify the checkpoint you started from and include source changes as reviewable files or a patch, not only a compiled DLL. Explain the problem, resulting behavior, checks performed and known limitations. Preserve earlier checkpoints.

Run the relevant test-*.ps1 scripts supplied with your checkpoint. Separate compilation and simulated checks from actual in-game/headset testing. State headset, VR runtime, mod version, reproduction steps and whether first person/stereo were active. Do not mark graphics stability as verified based only on compilation. Remove private details from logs before attaching them.

Priorities include early cutscene bars, stereo loss in snow, function-key failures at the red stair bottom, and missing interactable-object markers. Preserve working controls, puzzles, inventory, books and dialogue. Door/ladder markers are intentionally hidden in this variant; their interactions must keep working.

## License

Project-owned code is GPL-3.0-only. Submit only work you have the right to contribute under that license, and retain third-party attribution and license notices. Do not upload game assets, proprietary assemblies, credentials or unrelated personal files. The mod remains free; financial support is optional.
