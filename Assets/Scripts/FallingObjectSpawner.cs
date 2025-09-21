using UnityEngine;
using System.Collections;

public class RockSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject rockPrefab;
    public float spawnInterval = 2f;
    public float spawnHeight = 10f; // How high above the current board to spawn rocks
    
    [Header("Difficulty Settings")]
    public bool increaseDifficultyOverTime = true;
    public float difficultyIncreaseRate = 0.1f; // How much faster spawning gets per minute
    public float minSpawnInterval = 0.5f; // Minimum time between spawns
    
    private Camera playerCamera;
    private float currentSpawnInterval;
    private float gameStartTime;
    private GameBoardManager gameBoardManager;
    
    void Start()
    {
        playerCamera = Camera.main;
        currentSpawnInterval = spawnInterval;
        gameStartTime = Time.time;
        gameBoardManager = GameBoardManager.Instance;
        
        // Start spawning rocks
        StartCoroutine(SpawnRocks());
    }
    
    IEnumerator SpawnRocks()
    {
        while (true)
        {
            yield return new WaitForSeconds(currentSpawnInterval);
            
            SpawnRock();
            
            // Increase difficulty over time
            if (increaseDifficultyOverTime)
            {
                float gameTime = Time.time - gameStartTime;
                float difficultyMultiplier = 1f + (gameTime / 60f) * difficultyIncreaseRate;
                currentSpawnInterval = Mathf.Max(minSpawnInterval, spawnInterval / difficultyMultiplier);
            }
        }
    }
    
    void SpawnRock()
    {
        if (rockPrefab == null || gameBoardManager == null || gameBoardManager.curBoard == null) return;
        
        GameBoard currentBoard = gameBoardManager.curBoard;
        
        // Get random X position within the current board's width
        int randomX = Random.Range(0, currentBoard.boardWidth);
        
        // Convert grid position to world position and get the CENTER of the cell
        Vector3Int gridPos = new Vector3Int(randomX, 0, 0); // Y=0 is the top of the board
        Vector3 cellCenterWorld = currentBoard.CellToWorld(gridPos);
        
        // CellToWorld gives us the bottom-left corner of the cell, so we need to offset to center
        Vector3 cellSize = currentBoard.grid.cellSize;
        cellCenterWorld.x += cellSize.x * 0.5f; // Move to center of cell horizontally
        cellCenterWorld.y += cellSize.y * 0.5f; // Move to center of cell vertically
        
        // Spawn above the board at the exact center of the grid cell
        Vector3 spawnPosition = new Vector3(cellCenterWorld.x, cellCenterWorld.y + spawnHeight, 0f);
        
        // Create the rock
        GameObject rock = Instantiate(rockPrefab, spawnPosition, Quaternion.identity);
        
        // Optional: Add slight random rotation for visual variety
        rock.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
    }
    
    // Call this method to stop spawning (e.g., when game is paused or player dies)
    public void StopSpawning()
    {
        StopAllCoroutines();
    }
    
    // Call this method to resume spawning
    public void ResumeSpawning()
    {
        StartCoroutine(SpawnRocks());
    }
    
    // Call this method to reset difficulty (e.g., when game restarts)
    public void ResetDifficulty()
    {
        currentSpawnInterval = spawnInterval;
        gameStartTime = Time.time;
    }
}