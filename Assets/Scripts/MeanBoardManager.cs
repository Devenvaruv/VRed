using System.Collections.Generic;
using UnityEngine;

public class NumberManager : MonoBehaviour
{
    public static NumberManager Instance;

    private List<int> availableNumbers = new();
    private List<Color> availableColors = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        for (int i = 0; i < 10; i++) availableNumbers.Add(i);

        // Generate 20 visually distinct colors
        for (int i = 0; i < 10; i++)
        {
            float hue = i / 10f; // spread across hue spectrum
            availableColors.Add(Color.HSVToRGB(hue, 0.8f, 0.9f));
        }
    }

    public int GetUniqueNumber()
    {
        if (availableNumbers.Count == 0)
        {
            Debug.LogError("No more unique numbers available!");
            return -1;
        }

        int index = Random.Range(0, availableNumbers.Count);
        int number = availableNumbers[index];
        availableNumbers.RemoveAt(index);
        return number;
    }

    public Color GetUniqueColor()
    {
        if (availableColors.Count == 0)
        {
            Debug.LogError("No more unique colors available!");
            return Color.white;
        }

        int index = Random.Range(0, availableColors.Count);
        Color c = availableColors[index];
        availableColors.RemoveAt(index);
        return c;
    }
}
