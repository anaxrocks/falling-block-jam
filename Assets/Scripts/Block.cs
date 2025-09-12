using UnityEngine;


// Block types enum
public enum BlockType
{
    Empty = 0,
    Red = 1,
    Blue = 2,
    Green = 3,
    Yellow = 4,
    // Bomb = 5
}

[System.Serializable]
public class BlockData
{
    public BlockType blockType;
    public float health = 1f;
    public bool isMatched = false;
    
    public BlockData(BlockType type)
    {
        blockType = type;
    }
}
public class Block : MonoBehaviour
{
    public int gridX, gridY;
    public BlockType blockType;
    public GameBoard gameBoard;
    private Rigidbody2D rb;
    private bool hasLanded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(int x, int y, BlockType type, GameBoard board)
    {
        gridX = x;
        gridY = y;
        blockType = type;
        gameBoard = board;
    }
    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasLanded && rb.linearVelocity.y <= 0.1f)
        {
            hasLanded = true;
            // Snap to grid
            Vector3Int cellPos = gameBoard.WorldToCell(transform.position);
            transform.position = gameBoard.CellToWorld(cellPos);
            
            // Update grid position
            gridX = cellPos.x;
            gridY = cellPos.y;
            
            rb.isKinematic = true; // Stop physics once landed
        }
    }
}
