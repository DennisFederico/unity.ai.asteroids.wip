# Authoring Recipe: Task-003 — Laser Projectiles & Shooting Mechanism

**Target Main Scene:** `Assets/Scenes/Asteroids.unity`  
**Target SubScene:** `Asteroids_entities` (`Assets/Scenes/Asteroids_entities.unity`)  
**Safety Checkpoint:** `git status` — Working tree clean (verified at commit HEAD)  
**Reference Playbook:** `templates/authoring-recipe-template.md`  
**Source Task:** `plans/tasks/task-003.md`  
**Associated Visual Prefab:** `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab` (Visual Entity — Prefab Asset, Dynamic ECB)

---

## Tier 1: Structured Hierarchy & Inspector Recipe (Two Distinct Scopes)

### Scope 1: Main Scene Hierarchy (`Assets/Scenes/Asteroids.unity`)

> **Rule**: No changes needed to Main Scene for Task-003. The Main Scene is already configured from Task-001/002.

```
[Main Scene: Assets/Scenes/Asteroids.unity]
 ├── Main Camera (Camera, AudioListener, UniversalAdditionalCameraData)
 ├── Directional Light (Light, UniversalAdditionalLightData)
 └── Entities (GameObject with Unity.Scenes.SubScene component)
      └── Linked _SceneAsset -> Assets/Scenes/Asteroids_entities.unity
```

**Current State**: Clean — 3 root GameObjects, SubScene component properly wired. No modifications required.

### Scope 2: SubScene Hierarchy (`Assets/Scenes/Asteroids_entities.unity`)

> **Rule**: Contains GameObjects with Authoring `MonoBehaviour` and `Baker<T>` scripts to be converted into ECS entities during baking.

```
[SubScene: Assets/Scenes/Asteroids_entities.unity]
 ├── PlayerShip (Prefab Instance: SM_Ship_Fighter_02.prefab)
 │    ├── Transform: Position(0, 0, 0), Rotation(0, 0, 0), Scale(1, 1, 1)
 │    └── PlayerAuthoring
 │         ├── ThrustAcceleration: 35
 │         ├── MaxSpeed: 12
 │         ├── Drag: 2
 │         ├── RotationDamping: 18
 │         ├── LaserPrefab: [NEEDS WIRING] -> FX_Laser_Bullet_01.prefab
 │         ├── FireRate: 5
 │         └── MuzzleOffset: [NEEDS VERIFICATION] -> default (0, 0, 0) or (0, 0, 0.5)
 ├── InfiniteMapManager (Pure Data - Primitive Empty GameObject)
 │    ├── Transform: Position(0, 0, 0)
 │    └── InfiniteMapAuthoring
 └── FX_Laser_Bullet_01.prefab (Prefab Asset — NOT a scene instance)
      ├── Transform: Position(0, 0, 0)
      └── LaserAuthoring (NEEDS ATTACHMENT to prefab asset)
           ├── Speed: 35.0
           ├── LifetimeDuration: 1.8
           └── CollisionRadius: 0.3
```

### Component Wiring Details (SubScene Scope)

| Target Scope | GameObject Path | Component to Add/Modify | Property / Field | Value / Asset Reference |
| :--- | :--- | :--- | :--- | :--- |
| **SubScene** | `PlayerShip` | `PlayerAuthoring` (existing) | `LaserPrefab` | `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab` |
| **SubScene** | `PlayerShip` | `PlayerAuthoring` (existing) | `MuzzleOffset` | `0, 0, 0.6` (local Z-forward offset from ship center, matching fighter nose) |
| **Prefab Asset** | `FX_Laser_Bullet_01.prefab` | `LaserAuthoring` (attach if missing) | `Speed` | `35.0` |
| **Prefab Asset** | `FX_Laser_Bullet_01.prefab` | `LaserAuthoring` (attach if missing) | `LifetimeDuration` | `1.8` |
| **Prefab Asset** | `FX_Laser_Bullet_01.prefab` | `LaserAuthoring` (attach if missing) | `CollisionRadius` | `0.3` |
| **Main Scene** | `Entities` | `Unity.Scenes.SubScene` | `_SceneAsset` | `Assets/Scenes/Asteroids_entities.unity` |

---

## Execution Priority & Risk Assessment

### Priority 1: Wire LaserPrefab on PlayerAuthoring (HIGH)
- **Risk**: Low — simple object reference wire
- **Impact**: Without this reference, `LaserShootingSystem` cannot instantiate laser entities via ECB
- **Current State**: `LaserPrefab` field is `null` on PlayerShip's PlayerAuthoring

### Priority 2: Set MuzzleOffset on PlayerAuthoring (MEDIUM)
- **Risk**: Low — simple vector property
- **Impact**: Incorrect muzzle offset causes lasers to spawn from wrong position (e.g., ship center instead of nose)
- **Current State**: MuzzleOffset needs verification — default is likely `0, 0, 0`

### Priority 3: Attach LaserAuthoring to Laser Prefab Asset (HIGH)
- **Risk**: Low — standard component attachment to prefab
- **Impact**: Without LaserAuthoring on the prefab, baked entities won't have LaserTag, ProjectileData, or Lifetime components
- **Current State**: Needs verification — LaserAuthoring.cs exists in project but may not be attached to FX_Laser_Bullet_01.prefab

---

## Phase 0: Pre-Execution Verification Checklist

Before MCP execution begins:

- [x] Git working tree clean
- [x] Target Main Scene confirmed: `Assets/Scenes/Asteroids.unity`
- [x] Target SubScene confirmed: `Assets/Scenes/Asteroids_entities.unity`
- [x] Visual Prefab confirmed: `FX_Laser_Bullet_01.prefab` exists at `Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab`
- [x] SubScene component exists and is properly linked
- [x] Compilation clean: 0 errors, 0 Burst warnings
- [x] PlayerShip exists in SubScene with PlayerAuthoring component

---

## Phase 1: MCP Execution Sequence (Tier 2 — Pending Approval)

### Step 1: Wire LaserPrefab Reference on PlayerShip
```
Tool: unity_component_set_reference
Parameters:
  path: "PlayerShip"
  componentType: "PlayerAuthoring"
  propertyName: "LaserPrefab"
  assetPath: "Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab"
```

### Step 2: Set MuzzleOffset on PlayerShip
```
Tool: unity_component_set_property
Parameters:
  gameObjectPath: "PlayerShip"
  componentType: "PlayerAuthoring"
  propertyName: "MuzzleOffset"
  value: { x: 0, y: 0, z: 0.6 }
```

### Step 3: Attach LaserAuthoring to Laser Prefab Asset
- **Note**: MCP tools operate on scene GameObjects, not prefab assets directly.
- **Alternative approach**: Instantiate the laser prefab into the SubScene, attach LaserAuthoring, then extract as prefab — OR use Unity Execute Code to assign to prefab asset.
- **Recommended approach**: Use `unity_execute_code` to modify the prefab asset directly.

```
Tool: unity_execute_code
Code:
  var prefabPath = "Assets/ThirdParty/Synty/PolygonSciFiCity/Prefabs/FX/FX_Laser_Bullet_01.prefab";
  var prefabAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
  if (prefabAsset != null)
  {
      var existing = prefabAsset.GetComponent<Asteroids.Core.LaserAuthoring>();
      if (existing == null)
      {
          prefabAsset.AddComponent<Asteroids.Core.LaserAuthoring>();
          UnityEditor.Undo.RegisterCreatedObject(prefabAsset);
          return "LaserAuthoring attached to laser prefab successfully";
      }
      return "LaserAuthoring already exists on laser prefab";
  }
  return "Laser prefab not found";
```

### Step 4: Save SubScene
```
Tool: unity_scene_save
Parameters:
  path: "Assets/Scenes/Asteroids_entities.unity"
```

### Step 5: Save Main Scene
```
Tool: unity_scene_save
```

---

## Phase 2: Visual PlayMode & Baking Verification (Post-Execution)

1. **Enter Play Mode**: `unity_play_mode(action: "play")`
2. **Check Console**: `unity_console_log(type: "error", includeStackTrace: "errors")` — expect 0 errors
3. **Verify Laser Spawning**: In Game View, hold fire input (left mouse / space) to fire lasers
4. **Visual Mesh Audit**: Verify FX_Laser_Bullet_01 geometry is visible at muzzle position when firing
5. **Exit Play Mode**: `unity_play_mode(action: "stop")`
6. **Capture Screenshots**: `unity_screenshot_game` and `unity_screenshot_scene`

---

## Risk & Rollback Notes

- **Risk Level**: LOW — all operations are additive (wiring references, setting properties, attaching components)
- **Rollback**: `git checkout -- .` if any unexpected state changes occur
- **Safety Checkpoint**: Working tree was clean at session start (commit HEAD)
- **No Scene Structure Changes**: This task only modifies component properties and references — no GameObject creation/deletion in scenes
