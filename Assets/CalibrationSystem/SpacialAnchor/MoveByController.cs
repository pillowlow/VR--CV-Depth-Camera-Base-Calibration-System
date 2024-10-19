using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRInput;

public class MoveByController : MonoBehaviour
{
    [SerializeField] private Controller controller;
    [SerializeField] private float speed = 0.1f; // Set moving speed
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Get value from controller thumb stick.
        Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller);
        // Move the GameObject on xy plane.
        transform.Translate(new Vector3(axis.x, 0, axis.y) * speed * Time.deltaTime, Space.World);  
    }
}
