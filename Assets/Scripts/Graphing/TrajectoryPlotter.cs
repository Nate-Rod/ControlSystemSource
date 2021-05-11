using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryPlotter : MonoBehaviour
{
    //Class variables
    public int trajectorySteps = 50;
    public float stepInterval = 1f;
    public GameObject trajectoryMarker;
    
    //Outlets
    Rigidbody2D _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Vector2[] pointsToPlot = Plot(_rb, _rb.position, _rb.velocity, trajectorySteps);
        //print(pointsToPlot);
        //print("[");
        //string message = "[";
        foreach (Vector2 point in pointsToPlot) {
            //message = message + point.ToString() + ", "; 
            GameObject marker = Instantiate(trajectoryMarker);
            marker.transform.position = new Vector3(point.x, point.y, 0);
            //trajectoryMarker.transform.Translate(point);
            //Instantiate(trajectoryMarker, marker.transform);
        }
        //message = message + "]";
       // print(message);
    }
    Vector2[] Plot(Rigidbody2D rigidbody, Vector2 pos, Vector2 velocity, int steps)
    {
        Vector2[] results = new Vector2[steps];
        float timestep = stepInterval * Time.fixedDeltaTime / Physics2D.velocityIterations;
        Vector2 gravityAccel = Physics2D.gravity * rigidbody.gravityScale * timestep * timestep;
        float drag = 1f - timestep * rigidbody.drag;
        Vector2 moveStep = velocity * timestep;

        for (int i = 0; i < steps; ++i)
        {
            moveStep += gravityAccel;
            moveStep *= drag;
            pos += moveStep;
            results[i] = pos;
        }

        return results;
    }
}
