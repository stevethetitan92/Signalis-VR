# 0.7.166 launcher source

These are source/helper files, not a complete mod installation. Use the full/update ZIP from the [tester release](https://github.com/stevethetitan92/Signalis-VR/releases/tag/v0.7.166-launchers.1). Do not install by downloading GitHub's automatic Source code archive.

The runtime-changes folder contains the five compiled files changed from the preserved 0.7.163 baseline. It is not a standalone project. The 0.7.166 review ZIP contains the current complete logic snapshot with the same deliberate asset/proprietary-assembly omissions as earlier review archives; it is available with the tester release.

Launchers select independent providers. SteamVR does not require Virtual Desktop; VD uses VDXR through OpenComposite and does not require SteamVR. After failed discovery and a failed pasted path, diagnostics check launcher files, paths, mod dependencies, native bridge/provider loading and exports, and the selected connection. Reports are saved under Logs. Nothing is installed or repaired automatically by these checks.

Offline checks passed. No game/headset session was initialized during verification; both connection paths still need real headset testing.

