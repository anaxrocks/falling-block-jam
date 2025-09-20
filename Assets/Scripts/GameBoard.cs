using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System;

/* Generates a gameboard of size boardwidth * boardheight. For the intended use, multiple gameboards should be
   stacked on top for the appearance of an infinite board.
*/
public class GameBoard : MonoBehaviour
{
    public Grid grid;
    public Tilemap tilemap;
    public TilemapRenderer tilemapRenderer;
    public TilemapCollider2D tilemapCollider;

    [Header("Life Items")]
    public TileBase lifeItemTile; // Sprite for life items
    public AnimatedTile[] lifeItemAnimated;
    public float chanceOfLifeItem = 0.05f; // 5% chance of generating a life item
    private int numSpecialBlocks = 4; // Empty, Buffer, Hard, Life
    [Header("Tools")]
    public float chanceOfTool = 0.1f; //0.5% chance of generating tool
    public AnimatedTile[] toolTiles;

    [Header("Board Settings")]

    //might have to edit to camera size after, unless we want to set it skinny for all screen sizes
    public int boardWidth = 8;
    public int boardHeight = 12; //total height of board
    public int bufferHeight = 5; //buffer into the boardheight
    private int levelHeight; //boardHeight - bufferHeight
    public float chanceOfHardBlock = 0.08f;
    // public float blockSize = 1f;
    public float fallSpeed = 2f;

    [Header("Block Tiles")]
    public TileBase[] blockTiles; // Different colored block tiles
    public TileBase bufferTile; // Null or transparent tile for empty spaces
    public TileBase[] hardTiles; //3 sprites of hard blocks

    private BlockData[,] blockData;
    private bool isProcessingGravity = false;
    public bool bufferDestroyed = false;
    [Header("Player settings")]
    private PlayerMovement player;
    void Awake()
    {
        levelHeight = boardHeight - bufferHeight;
        grid = GetComponent<Grid>();
        tilemap = GetComponentInChildren<Tilemap>();
        tilemapRenderer = GetComponentInChildren<TilemapRenderer>();
        tilemapCollider = GetComponentInChildren<TilemapCollider2D>();
        player = FindFirstObjectByType<PlayerMovement>();
    }
    void Update()
    {
        // // Handle mouse clicks on tilemap
        // if (Input.GetMouseButtonDown(0))
        // {
        //     Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //     Vector3Int cellPos = WorldToCell(mousePos);

        //     if (IsValidPosition(cellPos.x, cellPos.y))
        //     {
        //         DestroyConnectedBlocks(cellPos.x, cellPos.y);
        //     }
        // }
    }
    public void Init()
    {
        InitializeBoard();
        GenerateInitialBlocks();
    }

    public void InitializeBoard()
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
            for (int y = 0; y > -levelHeight; y--)
            {
                float rng = UnityEngine.Random.Range(0.0f, 1.0f);
                // Small chance for life item
                if (rng < chanceOfLifeItem)
                {
                    CreateBlock(x, y, BlockType.Life);
                }
                else if (rng < chanceOfHardBlock + chanceOfLifeItem)
                {
                    CreateBlock(x, y, BlockType.Hard);
                }
                else if (rng < chanceOfHardBlock + chanceOfLifeItem + chanceOfTool)
                {
                    CreateBlock(x, y, BlockType.Tool);
                }
                else
                {
                    BlockType randomType = (BlockType)UnityEngine.Random.Range(numSpecialBlocks, System.Enum.GetValues(typeof(BlockType)).Length);
                    CreateBlock(x, y, randomType);
                }
            }
        }

        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = -levelHeight; y > -boardHeight; y--)
            {
                CreateBlock(x, y, BlockType.Buffer);
            }
        }
    }
    //y is negative
    public void CreateBlock(int x, int y, BlockType blockType)
    {
        if (!IsValidPosition(x, y) || blockType == BlockType.Empty) return;

        Vector3Int position = new Vector3Int(x, y, 0);
        TileBase tileToUse = null;
        blockData[x, -y] = new BlockData(blockType);
        switch (blockType)
        {
            case BlockType.Buffer:
                tileToUse = bufferTile;
                break;

            case BlockType.Life:
                tileToUse = lifeItemAnimated[UnityEngine.Random.Range(0, lifeItemAnimated.Length)];
                if (tileToUse == null)
                {
                    Debug.LogError("Life item tile is not assigned! Please assign a tile to lifeItemAnimated in the GameBoard inspector.");
                    return;
                }
                break;

            case BlockType.Hard:
                if (hardTiles == null || hardTiles.Length == 0)
                {
                    Debug.LogError("Hard tiles array is not set up properly!");
                    return;
                }
                tileToUse = blockTiles[0]; // Full health hard block
                break;

            case BlockType.Tool:
                int toolIndex = UnityEngine.Random.Range(0, toolTiles.Length);
                tileToUse = toolTiles[toolIndex];
                blockData[x, -y].SetTool(toolIndex + 1);
                break;

            default:
                // Handle colored blocks (Magenta, Green, Yellow, Blue, Red)
                if (blockTiles == null || blockTiles.Length == 0)
                {
                    Debug.LogError("Block tiles array is not set up properly!");
                    return;
                }

                int colorIndex = (int)blockType - numSpecialBlocks;
                if (colorIndex < 0 || colorIndex >= blockTiles.Length)
                {
                    Debug.LogError($"Invalid color block index: {colorIndex} for blockType: {blockType}");
                    return;
                }

                tileToUse = blockTiles[colorIndex];
                break;
        }

        if (tileToUse == null)
        {
            Debug.LogError($"No tile assigned for blockType: {blockType}");
            return;
        }

        // Set the tile
        tilemap.SetTile(position, tileToUse);

        // Verify it was set correctly
        TileBase verifyTile = tilemap.GetTile(position);
        if (verifyTile == null)
        {
            Debug.LogError($"Failed to place tile for {blockType} at ({x}, {y})");
        }
        else
        {
            Debug.Log($"Successfully placed {blockType} tile at ({x}, {y})");
        }
    }

    //x,y should be positive
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < boardWidth && y <= 0 && y > -boardHeight;
    }

    //y should be negative
    public BlockData GetBlockData(int x, int y)
    {
        if (IsValidPosition(x, y))
            return blockData[x, -y];
        return null;
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return grid.WorldToCell(worldPos);
    }

    public int getLevelHeight()
    {
        return levelHeight;
    }

    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        return grid.CellToWorld(cellPos);
    }

    public void DestroyBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;

        Vector3Int position = new Vector3Int(x, y, 0);
        tilemap.SetTile(position, null);
        if (blockData[x, -y].blockType == BlockType.Buffer)
        {
            bufferDestroyed = true;
        }
        blockData[x, -y] = new BlockData(BlockType.Empty);
    }

    //Destroy all blocks connected to the block x,y including the chosen block
    public void DestroyConnectedBlocks(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;
        if (player.tool == Tool.Hammer)
        {
            SoundManager.Instance.PlaySound2D("Hammer");
            DestroyThree(x, y);
            return;
        }
        if (player.tool == Tool.Pickaxe)
        {
            SoundManager.Instance.PlaySound2D("Pickaxe");
        }
        BlockData targetBlock = blockData[x, -y];
        BlockType targetType = targetBlock.blockType;

        if (targetType == BlockType.Empty || targetType == BlockType.Life) return;

        if (targetType == BlockType.Hard)
        {
            SoundManager.Instance.PlaySound2D("HardBreak");
            TryDamageHardBlock(targetBlock, x, y);
            return;
        }

        if (targetType == BlockType.Buffer)
        {
            HashSet<Vector2Int> connectedBuffers = new HashSet<Vector2Int>();
            Queue<Vector2Int> toCheck1 = new Queue<Vector2Int>();

            toCheck1.Enqueue(new Vector2Int(x, y));
            connectedBuffers.Add(new Vector2Int(x, y));

            // Find all connected buffer blocks
            while (toCheck1.Count > 0)
            {
                Vector2Int current = toCheck1.Dequeue();
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighbor = current + dir;
                    if (IsValidPosition(neighbor.x, neighbor.y) &&
                        !connectedBuffers.Contains(neighbor) &&
                        blockData[neighbor.x, -neighbor.y].blockType == BlockType.Buffer)
                    {
                        connectedBuffers.Add(neighbor);
                        toCheck1.Enqueue(neighbor);
                    }
                }
            }

            SoundManager.Instance.PlaySound2D("SoftBreak");

            // Destroy connected buffer blocks
            foreach (Vector2Int pos in connectedBuffers)
            {
                DestroyBlock(pos.x, pos.y);
            }

            // Trigger board transition
            GameBoardManager.Instance.TriggerBoardTransition();
            return;
        } 

        HashSet<Vector2Int> connectedBlocks = new HashSet<Vector2Int>();
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();

        toCheck.Enqueue(new Vector2Int(x, y));
        connectedBlocks.Add(new Vector2Int(x, y));

        // Find all connected blocks of the same type
        while (toCheck.Count > 0)
        {
            Vector2Int current = toCheck.Dequeue();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                if (IsValidPosition(neighbor.x, neighbor.y) &&
                    !connectedBlocks.Contains(neighbor) &&
                    blockData[neighbor.x, -neighbor.y].blockType == targetType)
                {
                    connectedBlocks.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        SoundManager.Instance.PlaySound2D("SoftBreak");

        // Destroy connected blocks
        if (connectedBlocks.Count > 0)
        {
            foreach (Vector2Int pos in connectedBlocks)
            {
                DestroyBlock(pos.x, pos.y);
            }
            StartCoroutine(ProcessGravity());
        }
    }

    // Creates a list of all the falling components
    private List<List<Vector2Int>> FindAllFallingComponents()
    {
        List<List<Vector2Int>> components = new List<List<Vector2Int>>();
        HashSet<Vector2Int> processedPositions = new HashSet<Vector2Int>();

        // Check all positions for unprocessed blocks (from top to bottom)
        for (int y = 0; y > -boardHeight; y--)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!processedPositions.Contains(pos) &&
                    blockData[x, -y].blockType != BlockType.Empty &&
                    blockData[x, -y].blockType != BlockType.Buffer)
                {
                    // Find connected component starting from this position
                    List<Vector2Int> component = FindConnectedComponent(x, y, blockData[x, -y].blockType);
                    if (component.Count > 0)
                    {
                        components.Add(component);

                        // Mark all positions in this component as processed
                        foreach (Vector2Int componentPos in component)
                        {
                            processedPositions.Add(componentPos);
                        }
                    }
                }
            }
        }

        return components;
    }

    private List<Vector2Int> FindConnectedComponent(int startX, int startY, BlockType targetType)
    {
        List<Vector2Int> component = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();

        toCheck.Enqueue(new Vector2Int(startX, startY));
        visited.Add(new Vector2Int(startX, startY));

        while (toCheck.Count > 0)
        {
            Vector2Int current = toCheck.Dequeue();
            component.Add(current);

            Vector2Int[] directions = {
                new Vector2Int(0, 1),   // up
                new Vector2Int(0, -1),  // down
                new Vector2Int(-1, 0),  // left
                new Vector2Int(1, 0)    // right
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;

                if (IsValidPosition(neighbor.x, neighbor.y) &&
                    !visited.Contains(neighbor) &&
                    blockData[neighbor.x, -neighbor.y].blockType == targetType)
                {
                    visited.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        return component;
    }

    private bool CanComponentFall(List<Vector2Int> component)
    {
        if (component.Count == 0) return false;
        // Check directly below
        foreach (Vector2Int pos in component)
        {
            if (blockData[pos.x, -pos.y].blockType == BlockType.Hard) return false;
            Vector2Int posBelow = new Vector2Int(pos.x, pos.y - 1);
            if (!IsValidPosition(posBelow.x, posBelow.y))
            {
                return false;
            }

            // Check if position below is occupied by a block not in this component
            if (blockData[posBelow.x, -posBelow.y].blockType != BlockType.Empty)
            {
                // Check if the position below is part of this same component
                bool isPartOfComponent = false;
                foreach (Vector2Int componentPos in component)
                {
                    if (componentPos.Equals(posBelow))
                    {
                        isPartOfComponent = true;
                        break;
                    }
                }

                if (!isPartOfComponent)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void MoveComponentDown(List<Vector2Int> component)
    {
        // Sort component by Y position (bottom to top) to avoid overwriting
        component.Sort((a, b) => a.y.CompareTo(b.y));

        // Store the block data for each position
        Dictionary<Vector2Int, BlockData> componentData = new Dictionary<Vector2Int, BlockData>();
        Dictionary<Vector2Int, TileBase> componentTiles = new Dictionary<Vector2Int, TileBase>();

        foreach (Vector2Int pos in component)
        {
            componentData[pos] = blockData[pos.x, -pos.y];
            componentTiles[pos] = tilemap.GetTile(new Vector3Int(pos.x, pos.y, 0));
        }

        // Clear original positions
        foreach (Vector2Int pos in component)
        {
            tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), null);
            blockData[pos.x, -pos.y] = new BlockData(BlockType.Empty);
        }

        // Place blocks in new positions (one row down)
        foreach (Vector2Int pos in component)
        {
            Vector2Int newPos = new Vector2Int(pos.x, pos.y - 1);
            tilemap.SetTile(new Vector3Int(newPos.x, newPos.y, 0), componentTiles[pos]);
            blockData[newPos.x, -newPos.y] = componentData[pos];
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
            List<List<Vector2Int>> components = FindAllFallingComponents();
            // Move each component that can fall
            foreach (List<Vector2Int> component in components)
            {
                if (CanComponentFall(component))
                {
                    MoveComponentDown(component);
                    blocksMovedThisPass = true;
                }
            }

            if (blocksMovedThisPass)
            {
                yield return new WaitForSeconds(1f / fallSpeed);
            }

        } while (blocksMovedThisPass);

        isProcessingGravity = false;
    }
    //MARK: Player helpers
    public void ClearBlocksAbove(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;
        for (int clearY = y; clearY <= 0; clearY++)
        {
            if (IsValidPosition(x, clearY))
            {
                // Only clear if there's actually a block there
                if (blockData[x, -clearY].blockType != BlockType.Empty)
                {
                    Vector3Int position = new Vector3Int(x, clearY, 0);
                    tilemap.SetTile(position, null);
                    blockData[x, -clearY] = new BlockData(BlockType.Empty);
                }
            }
        }
        SoundManager.Instance.PlaySound2D("Revive");
    }
    private void DestroyThree(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;
        if (player.tool != Tool.Hammer) return;

        for (int dx = x - 1; dx < x + 2; dx++)
        {
            for (int dy = y; dy > y - 3; dy--)
            {
                Debug.Log("ATtempting to break position" + dx + ", " + dy);
                if (IsValidPosition(dx, dy))
                {
                    BlockData block = blockData[dx, -dy];
                    //If block hard still only do one damage
                    if (block.blockType == BlockType.Hard)
                    {
                        TryDamageHardBlock(block, dx, dy);
                    }
                    else if (isNormalBlock(block.blockType))
                    {
                        DestroyBlock(dx, dy);
                    }
                }
            }
        }
        StartCoroutine(ProcessGravity());
    }

    void TryDamageHardBlock(BlockData block, int x, int y)
    {
        if (block.blockType != BlockType.Hard) return;

        block.Damage();
        if (player.tool == Tool.Pickaxe)
        {
            SoundManager.Instance.PlaySound2D("Pickaxe");   
        }

        if (player.tool == Tool.Pickaxe || block.Health() <= 0)
        {
            DestroyBlock(x, y);
        }
        else
        {
            Vector3Int position = new Vector3Int(x, y, 0);

            int spriteIndex = 3 - block.Health();
            spriteIndex = Mathf.Clamp(spriteIndex, 0, hardTiles.Length - 1);

            tilemap.SetTile(position, hardTiles[spriteIndex]);

            Debug.Log($"Hard block at ({x},{y}) has {block.Health()} health, using sprite index {spriteIndex}");
        }
    }
    public bool isNormalBlock(BlockType block)
    {
        return block > BlockType.Life;
    }

    public bool IsPlayerInDanger(Vector3Int playerPos)
    {
        // Check positions above the player for falling blocks
        for (int checkY = playerPos.y + 1; checkY <= 0; checkY++)
        {
            if (IsValidPosition(playerPos.x, checkY))
            {
                BlockData blockData = GetBlockData(playerPos.x, checkY);
                if (blockData != null && blockData.blockType != BlockType.Empty && blockData.blockType != BlockType.Buffer)
                {
                    // Check if this block can fall (has empty space below it)
                    if (CanBlockFall(playerPos.x, checkY))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    private bool CanBlockFall(int x, int y)
    {
        Vector3Int belowPos = new Vector3Int(x, y - 1, 0);
        if (!IsValidPosition(belowPos.x, belowPos.y)) return false;

        BlockData blockBelow = GetBlockData(belowPos.x, belowPos.y);
        return blockBelow != null && blockBelow.blockType == BlockType.Empty;
    }
    private void NotifyBlocksFalling()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            BlockCollisionDetector detector = player.GetComponent<BlockCollisionDetector>();
            if (detector != null)
            {
                detector.OnBlocksStartFalling();
            }
        }
    }
}