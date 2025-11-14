using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Collections.Generic;

public class ZoneAddressableLoader : MonoBehaviour
{
    public string labelToLoad = "RoomA";             // The Addressables label to load
    public Transform xrOrigin;                       // Reference to XR Origin or camera rig

    private List<GameObject> loadedObjects = new();  // Keep track of all loaded instances
    private List<AsyncOperationHandle<GameObject>> handles = new(); // So we can release them properly

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == xrOrigin)
        {
            Debug.Log($"[ZoneLoader] Player entered: Loading assets with label {labelToLoad}");
            LoadLabelGroup();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == xrOrigin)
        {
            Debug.Log($"[ZoneLoader] Player exited: Releasing assets with label {labelToLoad}");
            UnloadLabelGroup();
        }
    }

    void LoadLabelGroup()
    {
        Addressables.LoadResourceLocationsAsync(labelToLoad).Completed += locHandle =>
        {
            if (locHandle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (IResourceLocation location in locHandle.Result)
                {
                    var handle = Addressables.LoadAssetAsync<GameObject>(location);
                    handles.Add(handle);
                    handle.Completed += assetHandle =>
                    {
                        if (assetHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            GameObject obj = Instantiate(assetHandle.Result, transform.position, Quaternion.identity);
                            loadedObjects.Add(obj);
                        }
                    };
                }
            }
        };
    }

    void UnloadLabelGroup()
    {
        foreach (GameObject obj in loadedObjects)
        {
            Destroy(obj);
        }

        foreach (var handle in handles)
        {
            Addressables.Release(handle);
        }

        loadedObjects.Clear();
        handles.Clear();
    }

    // 🔧 OPTIONAL: Here's how to load a single prefab manually (commented out)
    /*
    public string prefabAddress = "WallPrefabA";
    private GameObject singleInstance;
    private AsyncOperationHandle<GameObject> singleHandle;

    void LoadSingle()
    {
        singleHandle = Addressables.LoadAssetAsync<GameObject>(prefabAddress);
        singleHandle.Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                singleInstance = Instantiate(handle.Result);
            }
        };
    }

    void UnloadSingle()
    {
        if (singleInstance != null)
            Destroy(singleInstance);

        Addressables.Release(singleHandle);
    }
    */
}
