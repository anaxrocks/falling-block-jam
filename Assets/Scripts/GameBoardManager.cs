using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoardManager : MonoBehaviour
{
    public static GameBoardManager Instance;
    public GameObject[] gameBoardPrefabs;
    public float chanceEasy = 0.1f;
    public float chanceMedium = 0.4f;
    public float chanceHard = 0.5f;
    public int distanceToLoad = 10;
    public Transform playerTransform;
    public Queue<GameBoard> activeBoards = new Queue<GameBoard>();
    public GameBoard curBoard;
    private int boardCounter = 0;
    private bool destroying = false;
    private bool created = false;
    private PlayerMovement player;
    public int highScore = 0;
    public bool firstGame = true;

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
        float rng = UnityEngine.Random.Range(0.0f, 1.0f);
        int idx;
        if (firstGame)
        {
            idx = 1;
            firstGame = false;
        }

        else
        {
            if (rng < chanceEasy) idx = 0;
            else if (rng < chanceMedium) idx = 1;
            else idx = 2;
        }
        GameObject newGB = Instantiate(gameBoardPrefabs[idx], position, Quaternion.identity);
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
            // player._gameBoard = curBoard;
            Destroy(gb.gameObject);
            destroying = false;
            created = false;
        }
    }

    // Call this method when player dies to reset the game state
    public void ResetGameState()
    {
        Debug.Log("Starting game state reset...");

        if (player != null)
        {
            player.ResetPlayerState();
        }

        while (activeBoards.Count > 0)
        {
            GameBoard board = activeBoards.Dequeue();
            if (board != null)
            {
                Destroy(board.gameObject);
            }
        }

        activeBoards.Clear();
        boardCounter = 0;
        destroying = false;
        created = false;
        curBoard = null;

        CreateInitialBoard();

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovement>();
        }

        if (player != null)
        {
            player.updateCurBoard(curBoard);
            player.StartOnTop();
        }

        ScoreKeeper scoreKeeper = FindFirstObjectByType<ScoreKeeper>();
        if (scoreKeeper != null)
        {
            scoreKeeper.ResetScore();
        }

        Debug.Log("Game state reset completed!");
    }

    // Alternative method to reset without creating a new board (if you want to handle board creation elsewhere)
    public void ClearAllBoards()
    {
        while (activeBoards.Count > 0)
        {
            GameBoard board = activeBoards.Dequeue();
            if (board != null)
            {
                Destroy(board.gameObject);
            }
        }

        activeBoards.Clear();
        boardCounter = 0;
        destroying = false;
        created = false;
        curBoard = null;
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