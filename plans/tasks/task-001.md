# Task-001: Player Ship Movement & Input Bridge

**Status:** `IMPLEMENTATION_COMPLETE`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---
## Section 1: Objective & Scope
* **Objective:** Implement the player input bridge and ship movement physics on the isometric $XZ$ plane ($Y=0$) using the modern Unity Input System (`com.unity.inputsystem`) and Burst-compiled `ISystem`.
* **In Scope:**
  - Unmanaged components: `PlayerTag`, `PlayerInput`, and `PlayerMovementData`.
  - Managed `PlayerInputBridgeSystem` (`SystemBase`) updating inside `InitializationSystemGroup`, reading `InputSystem_Actions`, computing isometric mouse raycast to the $Y=0$ plane, and writing pure unmanaged values to `PlayerInput`.
  - Burst-compiled unmanaged `PlayerMovementSystem` (`ISystem`) updating in `SimulationSystemGroup`, applying acceleration, linear damping (drag), velocity clamping, and smooth yaw rotation towards the mouse cursor.
  - `PlayerAuthoring` `MonoBehaviour` and nested `Baker<PlayerAuthoring>` for the player ship prefab.
* **Out of Scope:**
  - Weapons, firing, or projectile spawning (handled in `task-003`).
  - Screen boundary wrapping (handled in `task-002`).
  - Audio and particle effects.

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Components/PlayerTag.cs`
* `Assets/Scripts/Components/PlayerInput.cs`
* `Assets/Scripts/Components/PlayerMovementData.cs`
* `Assets/Scripts/Systems/PlayerInputBridgeSystem.cs`
* `Assets/Scripts/Systems/PlayerMovementSystem.cs`
* `Assets/Scripts/Authoring/PlayerAuthoring.cs`

### Data Contracts (Components)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;

    // Tag component for the player ship
    public struct PlayerTag : IComponentData { }

    // Snapshot of player input published each frame
    public struct PlayerInput : IComponentData
    {
        public float2 Movement;         // WASD / Stick direction on XZ plane
        public float3 AimWorldPosition; // Cursor intersection with Y=0 plane
        public bool FireHeld;           // Continuous trigger state
        public bool FireTriggered;      // Transient edge press (WasPressedThisFrame)
    }

    // Kinematics and control configuration
    public struct PlayerMovementData : IComponentData
    {
        public float ThrustAcceleration; // e.g., 30.0f
        public float MaxSpeed;           // e.g., 12.0f
        public float Drag;               // e.g., 2.0f
        public float RotationDamping;    // e.g., 18.0f
        public float3 CurrentVelocity;   // Accumulated linear velocity
    }
}
```

### System Signatures

#### 1. Input Bridge System (`PlayerInputBridgeSystem.cs`)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerInputBridgeSystem : SystemBase
    {
        private InputSystem_Actions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Enable();
            RequireForUpdate<PlayerInput>();
        }

        protected override void OnUpdate()
        {
            // 1. Read continuous movement
            Vector2 moveInput = _inputActions.Player.Move.ReadValue<Vector2>();

            // 2. Read attack trigger states
            bool fireHeld = _inputActions.Player.Attack.IsPressed();
            bool fireTriggered = _inputActions.Player.Attack.WasPressedThisFrame();

            // 3. Compute cursor intersection with Y=0 plane via Camera.main
            float3 aimPosition = float3.zero;
            Camera mainCamera = Camera.main;
            if (mainCamera != null && Mouse.current != null)
            {
                Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Y = 0
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    aimPosition = new float3(hitPoint.x, 0f, hitPoint.z);
                }
            }

            // 4. Publish snapshot to PlayerInput component(s)
            foreach (var input in SystemAPI.Query<RefRW<PlayerInput>>())
            {
                input.ValueRW.Movement = new float2(moveInput.x, moveInput.y);
                input.ValueRW.AimWorldPosition = aimPosition;
                input.ValueRW.FireHeld = fireHeld;
                input.ValueRW.FireTriggered = fireTriggered;
            }
        }

        protected override void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
            }
        }
    }
}
```

#### 2. Movement System (`PlayerMovementSystem.cs`)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Burst;

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, moveData, input) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<PlayerMovementData>, RefRO<PlayerInput>>()
                         .WithAll<PlayerTag>())
            {
                // 1. Acceleration along XZ plane
                float3 inputDir = new float3(input.ValueRO.Movement.x, 0f, input.ValueRO.Movement.y);
                if (math.lengthsq(inputDir) > 0.001f)
                {
                    inputDir = math.normalize(inputDir);
                    moveData.ValueRW.CurrentVelocity += inputDir * (moveData.ValueRO.ThrustAcceleration * dt);
                }

                // 2. Apply linear drag damping
                moveData.ValueRW.CurrentVelocity *= math.max(0f, 1f - (moveData.ValueRO.Drag * dt));

                // 3. Clamp top speed
                float currentSpeed = math.length(moveData.ValueRO.CurrentVelocity);
                if (currentSpeed > moveData.ValueRO.MaxSpeed)
                {
                    moveData.ValueRW.CurrentVelocity = (moveData.ValueRO.CurrentVelocity / currentSpeed) * moveData.ValueRO.MaxSpeed;
                }

                // 4. Translate position (constrained to Y = 0)
                float3 newPos = transform.ValueRO.Position + (moveData.ValueRO.CurrentVelocity * dt);
                newPos.y = 0f;
                transform.ValueRW.Position = newPos;

                // 5. Smoothly rotate yaw towards AimWorldPosition
                float3 toTarget = input.ValueRO.AimWorldPosition - transform.ValueRO.Position;
                toTarget.y = 0f;
                if (math.lengthsq(toTarget) > 0.01f)
                {
                    float targetAngle = math.atan2(toTarget.x, toTarget.z);
                    quaternion targetRot = quaternion.RotateY(targetAngle);
                    transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRot, math.saturate(moveData.ValueRO.RotationDamping * dt));
                }
            }
        }
    }
}
```

#### 3. Authoring & Baker (`PlayerAuthoring.cs`)
```csharp
namespace Asteroids.Core
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;

    public class PlayerAuthoring : MonoBehaviour
    {
        public float ThrustAcceleration = 35.0f;
        public float MaxSpeed = 12.0f;
        public float Drag = 2.0f;
        public float RotationDamping = 18.0f;

        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTag());
                AddComponent(entity, new PlayerInput());
                AddComponent(entity, new PlayerMovementData
                {
                    ThrustAcceleration = authoring.ThrustAcceleration,
                    MaxSpeed = authoring.MaxSpeed,
                    Drag = authoring.Drag,
                    RotationDamping = authoring.RotationDamping,
                    CurrentVelocity = float3.zero
                });
            }
        }
    }
}
```

### Visual & Scene Rigging Contracts
*(Defines how the player entity is assembled in the SubScene)*
* **Target Scope**: `SubScene` (`Assets/Scenes/Asteroids_entities.unity`)
* **Entity Visual Type**:
  - **Visual Entity (Prefab-Backed)**:
    - **Visual Prefab Asset Path**: `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Ship_Fighter_02.prefab`
    - **Rigging Mode**: `Prefab Instance (Root)`
    - **Initial Transform**: Position `(0, 0, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`
    - **Authoring Component**: `PlayerAuthoring` attached to the instantiated `SM_Ship_Fighter_02` GameObject in `Asteroids_entities.unity`
* **Scope 1 (Main Scene) Dependencies**:
  - `Assets/Scenes/Asteroids.unity` -> `Main Camera` (Camera, AudioListener, UniversalAdditionalCameraData)
  - `Entities` (GameObject with `Unity.Scenes.SubScene` pointing to `Assets/Scenes/Asteroids_entities.unity`)

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `unity-input-system`
*Source: `concepts/unity-input-system.md`*
* **Architecture**: Managed `SystemBase` in `InitializationSystemGroup` instantiates autogenerated wrapper `new InputSystem_Actions()`, enables it, and synchronously polls inputs each frame into an unmanaged `IComponentData`.
* **Disposal**: Implement `OnDestroy()` to call `.Disable()` and `.Dispose()` on the action wrapper instance.
* **Burst Consumer**: Simulation logic runs in Burst-compiled `ISystem` in `SimulationSystemGroup` consuming exclusively from the unmanaged `PlayerInput` component.

### Anti-Pattern Warnings
* ⛔ **NO `UnityEngine.Input` in `ISystem`**: Never call `UnityEngine.Input` or managed action wrappers inside `ISystem` or jobs.
* ⛔ **DO NOT USE `IAspect`**: Aspects are marked `[Obsolete]` in Entities 1.4+. Use direct queries in `SystemAPI.Query`.
* ⛔ **NO GC IN BURST**: Zero `new`, string formatting, or LINQ in `PlayerMovementSystem`.
* ⛔ **DO NOT FORGET `RequireForUpdate<PlayerInput>()`**: Ensures the bridge does not perform redundant work if the player entity is absent.

---

## Section 4: Developer Definition of Done (DoD)
- [x] Code implemented strictly within the designated paths in `Assets/Scripts/`.
- [x] Compilation verified via Unity MCP (`unity_get_compilation_errors` reports **0 errors**).
- [x] Domain reload verified via `unity_console_log` (**0 exceptions**).
- [x] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [x] **Static Audit**: `PlayerMovementSystem` is unmanaged `struct` and decorated with `[BurstCompile]` on both struct and methods.
- [x] **Allocation Audit**: Zero GC allocations observed in `PlayerMovementSystem.OnUpdate()`. All types are unmanaged (`float3`, `float2`, `quaternion`). No `new` class instantiations, no `string`, no LINQ.
- [x] **Update Phasing**: `PlayerInputBridgeSystem` executes in `InitializationSystemGroup`; `PlayerMovementSystem` executes in `SimulationSystemGroup`.
- [x] **Authoring Rigging**: `PlayerAuthoring` correctly uses `TransformUsageFlags.Dynamic`.
- [x] **Visual & Rigging Audit**: Verified `SM_Ship_Fighter_02.prefab` assigned as visual prefab asset path in SubScene scope.

---

## Section 6: Review Verification Report

**Review Date**: 2026-09-22  
**Reviewer**: Unity Reviewer & Gatekeeper (DOTS Reviewer Skill)  
**Verdict**: **PASSED**  

### Compilation & Burst
- [x] **0 compilation errors**, **0 Burst warnings** for original code (`Assets/Scripts/Components/`, `Assets/Scripts/Systems/`, `Assets/Scripts/Authoring/`).
- Verified via `unity_get_compilation_errors` at timestamp `17:37:30` (entry count: 0).

### Static Burst & Allocation Audit
- [x] `PlayerMovementSystem` is `[BurstCompile]` on struct and on `OnCreate`/`OnUpdate` methods.
- [x] No `string`, `class`, `List<T>`, LINQ, or `new` class instantiations in `OnUpdate`.
- [x] No `IAspect` usage (Entities 1.4+ compliant).
- [x] No ECB usage (no structural changes — no `.Playback()` or `.Dispose()`).
- [x] `PlayerInputBridgeSystem` is `SystemBase` (managed), acceptable for Input System polling.

### Runtime PlayMode Sanity
- [x] Zero console errors/warnings during PlayMode.
- [x] No `NullReferenceException`, Burst abort, or unexpected warnings.

---

## Actionable Implementation Checklist
- [x] Step 1: Create `PlayerTag.cs`, `PlayerInput.cs`, and `PlayerMovementData.cs` in `Assets/Scripts/Components/`.
- [x] Step 2: Implement `PlayerInputBridgeSystem.cs` in `Assets/Scripts/Systems/``.
- [x] Step 3: Implement `PlayerMovementSystem.cs` in `Assets/Scripts/Systems/`.
- [x] Step 4: Implement `PlayerAuthoring.cs` in `Assets/Scripts/Authoring/`.
- [x] Step 5: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.
- [x] Step 6: Static Burst & Allocation Audit — all checks passed.
- [x] Step 7: Runtime PlayMode Sanity — zero errors, zero warnings.
- [x] Step 8: Automated test plan formulated and test file written.
