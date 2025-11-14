using System;
using UnityEditor;
using UnityEngine;

public class ScaleMarkerTool : EditorWindow
{
    private float lengthInMeters = 2f;
    private float tickInterval = 0.1f;
    private float heightOffset = 0f;
    private Material baseMaterial;

    [MenuItem("Tools/Create Scale Marker")]
    public static void ShowWindow()
    {
        GetWindow<ScaleMarkerTool>("Scale Marker Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create a VR Scale Reference", EditorStyles.boldLabel);

        lengthInMeters = EditorGUILayout.FloatField("Length (meters)", lengthInMeters);
        tickInterval = EditorGUILayout.FloatField("Tick Interval (meters)", tickInterval);
        heightOffset = EditorGUILayout.FloatField("Y Offset (height)", heightOffset);
        baseMaterial = (Material)
            EditorGUILayout.ObjectField("Base Material", baseMaterial, typeof(Material), false);

        EditorGUILayout.HelpBox(
            "This tool creates a VR scale reference marker in the scene.\n"
                + "- 'Length' defines total marker size in meters.\n"
                + "- 'Tick Interval' controls the spacing between tick marks.\n"
                + "- 'Y Offset' lifts the marker vertically.\n"
                + "- 'Base Material' lets you assign a custom material."
                + "\n\n"
                + "Typical Ratio ≈ Height : Width : Depth = 1 : 1.5 : 2\n"
                + "Standard Room Dimensions: (H:W:D in meters):\n"
                + "• Small Studio/Dorm.......2.4 : 2.8 : 3.5\n"
                + "• Bedroom/Guest Room......2.4–2.7 : 3–4 : 4–5\n"
                + "• Living/Family Room......2.7–3.0 : 4–6 : 5–7\n"
                + "• Office/Study/Library....2.4–2.7 : 3–4 : 4–5\n"
                + "• Kitchen/Pantry..........2.4 : 3 : 4\n"
                + "• Bathroom/Washroom.......2.4 : 2 : 2.5\n"
                + "• Dining Room.............2.4–2.7 : 3.5 : 4.5\n"
                + "• Closet/Storage..........2.4 : 1.5 : 2\n"
                + "• Laundry/Utility Room....2.4 : 2 : 3\n"
                + "• Garage/Workshop.........2.4–3.0 : 4–6 : 5–7\n"
                + "• Hallway/Foyer...........2.4 : 1.2–2 : 4–6\n"
                + "• Balcony/Patio...........2.4 : 2–4 : 2–3\n"
                + "• Basement/Attic..........2.2–2.4 : varies (tight, low clearance)\n\n",
            MessageType.Info
        );

        if (GUILayout.Button("Create Scale Marker"))
        {
            CreateScaleMarker();
        }
    }

    private void CreateScaleMarker()
    {
        GameObject root = new GameObject("ScaleMarker_" + lengthInMeters + "m");
        root.transform.position = Vector3.zero;

        // Base strip
        GameObject baseStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseStrip.name = "Base";
        baseStrip.GetComponent<Renderer>().material = baseMaterial;
        baseStrip.transform.parent = root.transform;
        baseStrip.transform.localScale = new Vector3(lengthInMeters, 0.01f, 0.05f);
        baseStrip.transform.localPosition = new Vector3(lengthInMeters / 2f, heightOffset, 0);

        for (float i = 0f; i <= lengthInMeters + 0.001f; i += tickInterval)
        {
            GameObject tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tick.name = $"Tick_{i:F2}m";
            tick.transform.parent = root.transform;

            float tickHeight = (Mathf.Approximately(i % 1f, 0f)) ? 0.06f : 0.03f;
            tick.transform.localScale = new Vector3(0.01f, tickHeight, 0.01f);
            tick.transform.localPosition = new Vector3(i, heightOffset + tickHeight / 2f, 0);

            bool isWholeMeter = Mathf.Approximately(i, Mathf.Round(i));
            bool isLastTick = Mathf.Abs(i - lengthInMeters) < 0.01f;

            if (isWholeMeter || isLastTick)
            {
                GameObject label = new GameObject("Label_" + i + "m");
                label.transform.parent = root.transform;
                TextMesh tm = label.AddComponent<TextMesh>();
                tm.text = i.ToString("0.##") + "m";
                tm.characterSize = 0.1f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.fontSize = 10;
                tm.color = Color.white;
                label.transform.localPosition = new Vector3(i, heightOffset + 0.1f, 0.05f);
                label.transform.localRotation = Quaternion.Euler(90, 0, 0); // upright
            }
        }

        Selection.activeGameObject = root;
    }
}
