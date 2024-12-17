using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTester : MonoBehaviour
{
    [SerializeField] private VRWeaponManager weaponManager;
    [SerializeField] private string testWeaponName;
    [SerializeField] private float startDelay = 0.5f; // Optional delay after start

    private void Start()
    {
        // If weaponManager not assigned, try to get from same GameObject
        if (weaponManager == null)
            weaponManager = GetComponent<VRWeaponManager>();

        // Use coroutine to allow a small delay after start
        StartCoroutine(TestWeapon());
    }

    private IEnumerator TestWeapon()
    {
        yield return new WaitForSeconds(startDelay);
        
        if (weaponManager != null && !string.IsNullOrEmpty(testWeaponName))
        {
            weaponManager.SwitchWeapon(testWeaponName);
            
        }
        else
        {
            Debug.LogWarning("WeaponTester: Missing weapon manager or weapon name!");
        }
    }
}