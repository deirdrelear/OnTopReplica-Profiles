# AGENTS.md

## Project
OnTopReplica-Profiles is a read-only fork of OnTopReplica Refactor. It adds reusable profiles, role-based multi-client layouts, visual profile editing, fixed-size draggable replicas, and persistent per-character placement overrides.

## Hard safety boundary
This repository MUST remain read-only with respect to source applications.

Forbidden:
- DLL injection.
- DirectX/OpenGL/Vulkan hooks into source processes.
- ReadProcessMemory / WriteProcessMemory.
- SendInput targeted at game clients.
- PostMessage/SendMessage for synthetic mouse or keyboard input to source windows.
- Packet sniffing or protocol interception.
- Any feature whose purpose is to automate gameplay actions.

Allowed:
- Window enumeration.
- DWM thumbnails.
- Reading window title/class/geometry.
- Creating and moving this application's own replica windows.
- Visual region selection in this application's own UI.
- Saving layouts/profiles/placement metadata.

If a requested change conflicts with this boundary, stop and document the conflict instead of implementing it.

## Branches
- master: untouched upstream reference.
- profiles-v1: first working profile manager.
- profiles-v2-editor: current development branch.

Do not merge development work into master unless explicitly requested by the repository owner.

## Build
Solution: src/OnTopReplica.sln
Target: .NET Framework 4.8, Any CPU.
CI: .github/workflows/build-profiles.yml

Run a Windows CI build after material source changes. Do not claim a change compiles until CI succeeds.

## Architecture
Existing OnTopReplica DWM rendering is the source of truth. Avoid replacing it.

ProfileManagement:
- ProfileModels.cs: persisted profile schema.
- ProfileRepository.cs: JSON persistence and validation.
- WindowDiscovery.cs: top-level source window discovery.
- LayoutEngine.cs: default grid placement.
- ReplicaProcessManager.cs: launches/reconciles managed replicas and persists placement overrides.
- ProfileManagerForm.cs: runtime profile launcher/status UI.
- ProfileEditorForm.cs: visual editor for roles, bindings and replica properties.
- RegionPickerForm.cs: visual DWM region picker.

## Managed replicas
Managed replicas:
- are separate OnTopReplica processes;
- use --managedReplica;
- have no global hotkeys;
- do not persist normal shared OTR settings;
- have fixed size set by the profile;
- may be dragged by the user when clickThrough=false;
- must never forward input to the source window.

A manually moved position is persisted as a CharacterBinding placement override keyed by replica name. If no override exists, LayoutEngine computes the position.

## Profile model
Profile -> Roles + Bindings.
Role -> zero or more ReplicaDefinition objects.
Binding -> Character + Role + Slot + optional per-replica placement overrides.
Replica -> Source region + Output size + Layout + appearance flags.

Keep JSON backward compatible where practical. Normalize missing collections on load.

## UI expectations
The editor should let a non-developer configure profiles without hand-editing JSON:
- detect source windows;
- assign characters to roles and slots;
- create/remove roles;
- create/remove replicas;
- visually pick source regions;
- edit output size and default layout;
- save profile.

Do not make mouse-resizing of managed replicas part of the workflow. Output size is profile-controlled.

## Change discipline
Prefer small commits with one purpose.
Do not rewrite upstream rendering code unless required.
Preserve existing v1 behavior unless a v2 change explicitly supersedes it.
Update docs when schema or CLI changes.
