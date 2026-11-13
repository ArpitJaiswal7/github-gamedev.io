using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public GridSystem gridSystem;

    private void Start()
    {
        StartCoroutine(SetupLevel());
    }

    private IEnumerator SetupLevel()
    {
        yield return StartCoroutine(gridSystem.InitializeGrid());
        yield return new WaitForSeconds(0.2f);
    }    
}
