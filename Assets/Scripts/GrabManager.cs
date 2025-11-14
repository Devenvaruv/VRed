using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabMonitor : MonoBehaviour
{
    [SerializeField] private TransformPanel panel;

    void Start()
    {
        RegisterAllGrabInteractables();
    }

    void RegisterAllGrabInteractables()
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable[] grabObjects = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        foreach (var grab in grabObjects)
        {
            grab.selectEntered.AddListener(OnGrab);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        panel?.InspectFromSelect(args);
    }

    // Optional: support for dynamically added grabbables later (e.g., via pooling or scene loading)
    public void RegisterNewGrab(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
    {
        grab.selectEntered.AddListener(OnGrab);
    }
}
