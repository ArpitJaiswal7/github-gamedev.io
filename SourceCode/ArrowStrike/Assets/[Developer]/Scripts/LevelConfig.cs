using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", order = 0)]
public class LevelConfig : ScriptableObject
{
    [System.Serializable]
    public struct GridRange
    {
        [Range(20, 50)] public int min;
        [Range(20, 50)] public int max;
    }

    [Header("Grid Settings")]
    public GridRange gridWidth = new GridRange { min = 20, max = 30 };
    public GridRange gridHeight = new GridRange { min = 20, max = 30 };


    [System.Serializable]
    public struct ArrowLengthRange
    {
        [Range(1, 10)] public int min;
        [Range(1, 30)] public int max;
    }

    [Header("Arrow Settings")]
    public ArrowLengthRange arrowLength = new ArrowLengthRange { min = 2, max = 30 };
    public int minNumberofArrows;
    


    public int GetRandomArrowLength()
    {
        return UnityEngine.Random.Range(arrowLength.min, arrowLength.max+1);
    }

    //[Header("Gameplay Settings")]
    //public float timeLimit = 0f;      // Optional: 0 = no timer
    //public bool hintsEnabled = true;

    //[Header("Visuals")]
    //public Color backgroundColor = Color.gray;
    //public float cameraZoom = 5f;
}
