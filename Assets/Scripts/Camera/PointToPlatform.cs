using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointToPlatform : MonoBehaviour
{
    public Transform aimPivot; 
    public Transform pivotTowards; //object to pivot towards

    public Transform pivotPointer; //child of aimPivot

    // Start is called before the first frame update
    void Start()
    {
        if(!aimPivot || !pivotTowards)
        {
            print("Transform aimPivot or pivotTowards are not configured! Compass will not work!");
        }
        else
        {
            //pivotPointer = GetComponentInChildren<Transform>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 distanceBetweenObjects = pivotTowards.position - aimPivot.position;
        aimPivot.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * Mathf.Atan2(distanceBetweenObjects.y, distanceBetweenObjects.x));
        //Mathf.Sin(2*Mathf.PI*Time.time);
        
        pivotPointer.localPosition = new Vector3(
            Mathf.Clamp((distanceBetweenObjects.magnitude/50), 0, 2.0f) + 0.2f * Mathf.Sin(2 * Mathf.PI * Time.time),
            0,
            0);

    }
}
