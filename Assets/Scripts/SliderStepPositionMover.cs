using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SliderStepPositionMover : MonoBehaviour
{
    [Header("References")]
    public Slider slider;
    public Transform objectToMove;

    [Header("Task Events")]
    public UnityEvent onTask1Completed; // fired when user touches slider first time
    public UnityEvent onTask2Completed; // fired when slider value == 3

    private bool task1Done = false;
    private bool task2Done = false;

    private readonly Vector3[] stepPositions = new Vector3[]
    {
        new Vector3(42.990f, 0.703f, 1.715f),  // 0
        new Vector3(42.083f, 1.55f, 2.350f),   // 1
        new Vector3(43.083f, 0.792f, 3.692f),  // 2
        new Vector3(41.373f, 1.28f, 4.540f),   // 3
        new Vector3(42.478f, 0.486f, 5.376f),  // 4
        new Vector3(42.504f, 1.103f, 6.545f),  // 5
        new Vector3(43.136f, 0.6107f, 7.397f)  // 6
    };

    void Start()
    {
        if (slider != null)
        {
            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = stepPositions.Length - 1;

            slider.onValueChanged.AddListener(OnSliderValueChanged);
            OnSliderValueChanged(slider.value); // Set initial position
        }
    }

    void OnSliderValueChanged(float value)
    {
        int index = Mathf.RoundToInt(value);

        // --- original: move object ---
        if (objectToMove != null && index >= 0 && index < stepPositions.Length)
        {
            objectToMove.position = stepPositions[index];
        }

        // --- Task 1: first interaction (first time value changes) ---
        if (!task1Done)
        {
            task1Done = true;
            Debug.Log("Task 1 done: slider was touched!");
            onTask1Completed?.Invoke();
        }

        // --- Task 2: slider value is exactly 3 ---
        if (!task2Done && index == 3)
        {
            task2Done = true;
            Debug.Log("Task 2 done: slider reached value 3!");
            onTask2Completed?.Invoke();
        }
    }

    public bool IsTask1Completed() => task1Done;
    public bool IsTask2Completed() => task2Done;
}
