# Scene Setup Plan: Reactive Camera Configuration

## Objective
Configure the Main Camera and Camera Configuration entity in the Unity Editor to enable the reactive hybrid camera system implemented in tasks CAM-003 and CAM-004.

## Prerequisites
- The following scripts must be present and compiled with 0 errors:
  - [`Assets/Scripts/Authoring/CameraConfigurationAuthoring.cs`](../Assets/Scripts/Authoring/CameraConfigurationAuthoring.cs)
  - [`Assets/Scripts/Components/CameraConfiguration.cs`](../Assets/Scripts/Components/CameraConfiguration.cs)
  - [`Assets/Scripts/Components/IsCameraConfigTag.cs`](../Assets/Scripts/Components/CameraConfigTag.cs)
  - [`Assets/Scripts/Systems/CameraFollowBridgeSystem.cs`](../Assets/Scripts/Systems/CameraFollowBridgeSystem.cs)

## Architecture Note: Cross-Scene Reference Handling
The `CameraConfigurationAuthoring` component lives in a **SubScene**, while the **Main Camera** lives in the **Main Scene**. Serialized Inspector references across scenes cannot be used for Baker `DependsOn` dependencies.

**Solution**: The Baker programmatically finds the Main Camera via `GameObject.FindWithTag("MainCamera")` and establishes a `DependsOn(cameraTransform)` relationship. This ensures:
- No manual Inspector assignment is needed.
- Any edit-mode movement/rotation of the Main Camera triggers automatic rebaking.
- The camera's current position and rotation are baked into the ECS `CameraConfiguration` singleton.

## Step-by-Step Setup Instructions

### Step 1: Ensure Main Camera Has Correct Tag
In your Main Scene (e.g., `Asteroids.unity` or `Asteroids_entities.unity`):

1. Select the **Main Camera** GameObject in the Hierarchy.
2. In the Inspector, verify the **Tag** field is set to **`MainCamera`**.
   - If not, click the Tag dropdown and select `MainCamera`.
   - If `MainCamera` tag doesn't exist, create it via **Edit > Project Settings > Tags and Layers**.

### Step 2: Position the Main Camera in Scene View
1. In the Scene View, position and rotate the **Main Camera** to achieve your desired isometric framing of the player ship area.
2. The player ship typically starts at origin `(0, 0, 0)`, so position the camera at a diagonal offset (e.g., `(10, 10, 10)`) looking down at the origin.
3. **No Play Mode needed** — position the camera directly in Edit Mode Scene View.

### Step 3: Create/Verify Camera Config SubScene
In your SubScene (e.g., `Asteroids_entities.unity`):

1. Create or locate an empty GameObject named `CameraConfig`.
2. With the GameObject selected, click **Add Component** in the Inspector.
3. Search for and add **`Camera Configuration Authoring`**.
4. **Do NOT manually assign the Camera Anchor field** — it has been removed. The Baker handles this automatically.

### Step 4: Verify Player Ship Tag
Ensure the player ship GameObject (or its baked entity) has the `PlayerTag` component attached.

- If using Prefabs, verify the `PlayerShip.prefab` has the necessary authoring components that bake into an entity with `PlayerTag`.

### Step 5: Test Baker Reactivity (Optional)
1. Move the **Main Camera** slightly in the Scene View.
2. Enter Play Mode and observe the camera follows the player with the new framing.
3. Exit Play Mode, move the camera again, and re-enter Play Mode to confirm the new framing is applied.

### Step 6: Enter Play Mode
1. Press **Play** in the Unity Editor.
2. The `CameraFollowBridgeSystem` will:
   - Retrieve the `CameraConfiguration` singleton from the ECS world.
   - Find the player ship entity via `PlayerTag`.
   - Smoothly interpolate the Main Camera's position and rotation toward the player, maintaining the framed perspective defined in Step 2.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Camera does not follow player | Verify the Main Camera has the `MainCamera` tag. Check the Unity Console for errors. |
| Camera snaps instead of smoothing | Adjust the `FollowDamping` constant (default `15f`) in [`CameraFollowBridgeSystem.cs`](../Assets/Scripts/Systems/CameraFollowBridgeSystem.cs:22). |
| No camera configuration found (warning in console) | Ensure the Main Camera has the `MainCamera` tag and is active in the Main Scene. |
| Editor does not show changes after moving camera | ECS Baker only runs during domain reload or when dependencies change. Enter/exit Play Mode to see new framing, or use **Assets > Refresh** to force recompilation. |
| Bake fails silently | Check Unity Console for `[CameraConfigurationBaker]` warning messages. |

## Visual Reference
- **Main Camera**: Located in the Main Scene, tagged `MainCamera`. Positioned by the designer in Scene View.
- **CameraConfig GameObject**: Located in the SubScene, hosting the `CameraConfigurationAuthoring` component. No manual Inspector assignments needed.
- **Player Ship**: The entity being followed, must have `PlayerTag`.
