# Stable releases and automatic updates

No version is currently approved for the stable channel. 0.4.2 is a limited tested baseline; 0.4.3 is an untested checkpoint. Neither should be advertised as broadly stable.

## Release policy

GitHub Releases are reserved for versions that pass the testing gate in TESTING.md. Development candidates remain on branches or in private local test packages. Every eventual release must contain a complete compatible file set, checksums, required dependency versions, installation/rollback steps, actual test results, and known limitations. Never redistribute the game or third-party mods without their applicable permission.

## Planned update behavior — not yet implemented

The intended updater will check only this project's published stable channel. It must ignore drafts and prereleases, require explicit owner approval before a build enters the channel, verify package integrity and compatibility, and preserve the previous installation for rollback. Installation must happen with SIGNALIS closed. Updates should be opt-in; failures must leave the existing mod usable. No credentials should be embedded in a distributed mod.

The channel manifest in `updates/channel.json` is deliberately disabled. It is a planning artifact, not a working updater. Implementing and testing the updater is deferred until stereo stability and portable native-library loading are established.
