using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "WeaponPose", menuName = "Weapon/Single Pose")]
public class WeaponPoseData : ScriptableObject
{
    public string poseName;
    public List<MarkerPosition> markerPositions = new List<MarkerPosition>();
}