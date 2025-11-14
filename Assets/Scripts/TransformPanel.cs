using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Fixed world‑space transform inspector panel.
/// • Call Inspect(Transform) to show & update.
/// • Copy duplicates the current object and begins inspecting the clone.
/// • Close serialises the latest snapshot to JSON and POSTs it to apiUrl.
/// </summary>
public class TransformPanel : MonoBehaviour
{
    [Header("Current target (optional)")]
    [SerializeField]
    Transform current;

    [Header("UI refs")]
    [SerializeField]
    TMP_Text posText;

    [SerializeField]
    TMP_Text rotText;

    [SerializeField]
    TMP_Text scaleText;

    [SerializeField]
    Button copyBtn;

    [SerializeField]
    Button closeBtn;

    [Header("Send JSON")]
    [SerializeField]
    string apiUrl = "https://unity-api-backend.onrender.com/submit";

    [SerializeField]
    bool prettyJson = true;

    [SerializeField]
    bool liveRefresh = true;

    public string LatestJson => latestJson;
    public Action<string> OnJsonUpdated; // fires every refresh
    public Action<string> OnJsonReady; // fires on Close()

    string latestJson;

    public void InspectFromSelect(SelectEnterEventArgs args)
    {
        if (args == null)
            return;
        Inspect(args.interactableObject.transform);
    }

    /// <summary>Send LatestJson manually (panel can stay open).</summary>
    public void SendLatest()
    {
        if (string.IsNullOrEmpty(latestJson) || string.IsNullOrWhiteSpace(apiUrl))
            return;

        StartCoroutine(PostJson(apiUrl, latestJson));
    }

    void OnEnable()
    {
        // safe to hook listeners here; paired with OnDisable for clean-up
        copyBtn.onClick.AddListener(CopyCurrent);
        closeBtn.onClick.AddListener(Close);
        RefreshFields();
    }

    void OnDisable()
    {
        copyBtn.onClick.RemoveListener(CopyCurrent);
        closeBtn.onClick.RemoveListener(Close);
    }

    /// <summary>Show panel & inspect this transform.</summary>
    public void Inspect(Transform target)
    {
        current = target;
        RefreshFields();
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (current && liveRefresh)
        {
            RefreshFields();
        }
    }

    /// <summary>Hide panel (no JSON emitted).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void RefreshFields()
    {
        if (!current)
        {
            posText.text = "Pos  --";
            rotText.text = "Rot  --";
            scaleText.text = "Scl  --";
            latestJson = null;
            return;
        }

        var t = current;
        posText.text = $"Pos  {t.position.x:F2}, {t.position.y:F2}, {t.position.z:F2}";
        rotText.text = $"Rot  {t.eulerAngles.x:F1}, {t.eulerAngles.y:F1}, {t.eulerAngles.z:F1}";
        scaleText.text = $"Scl  {t.localScale.x:F2}, {t.localScale.y:F2}, {t.localScale.z:F2}";

        latestJson = BuildSnapshotJson(t, prettyJson);
        OnJsonUpdated?.Invoke(latestJson);
    }
    string BuildSnapshotJson(Transform t, bool pretty)
    {
        Snapshot s = new Snapshot
        {
            item  = t.name,
            pos   = new[] { t.position.x,   t.position.y,   t.position.z   },
            rot   = new[] { t.eulerAngles.x, t.eulerAngles.y, t.eulerAngles.z },
            scale = new[] { t.localScale.x, t.localScale.y, t.localScale.z }
        };
        return JsonUtility.ToJson(s, pretty);
    }

    void CopyCurrent()
    {
        if (!current)
            return;
        var clone = Instantiate(current.gameObject, current.position, current.rotation);
        clone.transform.localScale = current.localScale;
        Inspect(clone.transform); // now inspect the clone
    }

    void Close()
    {
        if (current)
        {   
            RefreshFields();  
            Debug.Log($"[Transform JSON]\n{latestJson}");
            OnJsonReady?.Invoke(latestJson);

            if (!string.IsNullOrWhiteSpace(apiUrl))
                StartCoroutine(PostJson(apiUrl, latestJson));
        }
        gameObject.SetActive(false);
    }

    IEnumerator PostJson(string url, string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !(req.isNetworkError || req.isHttpError);
#endif
            if (ok)
                Debug.Log($"POST {url} OK ({req.responseCode}) → {req.downloadHandler.text}");
            else
                Debug.LogWarning($"POST {url} failed: {req.error}");
        }
    }

    [System.Serializable]
    struct Snapshot
    {
        public string  item;   // object name
        public float[] pos;    // xyz
        public float[] rot;    // euler xyz
        public float[] scale;  // xyz
    }
}
