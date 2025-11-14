using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleScreenHUD : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup;     // Canvas → Add & drag
    public TextMeshProUGUI guideText;   // GuideText
    public Image progressFill;          // ProgressFill (Image Type = Filled)

    void Awake()
    {
        if (canvasGroup) {
            // start visible; set to 0 in inspector if you want hidden by default
            canvasGroup.alpha = Mathf.Approximately(canvasGroup.alpha, 0f) ? 0f : 1f;
            canvasGroup.blocksRaycasts = canvasGroup.alpha > 0f;
            canvasGroup.interactable = canvasGroup.alpha > 0f;
        }
    }

    // === Public API ===
    public void ShowHUD(bool on)
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = on ? 1f : 0f;
        canvasGroup.blocksRaycasts = on;
        canvasGroup.interactable = on;
    }

    public void SetGuide(string msg)
    {
        if (guideText) guideText.text = msg;
    }

    /// t in [0,1]
    public void SetProgress(float t)
    {
        if (progressFill) progressFill.fillAmount = Mathf.Clamp01(t);
    }
}
