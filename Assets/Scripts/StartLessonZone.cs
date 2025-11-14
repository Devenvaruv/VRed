using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

[RequireComponent(typeof(Collider))]
public class StartLessonZoneMinimal : MonoBehaviour
{
    [Header("Who can trigger")]
    [Tooltip("XR camera / head transform. If empty, uses Camera.main.")]
    public Transform headTransform;

    [Header("Prompt UI (world-space)")]
    public GameObject promptCanvas; // root canvas (world-space)
    public TextMeshProUGUI titleText; // optional
    public TextMeshProUGUI bodyText; // optional
    public Button startButton; // required
    public Button cancelButton; // optional

    [Header("Prompt placement")]
    public float distance = 1.4f;
    public float verticalOffset = -0.05f;
    public bool faceHead = true;
    public bool avoidWalls = true;
    public float minDistance = 0.6f;
    public float maxDistance = 2.0f;
    public LayerMask wallMask = ~0;

    [Header("Ground clamp (avoid underground)")]
    public bool clampAboveGround = true;
    public LayerMask groundMask = ~0;
    public float groundClearance = 0.05f;

    [Header("Zone visuals (optional)")]
    public GameObject glowIndicator; // e.g., a ring on the floor

    [Header("Locomotion to lock (do NOT assign interactors here)")]
    public LocomotionProvider[] locomotionProviders; // Move/Turn/Teleport providers

    [Header("Gating")]
    [Tooltip("If true, prompt only shows when the HEAD is inside the trigger (ignores hands).")]
    public bool requireHeadInside = true;

    [Header("Behavior")]
    [Tooltip(
        "If true, the prompt auto-appears while the head is inside the zone (works even without trigger events)."
    )]
    public bool showWhileInside = false;

    [Header("Events")]
    public UnityEvent onPromptShown;
    public UnityEvent onLessonStarted;
    public UnityEvent onLessonCancelled;
    public UnityEvent onLessonCompleted;

    private bool promptOpen = false;
    private bool lessonActive = false;
    private Transform head;
    private Collider zoneCol;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col)
            col.isTrigger = true; // this script expects a trigger
    }

    void Awake()
    {
        if (promptCanvas)
            promptCanvas.SetActive(false);
        if (startButton)
            startButton.onClick.AddListener(StartLesson);
        if (cancelButton)
            cancelButton.onClick.AddListener(CancelPrompt);
    }

    void Start()
    {
        if (!headTransform && Camera.main)
            headTransform = Camera.main.transform;
        head = headTransform;

        if (locomotionProviders == null || locomotionProviders.Length == 0)
            locomotionProviders = FindObjectsOfType<LocomotionProvider>(true);

        zoneCol = GetComponent<Collider>();
    }

    void Update()
    {
        if (!requireHeadInside)
            return;
        bool inside = IsHeadInsideZone();

        if (showWhileInside && !lessonActive)
        {
            // Auto-appear/disappear while inside (good for XR Device Simulator)
            if (inside && !promptOpen)
            {
                ShowPrompt();
                if (glowIndicator)
                    glowIndicator.SetActive(true);
            }
            else if (!inside && promptOpen)
            {
                HidePrompt();
                if (glowIndicator)
                    glowIndicator.SetActive(false);
            }
        }
        else
        {
            // Safety: if prompt is open and you step out, hide it
            if (promptOpen && !inside)
            {
                HidePrompt();
                if (glowIndicator)
                    glowIndicator.SetActive(false);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (lessonActive)
            return;
        if (!IsPlayer(other))
            return;

        if (requireHeadInside && !IsHeadInsideZone())
            return; // ignore hand/body pieces unless head is inside

        if (!showWhileInside) // triggers handle the first-show in this mode
        {
            ShowPrompt();
            if (glowIndicator)
                glowIndicator.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (!lessonActive && !showWhileInside)
        {
            HidePrompt();
            if (glowIndicator)
                glowIndicator.SetActive(false);
        }
        // If lesson is active, we remain locked until CompleteLesson() is called.
    }

    // ── Public: call when your task is done ────────────────────────────────
    public void CompleteLesson()
    {
        if (!lessonActive)
            return;
        ToggleLocomotion(true);
        lessonActive = false;
        onLessonCompleted?.Invoke();
    }

    // ── UI handlers ────────────────────────────────────────────────────────
    private void StartLesson()
    {
        if (lessonActive)
            return;
        lessonActive = true;
        HidePrompt();
        if (glowIndicator)
            glowIndicator.SetActive(false);

        ToggleLocomotion(false); // lock move/turn/teleport ONLY
        onLessonStarted?.Invoke(); // start your task logic (UI, steps, etc.)
    }

    private void CancelPrompt()
    {
        if (lessonActive)
            return;
        HidePrompt();
        onLessonCancelled?.Invoke();
    }

    // ── Prompt helpers ─────────────────────────────────────────────────────
    private void ShowPrompt()
    {
        if (!head)
            head = headTransform ? headTransform : (Camera.main ? Camera.main.transform : null);
        if (!head || !promptCanvas)
            return;

        if (titleText && string.IsNullOrEmpty(titleText.text))
            titleText.text = "Start Lesson?";
        if (bodyText && string.IsNullOrEmpty(bodyText.text))
            bodyText.text = "Begin this activity? Movement will be locked until you finish.";

        PlacePrompt();
        promptCanvas.SetActive(true);
        promptOpen = true;
        onPromptShown?.Invoke();
    }

    private void HidePrompt()
    {
        if (promptCanvas)
            promptCanvas.SetActive(false);
        promptOpen = false;
    }

    private void PlacePrompt()
    {
        // Use horizontal forward to avoid pitching into floor/ceiling
        Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (fwd.sqrMagnitude < 1e-4f)
            fwd = head.right; // fallback when looking straight up/down
        fwd.Normalize();

        float d = Mathf.Clamp(distance, minDistance, maxDistance);

        // Wall avoidance along that horizontal ray
        if (
            avoidWalls
            && Physics.Raycast(
                new Ray(head.position, fwd),
                out var hit,
                distance,
                wallMask,
                QueryTriggerInteraction.Ignore
            )
        )
            d = Mathf.Clamp(hit.distance - 0.05f, minDistance, maxDistance);

        Vector3 pos = head.position + fwd * d;
        pos.y = head.position.y + verticalOffset;

        // Clamp above ground so it never sinks under the floor
        if (clampAboveGround)
        {
            if (
                Physics.Raycast(
                    new Vector3(pos.x, pos.y + 2f, pos.z),
                    Vector3.down,
                    out var gHit,
                    4f,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                )
            )
                pos.y = Mathf.Max(pos.y, gHit.point.y + groundClearance);
        }

        promptCanvas.transform.position = pos;

        if (faceHead)
        {
            Vector3 toHead = (head.position - pos);
            toHead.y = 0f;
            if (toHead.sqrMagnitude < 1e-4f)
                toHead = -fwd;
            toHead = -toHead;
            promptCanvas.transform.rotation = Quaternion.LookRotation(
                toHead.normalized,
                Vector3.up
            );
        }
    }

    // ── Locking logic (ONLY locomotion) ────────────────────────────────────
    private void ToggleLocomotion(bool enable)
    {
        if (locomotionProviders == null)
            return;
        foreach (var p in locomotionProviders)
            if (p)
                p.enabled = enable;
    }

    // ── Player checks ──────────────────────────────────────────────────────
    private bool IsPlayer(Collider other)
    {
        // same rig root as head? good.
        if (head && other.transform.root == head.root)
            return true;
        return head == null; // if unknown, allow (editor testing)
    }

    private bool IsHeadInsideZone()
    {
        if (!head || !zoneCol)
            return false;

        if (zoneCol is BoxCollider bc)
        {
            // Convert head position into the box's local space
            Vector3 lp = bc.transform.InverseTransformPoint(head.position) - bc.center;
            Vector3 half = bc.size * 0.5f;
            return Mathf.Abs(lp.x) <= half.x
                && Mathf.Abs(lp.y) <= half.y
                && Mathf.Abs(lp.z) <= half.z;
        }
        if (zoneCol is SphereCollider sc)
        {
            Vector3 lp = sc.transform.InverseTransformPoint(head.position) - sc.center;
            float maxScale = Mathf.Max(
                Mathf.Abs(sc.transform.lossyScale.x),
                Mathf.Abs(sc.transform.lossyScale.y),
                Mathf.Abs(sc.transform.lossyScale.z)
            );
            float r = sc.radius * maxScale;
            return lp.sqrMagnitude <= r * r;
        }

        // Fallback for other collider types
        return zoneCol.bounds.Contains(head.position);
    }
}
