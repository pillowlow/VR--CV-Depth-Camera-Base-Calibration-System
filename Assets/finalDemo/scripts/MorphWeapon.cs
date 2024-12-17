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
            // Use average of both anchors
            weaponAnchorPosition = (anchorPoint1.position + anchorPoint2.position) / 2f;
            weaponAnchorRotation = Quaternion.Lerp(anchorPoint1.rotation, anchorPoint2.rotation, 0.5f);
            hasValidAnchorData = true;
        }
        else if (anchor1Valid)
        {
            // Use only anchor1
            weaponAnchorPosition = anchorPoint1.position;
            weaponAnchorRotation = anchorPoint1.rotation;
            hasValidAnchorData = true;
        }
        else if (anchor2Valid)
        {
            // Use only anchor2
            weaponAnchorPosition = anchorPoint2.position;
            weaponAnchorRotation = anchorPoint2.rotation;
            hasValidAnchorData = true;
        }
        else
        {
            hasValidAnchorData = false;
        }
    }

    private void UpdateWeaponTransform()
    {
        if (!hasValidAnchorData) return;

        switch (currentState)
        {
            case WeaponState.Instance:
                transform.position = Vector3.Lerp(transform.position, weaponAnchorPosition, Time.deltaTime * smoothSpeed);
                if (updateRotation)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, weaponAnchorRotation, Time.deltaTime * smoothSpeed);
                }
                break;

            case WeaponState.OnHover:
                transform.position = Vector3.Lerp(transform.position, weaponAnchorPosition, Time.deltaTime * smoothSpeed);
                break;
        }
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
                weaponInstance.SetActive(true);
                hoverIndicator.SetActive(false);
                onEnterInstance.Invoke();
                break;

            case WeaponState.OnHover:
                weaponInstance.SetActive(false);
                hoverIndicator.SetActive(true);
                onEnterHover.Invoke();
                break;

            case WeaponState.OnSwitch:
                weaponInstance.SetActive(false);
                hoverIndicator.SetActive(false);
                onEnterSwitch.Invoke();
                break;

            case WeaponState.Mute:
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