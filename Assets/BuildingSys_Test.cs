using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingSys_Test : MonoBehaviour
{
    [SerializeField]Tile tile;
    [SerializeField]Tilemap grid;
    Vector3 mousepos;
    Vector3Int cellpos;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cellpos = grid.WorldToCell(mousepos);
        if (Input.GetMouseButtonDown(0) && !grid.HasTile(cellpos))
        {
            grid.SetTile(cellpos, tile);
        }
        if (Input.GetMouseButtonDown(1) && grid.HasTile(cellpos))
        {
            grid.SetTile(cellpos, null);
        }
    }
}
