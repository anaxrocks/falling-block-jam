using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoardManager : MonoBehaviour
{
    public static GameBoardManager Instance;
    public GameObject gameBoardPrefab;
    public int distanceToLoad = 10;
    public Transform playerTransform;
    public Queue<GameBoard> activeBoards = new Queue<GameBoard>();
    public GameBoard curBoard;
    private int boardCounter = 0;
    private bool destroying = false;
    private bool created = false;
    private PlayerMovement player;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        CreateInitialBoard();
    }
    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>();
    }

    void Update()
    {
        if (curBoard)
        {
            CheckForNewBoardLoading();
            CheckforDestroyBoard();
        }
    }

    void CreateInitialBoard()
    {
        curBoard = CreateGameBoard(Vector3.zero);
    }

    // In GameBoardManager.cs, add this new method:

// In GameBoardManager.cs, add this new method:

public void TriggerBoardTransition()
{
    if (activeBoards.Count < 2) return; // Need at least 2 boards to transition

    // Remove the current board from the queue
    GameBoard oldBoard = activeBoards.Dequeue();
    
    // Set the next board as current
    curBoard = activeBoards.Peek();
    
    // Update player's board reference
    player.updateCurBoard(curBoard);
    
    // Teleport player to starting position of new board (top-left)
    Vector3Int startingGridPos = new Vector3Int(0, 0, 0);
    Vector3 corner = curBoard.CellToWorld(startingGridPos);
    Vector3 startingWorldPos = new Vector3(corner.x + 0.5f, corner.y + 0.5f, 0);
    
    // Update player position using the teleport method
    player.TeleportToPosition(startingWorldPos, startingGridPos);
    
    // Update collision detector's board reference if it exists
    BlockCollisionDetector collisionDetector = player.GetComponent<BlockCollisionDetector>();
    if (collisionDetector != null)
    {
        collisionDetector.UpdateGameBoard(curBoard);
    }
    
    // Destroy the old board
    Destroy(oldBoard.gameObject, 0.1f);

    created = false;

    CheckForNewBoardLoading();
    
    Debug.Log("Board transition completed - player teleported to new board starting position");
}

    GameBoard CreateGameBoard(Vector3 position)
    {
        GameObject newGB = Instantiate(gameBoardPrefab, position, Quaternion.identity);
        newGB.name = $"GameBoard_{boardCounter}";
        newGB.transform.parent = this.transform;

        GameBoard newBoard = newGB.GetComponent<GameBoard>();
        newBoard.Init();
        boardCounter++;
        activeBoards.Enqueue(newBoard);
        return newBoard;
    }

    void DestroyGameBoard()
    {
        if (activeBoards.Count > 1)
        {
            GameBoard gb = activeBoards.Dequeue();
            curBoard = activeBoards.Peek();
            player.updateCurBoard(curBoard);
            Destroy(gb.gameObject, 0.1f);
            destroying = false;
            created = false;
        }
    }
    //Next board loads in after player reaches a threshold
    void CheckForNewBoardLoading()
    {
        if (playerTransform == null || activeBoards.Count == 0 || created) return;
        Vector3Int playerCellPos = curBoard.WorldToCell(playerTransform.position);
        int distanceToBottom = playerCellPos.y + curBoard.boardHeight;
        // If player is close enough to the bottom board, create a new one below
        if (distanceToBottom < distanceToLoad)
        {
            created = true;
            Vector3Int bottomCellPos = new Vector3Int(0, -curBoard.boardHeight, 0);
            Vector3 curBoardBottom = curBoard.CellToWorld(bottomCellPos);

            Vector3 newPosition = new Vector3(0, curBoardBottom.y, 0);
            CreateGameBoard(newPosition);
        }
    }

    //Board gets destoryed once player breaks buffer
    void CheckforDestroyBoard()
    {
        if (activeBoards.Count <= 1) return;
        GameBoard oldest = activeBoards.Peek();
        if (!oldest) return;
        Vector3Int playerCellPos = oldest.WorldToCell(playerTransform.position);
        float distanceToBufferStart = playerCellPos.y - (-oldest.getLevelHeight());

        if (distanceToBufferStart > 0 && curBoard.bufferDestroyed && !destroying)
        {
            destroying = true;
            DestroyGameBoard();
        }
    }
}
