using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimpleSequenceTypewriterVR : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI textUI;     // required
    public CanvasGroup canvasGroup;    // optional (for fade)

    [Header("Parts (each entry = one part; use \\n for multiple lines)")]
    [TextArea(1, 6)]
    public List<string> parts = new List<string>()
    {
        "Hi there!\nI'm your assistant drone.",
        "Let me explain <i>mode</i>.\nIt's the thing that appears the most.",
        "Right now the mode is <b>Blue Books</b>.\nThey’re the most common on this shelf.",
        "Can you find all the <b>Red Books</b> and group them?\nIf red becomes the most, the mode will switch!"
    };

    [Header("Global Pacing")]
    [Min(1f)] public float charsPerSecond = 32f; // typewriter speed
    [Min(0f)] public float prePartDelay = 0.25f; // before typing each part
    [Min(0f)] public float betweenLinesDelay = 0.20f; // pause after each line
    [Min(0f)] public float postPartDelay = 1.00f; // linger after last line
    [Min(0f)] public float fadeIn = 0.30f;
    [Min(0f)] public float fadeOut = 0.30f;
    [Min(0f)] public float interPartGap = 0.15f; // after clear, before next fade-in
    public bool allowRichText = true;
    public bool autoStart = true;

    Coroutine runner;

    void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (autoStart && textUI && parts.Count > 0)
            Play();
    }

    public void Play()
    {
        if (runner != null) StopCoroutine(runner);
        runner = StartCoroutine(Run());
    }

    public void Stop()
    {
        if (runner != null) StopCoroutine(runner);
        runner = null;
    }

    IEnumerator Run()
    {
        textUI.text = "";
        textUI.richText = allowRichText;
        textUI.maxVisibleCharacters = 0;
        if (canvasGroup) canvasGroup.alpha = 0f;

        for (int i = 0; i < parts.Count; i++)
        {
            // Clear previous part so only one shows at a time
            textUI.text = "";
            textUI.maxVisibleCharacters = 0;

            if (interPartGap > 0f) yield return new WaitForSeconds(interPartGap);

            // Fade in
            if (canvasGroup && fadeIn > 0f)
                yield return StartCoroutine(Fade(canvasGroup, 0f, 1f, fadeIn));
            else if (canvasGroup) canvasGroup.alpha = 1f;

            if (prePartDelay > 0f) yield return new WaitForSeconds(prePartDelay);

            // Type each line in this part
            string[] lines = parts[i].Split('\n');
            for (int li = 0; li < lines.Length; li++)
            {
                yield return StartCoroutine(TypeLine(lines[li]));
                if (betweenLinesDelay > 0f && li < lines.Length - 1)
                    yield return new WaitForSeconds(betweenLinesDelay);
            }

            if (postPartDelay > 0f) yield return new WaitForSeconds(postPartDelay);

            // Fade out
            if (canvasGroup && fadeOut > 0f)
                yield return StartCoroutine(Fade(canvasGroup, 1f, 0f, fadeOut));
            else if (canvasGroup) canvasGroup.alpha = 0f;
        }
    }

    IEnumerator TypeLine(string line)
    {
        if (!string.IsNullOrEmpty(textUI.text))
            textUI.text += "\n";

        int startIndex = textUI.text.Length;
        textUI.text += line;

        // Let TMP update its textInfo
        yield return null;
        textUI.ForceMeshUpdate();

        int targetVisible = textUI.textInfo.characterCount;
        float secPerChar = 1f / Mathf.Max(1f, charsPerSecond);

        // Reveal from the start of this line onward
        textUI.maxVisibleCharacters = startIndex;

        while (textUI.maxVisibleCharacters < targetVisible)
        {
            textUI.maxVisibleCharacters++;
            yield return new WaitForSeconds(secPerChar);
        }

        textUI.maxVisibleCharacters = targetVisible;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
    {
        if (!cg) yield break;
        if (dur <= 0f) { cg.alpha = to; yield break; }

        float t = 0f;
        cg.alpha = from;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        cg.alpha = to;
    }
}
