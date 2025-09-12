using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;


public class GameBoard : MonoBehaviour
{
    [Header("Tilemap")]
    public Grid grid;
    public Tilemap tilemap;
    public TilemapRenderer tilemapRenderer;
    public TilemapCollider2D tilemapCollider;

    [Header("Board Settings")]

    //might have to edit to camera size after, unless we want to set it skinny for all screen sizes
    public int boardWidth = 8;
    public int boardHeight = 12;
    // public float blockSize = 1f;
    public float fallSpeed = 2f;
    public bool usePhysicsGravity;

    [Header("Block Tiles")]
    public TileBase[] blockTiles; // Different colored block tiles
    public TileBase emptyTile; // Null or transparent tile for empty spaces

    private BlockData[,] blockData;
    private bool isProcessingGravity = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        grid = GetComponent<Grid>();
        tilemap = GetComponentInChildren<Tilemap>();
        tilemapRenderer = GetComponentInChildren<TilemapRenderer>();
        tilemapCollider = GetComponentInChildren<TilemapCollider2D>();
    }

    void Start()
    {
        InitializeBoard();
        GenerateInitialBlocks();
    }

    void Update()
    {
        // Handle mouse clicks on tilemap
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = WorldToCell(mousePos);
            
            if (IsValidPosition(cellPos.x, cellPos.y))
            {
                DestroyConnectedBlocks(cellPos.x, cellPos.y);
            }
        }
    }
    void InitializeBoard()
    {
        blockData = new BlockData[boardWidth, boardHeight];

        // Initialize block data array
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                blockData[x, y] = new BlockData(BlockType.Empty);
            }
        }

        // Clear the tilemap
        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, boardWidth, boardHeight, 1), new TileBase[boardWidth * boardHeight]);
    }
    
    public void GenerateInitialBlocks()
    {
        // Fill bottom portion with random blocks
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight / 2; y++)
            {
                BlockType randomType = (BlockType)Random.Range(1, System.Enum.GetValues(typeof(BlockType)).Length);
                CreateBlock(x, y, randomType);
            }
        }
    }

    public void GenerateTopRow()
    {
        int topRow = boardHeight - 1;
        for (int x = 0; x < boardWidth; x++)
        {
            if (blockData[x, topRow].blockType == BlockType.Empty && Random.Range(0f, 1f) > 0.5f)
            {
                BlockType randomType = (BlockType)Random.Range(1, System.Enum.GetValues(typeof(BlockType)).Length);
                CreateBlock(x, topRow, randomType);
            }
        }
    }

    public void CreateBlock(int x, int y, BlockType blockType)
    {
        if (!IsValidPosition(x, y) || blockType == BlockType.Empty) return;

        Vector3Int position = new Vector3Int(x, y, 0);
        TileBase tileToPlace = blockType == BlockType.Empty ? emptyTile : blockTiles[(int)blockType - 1];

        tilemap.SetTile(position, tileToPlace);
        blockData[x, y] = new BlockData(blockType);
        // If using physics gravity, add Rigidbody2D to individual tiles
        // if (usePhysicsGravity && blockType != BlockType.Empty)
        // {
        //     CreatePhysicsBlock(x, y, blockType);
        // }
    }
    
    // void CreatePhysicsBlock(int x, int y, BlockType blockType)
    // {
    //     GameObject blockObj = new GameObject($"Block_{x}_{y}");
    //     blockObj.transform.position = grid.CellToWorld(new Vector3Int(x, y, 0));
        
    //     SpriteRenderer sr = blockObj.AddComponent<SpriteRenderer>();
    //     sr.sprite = GetSpriteFromTile(blockTiles[(int)blockType - 1]);
        
    //     Rigidbody2D rb = blockObj.AddComponent<Rigidbody2D>();
    //     rb.gravityScale = 1f;
        
    //     BoxCollider2D col = blockObj.AddComponent<BoxCollider2D>();
        
    //     Block physicsBlock = blockObj.AddComponent<Block>();
    //     physicsBlock.Initialize(x, y, blockType, this);
    // }

    Sprite GetSpriteFromTile(TileBase tile)
    {
        if (tile is Tile)
            return ((Tile)tile).sprite;
        return null;
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < boardWidth && y >= 0 && y < boardHeight;
    }
    public BlockData GetBlockData(int x, int y)
    {
        if (IsValidPosition(x, y))
            return blockData[x, y];
        return null;
    }
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return grid.WorldToCell(worldPos);
    }
    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        return grid.CellToWorld(cellPos);
    }
    public void DestroyBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        Vector3Int position = new Vector3Int(x, y, 0);
        tilemap.SetTile(position, emptyTile);
        blockData[x, y] = new BlockData(BlockType.Empty);
        StartCoroutine(ProcessGravity());
    }
    //Destroy all blocks connected to the block x,y including the chosen block
    public void DestroyConnectedBlocks(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        BlockData targetBlock = blockData[x, y];
        if (targetBlock.blockType == BlockType.Empty) return;

        BlockType targetType = targetBlock.blockType;
        HashSet<Vector2Int> connectedBlocks = new HashSet<Vector2Int>();
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();

        toCheck.Enqueue(new Vector2Int(x, y));
        connectedBlocks.Add(new Vector2Int(x, y));

        // Find all connected blocks of the same type
        while (toCheck.Count > 0)
        {
            Vector2Int current = toCheck.Dequeue();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                if (IsValidPosition(neighbor.x, neighbor.y) && 
                    !connectedBlocks.Contains(neighbor) &&
                    blockData[neighbor.x, neighbor.y].blockType == targetType)
                {
                    connectedBlocks.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        // Destroy connected blocks if there are enough (minimum 2 for matching)
        if (connectedBlocks.Count > 0)
        {
            foreach (Vector2Int pos in connectedBlocks)
            {
                DestroyBlock(pos.x, pos.y);
            }
        }
    }
    public IEnumerator ProcessGravity()
    {
        if (isProcessingGravity) yield break;
        isProcessingGravity = true;

        bool blocksMovedThisPass;
        do
        {
            blocksMovedThisPass = false;

            // Process from bottom to top
            for (int y = 1; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    if (blockData[x, y].blockType != BlockType.Empty &&
                        blockData[x, y - 1].blockType == BlockType.Empty)
                    {
                        // Move block down
                        MoveBlock(x, y, x, y - 1);
                        blocksMovedThisPass = true;
                    }
                }
            }

            if (blocksMovedThisPass)
            {
                yield return new WaitForSeconds(1f / fallSpeed);
            }

        } while (blocksMovedThisPass);

        isProcessingGravity = false;
    }
    private void MoveBlock(int fromX, int fromY, int toX, int toY)
    {
        Vector3Int fromPos = new Vector3Int(fromX, fromY, 0);
        Vector3Int toPos = new Vector3Int(toX, toY, 0);
        
        // Get the tile at the from position
        TileBase tileToMove = tilemap.GetTile(fromPos);
        
        // Move tile
        tilemap.SetTile(toPos, tileToMove);
        tilemap.SetTile(fromPos, emptyTile);
        
        // Update block data
        blockData[toX, toY] = blockData[fromX, fromY];
        blockData[fromX, fromY] = new BlockData(BlockType.Empty);
    }
}
