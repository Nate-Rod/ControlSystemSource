using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundColor : MonoBehaviour
{
    public Camera main;
    Color spaceColor = new Color(1, 11, 25);
    Color skyColor = new Color(143, 184, 234);

    // Update is called once per frame
    void Update()
    {
        float t = Mathf.PingPong(Time.time, 3.0f) / 3.0f;
        main.backgroundColor = Color.Lerp(spaceColor, skyColor, t);
    }
}
