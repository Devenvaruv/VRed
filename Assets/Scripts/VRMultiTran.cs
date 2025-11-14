using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRMultiTransformTool : MonoBehaviour
{
    public enum TransformMode
    {
        Position,
        Rotation,
        Scale,
    }

    private TransformMode currentMode = TransformMode.Position;

    public XRBaseInteractor interactor;

    public InputActionAsset inputAsset;

    private InputAction triggerAction;
    private InputAction stickAction;
    private InputAction primaryButtonAction;
    private InputAction secondaryButtonAction;
    private float lastSwitchTime;
    private float cooldown = 0.3f;

    private bool yAxisMode = false; // false = Z-mode, true = Y-mode
    private float lastToggleTime = 0f;

    private Transform target;
    private XRGrabInteractable heldObject;

    public float moveSpeed = 1f;
    public float rotateSpeed = 90f;
    public float scaleSpeed = 1f;

    private Vector2 stickInput = Vector2.zero;

    void Awake()
    {
        inputAsset = Resources.Load<InputActionAsset>("Test"); // expects MyControls.inputactions in Resources/
        if (inputAsset == null)
        {
            Debug.LogError("Could not load InputActionAsset!");
            return;
        }

        triggerAction = inputAsset.FindAction("RightHand/TriggerButton");
        stickAction = inputAsset.FindAction("RightHand/Primary2DAxis");
        primaryButtonAction = inputAsset.FindAction("RightHand/PrimaryButton");
        secondaryButtonAction = inputAsset.FindAction("RightHand/SecondaryButton");

        triggerAction.Enable();
        stickAction.Enable();
        primaryButtonAction.Enable();
        secondaryButtonAction.Enable();

        triggerAction.performed += ctx =>
        {
            if (Time.time - lastSwitchTime > cooldown)
            {
                currentMode = (TransformMode)(((int)currentMode + 1) % 3);
                lastSwitchTime = Time.time;
                Debug.Log($"[MODE] Switched to: {currentMode}");
            }
        };

        primaryButtonAction.performed += ctx =>
        {
            if (Time.time - lastToggleTime > cooldown)
            {
                yAxisMode = !yAxisMode;
                lastToggleTime = Time.time;
                Debug.Log($"[TOGGLE] Y-Axis Mode: {yAxisMode}");
            }
        };

        // Optional: Track stick movement continuously
        stickAction.performed += ctx => stickInput = ctx.ReadValue<Vector2>();
        stickAction.canceled += ctx => stickInput = Vector2.zero;
    }

    void Update()
    {
        var selected = (interactor as IXRSelectInteractor)?.interactablesSelected.FirstOrDefault();
        heldObject = selected as XRGrabInteractable;
        target = heldObject ? heldObject.transform : null;
        if (target == null || stickInput == Vector2.zero)
            return;

        switch (currentMode)
        {
            case TransformMode.Position:
                HandlePosition();
                break;

            case TransformMode.Rotation:
                HandleRotation();
                break;

            case TransformMode.Scale:
                HandleScale();
                break;
        }
    }

    private void HandlePosition()
    {
        Vector3 moveDir = yAxisMode
            ? new Vector3(stickInput.x, stickInput.y, 0f) // Y-axis mode
            : new Vector3(stickInput.x, 0f, stickInput.y); // Z-axis mode

        target.position += moveDir * moveSpeed * Time.deltaTime;
        Debug.Log($"[MOVE] Dir = {moveDir}, Stick = {stickInput}");
    }

    private void HandleRotation()
    {
        Vector3 axis = yAxisMode
            ? new Vector3(stickInput.x, stickInput.y, 0f) // Y-axis mode
            : new Vector3(stickInput.x, 0f, stickInput.y); // Z-axis mode

        target.Rotate(axis, stickInput.x * rotateSpeed * Time.deltaTime, Space.Self);
        Debug.Log($"[ROTATE] Axis: {axis}, Value: {stickInput.x}");
    }

    private void HandleScale()
    {
        float scaleAmount = stickInput.x * scaleSpeed * Time.deltaTime;

        // Apply the same scale to all axes
        Vector3 scaleChange = new Vector3(scaleAmount, scaleAmount, scaleAmount);

        target.localScale += scaleChange;

        Debug.Log($"[SCALE] Uniform Scale Amount = {scaleAmount}, Stick X = {stickInput.x}");
    }
}
