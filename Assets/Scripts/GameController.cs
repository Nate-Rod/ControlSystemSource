using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Random;

public class GameController : MonoBehaviour
{
    public GameObject rocketPrefab;

    [Serializable]
    public class SpawnCharacteristics
    {
        public float[] heightRange = { 300, 1000 };
        public float[] xRange = { -300, 300 };
    }

    public SpawnCharacteristics spawnCharacteristics;

    void Start()
    {
        SpawnRocket();
    }

    void SpawnRocket()
    {
        Vector3 spawnPoint = new Vector3(UnityEngine.Random.Range(spawnCharacteristics.xRange[0], spawnCharacteristics.xRange[1]),
                                         UnityEngine.Random.Range(spawnCharacteristics.heightRange[0], spawnCharacteristics.heightRange[1]),
                                         0);
        rocketPrefab.transform.position = spawnPoint;
    }
}

