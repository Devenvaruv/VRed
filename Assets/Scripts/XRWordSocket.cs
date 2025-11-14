using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactors;

[AddComponentMenu("XR/Interactors/XR Word-Match Socket")]
public class XRWordMatchSocket : XRSocketInteractor
{
    [Tooltip("Any of these words will let the object snap in.")]
    [SerializeField] private List<string> acceptedWords = new() { "Example" };

    /* ------------ helper ------------ */
    private bool IsAccepted(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable candidate)
    {
        if (acceptedWords == null || acceptedWords.Count == 0)
            return true;   // empty list ➜ accept everything

        var tmp = candidate.transform.GetComponentInChildren<TMP_Text>();
        if (tmp == null) return false;

        foreach (var word in acceptedWords)
            if (tmp.text.Equals(word, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    /* ------------ GATEKEEPERS ------------ */
    // Let *any* object hover so we get blue/red feedback.
    // Only matching objects can be selected.
    public override bool CanSelect(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable) =>
        base.CanSelect(interactable) && IsAccepted(interactable);

    /* ------------ Hover colour chooser ------------ */
    protected override Material GetHoveredInteractableMaterial(
        UnityEngine.XR.Interaction.Toolkit.Interactables.IXRHoverInteractable interactable)
    {
        // Blue if allowed, red if rejected
        return IsAccepted(interactable)
            ? interactableHoverMeshMaterial       // default blue
            : interactableCantHoverMeshMaterial;  // default red
    }
}
