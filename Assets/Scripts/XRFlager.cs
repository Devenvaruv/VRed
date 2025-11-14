// using UnityEngine;
// using UnityEngine.XR;
// using UnityEngine.XR.Interaction.Toolkit;

// public class HandBasedGrabLogic : MonoBehaviour
// {
//     private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

//     void Awake()
//     {
//         grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
//     }

//     void OnEnable()
//     {
//         grabInteractable.selectEntered.AddListener(OnGrabbed);
//         grabInteractable.selectExited.AddListener(OnReleased);
//     }

//     void OnDisable()
//     {
//         grabInteractable.selectEntered.RemoveListener(OnGrabbed);
//         grabInteractable.selectExited.RemoveListener(OnReleased);
//     }

//     private void OnGrabbed(SelectEnterEventArgs args)
//     {
//         if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
//         {
//             InputDevice device = controllerInteractor.xrController.inputDevice;
//             if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
//             {
//                 Debug.Log("Grabbed by LEFT hand");
//                 // Do left-hand-specific logic
//             }
//             else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
//             {
//                 Debug.Log("Grabbed by RIGHT hand");
//                 // Do right-hand-specific logic
//             }
//         }
//     }

//     private void OnReleased(SelectExitEventArgs args)
//     {
//         Debug.Log("Object Released");
//     }
// }
