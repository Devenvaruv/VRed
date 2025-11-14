using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
    [Header("Rotation Speed (degrees per second)")]
    public float rotationSpeed = 30f; // adjust speed

    [Header("Rotation Axis")]
    public Vector3 rotationAxis = Vector3.up; // X, Y, or Z axis

    void Update()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
    }
}
