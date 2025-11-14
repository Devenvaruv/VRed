// File: Assets/Editor/JsonEditorSpawner.cs

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Networking;

[Serializable]
public class ItemData
{
    public string item;
    public float[] pos;
    public float[] rot;
    public float[] scale;
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string wrapped = "{\"Items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return wrapper.Items;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

public class JsonEditorSpawner : EditorWindow
{
    private string dataUrl = "https://unity-api-backend.onrender.com/data";

    [MenuItem("Tools/Spawn From JSON")]
    public static void ShowWindow()
    {
        GetWindow<JsonEditorSpawner>("Spawn From JSON");
    }

    void OnGUI()
    {
        GUILayout.Label("Spawn Scene Objects From JSON", EditorStyles.boldLabel);
        dataUrl = EditorGUILayout.TextField("JSON API URL", dataUrl);

        if (GUILayout.Button("Fetch and Spawn"))
        {
            FetchAndSpawnBlocking();
        }
    }

    void FetchAndSpawnBlocking()
    {
        try
        {
            UnityWebRequest request = UnityWebRequest.Get(dataUrl);
            var operation = request.SendWebRequest();

            // Wait synchronously (Editor-safe)
            while (!operation.isDone) { }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to fetch data: " + request.error);
                return;
            }

            string rawJson = request.downloadHandler.text;
            ItemData[] items = JsonHelper.FromJson<ItemData>(rawJson);

            foreach (var item in items)
            {
                SpawnIfNotExists(item);
            }

            Debug.Log("Finished spawning items from JSON.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error fetching data: " + ex.Message);

        }

    }

    void SpawnIfNotExists(ItemData data)
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        bool exists = false;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith(data.item))
            {
                if (ApproximatelyEqual(obj.transform.position, data.pos) &&
                    ApproximatelyEqual(obj.transform.eulerAngles, data.rot) &&
                    ApproximatelyEqual(obj.transform.localScale, data.scale))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (!exists)
        {
            GameObject original = GameObject.Find(data.item);
            if (original == null)
            {
                Debug.LogWarning($"Original GameObject named '{data.item}' not found.");
                return;
            }

            GameObject clone = PrefabUtility.InstantiatePrefab(original) as GameObject;
            if (clone == null)
            {
                clone = GameObject.Instantiate(original);
            }

            clone.transform.position = new Vector3(data.pos[0], data.pos[1], data.pos[2]);
            clone.transform.eulerAngles = new Vector3(data.rot[0], data.rot[1], data.rot[2]);
            clone.transform.localScale = new Vector3(data.scale[0], data.scale[1], data.scale[2]);
            clone.name = data.item + "_Clone_" + Guid.NewGuid().ToString("N").Substring(0, 6);

            Undo.RegisterCreatedObjectUndo(clone, "Spawned JSON Object");
        }
    }

    bool ApproximatelyEqual(Vector3 a, float[] b, float tolerance = 0.01f)
    {
        if (b.Length != 3) return false;
        return Mathf.Abs(a.x - b[0]) < tolerance &&
               Mathf.Abs(a.y - b[1]) < tolerance &&
               Mathf.Abs(a.z - b[2]) < tolerance;
    }
}
