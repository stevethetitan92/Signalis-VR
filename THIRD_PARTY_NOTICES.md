# Third-party notices

Project-owned mod code is GPL-3.0-only; see LICENSE. External software retains its own notices and licenses. This does not license SIGNALIS, game assets or proprietary assemblies.

OpenVR bindings/native headers retain Valve's license. MelonLoader, BepInEx, the inventory plugin, FPv2, Camera Perspective Solid, SteamVR and Virtual Desktop retain their respective licenses and terms.

## OpenComposite in 0.7.166

The optional Virtual Desktop path bundles the unmodified official OpenComposite x64 OpenXR-branch runtime, version **1.0.1539**, commit `a27e7e6a64bdcd1eff6b7fba1ea2ea34bcf1273d`, under **GPLv3**.

- [Upstream source and documentation](https://gitlab.com/znixian/OpenOVR)
- [Exact source revision](https://gitlab.com/znixian/OpenOVR/-/tree/a27e7e6a64bdcd1eff6b7fba1ea2ea34bcf1273d)
- Binary SHA256: `827AD85F3606A4DC4A8F5561A8CA69E4C6C1B5D2B9CD3315A461B9270B08242C`

The full/update ZIPs include its license texts, exact source archive and matching GLM, OpenXR-SDK and libunwind submodule archives in `Sources`. Follow the upstream build instructions and unpack each matching submodule into its corresponding `libs` folder. OpenComposite translates the existing OpenVR calls to the installed OpenXR runtime; the SteamVR launcher does not require it.

Public code-review snapshots deliberately omit embedded model/texture/audio/artwork payloads and generated asset/data sources. Their manifests list those omissions. They are incomplete review snapshots rather than standalone build/install packages. Historical notices remain applicable to their historical snapshots.

