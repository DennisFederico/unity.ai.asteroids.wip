# Task CAM-003: Refactor Camera Configuration Authoring and Baking

## Section 1: Objective & Scope
Implement a reactive Hybrid ECS camera configuration where settings defined in a Managed MonoBehaviour (`CameraConfigurationAuthoring`) are baked into an Entity with a `CameraConfigurationData` component. This ensures that any updates to the Transform or properties on the GameObject in the Scene are reflected in the ECS world via baking dependencies during editor time or through a runtime bridge.

**Scope:**
- Modify existing `CameraConfigurationAuthoring.cs`.
- Implement/Refine `CameraConfigurationData.cs` (ECS Component).
- Ensure correct use of `BakingDependency` for the Main Camera's transform to enable reactivity when values change in the Editor.

## Section 2: Type & API Contracts

### Components & Data
| Name | Namespace | Type | Purpose |
| :--- | :--- | :--- | :--- |
| [`CameraConfigurationData`](Assets/Scripts/Components/CameraConfigurationData.cs:line) | `Asteroids.Runtime.Components` | `IComponentData` | Stores target sensitivity, smoothing, and zoom levels. |

### Visual & Scene Rigging Contract
- **Target GameObject**: The "Main Camera" object within the scene hierarchy.
- **Requirement**: The entity created by this authoring must be tagged with `IsCameraConfigTag` to allow systems to identify it easily.
- **Visual Mode**: Pure Data / Non-Visual (The data drives a managed Camera, but lives as an Entity).

### Implementation Details
- **Namespace**: `Asteroids.Runtime.Authoring`
- **Authoring Class**: `CameraConfigurationAuthoring : MonoBehaviour`
- **Baker**: `CameraConfigurationBaker : Baker<CameraConfigurationAuthoring>`

```csharp
// Required logic snippet for Baker
AddComponent(entity, new CameraConfigurationData { ... });
AddSingleton(entity); // Or add IsCameraConfigTag depending on design choice
```

## Section 3: Express OKF References & Architectural Directives

- **Concept ID Reference**: N/A (Direct implementation based on DOTS 1.4+ patterns).
- **Architectural Directive**: Use `TransformAccessArray`-like dependency tracking via `GetEntityQuery` isn't needed here; instead, ensure the Baker captures the Transform reference correctly using `GetEntity` and link it to the concept of "Reactive Hybrid Bridge".
- **Mandatory Hard Constraint**: Do NOT include `UnityEngine.Camera` inside the `CameraConfigurationData` struct. Only store primitive types (`float`, `bool`).
- **Anti-Pattern Warning**: Avoid adding components directly to the Main Camera GameObject via `AddComponent`. Instead, bake data onto a dedicated proxy entity or handle the mapping in a system.

## Section 4: Developer Definition of Done
- [ ] Code compiles with no errors via Unity MCP.
- [ ] No Burst compiler warnings regarding `Managed Object` access in Systems.
- [ ] Verification complete: Modifying the `CameraConfigurationAuthoring` inspector in Unity results in updated Entity data after baking.
- [ ] DO NOT interact with the Unity Editor directly; always use the MCP (`unity_get_compilation_errors`).

## Section 5: Reviewer Runtime & Style Checklist
- [x] Verify `CameraConfigurationData` uses only unmanaged types.
- [x] Check that `IsCameraConfigTag` is applied to the resulting entity.
- [x] Confirm the baker correctly maps Inspector fields to the component.

## Actionable Checklist
- [ ] Implement `Assets/Scripts/Components/CameraConfigurationData.cs`.
- [ ] Implement/Update `Assets/Scripts/Authoring/CameraConfigurationAuthoring.cs`.
- [ ] Verify compilation and baking behavior.
