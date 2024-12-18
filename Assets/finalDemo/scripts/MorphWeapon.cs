using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum WeaponState
{
    Instance,    // Fully activated, following position and rotation
    OnHover,     // Only hover indicator active, only following position
    OnSwitch,    // Preparing to switch
    Mute        // Both disabled
}

public class MorphWeapon : MonoBehaviour
{
    [Header("Transform References")]
    [SerializeField] private Transform anchorPoint1;
    [SerializeField] private Transform anchorPoint2;

    [Header("Child Objects")]
    [SerializeField] private GameObject hoverIndicator;
    [SerializeField] private GameObject weaponInstance;

    [Header("Settings")]
    [SerializeField] private bool updateRotation = true;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Transform Offsets")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private Vector3 scaleAlignment = Vector3.one;

    [Header("Debug Visualization")]
    [SerializeField] private bool showAnchorVisualizer = true;
    [SerializeField] private GameObject anchorVisualizer;  // Assign in inspector or create at runtime

    // Events for state transitions
    public UnityEvent onEnterInstance = new UnityEvent();
    public UnityEvent onEnterHover = new UnityEvent();
    public UnityEvent onEnterSwitch = new UnityEvent();
    public UnityEvent onEnterMute = new UnityEvent();

    // Private variables
    private WeaponState currentState = WeaponState.Mute;
    private Vector3 weaponAnchorPosition;
    private Quaternion weaponAnchorRotation;
    private bool hasValidAnchorData = false;

    private void Update()
    {
        UpdateAnchorData();
        UpdateWeaponTransform();
        
    }

     private void UpdateAnchorData()
    {
        bool anchor1Valid = anchorPoint1 != null && anchorPoint1.gameObject.activeInHierarchy;
        bool anchor2Valid = anchorPoint2 != null && anchorPoint2.gameObject.activeInHierarchy;

        if (anchor1Valid && anchor2Valid)
        {
            weaponAnchorPosition = (anchorPoint1.position + anchorPoint2.position) / 2f;
            weaponAnchorRotation = Quaternion.Lerp(anchorPoint1.rotation, anchorPoint2.rotation, 0.5f);
            hasValidAnchorData = true;
        }
        else if (anchor1Valid)
        {
            weaponAnchorPosition = anchorPoint1.position;
            weaponAnchorRotation = anchorPoint1.rotation;
            hasValidAnchorData = true;
        }
        else if (anchor2Valid)
        {
            weaponAnchorPosition = anchorPoint2.position;
            weaponAnchorRotation = anchorPoint2.rotation;
            hasValidAnchorData = true;
        }
        else
        {
            hasValidAnchorData = false;
        }

        // Update visualizer
        if (showAnchorVisualizer && anchorVisualizer != null && hasValidAnchorData)
        {
            // Update position and rotation of visualizer
            anchorVisualizer.transform.position = weaponAnchorPosition;
            anchorVisualizer.transform.rotation = weaponAnchorRotation;

            // Add direction indicator (forward arrow)
            Debug.DrawLine(weaponAnchorPosition, 
                         weaponAnchorPosition + weaponAnchorRotation * Vector3.forward * 0.2f, 
                         Color.blue);
        }
    }

    private void UpdateWeaponTransform()
    {
        if (!hasValidAnchorData) return;

        switch (currentState)
        {
            case WeaponState.Instance:
                // Normalize position values
                Vector3 targetPosition = weaponAnchorPosition + positionOffset;
                // Optional: Clamp position to reasonable ranges
                targetPosition = new Vector3(
                    Mathf.Clamp(targetPosition.x, -10f, 10f),
                    Mathf.Clamp(targetPosition.y, -10f, 10f),
                    Mathf.Clamp(targetPosition.z, -10f, 10f)
                );
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
                
                if (updateRotation)
                {
                    // Normalize rotation
                    Quaternion targetRotation = weaponAnchorRotation * Quaternion.Euler(rotationOffset);
                    // Ensure rotation stays within reasonable bounds
                    Vector3 eulerAngles = targetRotation.eulerAngles;
                    eulerAngles.x = NormalizeAngle(eulerAngles.x);
                    eulerAngles.y = NormalizeAngle(eulerAngles.y);
                    eulerAngles.z = NormalizeAngle(eulerAngles.z);
                    targetRotation = Quaternion.Euler(eulerAngles);
                    
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
                }
                break;

            case WeaponState.OnHover:
                Vector3 hoverPosition = weaponAnchorPosition + positionOffset;
                hoverPosition = new Vector3(
                    Mathf.Clamp(hoverPosition.x, -10f, 10f),
                    Mathf.Clamp(hoverPosition.y, -10f, 10f),
                    Mathf.Clamp(hoverPosition.z, -10f, 10f)
                );
                transform.position = Vector3.Lerp(transform.position, hoverPosition, Time.deltaTime * smoothSpeed);
                break;
        }
    }

    private float NormalizeAngle(float angle)
    {
        // Keep angle between -180 and 180
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void SwitchState(WeaponState newState)
    {
        if (currentState == newState) return;

        // Exit current state
        switch (currentState)
        {
            case WeaponState.Instance:
                weaponInstance.SetActive(false);
                break;
            case WeaponState.OnHover:
                hoverIndicator.SetActive(false);
                break;
        }

        // Enter new state
        currentState = newState;
        switch (newState)
        {
            case WeaponState.Instance:
                updateRotation=true;
                weaponInstance.SetActive(true);
                hoverIndicator.SetActive(false);
                onEnterInstance.Invoke();
                break;

            case WeaponState.OnHover:
                updateRotation=false;
                weaponInstance.SetActive(false);
                hoverIndicator.SetActive(true);
                onEnterHover.Invoke();
                break;

            case WeaponState.OnSwitch:
                updateRotation=true;
                weaponInstance.SetActive(false);
                hoverIndicator.SetActive(false);
                onEnterSwitch.Invoke();
                break;

            case WeaponState.Mute:
                updateRotation=false;
                weaponInstance.SetActive(false);
                hoverIndicator.SetActive(false);
                onEnterMute.Invoke();
                break;
        }

        Debug.Log($"Weapon state changed to: {newState}");
    }

    public WeaponState GetCurrentState()
    {
        return currentState;
    }

    public bool HasValidTracking()
    {
        return hasValidAnchorData;
    }
}