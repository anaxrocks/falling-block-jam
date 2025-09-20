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
            // CheckforDestroyBoard();
        }
    }

    void CreateInitialBoard()
    {
        curBoard = CreateGameBoard(Vector3.zero);
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

    public void DestroyGameBoard()
    {
        if (activeBoards.Count > 1)
        {
            GameBoard gb = activeBoards.Dequeue();
            curBoard = activeBoards.Peek();
            player._gameBoard = curBoard;
            Destroy(gb.gameObject);
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

        if (playerCellPos.y == oldest.boardHeight - 2 && !destroying)
        {
            destroying = true;
            DestroyGameBoard();
        }
    }
}
