// BalanceBoardXOnly.cs
// ──────────────────────
// • Drag this onto an empty GameObject (or the board itself).
// • Assign the “board” mesh/prefab (the thing that should tilt).
// • Sockets must still be named with numbers (e.g. “Socket3”) so the script
//   can pick up indices for mean / median maths.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BalanceBoardXOnly : MonoBehaviour
{
    [Header("Main board mesh / prefab")]
    [SerializeField] Transform board;                 // the thing that tilts

    [Header("Optional median marker")]
    [SerializeField] Transform medianMarker;
    [SerializeField] bool      autoPlaceMedian = true;

    [Header("Tilt settings")]
    [SerializeField] float degPerUnit = 4f;           // degrees per index step
    [SerializeField] float smoothSecs = .25f;         // smoothing time

    [Header("Debug")]
    [SerializeField] bool debugLogs = false;

    /* ───────── internal state ───────── */
    readonly HashSet<int> occupied = new();
    readonly Dictionary<int, XRSocketInteractor> sockets = new();
    float currentDeg, vel, unitSpacing;
    static readonly Regex numRx = new(@"(\d+)", RegexOptions.Compiled);

    /* ────────────────────────────────── */
    void Awake()
    {
        if (!board)
        {
            Debug.LogError("BalanceBoardXOnly: please assign the board Transform.");
            enabled = false;
            return;
        }

        // Gather sockets & hook up events
        foreach (var s in board.GetComponentsInChildren<XRSocketInteractor>(true))
        {
            var m = numRx.Match(s.gameObject.name);
            if (!m.Success) continue;

            int idx = int.Parse(m.Value);
            sockets[idx] = s;

            int captured = idx;                     // lambda capture
            s.selectEntered.AddListener(_ => { occupied.Add(captured); Recalc(); });
            s.selectExited .AddListener(_ => { occupied.Remove(captured); Recalc(); });
        }

        if (debugLogs) Debug.Log($"Sockets found: {string.Join(", ", sockets.Keys)}");

        DetectUnitSpacing();
    }

    /* ────────────────────────────────── */
    void DetectUnitSpacing()
    {
        // Needs at least one consecutive pair to know how far apart sockets are.
        var keys = new List<int>(sockets.Keys); keys.Sort();
        for (int i = 0; i < keys.Count - 1; ++i)
        {
            if (keys[i + 1] != keys[i] + 1) continue;

            Vector3 a = sockets[keys[i]    ].transform.localPosition;
            Vector3 b = sockets[keys[i] + 1].transform.localPosition;
            unitSpacing = Vector3.Distance(a, b);
            if (debugLogs) Debug.Log($"Detected per-unit spacing = {unitSpacing:F4} (local units)");
            return;
        }

        Debug.LogWarning("BalanceBoardXOnly: couldn’t find consecutive sockets – median marker may not move.");
    }

    /* ────────────────────────────────── */
    void Recalc()
    {
        float targetDeg = 0f;

        if (occupied.Count > 0)
        {
            float sum = 0; foreach (int i in occupied) sum += i;
            float mean = sum / occupied.Count;

            int min = int.MaxValue, max = int.MinValue;
            foreach (int i in sockets.Keys) { if (i < min) min = i; if (i > max) max = i; }
            float centreIndex = (min + max) * 0.5f;

            targetDeg = (mean - centreIndex) * degPerUnit;
        }

        StopAllCoroutines();
        StartCoroutine(SmoothTilt(targetDeg));

        UpdateMedianMarker();
    }

    /* ────────────────────────────────── */
    System.Collections.IEnumerator SmoothTilt(float tgt)
    {
        float t = 0f;
        while (t < smoothSecs)
        {
            currentDeg = Mathf.SmoothDampAngle(currentDeg, tgt, ref vel, smoothSecs);
            board.localRotation = Quaternion.AngleAxis(currentDeg, Vector3.right);  // ALWAYS local X!
            t += Time.deltaTime;
            yield return null;
        }

        currentDeg = tgt;
        board.localRotation = Quaternion.AngleAxis(currentDeg, Vector3.right);
    }

    /* ────────────────────────────────── */
    void UpdateMedianMarker()
    {
        if (!medianMarker || occupied.Count == 0 || unitSpacing == 0f) return;

        // Compute median index
        List<int> sorted = new(occupied); sorted.Sort();
        float median = sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) * 0.5f;

        // Default assumption: sockets run along board.localZ (forward).  
        // If yours run the other way, just swap .z with .x here.
        Vector3 p = medianMarker.localPosition;
        p.z = median * unitSpacing;
        medianMarker.localPosition = p;
        medianMarker.localRotation = Quaternion.identity;
    }
}
