using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class InputDebugger : MonoBehaviour
{
    private List<UnityEngine.XR.InputDevice> rightHandDevices = new();

    void Start()
    {
        InputDevices.deviceConnected += OnDeviceConnected;

        List<UnityEngine.XR.InputDevice> tempDevices = new();
        InputDevices.GetDevices(tempDevices);
        RegisterDevices(tempDevices);
    }

    void OnDeviceConnected(UnityEngine.XR.InputDevice device)
    {
        List<UnityEngine.XR.InputDevice> tempDevices = new();
        InputDevices.GetDevices(tempDevices);
        RegisterDevices(tempDevices);
    }

    void RegisterDevices(List<UnityEngine.XR.InputDevice> devices)
    {
        rightHandDevices.Clear();
        foreach (var device in devices)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
                device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
            {
                rightHandDevices.Add(device);
                Debug.Log($"[XR] Right-hand controller found: {device.name}");
            }
        }
    }

    void Update()
    {
        foreach (var device in rightHandDevices)
        {
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger)
                Debug.Log("[INPUT] Trigger pressed");

            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool aButton) && aButton)
                Debug.Log("[INPUT] A button pressed");

            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bButton) && bButton)
                Debug.Log("[INPUT] B button pressed");

            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick) && Mathf.Abs(thumbstick.x) > 0.1f)
                Debug.Log($"[INPUT] Thumbstick X: {thumbstick.x:F2}");
        }
    }
}
