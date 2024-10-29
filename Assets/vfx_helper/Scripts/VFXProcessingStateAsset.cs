using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine.VFX; // Required for VFX Graph


[CreateAssetMenu(fileName = "NewVFXProcessingState", menuName = "VFXHelper/VFXProcessingStateAsset")]
public class VFXProcessingStateAsset : ScriptableObject
{
    public VFXProcessingState processingState;

    // You can initialize the processing state in the constructor or through the Inspector
    public VFXProcessingStateAsset()
    {
        // Default values for the processing state
        processingState = new VFXProcessingState(
            duration: 1.0f,
            curve: AnimationCurve.Linear(0, 0, 1, 1)
        );
    }
}