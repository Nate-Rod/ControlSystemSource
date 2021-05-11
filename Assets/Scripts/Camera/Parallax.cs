using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class Parallax : MonoBehaviour
{
    //private float length, height;
    private Vector2 startpos, size;
    public GameObject cam;
    public float horizontalParallaxEffect = 0f;
    public float verticalParallaxEffect = 0.3f;

    void Start()
    {
        startpos = new Vector2(transform.position.x, transform.position.y);
        size = new Vector2(GetComponent<SpriteRenderer>().bounds.size.x,
                               GetComponent<SpriteRenderer>().bounds.size.y);
    }

    // Update is called once per frame
    void Update()
    {
        float temp = (cam.transform.position.x * (1 - horizontalParallaxEffect));
        float xDist = (cam.transform.position.x * horizontalParallaxEffect);
        float yDist = cam.transform.position.y * verticalParallaxEffect;

        transform.position = new Vector3(startpos.x + xDist, startpos.y + yDist, transform.position.z);

        if (temp > startpos.x + size.x)
        {
            //print(temp + " > " + startpos + " + " + length);
            //print("Resetting " + gameObject.ToString() + " to right");
            startpos.x += size.x;
        }
        else if (temp < startpos.x - size.x)
        {
            //print(temp + " < " + startpos + " - " + length);
            //print("Resetting " + gameObject.ToString() + " to left");
            startpos.x -= size.x;
        }
    }
}
