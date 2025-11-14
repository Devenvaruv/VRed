using UnityEngine;

public class PlayerTeleporter : MonoBehaviour
{
    [Header("Assign")]
    public Transform playerRig;      // XR Origin / rig root
    public Transform headCamera;     // HMD camera (child of rig)
    public Transform targetTransform;// Where the HEAD should end up

    [Header("Options")]
    public bool teleportOnStart = false;
    public bool onlyYaw = true;      // ignore pitch/roll when aligning

    void Start()
    {
        if (teleportOnStart) TeleportPlayer();
    }

    public void TeleportPlayer()
    {
        if (!playerRig || !headCamera || !targetTransform)
        {
            Debug.LogWarning("Assign playerRig, headCamera, targetTransform.");
            return;
        }

        // --- 1) ROTATION: rotate rig around the HEAD so head doesn’t drift ---
        float currentHeadYaw = headCamera.rotation.eulerAngles.y;
        Vector3 targetEuler = targetTransform.rotation.eulerAngles;
        float desiredYaw = onlyYaw ? targetEuler.y : targetEuler.y; // yaw-only by default
        float yawDelta = desiredYaw - currentHeadYaw;

        Quaternion deltaRot = onlyYaw
            ? Quaternion.Euler(0f, yawDelta, 0f)
            : Quaternion.Inverse(headCamera.rotation) * targetTransform.rotation;

        RotateAroundPoint(playerRig, headCamera.position, deltaRot);

        // --- 2) POSITION: place rig so the HEAD world pos == target world pos ---
        Vector3 rigToHead = headCamera.position - playerRig.position;     // after rotation
        Vector3 newRigPos = targetTransform.position - rigToHead;

        // Temporarily relax blockers
        var cc = playerRig.GetComponent<CharacterController>();
        var rb = playerRig.GetComponent<Rigidbody>();
        bool ccWas = false; Vector3 savedV = Vector3.zero, savedW = Vector3.zero;
        if (cc) { ccWas = cc.enabled; cc.enabled = false; }
        if (rb) { savedV = rb.linearVelocity; savedW = rb.angularVelocity; rb.isKinematic = true; }

        playerRig.position = newRigPos;

        if (rb) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        if (cc) cc.enabled = ccWas;
    }

    // Rotate a transform around a world-space pivot by a world-space rotation
    private void RotateAroundPoint(Transform t, Vector3 pivot, Quaternion q)
    {
        Vector3 dir = t.position - pivot;
        t.position = pivot + q * dir;
        t.rotation = q * t.rotation;
    }
}
