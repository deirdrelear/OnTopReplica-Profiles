# Changelog

## profiles-v2-editor

- Added visual profile editor for roles, character bindings, slots and replica definitions.
- Added live DWM source-region picker.
- Added fixed-size, draggable managed replica windows.
- Added persistent per-character/per-replica manual position overrides.
- Added New profile / Edit profile actions to Profile Manager.
- Added AGENTS.md, profile contract and Russian technical specification.
- Kept read-only boundary: no input forwarding or source-process interaction.


## profiles-v1

- Added JSON profiles with roles, character bindings and persistent slots.
- Added multi-replica layouts per role.
- Added profile manager UI and `--profiles` / `--applyProfile=<name>` modes.
- Added automatic recovery of replicas after source client restarts.
- Added managed replica mode that disables duplicate global hotkeys and shared settings persistence.
- Removed the CLI path for click forwarding.
- Disabled legacy click forwarding in the UI and removed its mouse-injection implementation from the read-only build.
- Added Mining and Rampant example profiles.
