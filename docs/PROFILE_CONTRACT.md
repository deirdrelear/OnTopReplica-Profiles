# OnTopReplica-Profiles Contract

## 1. Purpose
Provide reusable, read-only visual replicas of selected regions from many top-level windows, with role-based layouts and persistent configuration.

## 2. Read-only contract
The application observes source windows through Windows/DWM and may inspect ordinary window metadata. It never injects code, reads source-process memory, intercepts graphics calls inside the source process, or sends synthetic input to the source window.

This is an architectural invariant, not a preference.

## 3. Runtime contract
A profile is selected in Profile Manager and applied.

For each enabled character binding:
1. Find exactly one matching visible source window.
2. Resolve its role.
3. For every replica in the role, compute source region and fixed output size.
4. Use a saved placement override if one exists; otherwise compute the default grid position.
5. Launch one managed OnTopReplica worker per replica.
6. Reconcile workers periodically.
7. If the source window restarts, replace the affected worker.
8. If the user drags a worker, persist the new position without changing its size.

If zero windows match, status is "Window not found".
If multiple windows match, status is "AMBIGUOUS" and no guess is made.

## 4. Profile schema contract
ProfileDefinition:
- name: unique human-readable profile name.
- windowTitleFilter: coarse source-window filter.
- roles: list of RoleDefinition.
- bindings: list of CharacterBinding.

RoleDefinition:
- name.
- replicas.

CharacterBinding:
- character.
- windowTitleContains.
- role.
- slot >= 1.
- enabled.
- placements: optional list of { replica, x, y }.

ReplicaDefinition:
- name.
- source: x, y, width, height in source-window pixels.
- output: fixed width/height of the OTR replica.
- layout: default monitor/grid placement.
- clickThrough.
- borderless.
- opacity.

## 5. Placement contract
The profile controls replica size.
The user may drag a managed replica when clickThrough=false.
Manual movement creates or updates a placement override.
A placement override affects position only, never source region or output size.
Changing a role's default layout must not silently delete existing overrides.

## 6. Editor contract
The visual editor must support:
- create/edit profile;
- detect source windows;
- bind detected characters to roles;
- edit slots;
- create/remove roles;
- create/remove replicas;
- visually pick source region from a live DWM thumbnail;
- edit fixed output size and default layout;
- save valid JSON.

Hand-editing JSON must remain supported but must not be required for normal configuration.

## 7. Backward compatibility
profiles-v1 JSON without placements must continue to load.
Missing placement lists are normalized to empty lists.

## 8. Build contract
The project must build as Release / Any CPU targeting .NET Framework 4.8.
GitHub Actions on Windows is the canonical compilation check.

## 9. Non-goals
- game automation;
- input broadcasting;
- input forwarding;
- OCR/computer vision;
- process injection;
- anti-cheat evasion;
- source-process introspection.
