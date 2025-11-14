using UnityEngine;
using UnityEngine.InputSystem;

public class PureInputSystemVR : MonoBehaviour
{
    private InputActionAsset inputAsset;
    private InputAction primaryButtonAction;

    void Awake()
    {
        // Load the InputActions asset from Resources or path
        inputAsset = Resources.Load<InputActionAsset>("Test"); // expects MyControls.inputactions in Resources/

        if (inputAsset == null)
        {
            Debug.LogError("Could not load InputActionAsset!");
            return;
        }

        // Find the action from the map
        primaryButtonAction = inputAsset.FindAction("RightHand/PrimaryButton");

        if (primaryButtonAction == null)
        {
            Debug.LogError("PrimaryButton action not found!");
            return;
        }

        primaryButtonAction.Enable();
    }

    void OnEnable()
    {
        if (primaryButtonAction != null)
            primaryButtonAction.performed += OnPrimaryPressed;
    }

    void OnDisable()
    {
        if (primaryButtonAction != null)
            primaryButtonAction.performed -= OnPrimaryPressed;
    }

    private void OnPrimaryPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("🔥 Primary button (A/X) was pressed!");
    }
}
