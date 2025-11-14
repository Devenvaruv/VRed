using System.Collections;
using UnityEngine;
using TMPro; // or UnityEngine.UI for standard UI Text

public class TypingEffect : MonoBehaviour
{
    public TMP_Text textMeshPro; // (Or Text for UI Text)
    public float typingSpeed = 0.1f; // seconds per character

    private string fullText;

    private void Start()
    {
        fullText = textMeshPro.text;
        textMeshPro.text = string.Empty;
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        foreach (char letter in fullText)
        {
            textMeshPro.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
