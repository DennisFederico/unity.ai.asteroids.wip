# Task CAM-004: Update Camera Follow System Math

## Section 1: Objective & Scope
Implement the mathematical logic for the reactive camera follow system. This involves updating the `CameraFollowBridgeSystem` to smoothly interpolate the Managed Main Camera's transform toward the position of the Player Ship entity, utilizing the settings defined in `CameraConfigurationData`.

**Scope:**
- Refine math in `Assets/Scripts/Systems/CameraFollowBridgeSystem.cs`.
- Ensure smooth damping/interpolation between frames.
- Integrate zoom functionality based on distance scaling.

## Section 2: Type & API Contracts

### Components & Data
| Name | Namespace | Type | Purpose |
| :--- | :--- | :--- | :--- |
| [`PlayerShip`](Assets/Prefabs/Vehicles/SM_Ship_Fighter_02.prefab:line) | `Asteroids.Runtime.Entities` | `LocalTransform` | The target object being followed. |
| [`CameraConfigurationData`](Assets/Scripts/Components/CameraConfigurationData.cs:line) | `Asteroids.Runtime.Components` | `IComponentData` | Provides sensitivity and smoothing parameters. |

### Visual & Scene Rigging Contract
- **Target Managed Component**: `UnityEngine.Camera.transform`.
- **Requirement**: The system must run in the `LastSimulationSystemGroup` to ensure player movement has been processed before the camera updates its pose.
- **Visual Mode**: Hybrid Bridge (ECS drives managed `Transform`).

### Implementation Details
- **Namespace**: `Asteroids.Runtime.Systems`
- **Class**: `CameraFollowBridgeSystem : SystemBase` (using `SystemBase` because it requires accessing managed Transforms).
- **Core Logic Snippet**:
```csharp
// Pseudo-code for interpolation
float3 targetPos = playerPosition + offset; // offset derived from rotation/zoom
cameraTransform.position = math.lerp(cameraTransform.position, targetPos, deltaTime * smoothing);
cameraTransform.rotation = math.slerp(cameraTransform.rotation, targetRotation, deltaTime * smoothing);
```

## Section 3: Express OKF References & Architectural Directives

- **Concept ID Reference**: N/A.
- **Architectural Directive**: Use a "Hybrid Bridge" pattern. Access the managed `Transform` via a singleton or reference stored during baking. Since we are moving a managed GameObject, use `SystemBase` rather than `ISystem` for this specific system to allow safe access to non-unmanaged components like `Camera`.
- **Mandatory Hard Constraint**: Do NOT perform structural changes (`AddComponent`, etc.) inside the main loop. All configuration data must be read through existing components.
- **Anti-Pattern Warning**: Avoid heavy math calculations inside loops that could be offloaded to Jobs if possible. However, since the final write is to a managed `Transform`, the management overhead occurs here.

## Section 4: Developer Definition of Done
- [ ] Code compiles with no errors via Unity MCP.
- [ ] No Burst warnings regarding unmanaged vs managed pointer access.
- [ ] Verification complete: Moving the Player Entity causes the scene camera to follow smoothly with appropriate easing.
- [ ] DO NOT interact with the Unity Editor directly; always use the MCP (`unity_get_compilation_errors`).

## Section 5: Reviewer Runtime & Style Checklist
- [x] Verify smoothness feels "organic".
- [x] Check that zooming scales correctly using the component data.
- [x] Confirm usage of `math` library over `Vector3` where possible.

## Actionable Checklist
- [ ] Implement/Update `Assets/Scripts/Systems/CameraFollowBridgeSystem.cs`.
- [ ] Test camera following behavior in PlayMode.
