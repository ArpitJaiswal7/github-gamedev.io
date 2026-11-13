using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool isOccupied = false;
    public GameObject occupant;    
    MeshRenderer gridMesh;

    private void Start()
    {
        gridMesh = GetComponent<MeshRenderer>();
    }
    public void OccupyCell(GameObject occupantObject = null)
    {
        isOccupied = true;
        occupant = occupantObject;
    }

    public void FreeCell()
    {
        isOccupied = false;
        occupant = null;
    }

    public bool IsFree()
    {
        return !isOccupied;
    }

    public GameObject GetOccupant()
    {
        return occupant;
    }

    public void HighlightGrid(Color highlightColor)
    {
        gridMesh.material.color = highlightColor;
    }

    public void DisableGridHighlight()
    {
        gridMesh.material.color = Color.white;
    }
}
