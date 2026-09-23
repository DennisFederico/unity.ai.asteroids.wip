# Authoring Recipe: [Scene / Subscene / Feature Name]

**Target Main Scene:** `Assets/Scenes/[SceneName].unity`  
**Target SubScene:** `[SceneName]_entities` (`Assets/Scenes/[SceneName]_entities.unity`)  
**Safety Checkpoint:** `git rev-parse --short HEAD` (Verified clean working tree)  
**Reference Playbook:** `concepts/agentic-scene-assembly.md`  

---

## Tier 1: Structured Hierarchy & Inspector Recipe (Two Distinct Scopes)
*(Manual walkthrough for human developer OR blueprint for Tier 2 MCP automation)*

### Scope 1: Main Scene Hierarchy (Managed GameObjects)
> **Rule**: Contains runtime managed objects (Camera, Lights, UI, AudioListener, and Hybrid bridges). **Never** place unmanaged ECS authoring components here.

```
[Main Scene: Assets/Scenes/[SceneName].unity]
 ├── Main Camera (Camera, AudioListener, UniversalAdditionalCameraData)
 ├── Directional Light (Light)
 ├── [UI Canvas / EventSystem] (Canvas, CanvasScaler, GraphicRaycaster)
 ├── [CompanionGameObject (Optional Hybrid Bridge)]
 │    └── [CompanionComponent] (MonoBehaviour reading ECS data)
 └── Entities (GameObject with Unity.Scenes.SubScene component)
      └── Linked _SceneAsset -> Assets/Scenes/[SceneName]_entities.unity
```

### Scope 2: SubScene Hierarchy (ECS Authoring GameObjects -> Entities)
> **Rule**: Contains GameObjects with Authoring `MonoBehaviour` and `Baker<T>` scripts to be converted into ECS entities during baking.  
> ⛔ **NEVER place Cameras, Lights, UI Canvases, or AudioListeners in the SubScene.**

```
[SubScene: Assets/Scenes/[SceneName]_entities.unity]
 ├── [Visual Entity (Prefab-Backed), e.g. PlayerShip / Asteroid]
 │    ├── [Instantiated from Assets/Prefabs/[EntityPrefab].prefab]
 │    ├── Transform: Position([X], [Y], [Z]), Rotation([X], [Y], [Z]), Scale(1, 1, 1)
 │    └── [AuthoringComponent] (Script with Baker<T>)
 │         ├── [NumericParameterField]: [Value]
 │         └── [ConfigurationField]: [Value]
 ├── [Pure Data Entity (Explicitly Empty), e.g. AsteroidSpawner / LevelManager]
 │    ├── [Primitive Empty GameObject - No Mesh/Renderer]
 │    ├── Transform: Position([X], [Y], [Z])
 │    └── [SpawnerOrConfigAuthoring] (Script with Baker<T>)
 └── [Environment / Static Geometry (Optional)]
      └── [StaticEntityGameObject]
```

### Component Wiring Details (SubScene Scope)
| Target Scope | GameObject Path | Component to Add | Property / Field | Value / Asset Reference |
| :--- | :--- | :--- | :--- | :--- |
| **SubScene** | `[VisualEntity]` | `[AuthoringComponent]` | Visual Asset | `Assets/Prefabs/[EntityPrefab].prefab` |
| **SubScene** | `[VisualEntity]` | `[AuthoringComponent]` | `[ParameterField]` | `[Numeric or String Value]` |
| **SubScene** | `[PureDataEntity]` | `[SpawnerAuthoring]` | Visual Asset | `None (Pure Data - Primitive Empty)` |
| **Main Scene** | `Entities` | `Unity.Scenes.SubScene` | `_SceneAsset` | `Assets/Scenes/[SceneName]_entities.unity` |

---

## Tier 2: Executable Unity MCP Command Sequence
*(Presented to user for approval before running)*

### Phase 0: Target Scene Confirmation & Safety Checkpoint
1. Verify clean git working tree: `git status`.
2. Confirm target Main Scene path (`Assets/Scenes/[SceneName].unity`). If ambiguous, query `unity_scene_info` and confirm with the user before touching scenes.

### Phase 1: Editor Discovery & Selection
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_list_instances", "Arguments": {} }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_select_instance", "Arguments": { "projectName": "[ProjectName]" } }
```

### Phase 2: SubScene Verification & Initialization
1. Ensure Main Scene is open:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/[SceneName].unity", "saveFirst": true } }
```
2. Detect if SubScene already exists in hierarchy:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_selection_find_by_type", "Arguments": { "typeName": "Unity.Scenes.SubScene" } }
```
*(If SubScene exists, proceed directly to Phase 3. If SubScene is missing, execute steps 3–11 below):*

3. Capture scene info:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_info", "Arguments": {} }
```
4. Create new scene for SubScene:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_new", "Arguments": { "saveFirst": true } }
```
5. Inspect hierarchy to find default Camera and Light:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_hierarchy", "Arguments": {} }
```
6. Delete default environment objects (SubScene Hygiene):
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_gameobject_delete", "Arguments": { "path": "Main Camera" } }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_gameobject_delete", "Arguments": { "path": "Directional Light" } }
```
7. Save clean SubScene asset:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": { "path": "Assets/Scenes/[SceneName]_entities.unity" } }
```
8. Re-open Main Scene:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/[SceneName].unity", "saveFirst": true } }
```
9. Create SubScene anchor GameObject:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_gameobject_create", "Arguments": { "name": "Entities", "primitiveType": "Empty" } }
```
10. Attach SubScene component:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_add", "Arguments": { "gameObjectPath": "Entities", "componentType": "Unity.Scenes.SubScene" } }
```
11. Wire SubScene reference to newly created asset:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_set_reference", "Arguments": { "path": "Entities", "componentType": "Unity.Scenes.SubScene", "propertyName": "_SceneAsset", "assetPath": "Assets/Scenes/[SceneName]_entities.unity" } }
```
12. Save Main Scene:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": {} }
```

### Phase 3: SubScene Authoring (Direct SubScene Workflow)
1. Open SubScene file directly:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/[SceneName]_entities.unity", "saveFirst": true } }
```

2. Populate Authoring hierarchy:

#### Branch A: Visual Entity (Prefab-Backed)
*(Use for physical game actors: Player ships, enemies, obstacles, projectiles)*
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_asset_instantiate_prefab", "Arguments": { "prefabPath": "Assets/Prefabs/[EntityPrefab].prefab", "name": "[AuthoringGameObject]" } }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_add", "Arguments": { "gameObjectPath": "[AuthoringGameObject]", "componentType": "[AuthoringComponent]" } }
```

#### Branch B: Pure Data / Non-Visual Entity (Explicitly Empty)
*(Use ONLY for purely logical entities: Spawners, Config Singletons, Zone Triggers)*
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_gameobject_create", "Arguments": { "name": "[AuthoringGameObject]", "primitiveType": "Empty" } }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_add", "Arguments": { "gameObjectPath": "[AuthoringGameObject]", "componentType": "[AuthoringComponent]" } }
```

> [!CAUTION]
> **Visual Confirmation Gate**: If an entity represents a visible game object and no visual prefab is specified in the task contract or prompt, DO NOT default to Branch B. Pause and confirm the visual asset path with the user, or search via `unity_search_assets`.

3. Wire Prefab / Material asset references (if any):
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_set_reference", "Arguments": { "path": "[AuthoringGameObject]", "componentType": "[AuthoringComponent]", "propertyName": "[PrefabField]", "assetPath": "Assets/Prefabs/[PrefabName].prefab" } }
```
4. Set scalar / numeric configuration properties:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_set_property", "Arguments": { "gameObjectPath": "[AuthoringGameObject]", "componentType": "[AuthoringComponent]", "propertyName": "[ParameterField]", "value": "[Value]" } }
```
5. Save SubScene:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": {} }
```

### Phase 4: Main Scene Managed Setup & Re-linking
1. Re-open Main Scene:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/[SceneName].unity", "saveFirst": true } }
```
2. Configure or verify managed GameObjects (Camera, Directional Light, UI Canvas, Companion GameObjects).
3. Save Main Scene:
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": {} }
```

---

## Phase 5: Baking & Visual PlayMode Verification
- [ ] Enter Play Mode: `unity_play_mode` (`action: "play"`)
- [ ] Check console for baking errors: `unity_console_log` (`log_types: ["Error", "Exception"]` -> 0 errors)
- [ ] Capture visual scene screenshot: `unity_screenshot_scene`
- [ ] Capture visual game view HUD screenshot: `unity_screenshot_game`
- [ ] **Visual Mesh Audit**: For visual entities, inspect screenshot to verify that the 3D model/mesh is visible and rendered at expected coordinates (not just an invisible empty transform).
- [ ] Exit Play Mode: `unity_play_mode` (`action: "stop"`)
- [ ] Confirm entity baking verified matching Tier 1 recipe
