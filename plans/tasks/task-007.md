# Task-007: Score Management & Hybrid uGUI Bridge

**Status:** `READY_FOR_IMPLEMENTATION`  
**Assignee:** Implementer (Local Qwen / Cloud Gemini)  
**Reviewer:** Reviewer Persona  
**Target Entities Version:** Unity Entities 1.4+ (Unity 6.6)  

---\n
## Section 1: Objective & Scope
* **Objective:** Bridge the unmanaged `GameScore` singleton into managed Unity UI (uGUI) using a managed `SystemBase` running in `PresentationSystemGroup`, updating a screen-space Canvas score counter with minimal string allocations.
* **In Scope:**
  - Managed `ScoreUIBridgeSystem` (`SystemBase`) in `PresentationSystemGroup`.
  - Managed `ScoreUIAuthoring` / `ScoreDisplayView` `MonoBehaviour` providing the UI Text / TextMeshPro reference to the bridge.
  - String caching / dirty checking to avoid garbage collection allocations when score has not changed.
* **Out of Scope:**
  - Firing or collision logic (handled in `task-006`).
  - Menu navigation or pause menus.

---

## Section 2: Type & API Contracts

### Target Files
* `Assets/Scripts/Authoring/ScoreDisplayView.cs`
* `Assets/Scripts/Systems/ScoreUIBridgeSystem.cs`

### UI View (`ScoreDisplayView.cs`)
```csharp
namespace Asteroids.Core
{
    using UnityEngine;
    using TMPro;

    public class ScoreDisplayView : MonoBehaviour
    {
        public static ScoreDisplayView Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _scoreText;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"SCORE: {score:D6}";
            }
        }
    }
}
```

### System Signature (`ScoreUIBridgeSystem.cs`)
```csharp
namespace Asteroids.Core
{
    using Unity.Entities;

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ScoreUIBridgeSystem : SystemBase
    {
        private int _lastDisplayedScore = -1;

        protected override void OnCreate()
        {
            RequireForUpdate<GameScore>();
        }

        protected override void OnUpdate()
        {
            if (ScoreDisplayView.Instance == null) return;

            var score = SystemAPI.GetSingleton<GameScore>();
            if (score.CurrentScore != _lastDisplayedScore)
            {
                _lastDisplayedScore = score.CurrentScore;
                ScoreDisplayView.Instance.SetScore(_lastDisplayedScore);
            }
        }
    }
}
```

### Visual & Scene Rigging Contracts
*(Defines how the managed UI is assembled in the Main Scene)*
* **Target Scope**: `Scope 1: Main Scene` (`Assets/Scenes/Asteroids.unity`)
* **Entity Visual Type**:
  - [x] **Managed UI Companion (Hybrid Bridge)**:
    - **Hierarchy Path**: `[Main Scene] -> UI Canvas -> ScoreText`
    - **Canvas Configuration**: `Render Mode = Screen Space - Overlay`, `CanvasScaler` (`Scale With Screen Size`, `1920x1080`), `GraphicRaycaster`.
    - **Text Component**: `TextMeshProUGUI` positioned at top-left anchor (`Pos X: 120, Pos Y: -60`, Font Size `36`).
    - **Authoring Component**: `ScoreDisplayView` attached to `ScoreText` GameObject with serialized `_scoreText` wired to its own `TextMeshProUGUI` component.
  - [ ] **Pure Data / Non-Visual Entity (Explicitly Empty)**:
* **SubScene Bridge Dependency**:
  - `ScoreUIBridgeSystem` queries the unmanaged `GameScore` singleton baked from `ScoreManager` in `Assets/Scenes/Asteroids_entities.unity`.

---

## Section 3: Injected OKF Patterns & Anti-Pattern Warnings

### Injected OKF Pattern: `hybrid-ecs`
*Source: `concepts/hybrid-ecs.md`*
* **SystemBase for Managed UI**: While core simulation belongs in Burst `ISystem` structs, managed engine frameworks like uGUI / TextMeshPro must be driven via managed `SystemBase` in `PresentationSystemGroup`.
* **String Allocation Discipline**: Check `score.CurrentScore != _lastDisplayedScore` to avoid allocating string heap garbage every single frame.

### Anti-Pattern Warnings
* ⛔ **NO `[BurstCompile]` ON `SystemBase`**: `SystemBase` classes run on the managed runtime; never add `[BurstCompile]`.
* ⛔ **DO NOT CALL `UnityEngine.Object` METHODS WITHOUT QUALIFICATION**: Prefix static engine methods like `Destroy` with `UnityEngine.Object.` when needed inside `SystemBase`.

---

## Section 4: Developer Definition of Done (DoD)
- [x] Code implemented strictly within Section 2 target paths.
- [x] Clean compilation verified via Unity MCP (`unity_get_compilation_errors` = **0 errors**).
- [x] Zero Burst compiler warnings.

---

## Section 5: Reviewer Runtime & Style Checklist
- [x] **Dirty Checking**: Score text only updates when `CurrentScore` changes.
- [x] **System Group**: Updates strictly within `PresentationSystemGroup`.
- [x] **Null Safety**: Gracefully handles absence of `ScoreDisplayView` instance without throwing exceptions.
- [ ] **Visual & Rigging Audit**: Verified `ScoreDisplayView` is attached to `ScoreText` under the Screen-Space Canvas in `Assets/Scenes/Asteroids.unity`.

---

## Actionable Implementation Checklist
- [x] Step 1: Implement `ScoreDisplayView.cs` in `Assets/Scripts/Authoring/`.
- [x] Step 2: Implement `ScoreUIBridgeSystem.cs` in `Assets/Scripts/Systems/`.
- [x] Step 3: Verify clean compilation and 0 Burst warnings via `anklebreaker-unity-mcp`.

---

## Reviewer Sign-off & Verdict
* **Review Date:** `YYYY-MM-DD`
* **Verdict:** `PENDING`
* **Findings:**
  * [Notes or verification logs here]
