using UnityEngine;
using System.Collections.Generic; // Only if you want to keep track of spawned objects, not strictly needed for this setup.

public class MapGenerator : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int mapWidth = 100;
    public int mapHeight = 100;

    [Header("Object Prefabs")]
    public GameObject treePrefab;
    public GameObject grassPrefab;

    [Header("Generation Settings")]
    // To control object density, we can think in terms of "cells"
    // Objects will be spawned randomly within each cell.
    public int cellSize = 10; // Defines the size of the area for density calculation.
                               // e.g., if 10, the 100x100 map has 10x10 cells.

    public int minTreesPerCell = 1;
    public int maxTreesPerCell = 3;

    public int minGrassPatchesPerCell = 2;
    public int maxGrassPatchesPerCell = 5;

    [SerializeField]private Transform worldObjectsParent;

    void Start()
    {
        if (treePrefab == null || grassPrefab == null)
        {
            Debug.LogError("StaticMapGenerator: TreePrefab or GrassPrefab is not assigned!");
            return;
        }
        GenerateMap();
    }

    void GenerateMap()
    {
        if (cellSize <= 0)
        {
            Debug.LogError("Cell size must be greater than 0.");
            return;
        }

        // Loop through each cell in the map
        for (float x = 0; x < mapWidth; x += cellSize)
        {
            for (float y = 0; y < mapHeight; y += cellSize)
            {
                // Define the current cell's boundaries
                Vector2 cellOrigin = new Vector2(x, y);
                // Ensure the cell doesn't go beyond map boundaries for the last cells
                float currentCellWidth = Mathf.Min(cellSize, mapWidth - x);
                float currentCellHeight = Mathf.Min(cellSize, mapHeight - y);
                Vector2 currentCellSize = new Vector2(currentCellWidth, currentCellHeight);

                PopulateCell(cellOrigin, currentCellSize);
            }
        }

        Debug.Log($"Map generation complete. {mapWidth}x{mapHeight} area processed.");
    }

    void PopulateCell(Vector2 cellOrigin, Vector2 currentCellSize)
    {
        // Generate Trees in this cell
        int treeCount = Random.Range(minTreesPerCell, maxTreesPerCell + 1);
        for (int i = 0; i < treeCount; i++)
        {
            SpawnObjectInCell(treePrefab, cellOrigin, currentCellSize);
        }

        // Generate Grass in this cell
        int grassCount = Random.Range(minGrassPatchesPerCell, maxGrassPatchesPerCell + 1);
        for (int i = 0; i < grassCount; i++)
        {
            SpawnObjectInCell(grassPrefab, cellOrigin, currentCellSize);
        }
    }

    void SpawnObjectInCell(GameObject prefab, Vector2 cellOrigin, Vector2 currentCellSize)
    {
        // Calculate a random position within the current cell
        float randomX = cellOrigin.x + Random.Range(0f, currentCellSize.x);
        float randomY = cellOrigin.y + Random.Range(0f, currentCellSize.y);

        Vector3 spawnPosition = new Vector3(randomX, randomY, 0);

        Instantiate(prefab, spawnPosition, Quaternion.identity, worldObjectsParent);
    }
}