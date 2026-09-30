# TASK CONTRACT: CAM-001 - Camera Configuration Data & Baking

## Section 1: Objective & Scope
Implement the fundamental data structures and baking logic required for a global isometric camera configuration. This task focuses strictly on defining the managed authoring components and their corresponding unmanaged ECS representations, including the conversion process during scene baking.

## Section 2: Type & API Contracts

### Components & Data Types
- **Namespace**: `Asteroids.Components`
- **Managed Component (Authoring)**: [`Assets/Scripts/Authoring/CameraConfigurationAuthoring.cs`](Assets/Scripts/Authoring/CameraConfigurationAuthoring.cs:line)
    - `public Vector3 Offset;` // Distance relative to target
    - `public float Pitch;`   // X-axis rotation
    - `public float Yaw;`     // Y-axis rotation
- **Unmanaged Component (ECS)**: [`Assets/Scripts/Components/CameraConfiguration.cs`](Assets/Scripts/Components/CameraConfiguration.cs:line)
    - Implementing `IComponentData`.
    - Fields:
        - `float3 offset;`
        - `float pitch;`
        - `float yaw;`
- **Singleton Marker Tag**: [`Assets/Scripts/Components/IsCameraConfigTag.cs`](Assets/Scripts/Components/IsCameraConfigTag.cs:line)
    - Implementing `IComponentData`. Used to identify the unique config entity.

### Visual & Scene Rigging Contract
- **Pure Data / Non-Visual**: The resulting Entity produced by this task is purely logical and holds no visual representation. It serves only as a source of truth for the `CameraFollowBridgeSystem`.

## Section 3: Architectural Directives

### OKF Implementation Patterns
- **Concept ID Reference**: Ensure adherence to standard Baking patterns for Singletons.
- **Baking Strategy**: Implement `Baker<CameraConfigurationAuthoring>` within the same file as the authoring class or a dedicated baker folder. Use `AddComponent` to attach both `CameraConfiguration` and `IsCameraConfigTag`.

### Mandatory Hard Constraints
- ✅ Use `Unity.Mathematics` types (`float3`).
- ✅ All ECS components must be defined in `Assets/Scripts/Components/`.
- ✅ Avoid using `[Obsolete] IAspect`; rely on direct component access.
- ⛔ No runtime instantiation of these entities; they must be baked from a GameObject in the Main Scene.

## Section 4: Developer Definition of Done (DoD)
- [ ] Code compiles with 0 errors and 0 Burst warnings.
- [ ] Verify compilation via MCP (`unity-compile-check` skill).
- [ ] **CRITICAL**: Do not interact with the Unity Editor directly or using the filesystem; always use the MCP tools provided.

## Section 5: Reviewer Checklist
- [x] `CameraConfiguration` uses `float3`, `float`, `float`.
- [x] `CameraConfigurationAuthoring` provides Inspector fields matching requirements.
- [x] Entities are correctly tagged with `IsCameraConfigTag`.
- [x] File structure follows project conventions.

---
**Actionable Checklist:**
- [ ] Create `Assets/Scripts/Components/CameraConfiguration.cs`
- [ ] Create `Assets/Scripts/Components/IsCameraConfigTag.cs`
- [ ] Create `Assets/Scripts/Authoring/CameraConfigurationAuthoring.cs` (includes `Baker<T>`)
