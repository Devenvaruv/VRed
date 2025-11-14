using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

using System.Text;

public class TaskCanvasUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject taskCanvas;          // World Space Canvas root
    public TextMeshProUGUI taskText;       // Main text area

    [Header("XR Control (disable while menu open)")]
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider[] locomotionProviders; // e.g., ContinuousMoveProvider, SnapTurnProvider, ContinuousTurnProvider, TeleportationProvider
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor leftRayInteractor;       // Near-Far (XR Ray Interactor) on Left Controller
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor rightRayInteractor;      // Near-Far (XR Ray Interactor) on Right Controller

    [Header("Positioning")]
    public Transform headTransform;        // XR camera/head. If null, will try Camera.main
    public float defaultDistance = 1.5f;   // meters
    public float verticalOffset = -0.1f;   // slightly below eye level
    public bool recenterIfAlreadyOpen = true;
    public bool useWallAvoidance = true;
    public float minDistance = 0.6f;
    public float maxDistance = 2.0f;
    public LayerMask wallMask = ~0;        // which layers count as walls

    [Header("Input System (Resources/Test.inputactions)")]
    public string resourcesAssetName = "Test";            // Resources/Test.inputactions
    public string actionPath = "RightHand/PrimaryButton"; // Map/Action e.g., RightHand/PrimaryButton

    private bool isVisible = false;
    private TaskManager taskManager;

    // Input System
    private InputActionAsset inputAsset;
    private InputAction toggleAction;

    void Awake()
    {
        // Load Input Actions from Resources
        inputAsset = Resources.Load<InputActionAsset>(resourcesAssetName);
        if (inputAsset == null)
        {
            Debug.LogError($"[TaskCanvasUI] Could not load InputActionAsset at Resources/{resourcesAssetName}.inputactions");
            return;
        }

        toggleAction = inputAsset.FindAction(actionPath);
        if (toggleAction == null)
        {
            Debug.LogError($"[TaskCanvasUI] Action '{actionPath}' not found in InputActionAsset.");
            return;
        }
        toggleAction.Enable();
    }

    void OnEnable()
    {
        if (toggleAction != null)
            toggleAction.performed += OnTogglePressed;
    }

    void OnDisable()
    {
        if (toggleAction != null)
            toggleAction.performed -= OnTogglePressed;
    }

    void Start()
    {
        if (headTransform == null && Camera.main != null)
            headTransform = Camera.main.transform;

        taskManager = GetComponent<TaskManager>();
        if (taskCanvas != null) taskCanvas.SetActive(false);

        TryAutoWire();      // Optional auto-detection for providers & rays
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        if (taskCanvas == null) return;

        if (!isVisible)
        {
            isVisible = true;
            taskCanvas.SetActive(true);
            PositionCanvasInFrontOfHead();
            DisableMovementAndInteractors();
            UpdateTaskDisplay();
        }
        else
        {
            if (recenterIfAlreadyOpen)
            {
                PositionCanvasInFrontOfHead();
                UpdateTaskDisplay();
            }
            else
            {
                isVisible = false;
                taskCanvas.SetActive(false);
                EnableMovementAndInteractors();
            }
        }
    }

    private void UpdateTaskDisplay()
    {
        if (taskManager == null || taskText == null) return;

        var sb = new StringBuilder(256);
        var tasks = taskManager.GetTasks();

        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            bool done = task.IsCompleted();
            sb.Append("<b>").Append(done ? "✔️ " : "❌ ").Append(task.taskName).Append("</b>\n");
            sb.Append("   ").Append(task.description).Append("\n");

            for (int j = 0; j < task.subTasks.Count; j++)
            {
                var sub = task.subTasks[j];
                sb.Append("   ").Append(sub.isCompleted ? "✅ " : "🔲 ")
                  .Append(sub.subTaskName).Append(" — ")
                  .Append("<i>").Append(sub.description).Append("</i>\n");
            }

            var next = task.GetFirstIncomplete();
            if (next != null)
                sb.Append("   💡 Hint: ").Append(next.hint).Append("\n");

            sb.Append("\n");
        }

        taskText.text = sb.ToString();
    }

    private void PositionCanvasInFrontOfHead()
    {
        if (headTransform == null) return;

        // Use horizontal forward
        Vector3 forward = headTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = headTransform.forward.normalized;
        else forward.Normalize();

        float placeDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);

        if (useWallAvoidance)
        {
            if (Physics.Raycast(headTransform.position, forward, out var hit, defaultDistance, wallMask, QueryTriggerInteraction.Ignore))
            {
                placeDistance = Mathf.Clamp(hit.distance - 0.05f, minDistance, maxDistance);
            }
        }

        Vector3 spawnPos = headTransform.position + forward * placeDistance;
        spawnPos.y = headTransform.position.y + verticalOffset;

        taskCanvas.transform.position = spawnPos;
        taskCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void DisableMovementAndInteractors()
    {
        if (locomotionProviders != null)
            foreach (var p in locomotionProviders)
                if (p) p.enabled = false;

        if (leftRayInteractor)  leftRayInteractor.enabled  = false;
        if (rightRayInteractor) rightRayInteractor.enabled = false;
    }

    private void EnableMovementAndInteractors()
    {
        if (locomotionProviders != null)
            foreach (var p in locomotionProviders)
                if (p) p.enabled = true;

        if (leftRayInteractor)  leftRayInteractor.enabled  = true;
        if (rightRayInteractor) rightRayInteractor.enabled = true;
    }

    private void TryAutoWire()
    {
        // Auto-find locomotion providers if not assigned
        if (locomotionProviders == null || locomotionProviders.Length == 0)
            locomotionProviders = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(true);

        // Auto-find ray interactors if not assigned
        if (!leftRayInteractor || !rightRayInteractor)
        {
            var rays = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true);
            foreach (var r in rays)
            {
                var n = r.name.ToLower();
                if (!leftRayInteractor  && (n.Contains("left")  || n.EndsWith("_l"))) { leftRayInteractor  = r; continue; }
                if (!rightRayInteractor && (n.Contains("right") || n.EndsWith("_r"))) { rightRayInteractor = r; continue; }
            }

            // Fallback: just grab first two XRBaseInteractors if names aren’t clear
            if (!leftRayInteractor || !rightRayInteractor)
            {
                var any = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true);
                foreach (var a in any)
                {
                    if (!leftRayInteractor) { leftRayInteractor = a; continue; }
                    if (!rightRayInteractor && a != leftRayInteractor) { rightRayInteractor = a; break; }
                }
            }
        }
    }
}
