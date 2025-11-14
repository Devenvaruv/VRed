# Task System Refactor Plan

Goal: decouple UI from data, move to event-driven updates, and make tasks data-driven with stable IDs (not names). Keep changes incremental and Unity-friendly.

## Scope
- Current files touched: `Assets/Scripts/TaskManager.cs`, `Assets/Scripts/TaskCanvasUI.cs`.
- New assets/scripts: ScriptableObject data under `Assets/Data/Tasks`, optional presenter `Assets/Scripts/TaskPresenter.cs`.

## Milestones
1) Add TaskManager events
2) Update UI to observe events
3) Introduce ScriptableObject data
4) Refactor to ID-based API
5) Add presenter for UI text
6) Scene wiring + smoke test

## Architecture Target
- Service (TaskManager/TaskService): owns runtime state and raises events.
- Data (ScriptableObjects): authorable `TaskListAsset` -> `TaskData` -> `SubTaskData` with GUID IDs.
- Presentation: lightweight presenter builds display strings; `TaskCanvasUI` stays as view.
- Contracts: use IDs for lookups; keep name-based overloads for transition.

---

## Phase 1 — Add TaskManager events

Changes
- Add events:
  - `public event Action<Task, SubTask> SubTaskCompleted;`
  - `public event Action<Task> TaskCompleted;`
- Invoke after setting `sub.isCompleted = true` and when a task becomes fully complete.
- Guard against duplicate firing if called repeatedly.

Acceptance
- Both events fire exactly once per completion.
- Existing behavior/logging remains intact.

Notes
- Keep current in-memory initialization for now.

---

## Phase 2 — Update UI to observe events

Changes
- In `TaskCanvasUI`, subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- Handlers: call `UpdateTaskDisplay()`; keep toggle positioning behavior.
- Optional: debounce if performance becomes a concern (not expected).

Acceptance
- UI updates automatically on sub/task completion without manual refresh calls.

---

## Phase 3 — Introduce ScriptableObject data

Data Types (new)
- `TaskListAsset` (`CreateAssetMenu`): `List<TaskData>`.
- `TaskData`: `string id`, `string name`, `string description`, `List<SubTaskData>`.
- `SubTaskData`: `string id`, `string name`, `string description`, `string hint`.

Changes
- Add optional `TaskListAsset` reference on TaskManager.
- At startup, if asset assigned, hydrate runtime model from asset; else fallback to existing hardcoded list.
- Provide editor tooling: context button to generate GUIDs if blank.

Acceptance
- Content can be authored in the Editor without code changes.
- Runtime state mirrors authored data.

---

## Phase 4 — Refactor to ID-based API

Changes
- Add IDs to runtime `Task`/`SubTask` classes.
- New APIs: `CompleteSubTask(string taskId, string subTaskId)`.
- Keep name-based overloads that delegate to ID-based lookup for backward compatibility.
- Internally, store lookups in dictionaries keyed by ID.

Acceptance
- All internal logic uses IDs; name lookups are thin wrappers.
- No behavior regressions with existing callers.

---

## Phase 5 — Add presenter for UI text

Changes
- New pure C# `TaskPresenter` with a method like `BuildText(IEnumerable<Task>) : string`.
- Move string building and icon selection from `TaskCanvasUI` to the presenter.
- `TaskCanvasUI` becomes a thin view: gets tasks from manager, calls presenter, assigns `taskText.text`.

Acceptance
- Display output matches current format.
- `TaskCanvasUI` loses formatting responsibilities, easier to test/maintain.

---

## Phase 6 — Scene wiring + smoke test

Checklist
- Assign `TaskListAsset` in Inspector (optional during transition).
- Ensure `TaskCanvasUI` has reference to the `TaskManager` (or via bootstrap).
- Verify: toggle opens/closes, positions near head, disables locomotion, and updates when completing subtasks.

Acceptance
- Manual test path works end-to-end in Play Mode.

---

## Stretch (Optional)
- Persistence: save completion state to `PlayerPrefs` or JSON.
- Unit tests: presenter formatting and completion state transitions.
- Composite model: if deeper nesting is needed beyond subtasks.

## Risks & Mitigations
- Event duplication: guard with checks and set-after-fire patterns.
- Asset drift: keep fallback to hardcoded data during migration.
- String/icon rendering differences: add a snapshot test for presenter output.

## Next Actions
1) Implement Phase 1 (events) in `TaskManager.cs`.
2) Wire Phase 2 (UI observers) in `TaskCanvasUI.cs`.
3) Pause for review before introducing ScriptableObjects.

