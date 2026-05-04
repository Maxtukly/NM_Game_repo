using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HIghlightTest : MonoBehaviour
{
    [SerializeField] Tilemap highlightMap;
    [SerializeField] Tile highlightTile;
    [SerializeField] Tile RegularTile;

    // Update is called once per frame
    Vector3Int lastTilePos = Vector3Int.zero; // Store last tile position to avoid unnecessary updates
    void Update()
    {
        // Example: Basic Mouseover Highlight
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int tilePos = highlightMap.WorldToCell(mouseWorldPos);


        if (tilePos != lastTilePos)
        {
            highlightMap.SetTile(lastTilePos, RegularTile); // Remove old highlight
            highlightMap.SetTile(tilePos, highlightTile); // Add new highlight
            lastTilePos = tilePos;
        }

    }
}