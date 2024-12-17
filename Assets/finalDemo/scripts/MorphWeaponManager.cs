using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MorphWeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MarkerPoseDetector poseDetector;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private string defaultWeaponName = "Sword";

    private List<MorphWeapon> allWeapons;
    private MorphWeapon currentWeapon;
    private MorphWeapon targetWeapon;

    private void Start()
    {
        // Find all weapons in scene
        allWeapons = new List<MorphWeapon>(FindObjectsOfType<MorphWeapon>());
        
        // Initialize with default weapon
        SetInitialWeapon();
    }

    private void SetInitialWeapon()
    {
        // Mute all weapons first
        foreach (var weapon in allWeapons)
        {
            weapon.SwitchState(WeaponState.Mute);
        }

        // Set default weapon to instance state
        MorphWeapon defaultWeapon = allWeapons.Find(w => w.name == defaultWeaponName);
        if (defaultWeapon != null)
        {
            defaultWeapon.SwitchState(WeaponState.Instance);
            currentWeapon = defaultWeapon;
        }
        else
        {
            Debug.LogError($"Default weapon '{defaultWeaponName}' not found!");
        }

        // Hide progress text initially
        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }
    }

    // Called by MarkerPoseDetector when hover starts
    public void TellHoverStart()
    {
        string detectedPoseName = poseDetector.GetCurrentPose();
        targetWeapon = allWeapons.Find(w => w.name == detectedPoseName);

        if (targetWeapon != null && targetWeapon != currentWeapon)
        {
            // Show progress text
            if (progressText != null)
            {
                progressText.gameObject.SetActive(true);
            }

            // Set states
            currentWeapon.SwitchState(WeaponState.OnSwitch);
            targetWeapon.SwitchState(WeaponState.OnHover);

            // Start progress update
            StartCoroutine(UpdateHoverProgress());
        }
    }

    private System.Collections.IEnumerator UpdateHoverProgress()
    {
        while (poseDetector.IsChecking())
        {
            float progress = poseDetector.GetDetectionProgress();
            if (progressText != null)
            {
                progressText.text = $"Hover Progress: {progress * 100:F0}%";
            }
            yield return null;
        }
    }

    // Called by MarkerPoseDetector when hover ends
    public void TellHoverEnd()
    {
        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }

        if (targetWeapon != null)
        {
            // Complete the transition
            currentWeapon.SwitchState(WeaponState.Mute);
            targetWeapon.SwitchState(WeaponState.Instance);

            // Update current weapon reference
            currentWeapon = targetWeapon;
            targetWeapon = null;
        }

        StopAllCoroutines(); // Stop progress update
    }

    // Helper method to get current weapon
    public MorphWeapon GetCurrentWeapon()
    {
        return currentWeapon;
    }

    // Helper method to get target weapon (if in transition)
    public MorphWeapon GetTargetWeapon()
    {
        return targetWeapon;
    }
}