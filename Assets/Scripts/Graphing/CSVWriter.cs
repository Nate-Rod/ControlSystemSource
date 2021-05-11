using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVWriter : MonoBehaviour
{
    [Tooltip("The write location. Defaults to *Assets/Resources/graph.csv")]
    public string filename = "graph";

    [Tooltip("The GameObject that we are logging positional (x/y) data from.")]
    public GameObject positionDataObject;

    public bool enablePrintingToConsole = true;

    [Tooltip("How long this script will collect data. Default is 10 seconds.")]
    public int runTime = 10;

    [Tooltip("How many samples this script will collect per second. Default is 5 samples per second.")]
    public int samplesPerSecond = 5;

    private int currentSample = 0; 
    private StreamWriter writer;

    // Start is called before the first frame update
    void Start()
    {
        //Check configuration variables
        CheckSetup();

        string filePath = GetPath();
        if(enablePrintingToConsole) print(filePath);
        writer = new StreamWriter(filePath);
        writer.WriteLine("t,XPos,YPos");
        StartCoroutine("Timer");
       
    }

    void CheckSetup()
    {
        if (filename == null)
        {
            throw new Exception("Filename not found!");
        }
        else if (positionDataObject == null)
        {
            throw new Exception("The GameObject you want to track is not defined!");
        }
    }
    
    //Fetches the filepath based on the system architecture (Windows vs. Mac vs. mobile)
    private string GetPath()
    {
#if UNITY_EDITOR
        return Application.dataPath + "/Resources/" + filename + ".csv";
#elif UNITY_ANDROID
        return Application.persistentDataPath+"/Resources/" + filename + ".csv";
#elif UNITY_IPHONE
        return Application.persistentDataPath+"/Resources/" + filename + ".csv";
#else
        return Application.dataPath +"/Resources/" + filename + ".csv";
#endif

    }

    //Coroutine to handle when a data point should be collected.
    IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f / samplesPerSecond);
        CollectData(currentSample);
        currentSample++;
        if (currentSample <= runTime * samplesPerSecond)
        {
            StartCoroutine("Timer");
        }
        else
        {
            CloseFile();
        }
    }

    void CollectData(int currentSample)
    {
        if(enablePrintingToConsole) print("Collecting data for entry " + (currentSample) + " of " + runTime*samplesPerSecond);
        writer.WriteLine(((float)currentSample / (float)samplesPerSecond)
            + "," + positionDataObject.transform.position.x
            + "," + positionDataObject.transform.position.y);
    }

    void CloseFile()
    {
        writer.Flush();
        writer.Close();
    }

}
