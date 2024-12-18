using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Collections;
using static OVRInput;
using System.Linq;  // Add this for ToDictionary
using System;  // Add this for Exception
using UnityEngine.Events;

public class MarkerPoseDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CalibrationCalculator calibrationCalculator;
    
    [Header("Detection Settings")]
    [SerializeField] private float positionTolerance = 0.05f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private int requiredConsistentChecks = 4;
    [SerializeField]  private bool checkPose = false;
     [SerializeField] private string stateToCheck = null;  // The specific weapon pose we're looking for
      [SerializeField] private float maxCheckDuration = 10f;  // Maximum time to try detecting

    [Header("Weapon Poses")]
    //[SerializeField] private List<WeaponPose> weaponPoses = new List<WeaponPose>();
    [SerializeField] private Controller controller;
    [SerializeField] private string RecordingPoseName;

    [Header("Pose References")]
    [SerializeField] private List<WeaponPoseData> weaponPoses = new List<WeaponPoseData>();
    [Header("Event when hover")]
    public UnityEvent OnHoverStart;
    public UnityEvent OnHoverEnd;


    private string currentPoseName = "";
    private int consistentChecksCount = 0;
    private WeaponPoseData lastMatchedPose = null;
    private bool isChecking = false;
    private float checkStartTime;

    private void Update()
    {
        if (checkPose && !isChecking)
        {
            StartCoroutine(CheckPoseRoutine());
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            SaveCurrentPoseAsAsset(RecordingPoseName);
        }

        /*
        if(Input.GetKeyDown(KeyCode.R))
        {
            StartCheckingPose("Shield");
        }*/
        /*
        if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger, controller) ){
            StartHover();
        }*/

    }
    private void StartHover(){
        OnHoverStart?.Invoke();
    }
    private void EndHover(){
        OnHoverEnd?.Invoke();
        
    }
    public bool StartCheckingPose(string weaponName)
    {
        if (checkPose)
        {
            Debug.Log($"Already checking pose: {stateToCheck}");
            return false;
        }

        stateToCheck = weaponName;
        checkPose = true;
        consistentChecksCount = 0;
        lastMatchedPose = null;
        checkStartTime = Time.time;
        StartHover();
        StartCoroutine(CheckPoseRoutine());
        Debug.Log($"Started checking for weapon pose: {weaponName}");
        return true;
    }

    private IEnumerator CheckPoseRoutine()
    {
        float lastCheckTime = 0;
        
        while (checkPose)
        {
            if (Time.time - checkStartTime > maxCheckDuration)
            {
                Debug.Log("Pose check timed out");
                EndChecking(false);
                yield break;
            }

            if (Time.time - lastCheckTime >= checkInterval)
            {
                bool poseMatched = CheckCurrentPose();
                
                if (poseMatched)
                {   
                    consistentChecksCount++;
                    float progress = (float)consistentChecksCount / requiredConsistentChecks;
                    Debug.Log($"Pose: {stateToCheck} check progress {progress:F2}");

                    if (consistentChecksCount >= requiredConsistentChecks)
                    {
                        Debug.Log("Pose check successful");
                        EndChecking(true);
                        yield break;
                    }
                }
                else
                {
                    if (consistentChecksCount > 0)
                    {
                        Debug.Log($"Pose check interrupted at {GetDetectionProgress():F2}");
                    }
                    consistentChecksCount = 0;
                }
                
                lastCheckTime = Time.time;
            }

            yield return new WaitForSeconds(checkInterval); // Small wait for performance
        }
    }

    private bool CheckCurrentPose()
    {
        if (string.IsNullOrEmpty(stateToCheck))
            return false;

        WeaponPoseData targetPose = weaponPoses.Find(p => p.poseName == stateToCheck);
        if (targetPose == null)
        {
            Debug.LogWarning($"No pose data found for weapon: {stateToCheck}");
            return false;
        }

        var currentPositions = calibrationCalculator.GetAllRelativePositions();
        
        foreach (var markerPos in targetPose.markerPositions)
        {
            if (!currentPositions.TryGetValue(markerPos.markerId, out var currentPos) || 
                Vector3.Distance(currentPos.position, markerPos.GetPosition()) > positionTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private void EndChecking(bool success)
    {
        checkPose = false;
        stateToCheck = null;
        Debug.Log("Pose check end");
        EndHover();
        StopAllCoroutines();
    }

      public void SaveCurrentPoseAsAsset(string poseName)
    {
        #if UNITY_EDITOR
        // Create new pose asset
        var poseAsset = ScriptableObject.CreateInstance<WeaponPoseData>();
        poseAsset.poseName = poseName;
        
        // Get current positions
        var positions = calibrationCalculator.GetAllRelativePositions();
        foreach (var pos in positions)
        {
            poseAsset.markerPositions.Add(new MarkerPosition(
                pos.Key, 
                pos.Value.position
            ));
        }

        // Save asset
        string directory = "Assets/PoseData";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string assetPath = $"{directory}/{poseName}.asset";
        UnityEditor.AssetDatabase.CreateAsset(poseAsset, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        Debug.Log($"Saved pose asset: {assetPath}");
        #endif
    }
    private string GetPoseSavePath(string poseName)
    {
        // Save directly in Assets/PoseData
        string directory = Path.Combine(Application.dataPath, "PoseData");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"Created directory at: {directory}");
        }
        
        string fullPath = Path.Combine(directory, $"{poseName}.json");
        Debug.Log($"Full save path: {fullPath}");

        return fullPath;
    }
    

    private void SetCurrentPose(string poseName)
    {
        currentPoseName = poseName;
        Debug.Log($"Detected pose: {poseName}");
    }

    public string GetCurrentPose()
    {
        return currentPoseName;
    }

     public float GetDetectionProgress()
    {
        if (!checkPose || consistentChecksCount == 0)
            return 0f;
        
        return (float)consistentChecksCount / requiredConsistentChecks;
    }

    public bool IsChecking()
    {
        return checkPose;
    }

    public string GetCurrentlyChecking()
    {
        return stateToCheck;
    }
}




[System.Serializable]
public class WeaponPose
{
    public string poseName;
    public List<MarkerPosition> markerPositions = new List<MarkerPosition>();

    // Helper method to convert to dictionary when needed
    public Dictionary<int, Vector3> ToPositionDictionary()
    {
        return markerPositions.ToDictionary(m => m.markerId, m => m.position.ToVector3());
    }
}



[System.Serializable]
public class MarkerPosition
{
    public int markerId;
    public SerializableVector3 position;  // Changed from Vector3 to SerializableVector3

    public MarkerPosition(int id, Vector3 pos)
    {
        markerId = id;
        position = new SerializableVector3(pos);
    }
    public Vector3 GetPosition(){
        return position.ToVector3();
    }
}



[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

