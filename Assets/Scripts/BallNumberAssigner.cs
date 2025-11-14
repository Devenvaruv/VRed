using TMPro;
using UnityEngine;

public class BallNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private Renderer ballRenderer; // Assign in inspector


    void Start()
    {
        int number = NumberManager.Instance.GetUniqueNumber();
        Color color = NumberManager.Instance.GetUniqueColor();

        textMesh.text = number >= 0 ? number.ToString() : "?";

        // Create a new material instance so it doesn't affect all balls
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        ballRenderer.material = mat;
    }
}
