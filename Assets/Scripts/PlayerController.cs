// Core Gameplay Loop


using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Notice that in C#, single-line comments are introduced with //, while multi-line comments use /* (text) */.

public class PlayerController : MonoBehaviour
{    /* A full list of Unity's built-in event-handling functions (and their order of execution) can be found
     * here: https://docs.unity3d.com/Manual/ExecutionOrder.html
     * You do NOT have to memorize them all, but keep this link handy for future reference.
     * 
     * Awake(), FixedUpdate(), Start(), Update(), and the various yield functions are the most relevant but will be covered as we need them.
     */

    // We define our outlets (i.e. the components of our gameObject) here.
    Rigidbody2D _rb;
    SpriteRenderer _ball;

    // We can also define our customizable (public/private) variables here too.
    public float speed;
    public KeyCode UpKey;
    public KeyCode DownKey;
    public KeyCode LeftKey;
    public KeyCode RightKey;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _ball = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetKey(UpKey))
        {
            _rb.AddForce(Vector2.up * Time.deltaTime * speed);
        }

        if (Input.GetKey(DownKey))
        {
            _rb.AddForce(Vector2.down * Time.deltaTime * speed); // Is it realistic to have a "down" funciton?
        }

        if (Input.GetKey(LeftKey))
        {
            _rb.AddForce(Vector2.left * Time.deltaTime * speed);
        }

        if (Input.GetKey(RightKey))
        {
            _rb.AddForce(Vector2.right * Time.deltaTime * speed);
        }
    }
}
