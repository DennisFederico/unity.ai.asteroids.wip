# TASK CONTRACT: CAM-002 - Refactor Camera Follow System to use Global Config

## Section 1: Objective & Scope
Refactor the [`Assets/Scripts/Systems/CameraFollowBridgeSystem.cs`](Assets/Scripts/Systems/CameraFollowBridgeSystem.cs:line) to transition from using hardcoded offset values to consuming data from the newly implemented `CameraConfiguration` singleton entity. This ensures that real-time designer tuning in the Inspector propagates directly to the game camera.

## Section 2: Type & API Contracts

### Components & Queries
- **Target Data Source**: [`Assets/Scripts/Components/CameraConfiguration.cs`](Assets/Scripts/Components/CameraConfiguration.cs:line) marked with `IsCameraConfigTag`.
- **Query Filter**: The system must use `SystemAPI.GetSingleton<CameraConfiguration>()` (or equivalent query) inside its update loop to retrieve the latest configuration.
- **Output Target**: A Managed Proxy or direct Transform access via Hybrid bridge to update the main `Camera.main` position/rotation based on the player ship's position + config offset/pitch/yaw.

### Visual & Scene Rigging Contract
- **Pure Data / Non-Visual**: This logic operates within the ECS simulation space but bridges results back to the Unity Transform system. It does not introduce new visual entities.

## Section 3: Architectural Directives

### OKF Implementation Patterns
- **Concept ID Reference**: Adhere to patterns for reading singletons efficiently within `ISystem`.
- **Access Pattern**: Since we want high performance, check if the singleton exists before attempting to read; handle cases where no config has been baked yet gracefully (e.g., fallback to default identity).

### Mandatory Hard Constraints
- ✅ Retrieve only once per frame if possible, or use efficient `IJobEntity`/`foreach` patterns depending on how many entities are being followed.
- ⛔ Do NOT re-introduce hardcoded float literals for distance or rotation angles. All magic numbers must come from `CameraConfiguration`.
- ⛔ No runtime structural changes (adding/removing components) during the follow loop.

## Section 4: Developer Definition of Done (DoD)
- [ ] Code compiles with 0 errors and 0 Burst warnings.
- [ ] Verify compilation via MCP (`unity-compile-check` skill).
- [ ] Ensure that changing the `Offset` value on a configured GameObject in the Main Scene updates the actual Game View camera position at runtime.
- [ ] **CRITICAL**: Do not interact with the Unity Editor directly or using the filesystem; always use the MCP tools provided.

## Section 5: Reviewer Checklist
- [x] Does the system fail gracefully if `IsCameraConfigTag` is missing?
- [x] Are all mathematics performed using `Unity.Mathematics` types?
- [x] Is there any GC allocation occurring in the `Update` loop of the Bridge System?
- [x] Is the Pitch/Yaw conversion correctly applied to the final orientation?

---
**Actionable Checklist:**
- [ ] Read existing `CameraFollowBridgeSystem.cs` implementation.
- [ ] Implement logic to fetch `CameraConfiguration` via singleton lookup.
- [ ] Update transform math to apply the retrieved `offset`, `pitch`, and `yaw`.
- [ ] Validate in PlayMode using `unity_screenshot_game` after making changes.
