using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponHoverTest
{
    public KeyCode triggerKey;
    public string weaponName;
}

public class WeaponHoverTester : MonoBehaviour
{
    [SerializeField] private MorphWeaponManager weaponManager;
    [SerializeField] private List<WeaponHoverTest> hoverTests = new List<WeaponHoverTest>();

    void Update()
    {
        foreach (var test in hoverTests)
        {
            if (Input.GetKeyDown(test.triggerKey))
            {
                bool started = weaponManager.StartWeaponHover(test.weaponName);
                Debug.Log($"Attempting to hover {test.weaponName}: {(started ? "Started" : "Failed")}");
            }
        }
    }
}