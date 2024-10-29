
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine.VFX; // Required for VFX Graph


[CreateAssetMenu(fileName = "NewVFXState", menuName = "VFXHelper/VFXStateAsset")]
public class VFXStateAsset : ScriptableObject
{   
    public MaterialState materialState;
    public VFXGraphState vfxGraphState;
    public ParticleSystemState particleSystemState;
    public TransformState transformState;
    public LightState lightState;
    

}