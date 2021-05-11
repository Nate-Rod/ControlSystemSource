using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterOfMassCalculator : MonoBehaviour
{
    //Outlets
    Transform transform;
    Rigidbody2D _rb;

    void Start()
    {
        transform = GetComponent<Transform>();
        _rb = GetComponentInParent<Rigidbody2D>();

        //print(_rb.centerOfMass);
        float xPosition = _rb.centerOfMass.x;
        Vector3 newPosition = new Vector3(xPosition, 0, 0);
        transform.position = newPosition;
    }
}
