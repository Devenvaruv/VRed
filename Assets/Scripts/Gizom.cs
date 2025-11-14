using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class TwoHandGrabRotatable : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor firstInteractor;     // primary hand
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor secondInteractor;    // optional second hand

    Quaternion objRotationAtFirstGrab;
    Quaternion firstHandRotationAtGrab;
    Quaternion twoHandRotationOffset;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        var interactor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;

        // First hand grabs
        if (firstInteractor == null)
        {
            firstInteractor        = interactor;
            objRotationAtFirstGrab = transform.rotation;
            firstHandRotationAtGrab = firstInteractor.transform.rotation;
        }
        // Second hand grabs
        else if (secondInteractor == null)
        {
            secondInteractor      = interactor;
            twoHandRotationOffset = Quaternion.Inverse(GetTwoHandRotation()) * transform.rotation;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        var interactor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;

        // Second hand lets go
        if (interactor == secondInteractor)
        {
            secondInteractor = null;
        }
        // First hand lets go (swap roles if second hand is still holding)
        if (interactor == firstInteractor)
        {
            firstInteractor = secondInteractor;
            secondInteractor = null;

            if (firstInteractor != null)
            {
                firstHandRotationAtGrab = firstInteractor.transform.rotation;
                objRotationAtFirstGrab  = transform.rotation;
            }
        }
    }

    void Update()
    {
        // Two-hand mode
        if (firstInteractor && secondInteractor)
        {
            transform.rotation = GetTwoHandRotation() * twoHandRotationOffset;
        }
        // One-hand mode
        else if (firstInteractor)
        {
            Quaternion handDelta = firstInteractor.transform.rotation *
                                   Quaternion.Inverse(firstHandRotationAtGrab);
            transform.rotation = handDelta * objRotationAtFirstGrab;
        }
    }

    /// <summary>
    /// Returns a rotation whose forward axis points from first → second hand.
    /// </summary>
    Quaternion GetTwoHandRotation()
    {
        Vector3 dir = secondInteractor.transform.position - firstInteractor.transform.position;
        // Use world up as secondary axis – adjust if you want object-local up
        return Quaternion.LookRotation(dir, Vector3.up);
    }
}
