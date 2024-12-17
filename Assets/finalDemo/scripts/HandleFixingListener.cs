using UnityEngine;
using System;
using UnityEngine.Events;
using System.Collections;
using TMPro;



public class HandleFixingListener : MonoBehaviour
{
    [Header("Transform References")]
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;

    [Header("State Configuration")]
    [SerializeField] private VRWeaponManager weaponManager;
    [SerializeField] private TextMeshProUGUI stateProgressText;
    [SerializeField] private FixingState initState = FixingState.Separate;

    [Header("Combine Parameters")]
    [SerializeField] private float positionTolerance = 0.05f;
    [SerializeField] private float rotationTolerance = 5f;
    [SerializeField] private float hoverCombineTime = 0.5f;
    [SerializeField] private float combineVerticalTime = 1.0f;

    [Header("Separate Parameters")]
    [SerializeField] private float horizontalTolerance = 0.05f;
    [SerializeField] private float hoverSeparateTime = 1.0f;
    [SerializeField] private float separateUnfixedTime = 0.2f;

    [Header("Event Categories")]
    [SerializeField] private UnityEvent onInitialize;      // New initialize event
    [SerializeField] private UnityEvent onHoverCombine;
    [SerializeField] private UnityEvent onCombined;
    [SerializeField] private UnityEvent onHoverSeparate;
    [SerializeField] private UnityEvent onSeparate;

    // Current state
    private FixingState currentState = FixingState.Separate;
    private float stateTimer = 0f;
    private Vector3 lastLeftPosition;
    private Quaternion lastLeftRotation;
    private Vector3 relativePosOffset;
    private Quaternion relativeRotOffset;

    private void Start()
    {
        InitializeState();
    }

    public void InitializeState()
    {
        currentState = initState;
        onInitialize?.Invoke();     // Invoke initialize events instead of direct weapon manager call
        UpdateProgressText(0);
        
    }

    private void Update()
    {
        switch (currentState)
        {
            case FixingState.Separate:
                CheckForHoverCombine();
                break;

            case FixingState.HoverCombine:
                CheckForCombine();
                break;

            case FixingState.Combined:
                CheckForHoverSeparate();
                break;

            case FixingState.HoverSeparate:
                CheckForSeparate();
                break;
        }
    }

    private void CheckForHoverCombine()
    {
        if (AreControllersFixed())
        {
            stateTimer += Time.deltaTime;
            UpdateProgressText(stateTimer / hoverCombineTime);

            if (stateTimer >= hoverCombineTime)
            {
                currentState = FixingState.HoverCombine;
                SaveRelativeTransform();
                onHoverCombine?.Invoke();
                stateTimer = 0f;
            }
        }
        else
        {
            stateTimer = 0f;
            UpdateProgressText(0);
        }
    }

     private void CheckForCombine()
    {
        if (AreControllersVertical())
        {
            stateTimer += Time.deltaTime;
            UpdateProgressText(stateTimer / combineVerticalTime);

            if (stateTimer >= combineVerticalTime)
            {
                currentState = FixingState.Combined;
                onCombined?.Invoke();    // Just invoke the event
                stateTimer = 0f;
            }
        }
        else
        {
            stateTimer = 0f;
            UpdateProgressText(0);
        }
    }

    private void CheckForHoverSeparate()
    {
        if (AreControllersHorizontal())
        {
            stateTimer += Time.deltaTime;
            UpdateProgressText(stateTimer / hoverSeparateTime);

            if (stateTimer >= hoverSeparateTime)
            {
                currentState = FixingState.HoverSeparate;
                onHoverSeparate?.Invoke();
                stateTimer = 0f;
            }
        }
        else
        {
            stateTimer = 0f;
            UpdateProgressText(0);
        }
    }

    private void CheckForSeparate()
    {
        if (!AreControllersFixed())
        {
            stateTimer += Time.deltaTime;
            UpdateProgressText(stateTimer / separateUnfixedTime);

            if (stateTimer >= separateUnfixedTime)
            {
                currentState = FixingState.Separate;
                onSeparate?.Invoke();    // Just invoke the event
                stateTimer = 0f;
            }
        }
        else
        {
            stateTimer = 0f;
            UpdateProgressText(0);
        }
    }

    private bool AreControllersFixed()
    {
        Vector3 currentRelativePos = rightController.position - leftController.position;
        Quaternion currentRelativeRot = Quaternion.Inverse(leftController.rotation) * rightController.rotation;

        if (lastLeftPosition == Vector3.zero)
        {
            lastLeftPosition = leftController.position;
            lastLeftRotation = leftController.rotation;
            return false;
        }

        bool positionFixed = Vector3.Distance(currentRelativePos, lastLeftPosition) < positionTolerance;
        bool rotationFixed = Quaternion.Angle(currentRelativeRot, lastLeftRotation) < rotationTolerance;

        lastLeftPosition = currentRelativePos;
        lastLeftRotation = currentRelativeRot;

        return positionFixed && rotationFixed;
    }

    private void SaveRelativeTransform()
    {
        relativePosOffset = rightController.position - leftController.position;
        relativeRotOffset = Quaternion.Inverse(leftController.rotation) * rightController.rotation;
    }

    private bool AreControllersVertical()
    {
        Vector3 relativePos = rightController.position - leftController.position;
        return Mathf.Abs(relativePos.x) < positionTolerance &&
               Mathf.Abs(relativePos.z) < positionTolerance &&
               Mathf.Abs(relativePos.y) > positionTolerance;
    }

    private bool AreControllersHorizontal()
    {
        Vector3 relativePos = rightController.position - leftController.position;
        return Mathf.Abs(relativePos.z) < horizontalTolerance &&
               Mathf.Abs(relativePos.x) > horizontalTolerance &&
               Mathf.Abs(relativePos.y) > horizontalTolerance;
    }

    private void UpdateProgressText(float progress)
    {
        if (stateProgressText != null)
        {
            stateProgressText.text = $"{currentState}: {(progress * 100):F0}%";
        }
    }

    // Public method to get current state
    public FixingState GetCurrentState()
    {
        return currentState;
    }
}

public enum FixingState
{
    Separate,
    HoverCombine,
    Combined,
    HoverSeparate
}