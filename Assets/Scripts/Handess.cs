using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandAwareGrab : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (args.interactorObject is XRBaseInteractor interactor)
        {
            if (interactor.handedness == InteractorHandedness.Left)
            {
                Debug.Log("Grabbed by LEFT hand");
                trackPosition = true;
                trackRotation = true;
                trackScale = true;
            }
            else if (interactor.handedness == InteractorHandedness.Right)
            {
                Debug.Log("Grabbed by RIGHT hand");
                // Right-hand specific behavior here
                trackPosition = false;
                trackRotation = false;
                trackScale = false;
            }
        }
    }
}
