# Authoring Recipe: [Scene / Subscene / Feature Name]

**Target Scene:** `Assets/Scenes/[SceneName].unity`  
**Target SubScene:** `[SubSceneName]` (`Assets/Scenes/SubScenes/[SubSceneName].unity`)  
**Safety Checkpoint:** `git rev-parse --short HEAD` (Verified clean working tree)  
**Reference Playbook:** `concepts/agentic-scene-assembly.md`  

---

## Tier 1: Structured Hierarchy & Inspector Recipe
*(Manual walkthrough for human developer OR blueprint for Tier 2 MCP automation)*

```
[Main Scene: [SceneName].unity]
 └── [SubScene: [SubSceneName]]
      ├── [EntityCategory / Group]
      │    └── [AuthoringGameObject]
      │         ├── Transform: Position([X], [Y], [Z]), Rotation([X], [Y], [Z]), Scale([X], [Y], [Z])
      │         └── [AuthoringComponent] (Script)
      │              ├── [PrefabReferenceField]: Assets/Prefabs/[EntityPrefab].prefab
      │              ├── [NumericParameterField]: [Value]
      │              └── [ConfigurationField]: [Value]
      └── [CompanionGameObject (Optional Hybrid)]
           └── [CompanionComponent] (Script)
```

### Component Wiring Details
| GameObject Path | Component to Add | Serialized Field | Value / Asset Reference |
| :--- | :--- | :--- | :--- |
| `[SubSceneName]/[Group]/[AuthoringGameObject]` | `[AuthoringComponent]` | `[PrefabField]` | `Assets/Prefabs/[PrefabName].prefab` |
| `[SubSceneName]/[Group]/[AuthoringGameObject]` | `[AuthoringComponent]` | `[ParameterField]` | `[Numeric or String Value]` |

---

## Tier 2: Executable Unity MCP Command Sequence
*(Presented to user for approval before running)*

### 1. Editor Connectivity & Selection
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_list_instances", "Arguments": {} }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_select_instance", "Arguments": { "instance_id": "[TargetInstanceId]" } }
```

### 2. Scene Preparation
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_scene_open", "Arguments": { "scene_path": "Assets/Scenes/[SceneName].unity" } }
```

### 3. Hierarchy & Authoring Setup
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_gameobject_create", "Arguments": { "name": "[AuthoringGameObject]" } }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_gameobject_reparent", "Arguments": { "child_name": "[AuthoringGameObject]", "parent_name": "[GroupOrSubScene]" } }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_add", "Arguments": { "game_object_name": "[AuthoringGameObject]", "component_type": "[AuthoringComponent]" } }
```
```json
{ "ServerName": "anklebreaker-unity-mcp", "ToolName": "unity_component_set_property", "Arguments": { "game_object_name": "[AuthoringGameObject]", "component_type": "[AuthoringComponent]", "property_name": "[ParameterField]", "value": "[Value]" } }
```

---

## Baking & Visual PlayMode Verification
- [ ] Save scene: `unity_scene_save`
- [ ] Enter Play Mode (start paused): `unity_play_mode` (`action: "play"`)
- [ ] Check console for baking logs: `unity_console_log` (0 errors)
- [ ] Capture visual screenshot: `unity_screenshot_game`
- [ ] Exit Play Mode: `unity_play_mode` (`action: "stop"`)
- [ ] Hierarchy verified matching Tier 1 recipe

