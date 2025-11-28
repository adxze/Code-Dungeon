using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int height, width;
    [SerializeField] private Tile tilePrefab;

    [SerializeField] private Transform cam; 

    private void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        cam.transform.position = new Vector3(width / 2f - 0.5f, height / 2f -0.5f, -10);
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                var spawnedTile = Instantiate(tilePrefab, new Vector3(x, y), quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";
            }
        }
    }
}
