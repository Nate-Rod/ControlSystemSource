using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphHanlder : MonoBehaviour
{
    void Awake()
    {
        List<Dictionary<string, object>> data = CSVReader.Read("example");
        for (var i = 0; i < data.Count; i++)
        {
            print("name " + data[i]["name"] + " " +
                   "age " + data[i]["age"] + " " +
                   "speed " + data[i]["speed"] + " " +
                   "desc " + data[i]["description"]);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
