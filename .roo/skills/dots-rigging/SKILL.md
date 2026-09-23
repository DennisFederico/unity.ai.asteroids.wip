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

## 1. Scope Analysis & OKF Guidance
1. Read the active task contract or scene requirement.
2. Query the knowledge base via `ecs-knowledge`:
   - Subscene architecture $\rightarrow$ `concepts/ecs-scene-architecture.md`
   - Baker authoring patterns $\rightarrow$ `concepts/baking.md`
   - **Agentic assembly playbook $\rightarrow$ `concepts/agentic-scene-assembly.md` (Mandatory guidance for multi-editor port routing, instance selection, and deterministic tool call sequences)**

## 2. Mandatory Safety Checkpoint
Before performing any batch scene modifications via Unity MCP:
- Verify that a clean Git checkpoint (commit, branch, or stash) exists:
  ```powershell
  git status
  ```
- If uncommitted changes exist, checkpoint them first (unless explicitly vetoed by the user) to ensure effortless rollback if scene operations diverge.

## 3. Tier 1: Formulate the Authoring Recipe
Always produce a human-readable setup recipe before touching the Editor:
- **Target Scene & Subscene**: Name and asset path.
- **Hierarchy Tree**: Exact parent-child GameObject layout (e.g. `[SubScene: GameWorld] -> Spawners -> AsteroidSpawner`).
- **Components & Values**: Exact Authoring components to attach and serialized field values.
- **Prefab References**: Asset paths required for entity instantiators.
*(This recipe serves as the plan for MCP execution AND as a manual fallback walkthrough for the user).*

## 4. Tier 2: Gated Unity MCP Execution (Following `concepts/agentic-scene-assembly.md`)
Execute operations step-by-step using `anklebreaker-unity-mcp`:

1. **Editor Discovery & Connection**:
   - Call `unity_list_instances` to identify the active editor matching this workspace.
   - Call `unity_select_instance` (or pass the instance port) and ping via `unity_editor_ping` or `unity_editor_state` to ensure the editor is ready.
2. **Canonical SubScene Assembly Sequence**:
   - Follow the deterministic sequence documented in `concepts/agentic-scene-assembly.md`:
     - **Create SubScene Asset First**:
       - `unity_scene_new({ "discardUnsavedChanges": true })`.
       - **SubScene Hygiene (Mandatory)**: Delete default `Main Camera` and `Directional Light` via `unity_gameobject_delete` so the SubScene is 100% empty of environment objects.
       - Populate authoring hierarchy using `unity_asset_instantiate_prefab` and `unity_gameobject_create`.
       - Attach Authoring components using `unity_component_add` (`gameObjectPath`, `componentType`).
       - Wire asset/prefab references using `unity_component_set_reference` (`path`, `componentType`, `propertyName`, `assetPath`).
       - Save SubScene: `unity_scene_save({ "path": "Assets/Scenes/<SceneName>_entities.unity" })`.
     - **Create Main Scene Asset**:
       - `unity_scene_new({ "saveFirst": true })`.
       - Configure Main Camera position/rotation via `unity_gameobject_set_transform` (ensure `UniversalAdditionalCameraData` exists in URP).
       - Configure Directional Light.
       - Create UI Canvas, EventSystem, and managed companion scripts.
     - **Link and Open SubScene**:
       - Create anchor: `unity_gameobject_create({ "name": "<SceneName>_Entities", "primitiveType": "Empty" })`.
       - Attach component: `unity_component_add({ "gameObjectPath": "<SceneName>_Entities", "componentType": "Unity.Scenes.SubScene" })`.
       - Wire reference: `unity_component_set_reference({ "path": "<SceneName>_Entities", "componentType": "Unity.Scenes.SubScene", "propertyName": "_SceneAsset", "assetPath": "Assets/Scenes/<SceneName>_entities.unity" })`.
       - Save scene: `unity_scene_save({ "path": "Assets/Scenes/<SceneName>.unity" })`.
       - **Open SubScene in Hierarchy**: Call `unity_execute_code` with `SubSceneUtility.EditScene` to load the SubScene additively so its contents display in Scene View and live-bake into the Entity World:
         ```csharp
         var subScene = UnityEngine.Object.FindFirstObjectByType<Unity.Scenes.SubScene>();
         var t = System.Type.GetType("Unity.Scenes.Editor.SubSceneUtility, Unity.Scenes.Editor");
         t.GetMethod("EditScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { new Unity.Scenes.SubScene[] { subScene } });
         ```
       - Save again to lock the open state: `unity_scene_save({ "path": "Assets/Scenes/<SceneName>.unity" })`.
3. **Core Rule: Zero Disposable Editor Scripts**:
   - Never write throwaway runtime C# scripts (e.g. `[MenuItem]`) in `Assets/` to spawn scenes. Use native MCP commands.
4. **Anti-Thrashing Circuit Breaker**:
   - If an MCP tool call fails or produces an unexpected hierarchy, **STOP immediately**.
   - Do NOT enter multi-turn guessing loops. Report the failure and ask the user whether to rollback to the checkpoint or continue manually via the Tier 1 recipe.

## 5. Visual & Baking Play Mode Verification (DOTS Reality)
Because baked ECS entities are live-converted at runtime:
1. Enter Play Mode:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_play_mode", "Arguments": { "action": "play" } }
   ```
2. Check the console for baking or Burst errors:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_console_log", "Arguments": { "type": "error" } }
   ```
3. Capture a visual Scene View confirmation screenshot:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_screenshot_scene", "Arguments": { "path": "Assets/Screenshots/<SceneName>_SceneView.png" } }
   ```
4. Capture a Game View HUD confirmation screenshot:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_screenshot_game", "Arguments": { "path": "Assets/Screenshots/<SceneName>_GameView.png" } }
   ```
5. Exit Play Mode:
   ```json
   { "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_play_mode", "Arguments": { "action": "stop" } }
   ```

## 6. Completion Report
Report completion to the user/orchestrator with:
- Summary of scene modifications.
- Baking log verification (0 errors).
- Visual screenshot artifact confirming entity rendering.

