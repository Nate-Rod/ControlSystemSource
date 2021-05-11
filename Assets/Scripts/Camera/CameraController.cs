using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Outlets
    public GameObject target;
    public Camera main;
    private Rigidbody2D _rb; 

    //Configuration
    public Vector3 offset;
    public float cameraLowerBound = 0f;
    public float dampFactor = 1f;
    public float minFOV = 60f;  //closest zoom
    public float maxFOV = 75f;  //furthest zoom

    //State tracking
    private bool targetIsAttached = false;

    // Start is called before the first frame update
    void Start()
    {
        if (target)
        {
            if (target.transform)
            {
                //offset = transform.position - target.transform.position;
                _rb = target.GetComponent<Rigidbody2D>();
                targetIsAttached = true;
            }
        }
        else
        {
            print("No target specified for CameraController! The camera will remain static.");
        }
    }

    void Update()
    {
        
        float FOV = Mathf.SmoothStep(minFOV, _rb.velocity.sqrMagnitude, dampFactor);
        main.fieldOfView = Mathf.Clamp(FOV, minFOV, maxFOV);
    }

    void LateUpdate()
    {
        if (targetIsAttached)
        {
            transform.position = target.transform.position + new Vector3(0, 0, -10); //+ offset;
        }
        transform.position = new Vector3(transform.position.x,
            Mathf.Clamp(transform.position.y, cameraLowerBound, Mathf.Infinity),
            transform.position.z);
    }
}
