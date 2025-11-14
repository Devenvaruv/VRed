using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;            // ← new Input System
using UnityEngine.InputSystem.XR;

public class LeftControllerButton : MonoBehaviour
{
    [Header("Assign an InputAction from your .inputactions asset")]
    public InputActionProperty buttonAction;    
    public GameObject targetCanvas;      // drag <XRController>{LeftHand}/buttonSouth, etc.

    public UnityEvent onPressed;   // drop any function(s) here in the Inspector

    void OnEnable()
    {
        buttonAction.action.Enable();
        buttonAction.action.performed += HandlePress;
    }

    void OnDisable()
    {
        buttonAction.action.performed -= HandlePress;
        buttonAction.action.Disable();
    }

    void HandlePress(InputAction.CallbackContext ctx)
    {
        if (targetCanvas != null)
        {
            // Toggle logic here
            targetCanvas.SetActive(!targetCanvas.activeSelf);
        }  // triggers whatever you hooked up
    }
}
