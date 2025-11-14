using UnityEngine;
using UnityEditor;

public class GridTilePlacer : EditorWindow
{
    GameObject tilePrefab;
    Transform startPoint;

    int countX = 0;
    int countY = 0;
    int countZ = 0;

    float spacingX = 1f;
    float spacingY = 1f;
    float spacingZ = 1f;

    [MenuItem("Tools/Grid Tile Placer")]
    public static void ShowWindow()
    {
        GetWindow<GridTilePlacer>("Grid Tile Placer");
    }

    void OnGUI()
    {
        GUILayout.Label("Tile Grid Settings", EditorStyles.boldLabel);

        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        startPoint = (Transform)EditorGUILayout.ObjectField("Start Position", startPoint, typeof(Transform), true);

        countX = EditorGUILayout.IntField("Count X (Width)", countX);
        countY = EditorGUILayout.IntField("Count Y (Height)", countY);
        countZ = EditorGUILayout.IntField("Count Z (Depth)", countZ);

        spacingX = EditorGUILayout.FloatField("Spacing X", spacingX);
        spacingY = EditorGUILayout.FloatField("Spacing Y", spacingY);
        spacingZ = EditorGUILayout.FloatField("Spacing Z", spacingZ);

        if (GUILayout.Button("Place Tiles"))
        {
            if (tilePrefab == null || startPoint == null)
            {
                Debug.LogWarning("❗ Assign both a prefab and a starting Transform!");
                return;
            }

            PlaceTiles();
        }
    }

    void PlaceTiles()
    {
        GameObject parent = new GameObject("Generated_TileGrid");

        int xDir = countX >= 0 ? 1 : -1;
        int yDir = countY >= 0 ? 1 : -1;
        int zDir = countZ >= 0 ? 1 : -1;

        int absX = Mathf.Abs(countX);
        int absY = Mathf.Abs(countY);
        int absZ = Mathf.Abs(countZ);

        for (int x = 0; x < (absX == 0 ? 1 : absX); x++)
        {
            for (int y = 0; y < (absY == 0 ? 1 : absY); y++)
            {
                for (int z = 0; z < (absZ == 0 ? 1 : absZ); z++)
                {
                    float posX = x * spacingX * xDir;
                    float posY = y * spacingY * yDir;
                    float posZ = z * spacingZ * zDir;

                    Vector3 position = startPoint.position + new Vector3(posX, posY, posZ);
                    GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab);
                    tile.transform.position = position;
                    tile.transform.SetParent(parent.transform);
                }
            }
        }


        Debug.Log($"✅ Placed tiles");
    }
}
