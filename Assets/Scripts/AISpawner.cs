using System.Collections.Generic;
using UnityEngine;

public class AISpawner : MonoBehaviour
{
    public List<GameObject> enemyPrefabs; // Assign your cyber-foe prefabs
    public float spawnInterval = 5f;
    public float spawnDistanceMultiplier = 1.2f; // How far outside the camera view to spawn
    public int maxEnemies = 20; // Max concurrent enemies

    private Camera mainCamera;
    private float spawnTimer;
    private List<GameObject> activeEnemies = new List<GameObject>();

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("AISpawner: Main Camera not found!");
            enabled = false;
            return;
        }
        spawnTimer = spawnInterval;
    }

    void Update()
    {
        // Clean up list of destroyed enemies
        activeEnemies.RemoveAll(item => item == null);

        if (activeEnemies.Count >= maxEnemies)
        {
            return; // Don't spawn if max is reached
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnEnemy();
            spawnTimer = spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("AISpawner: No enemy prefabs assigned.");
            return;
        }

        // Determine spawn position outside camera view
        float camHeight = mainCamera.orthographicSize;
        float camWidth = mainCamera.orthographicSize * mainCamera.aspect;

        float spawnX = 0f, spawnY = 0f;
        int edge = Random.Range(0, 4); // 0: top, 1: bottom, 2: left, 3: right

        Vector3 cameraPos = mainCamera.transform.position;

        switch (edge)
        {
            case 0: // Top
                spawnX = Random.Range(cameraPos.x - camWidth * spawnDistanceMultiplier, cameraPos.x + camWidth * spawnDistanceMultiplier);
                spawnY = cameraPos.y + camHeight * spawnDistanceMultiplier;
                break;
            case 1: // Bottom
                spawnX = Random.Range(cameraPos.x - camWidth * spawnDistanceMultiplier, cameraPos.x + camWidth * spawnDistanceMultiplier);
                spawnY = cameraPos.y - camHeight * spawnDistanceMultiplier;
                break;
            case 2: // Left
                spawnX = cameraPos.x - camWidth * spawnDistanceMultiplier;
                spawnY = Random.Range(cameraPos.y - camHeight * spawnDistanceMultiplier, cameraPos.y + camHeight * spawnDistanceMultiplier);
                break;
            case 3: // Right
                spawnX = cameraPos.x + camWidth * spawnDistanceMultiplier;
                spawnY = Random.Range(cameraPos.y - camHeight * spawnDistanceMultiplier, cameraPos.y + camHeight * spawnDistanceMultiplier);
                break;
        }

        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0); // Z=0 for 2D
        GameObject selectedEnemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        GameObject newEnemy = Instantiate(selectedEnemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(newEnemy);


    }

    public void EnemyDefeated(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
    }
}