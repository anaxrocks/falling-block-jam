using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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

    [Header("Board Settings")]

    //might have to edit to camera size after, unless we want to set it skinny for all screen sizes
    public int boardWidth = 8;
    public int boardHeight = 12; //total height of board
    public int bufferHeight = 5; //buffer into the boardheight
    private int levelHeight; //boardHeight - bufferHeight
    // public float blockSize = 1f;
    public float fallSpeed = 2f;
    public bool usePhysicsGravity;

    [Header("Block Tiles")]
    public TileBase[] blockTiles; // Different colored block tiles
    public TileBase emptyTile; // Null or transparent tile for empty spaces

    private BlockData[,] blockData;
    private bool isProcessingGravity = false;
    public bool bufferDestroyed = false;
    void Awake()
    {
        levelHeight = boardHeight - bufferHeight;
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
                ClearBlocksAbove(cellPos.x, cellPos.y);
            }
        }
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
                BlockType randomType = (BlockType)UnityEngine.Random.Range(2, System.Enum.GetValues(typeof(BlockType)).Length);
                CreateBlock(x, y, randomType);
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

        if (blockType == BlockType.Buffer)
        {
            tilemap.SetTile(position, emptyTile);
            blockData[x, -y] = new BlockData(BlockType.Buffer);
        }
        else
        {
            TileBase tileToPlace = blockTiles[(int)blockType - 2];
            tilemap.SetTile(position, tileToPlace);
            blockData[x, -y] = new BlockData(blockType);
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

        BlockData targetBlock = blockData[x, -y];
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
        if (isProcessingGravity || usePhysicsGravity) yield break;
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
    public void ClearBlocksAbove(int x, int y)
    {
        if (!IsValidPosition(x, y)) return;
        for (int clearY = y + 1; clearY <= 0; clearY++)
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
    }
}