using System;
using UnityEngine;
using UnityEngine.UI;

public class OffsetManager : MonoBehaviour
{
    // Offset values
    public float xOffset = 0;
    public float yOffset = 0;
    public float zOffset = 0;

    // UI Sliders for X, Y, Z offsets
    public Slider xOffsetSlider;
    public Slider yOffsetSlider;
    public Slider zOffsetSlider;

    // Event to notify listeners (anchors) when offsets change
    public event Action OnOffsetsChanged;

    private void Start()
    {
        // Initialize the slider values and bind the change listeners
        if (xOffsetSlider != null)
        {
            xOffsetSlider.value = xOffset;
            xOffsetSlider.onValueChanged.AddListener(OnXOffsetChanged);
        }

        if (yOffsetSlider != null)
        {
            yOffsetSlider.value = yOffset;
            yOffsetSlider.onValueChanged.AddListener(OnYOffsetChanged);
        }

        if (zOffsetSlider != null)
        {
            zOffsetSlider.value = zOffset;
            zOffsetSlider.onValueChanged.AddListener(OnZOffsetChanged);
        }
    }

    // Called when X slider changes
    private void OnXOffsetChanged(float value)
    {
        xOffset = value;
        OnOffsetsChanged?.Invoke();  // Notify listeners
    }

    // Called when Y slider changes
    private void OnYOffsetChanged(float value)
    {
        yOffset = value;
        OnOffsetsChanged?.Invoke();  // Notify listeners
    }

    // Called when Z slider changes
    private void OnZOffsetChanged(float value)
    {
        zOffset = value;
        OnOffsetsChanged?.Invoke();  // Notify listeners
    }

    // Optional: Method to get current offsets as a Vector3
    public Vector3 GetOffsets()
    {
        return new Vector3(xOffset, yOffset, zOffset);
    }
}
