# Requirements and limitations

## Current development setup

1. A purchased Steam copy of SIGNALIS on Windows, using the observed Unity 2020.3.36f1 IL2CPP build.
2. MelonLoader 0.5.7 and the game's generated assemblies. Compatibility with newer loader versions is unverified.
3. Camera Perspective Change 1.6.4 (`CameraPerspectiveSolid.dll`) with first-person mode available. Obtain the original mod from its author on Nexus Mods; it is not redistributed here.
4. SteamVR running with a connected PC VR headset. Tests use Meta Quest 3S through Virtual Desktop and its PC Streamer.
5. Matching managed VR mod, native render bridge, and OpenVR runtime library.

ReShade is not used. FPv2 1.1, EnhancedResolution, item_capacity_9000, flashlight, and a VR probe were present in the development installation. Their presence does not prove each is required; minimal-dependency testing remains outstanding. Tests used an NVIDIA RTX 5070 Ti; other GPUs remain unverified.

## Building

Source starts from 0.4.2. Managed compilation uses PowerShell with Microsoft.CodeAnalysis assemblies available in `$PSHOME`, local .NET Framework references, and MelonLoader-generated game assemblies. Native compilation requires compatible Visual Studio C++ build tools and a Windows SDK; see `src/build-native.ps1`. Run the managed build with `-GamePath` pointing to your own installation.

**Release blocker:** the current managed build embeds the selected installation's absolute native-library paths. A DLL built for one location is not yet a portable general-purpose release. Runtime library discovery must be corrected and verified before distribution. The repository contains no game assemblies or compiled game files. OpenVR DLL acquisition and version pinning also need a reproducible release process.
