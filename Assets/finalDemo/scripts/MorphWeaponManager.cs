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
        allWeapons = new List<MorphWeapon>(FindObjectsOfType<MorphWeapon>());
        SetInitialWeapon();
    }

    private void SetInitialWeapon()
    {
        foreach (var weapon in allWeapons)
        {
            weapon.SwitchState(WeaponState.Mute);
        }

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

        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }
    }

    // Public method for external classes to initiate hover
    public bool StartWeaponHover(string weaponName)
    {
        targetWeapon = allWeapons.Find(w => w.name == weaponName);

        if (targetWeapon != null && targetWeapon != currentWeapon)
        {
            bool started = poseDetector.StartCheckingPose(weaponName);
            if (started)
            {
                HandleHoverStart();
                return true;
            }
        }
        
        return false;
    }

    // Private method to handle hover state changes
    private void HandleHoverStart()
    {
        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
        }

        currentWeapon.SwitchState(WeaponState.OnSwitch);
        targetWeapon.SwitchState(WeaponState.OnHover);
        StartCoroutine(UpdateHoverProgress());
    }

    // Called by MarkerPoseDetector when hover check completes
    public void ReceiveHoverEnd()
    {
        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }

        if (targetWeapon != null)
        {
            currentWeapon.SwitchState(WeaponState.Mute);
            targetWeapon.SwitchState(WeaponState.Instance);
            currentWeapon = targetWeapon;
            targetWeapon = null;
        }

        StopAllCoroutines();
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

    public MorphWeapon GetCurrentWeapon() => currentWeapon;
    public MorphWeapon GetTargetWeapon() => targetWeapon;
}