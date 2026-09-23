# Task-001 Authoring Recipe — Player Ship Movement & Input Bridge

**Target Scene**: `Assets/Scenes/Asteroids.unity`  
**Target SubScene**: `Assets/Scenes/Asteroids_entities.unity`  
**Visual Prefab**: `Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Ship_Fighter_02.prefab`  
**Rigging Mode**: `Prefab Instance (Root)` in SubScene  

---

## Scope 1: Main Scene Hierarchy (`Assets/Scenes/Asteroids.unity`)

```
[Main Scene: Assets/Scenes/Asteroids.unity]
 ├── Main Camera (Camera, AudioListener, UniversalAdditionalCameraData)
 │    └── Position: (0, 1, -10)
 ├── Directional Light (Light, UniversalAdditionalLightData)
 │    └── Position: (0, 3, 0)
 └── Entities (GameObject with Unity.Scenes.SubScene component)
      └── _SceneAsset -> Assets/Scenes/Asteroids_entities.unity
      └── AutoLoadScene -> true
```

**Status**: Already configured correctly. No changes required.

---

## Scope 2: SubScene Hierarchy (`Assets/Scenes/Asteroids_entities.unity`)

### Current State
- **Root Objects**: 0 (empty scene)

### Target State After Rigging

```
[SubScene: Assets/Scenes/Asteroids_entities.unity]
 └── PlayerShip (Prefab Instance: SM_Ship_Fighter_02.prefab)
      ├── Transform: Position(0, 0, 0), Rotation(0, 0, 0), Scale(1, 1, 1)
      └── PlayerAuthoring (MonoBehaviour)
           ├── ThrustAcceleration: 35.0f
           ├── MaxSpeed: 12.0f
           ├── Drag: 2.0f
           └── RotationDamping: 18.0f
```

---

## Component Wiring Details (SubScene Scope)

| GameObject Path | Component | Property | Value |
|----------------|-----------|----------|-------|
| `PlayerShip` | `PlayerAuthoring` | `ThrustAcceleration` | `35.0f` |
| `PlayerShip` | `PlayerAuthoring` | `MaxSpeed` | `12.0f` |
| `PlayerShip` | `PlayerAuthoring` | `Drag` | `2.0f` |
| `PlayerShip` | `PlayerAuthoring` | `RotationDamping` | `18.0f` |

---

## Tier-2 MCP Execution Plan

| Step | Action | Tool Call | Parameters |
|------|--------|-----------|------------|
| 1 | Open SubScene | `unity_scene_open` | `path: "Assets/Scenes/Asteroids_entities.unity"`, `saveFirst: true` |
| 2 | Instantiate PlayerShip prefab | `unity_asset_instantiate_prefab` | `prefabPath: "Assets/ThirdParty/PolygonSciFiSpace/Prefabs/Vehicles/SM_Ship_Fighter_02.prefab"`, `name: "PlayerShip"`, `position: {x:0, y:0, z:0}` |
| 3 | Attach PlayerAuthoring component | `unity_component_add` | `gameObjectPath: "PlayerShip"`, `componentType: "Asteroids.Core.PlayerAuthoring"` |
| 4 | Set ThrustAcceleration | `unity_component_set_property` | `propertyName: "ThrustAcceleration"`, `value: 35.0` |
| 5 | Set MaxSpeed | `unity_component_set_property` | `propertyName: "MaxSpeed"`, `value: 12.0` |
| 6 | Set Drag | `unity_component_set_property` | `propertyName: "Drag"`, `value: 2.0` |
| 7 | Set RotationDamping | `unity_component_set_property` | `propertyName: "RotationDamping"`, `value: 18.0` |
| 8 | Save SubScene | `unity_scene_save` | `path: "Assets/Scenes/Asteroids_entities.unity"` |
| 9 | Enter PlayMode | `unity_play_mode` | `action: "play"` |
| 10 | Check console for errors | `unity_console_log` | `type: "error"`, `includeStackTrace: true` |
| 11 | Capture game view screenshot | `unity_screenshot_game` | — |
| 12 | Exit PlayMode | `unity_play_mode` | `action: "stop"` |

---

## Verification Checklist

- [ ] SubScene contains `PlayerShip` prefab instance at position `(0, 0, 0)`
- [ ] `PlayerAuthoring` component attached with correct property values
- [ ] SubScene saves without errors
- [ ] PlayMode enters without baking errors
- [ ] Console shows 0 errors/warnings
- [ ] Player ship mesh is visible in game view screenshot
