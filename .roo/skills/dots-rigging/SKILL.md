---
name: dots-rigging
description: >-
  Use when setting up or modifying Unity Subscenes, attaching Authoring components and Bakers to GameObjects,
  configuring Prefabs, or performing visual scene assembly and PlayMode baking verification.
user-invocable: true
allowed-tools: view_file write_to_file replace_file_content run_command call_mcp_tool
---

# Unity DOTS Scene & Rigging Specialist

Guide the Rigging Specialist persona to bridge C# Authoring scripts into functional Subscenes and baking hierarchies via the Two-Tier Rigging Strategy.

---

## 1. The Two-Scope Rigging Mental Model

Every Unity DOTS scene setup consists of two mutually exclusive scopes:

1. **Scope 1: Main Scene (Managed GameObjects)**:
   - Asset path: `Assets/Scenes/<SceneName>.unity`.
   - Contains runtime managed objects: Main Camera, Directional Light, UI Canvas, EventSystem, AudioListener, and managed Hybrid bridges (`SystemBase` companions).
   - Contains the anchor GameObject (`Entities`) with the `Unity.Scenes.SubScene` component linking to the SubScene.
   - **Never** place pure ECS Authoring components or Bakers directly in the Main Scene root.
2. **Scope 2: SubScene (ECS Authoring -> Entities)**:
   - Asset path: `Assets/Scenes/<SceneName>_entities.unity`.
   - Contains Authoring `MonoBehaviour` and `Baker<T>` components that the Entities package converts into runtime ECS entities during baking.
   - ⛔ **NEVER place Cameras, Lights, UI Canvases, or AudioListeners in the SubScene.**

---

## 2. Mandatory Safety Checkpoint & Scene Confirmation

1. **Clean Git Checkpoint**:
   - Check git status to ensure changes can be rolled back:
     ```powershell
     git status
     ```
2. **Main Scene Confirmation**:
   - The target Main Scene must be confirmed before performing modifications.
   - If not explicitly provided in the task contract or prompt, call `unity_scene_info` and confirm the active scene path with the user before touching scenes.

---

## 3. Tier 1: Formulate the Authoring Recipe

Always produce an explicit human-readable setup recipe using `templates/authoring-recipe-template.md`:
- **Scope 1: Main Scene Hierarchy**: Camera, Light, UI, SubScene anchor (`Entities`).
- **Scope 2: SubScene Hierarchy**: Spawners, Authoring components, Bakers, Prefabs.
- **Component Wiring Details Table**: Serialized fields, asset paths, and target parameters.

---

## 4. Tier 2: Gated Unity MCP Execution

Execute operations step-by-step using `anklebreaker-unity-mcp`:

### Phase 1: Editor Discovery & Selection
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_list_instances", "Arguments": {} }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_select_instance", "Arguments": { "projectName": "<ProjectName>" } }
```

### Phase 2: SubScene Detection & Initialization
1. Ensure Main Scene is open:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/<SceneName>.unity", "saveFirst": true } }
   ```
2. Detect existing SubScene in hierarchy:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_selection_find_by_type", "Arguments": { "typeName": "Unity.Scenes.SubScene" } }
   ```
   *(If SubScene exists, extract its linked asset path and proceed to Phase 3. If SubScene is missing, execute steps 3–11 below):*

3. Capture scene info:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_info", "Arguments": {} }
   ```
4. Create empty scene for the SubScene:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_new", "Arguments": { "saveFirst": true } }
   ```
5. Inspect hierarchy to identify default Camera and Light:
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
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": { "path": "Assets/Scenes/<SceneName>_entities.unity" } }
   ```
8. Re-open Main Scene:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/<SceneName>.unity", "saveFirst": true } }
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
    { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_set_reference", "Arguments": { "path": "Entities", "componentType": "Unity.Scenes.SubScene", "propertyName": "_SceneAsset", "assetPath": "Assets/Scenes/<SceneName>_entities.unity" } }
    ```
12. Save Main Scene:
    ```json
    { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": {} }
    ```

### Phase 3: SubScene Authoring (Direct SubScene Workflow)
1. Open the SubScene file directly:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/<SceneName>_entities.unity", "saveFirst": true } }
   ```

2. Populate Authoring hierarchy:

#### Branch A: Visual Entity (Prefab-Backed)
*(Use for physical/visible actors: Player ships, enemies, projectiles, obstacles)*
- Instantiate the visual prefab asset directly into the SubScene:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_asset_instantiate_prefab", "Arguments": { "prefabPath": "Assets/Prefabs/<EntityPrefab>.prefab", "name": "<AuthoringGameObject>" } }
   ```
- Attach the Authoring `MonoBehaviour` (with Baker) to the instantiated prefab:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_add", "Arguments": { "gameObjectPath": "<AuthoringGameObject>", "componentType": "<AuthoringComponent>" } }
   ```

#### Branch B: Pure Data / Non-Visual Entity (Explicitly Empty)
*(Use ONLY for purely logical entities: Spawners, Config Singletons, Zone Triggers)*
- Create empty primitive GameObject:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_gameobject_create", "Arguments": { "name": "<AuthoringGameObject>", "primitiveType": "Empty" } }
   ```
- Attach the Authoring `MonoBehaviour` (with Baker):
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_add", "Arguments": { "gameObjectPath": "<AuthoringGameObject>", "componentType": "<AuthoringComponent>" } }
   ```

> [!CAUTION]
> **Visual Confirmation Gate**: If an entity represents a visible gameplay actor and no visual prefab is specified, DO NOT default to Branch B. Pause and confirm the visual prefab path with the user or search via `unity_search_assets`.

3. Wire Prefab / Material asset references:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_set_reference", "Arguments": { "path": "<AuthoringGameObject>", "componentType": "<AuthoringComponent>", "propertyName": "<PrefabField>", "assetPath": "Assets/Prefabs/<PrefabName>.prefab" } }
   ```
4. Set scalar / numeric configuration properties:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_set_property", "Arguments": { "gameObjectPath": "<AuthoringGameObject>", "componentType": "<AuthoringComponent>", "propertyName": "<ParameterField>", "value": "<Value>" } }
   ```
5. Save SubScene:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": {} }
   ```

### Phase 4: Main Scene Managed Setup & Re-linking
1. Re-open Main Scene:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "path": "Assets/Scenes/<SceneName>.unity", "saveFirst": true } }
   ```
2. Configure or verify managed GameObjects (Camera, Directional Light, UI Canvas, Companion GameObjects).
3. Save Main Scene:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_save", "Arguments": {} }
   ```

### Phase 5: Anti-Thrashing Circuit Breaker
- Never write throwaway runtime C# scripts (`[MenuItem]`) in `Assets/` to spawn scenes.
- If an MCP tool call fails or produces an unexpected hierarchy, **STOP immediately**.
- Do NOT enter multi-turn guessing loops. Report the issue and ask the user whether to rollback to the checkpoint or proceed manually via the Tier 1 recipe.

---

## 5. Visual & Baking Play Mode Verification (DOTS Reality)

With the Main Scene open and saved:
1. Enter Play Mode:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_play_mode", "Arguments": { "action": "play" } }
   ```
2. Check the console for baking or runtime errors:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_console_log", "Arguments": { "log_types": ["Error", "Exception"] } }
   ```
3. Capture visual Scene View confirmation screenshot:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_screenshot_scene", "Arguments": { "path": "Assets/Screenshots/<SceneName>_SceneView.png" } }
   ```
4. Capture visual Game View HUD confirmation screenshot:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_screenshot_game", "Arguments": { "path": "Assets/Screenshots/<SceneName>_GameView.png" } }
   ```
5. **Visual Mesh Audit**: Inspect screenshots to verify that the 3D entity geometry/mesh is clearly rendered in the scene at expected coordinates (not just an invisible empty transform).
6. Exit Play Mode:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_play_mode", "Arguments": { "action": "stop" } }
   ```

## 6. Completion Report
Report completion to the user/orchestrator with:
- Summary of scene modifications separated by Main Scene and SubScene scopes.
- Entity Visual Type confirmation (Visual Prefab vs Pure Data).
- Baking log verification (0 errors).
- Visual screenshot artifacts confirming entity rendering.
