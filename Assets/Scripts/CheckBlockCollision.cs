using UnityEngine;
using System.Collections;

public class BlockCollisionDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask blockLayerMask = -1;
    [SerializeField] private float detectionRadius = 0.4f;
    [SerializeField] private float damageInvulnerabilityTime = 1f;
    
    private PlayerMovement playerMovement;
    private HealthManager healthSystem;
    private GameBoard gameBoard;
    private Vector3Int lastPlayerPosition;
    private bool isInvulnerable = false;
    private float lastDamageTime = 0f;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        healthSystem = GetComponent<HealthManager>();
        
        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem component not found on player!");
        }
    }

    void Start()
    {
        // Find the current game board
        gameBoard = FindFirstObjectByType<GameBoard>();
        if (gameBoard == null)
        {
            Debug.LogError("GameBoard not found!");
        }
        lastPlayerPosition = playerMovement.GetCurrentGridPosition();
    }

    void Update()
    {
        CheckForBlockCollisions();
        UpdateInvulnerability();
    }

    private void CheckForBlockCollisions()
    {
        if (gameBoard == null || healthSystem == null || isInvulnerable) return;

        Vector3Int currentPlayerPos = playerMovement.GetCurrentGridPosition();
        
        // Check if player moved to a new position
        if (currentPlayerPos != lastPlayerPosition)
        {
            CheckPositionForBlocks(currentPlayerPos);
            lastPlayerPosition = currentPlayerPos;
        }
        
        // Also check current position in case blocks fell onto player
        CheckPositionForBlocks(currentPlayerPos);
    }

    private void CheckPositionForBlocks(Vector3Int playerPos)
    {
        BlockData currentBlock = gameBoard.GetBlockData(playerPos.x, playerPos.y);
        
        if (currentBlock == null) return;

        // Handle different block types
        if (currentBlock.blockType == BlockType.Life)
        {
            // Collect life item
            HandleLifeItemCollection(playerPos.x, playerPos.y);
        }
        else if (currentBlock.blockType != BlockType.Empty && 
                 currentBlock.blockType != BlockType.Buffer)
        {
            // Player is occupying same space as a solid block - take damage
            HandleBlockCollisionDamage(playerPos.x, playerPos.y, currentBlock);
        }
    }

    private void HandleLifeItemCollection(int x, int y)
    {
        // Heal player
        healthSystem.OnLifeItemCollected();

        // Remove the life item from the game board
        gameBoard.DestroyBlock(x, y);

        // Start gravity processing
        StartCoroutine(gameBoard.ProcessGravity());

        Debug.Log($"Collected life item at ({x}, {y})!");
    }

    private void HandleBlockCollisionDamage(int x, int y, BlockData blockData)
    {
        if (isInvulnerable) return;

        SoundManager.Instance.PlaySound2D("Die");

        // Take damage from the falling block
        healthSystem.OnBlockFallDamage();
        
        // Clear blocks above player so they don't get stuck
        gameBoard.ClearBlocksAbove(x, y);
        
        // Set invulnerability period
        SetInvulnerable();
        
        Debug.Log($"Player hit by block of type: {blockData.blockType} at ({x}, {y})");
        Debug.Log($"Cleared blocks above player position ({x}, {y})");
    }

    private void SetInvulnerable()
    {
        isInvulnerable = true;
        lastDamageTime = Time.time;
        
        // Visual feedback for invulnerability
        StartCoroutine(InvulnerabilityFlash());
    }

    private void UpdateInvulnerability()
    {
        if (isInvulnerable && Time.time - lastDamageTime >= damageInvulnerabilityTime)
        {
            isInvulnerable = false;
        }
    }

    private IEnumerator InvulnerabilityFlash()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        float flashDuration = 0.1f;
        int flashCount = Mathf.RoundToInt(damageInvulnerabilityTime / (flashDuration * 2));

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        spriteRenderer.color = originalColor;
    }

    // Method to be called when blocks start falling (optional for future use)
    public void OnBlocksStartFalling()
    {
        Debug.Log("Blocks started falling - collision detection active");
    }

    // Method to manually check if player is in danger (optional utility)
    public bool IsPlayerInDanger()
    {
        if (gameBoard == null) return false;
        
        Vector3Int playerPos = playerMovement.GetCurrentGridPosition();
        return gameBoard.IsPlayerInDanger(playerPos);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize detection area in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw player grid position
        if (playerMovement != null)
        {
            Vector3Int gridPos = playerMovement.GetCurrentGridPosition();
            Vector3 worldPos = transform.position;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
        }
    }
}