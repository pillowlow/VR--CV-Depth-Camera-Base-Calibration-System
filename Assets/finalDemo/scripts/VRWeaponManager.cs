using UnityEngine;
using System;
using System.Collections.Generic;

// Serializable struct for weapon configuration
[Serializable]
public struct WeaponAlignment
{
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 scaleOffset;

    // Constructor with default values
    public WeaponAlignment(Vector3 pos, Vector3 rot, Vector3 scale)
    {
        positionOffset = pos;
        rotationOffset = rot;
        scaleOffset = scale;
    }

    // Static property for default values
    public static WeaponAlignment Default => new WeaponAlignment(
        Vector3.zero,           // Default position offset
        Vector3.zero,           // Default rotation offset
        Vector3.one            // Default scale offset (1,1,1)
    );
}

[Serializable]
public struct SwitchableWeapon
{
    public string weaponName;
    public GameObject leftHandObject;
    public GameObject rightHandObject;
    public WeaponAlignment alignmentData; // Changed from Transform to WeaponAlignment
}

public class VRWeaponManager : MonoBehaviour
{
    [Header("Tracking References")]
    [SerializeField] private Transform leftHandTracker;
    [SerializeField] private Transform rightHandTracker;
    
    [Header("Default Weapon")]
    [SerializeField] private string initialWeaponName;

    [Header("Weapon Configuration")]
    [SerializeField] private List<SwitchableWeapon> weapons = new List<SwitchableWeapon>();

    public string CurrentWeapon { get; private set; } = "";

    private void Start()
    {
        InitializeWeapons();
        if (!string.IsNullOrEmpty(initialWeaponName))
        {
            SwitchWeapon(initialWeaponName);
        }
    }

    private void AlignWeaponToHand(GameObject weaponObject, Transform tracker, WeaponAlignment alignment)
    {
        if (tracker == null || weaponObject == null) return;

        weaponObject.transform.SetParent(tracker);
        weaponObject.transform.localPosition = alignment.positionOffset;
        weaponObject.transform.localRotation = Quaternion.Euler(alignment.rotationOffset);
       
        // Multiply current scale by the scale offset
        Vector3 currentScale = weaponObject.transform.localScale;
        weaponObject.transform.localScale = new Vector3(
            currentScale.x * alignment.scaleOffset.x,
            currentScale.y * alignment.scaleOffset.y,
            currentScale.z * alignment.scaleOffset.z
        );
    }


    private void InitializeWeapons()
    {
        // Deactivate all weapons initially
        foreach (var weapon in weapons)
        {
            if (weapon.leftHandObject != null)
                weapon.leftHandObject.SetActive(false);
            if (weapon.rightHandObject != null)
                weapon.rightHandObject.SetActive(false);
        }
    }

    // Public method to switch weapons by name
    // Public void method for Unity Event System
public void SwitchWeapon(string weaponName)
{   
    Debug.Log($" Start Switch to '{weaponName}'.");
    // Deactivate current weapon first
    DeactivateCurrentWeapon();
    Debug.Log($" Switch to '{weaponName}'. deactive prepared");
    // Find and activate new weapon
    SwitchableWeapon? newWeapon = weapons.Find(w => w.weaponName.Equals(weaponName, StringComparison.OrdinalIgnoreCase));
    
    if (newWeapon.HasValue)
    {
        ActivateWeapon(newWeapon.Value);
        CurrentWeapon = weaponName;
        Debug.Log($"Switch to '{weaponName}'.");
    }
    else 
    {
        Debug.LogWarning($"Weapon '{weaponName}' not found in the weapons list.");
    }
}

// Optional: Internal method if you need to check success in scripts
private bool TrySwitchWeapon(string weaponName)
{
    // Deactivate current weapon first
    DeactivateCurrentWeapon();

    // Find and activate new weapon
    SwitchableWeapon? newWeapon = weapons.Find(w => w.weaponName.Equals(weaponName, StringComparison.OrdinalIgnoreCase));

    if (newWeapon.HasValue)
    {
        ActivateWeapon(newWeapon.Value);
        CurrentWeapon = weaponName;
        return true;
    }

    Debug.LogWarning($"Weapon '{weaponName}' not found in the weapons list.");
    return false;
}

    private void DeactivateCurrentWeapon()
    {
        if (string.IsNullOrEmpty(CurrentWeapon))
            return;

        var currentWeapon = weapons.Find(w => w.weaponName == CurrentWeapon);
        if (currentWeapon.leftHandObject != null)
            currentWeapon.leftHandObject.SetActive(false);
        if (currentWeapon.rightHandObject != null)
            currentWeapon.rightHandObject.SetActive(false);
    }

    private void ActivateWeapon(SwitchableWeapon weapon)
    {
        // Handle left hand weapon
        if (weapon.leftHandObject != null)
        {
            weapon.leftHandObject.SetActive(true);
            AlignWeaponToHand(weapon.leftHandObject, leftHandTracker, weapon.alignmentData);
        }

        // Handle right hand weapon
        if (weapon.rightHandObject != null)
        {
            weapon.rightHandObject.SetActive(true);
            AlignWeaponToHand(weapon.rightHandObject, rightHandTracker, weapon.alignmentData);
        }
    }

    
    private void Update()
    {
        if (!string.IsNullOrEmpty(CurrentWeapon))
        {
            var currentWeapon = weapons.Find(w => w.weaponName == CurrentWeapon);
            
            // Continuously update positions (if needed)
            if (currentWeapon.leftHandObject != null && leftHandTracker != null)
                AlignWeaponToHand(currentWeapon.leftHandObject, leftHandTracker, currentWeapon.alignmentData);
            
            if (currentWeapon.rightHandObject != null && rightHandTracker != null)
                AlignWeaponToHand(currentWeapon.rightHandObject, rightHandTracker, currentWeapon.alignmentData);
        }
    }
    // Optional: Method to get all available weapon names
    public List<string> GetAvailableWeapons()
    {
        List<string> weaponNames = new List<string>();
        foreach (var weapon in weapons)
        {
            weaponNames.Add(weapon.weaponName);
        }
        return weaponNames;
    }
}