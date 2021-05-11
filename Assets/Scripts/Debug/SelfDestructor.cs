using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestructor : MonoBehaviour
{
    public float timeBeforeSelfDestruct = 20f;
    void Update()
    {
        Destroy(gameObject, timeBeforeSelfDestruct);
    }
}
