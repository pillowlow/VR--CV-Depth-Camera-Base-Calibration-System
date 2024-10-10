using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRInput;
using TMPro;

public class SpacialAnchorSpawn : MonoBehaviour
{
    [SerializeField] private Transform trackingSpace;
    [SerializeField] private WebSocketClient Wcleint;
    //Specify controller to create Spatial Anchors
    [SerializeField] private Controller controller;
    private int count = 0;
    // Spatial Anchor Prefab
    public GameObject anchorPrefab;
    private Canvas canvas;
    private TextMeshProUGUI idText;
    private TextMeshProUGUI positionText;
    private TextMeshProUGUI rotationText;

    // Update is called once per frame
    void Update()
    {
        // Create Anchor when user press the index trigger on specified controller
        if(OVRInput.GetDown(OVRInput.Button.One, controller))
        {   
            CreateSpatialAnchor();
        }
    }
    
    public void CreateSpatialAnchor()
    {   

        
        // Create anchor at Controller Position and Rotation
        GameObject anchor = Instantiate(anchorPrefab, trackingSpace.position
                                            , trackingSpace.rotation);
        
        Wcleint.Log(OVRInput.GetLocalControllerPosition(controller).ToString());
        Wcleint.Log(trackingSpace.position.ToString());
        
        canvas = anchor.GetComponentInChildren<Canvas>();
        
        // Show anchor id
        idText = canvas.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        idText.text = "ID: " + count.ToString();

        // Show anchor position
        positionText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        positionText.text = anchor.transform.GetChild(0).GetChild(0).position.ToString();

        // Show anchor rotation
        rotationText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        rotationText.text = anchor.transform.GetChild(0).GetChild(0).rotation.ToString();

        // Make the anchor become a Meta Quest Spatial Anchor
        anchor.AddComponent<OVRSpatialAnchor>();

        // Increase Id by 1
        count += 1;
    }
}
