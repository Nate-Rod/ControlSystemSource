using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionTrailer : MonoBehaviour
{
    public GameObject Breadcrumb;

    public float spawnsPerSecond = 5f;

    void Start()
    {
        if (Breadcrumb != null) { 
            StartCoroutine("Timer");
        }
            
    }

    IEnumerator Timer()
    {
        yield return new WaitForSeconds(1/spawnsPerSecond);
        SpawnBreadcrumb();
        StartCoroutine("Timer");
    }
    void SpawnBreadcrumb()
    {
        Instantiate(Breadcrumb, gameObject.transform.position, Quaternion.identity);
    }
}
