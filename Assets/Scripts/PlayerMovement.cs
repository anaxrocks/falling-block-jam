using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _fallSpeed = 8f;
    [SerializeField] private float offset = 0.25f;
    // [SerializeField] private GameBoard _gameBoard;
    private GameBoard _gameBoard;
    [SerializeField] private GameBoardManager gameBoardManager;

    private Vector3Int _currentGridPosition;
    private Vector3 _targetWorldPosition;
    private bool _isMoving = false;
    private bool _inputProcessed = false;

    void Awake()
    {
        if (gameBoardManager == null)
            gameBoardManager = GameObject.FindAnyObjectByType<GameBoardManager>();
        // if (_gameBoard == null)
        //         _gameBoard = GameObject.FindFirstObjectByType<GameBoard>();
    }

    void Start()
    {
        _gameBoard = gameBoardManager.curBoard;
        // Get grid position and snap to center
        Vector3 pos = transform.position;
        _currentGridPosition = _gameBoard.WorldToCell(new Vector3(pos.x - offset, pos.y - offset, 0));

        // Move to center of grid cell
        Vector3 corner = _gameBoard.CellToWorld(_currentGridPosition);
        _targetWorldPosition = new Vector3(corner.x + offset, corner.y + offset, 0);
        transform.position = _targetWorldPosition;
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        CheckGravity();
    }

    void HandleInput()
    {
        Vector2 input = InputManager.movement;

        // Only move if not currently moving and have input
        if (!_isMoving && !_inputProcessed && input.magnitude > 0.5f)
        {
            Vector3Int direction = Vector3Int.zero;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                direction = input.x > 0 ? Vector3Int.right : Vector3Int.left;
            else
                direction = input.y > 0 ? Vector3Int.up : Vector3Int.down;

            TryMove(direction);
            _inputProcessed = true;
        }

        if (input.magnitude < 0.1f)
            _inputProcessed = false;
    }

    void HandleMovement()
    {
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetWorldPosition, _moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _targetWorldPosition) < 0.01f)
            {
                transform.position = _targetWorldPosition;
                _isMoving = false;
            }
        }
    }

    void CheckGravity()
    {
        // Only check gravity if not moving
        if (!_isMoving)
        {
            Vector3Int belowPos = _currentGridPosition + Vector3Int.down;

            // Check if there's an empty space below
            if (_gameBoard.IsValidPosition(belowPos.x, belowPos.y))
            {
                BlockData blockBelow = _gameBoard.GetBlockData(belowPos.x, belowPos.y);
                if (blockBelow != null && blockBelow.blockType == BlockType.Empty)
                {
                    // Fall down
                    _currentGridPosition = belowPos;
                    Vector3 corner = _gameBoard.CellToWorld(_currentGridPosition);
                    _targetWorldPosition = new Vector3(corner.x + 0.25f, corner.y + 0.25f, 0);
                    _isMoving = true;

                    // Use fall speed for gravity
                    StartCoroutine(FallToTarget());
                }
            }
        }
    }

    System.Collections.IEnumerator FallToTarget()
    {
        while (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetWorldPosition, _fallSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _targetWorldPosition) < 0.01f)
            {
                transform.position = _targetWorldPosition;
                _isMoving = false;
            }

            yield return null;
        }
    }

    void TryMove(Vector3Int direction)
    {
        Vector3Int newPos = _currentGridPosition + direction;

        // Check if valid position
        if (_gameBoard.IsValidPosition(newPos.x, newPos.y))
        {
            BlockData block = _gameBoard.GetBlockData(newPos.x, newPos.y);
            if (block != null && block.blockType == BlockType.Empty)
            {
                _currentGridPosition = newPos;
                Vector3 corner = _gameBoard.CellToWorld(_currentGridPosition);
                _targetWorldPosition = new Vector3(corner.x + 0.25f, corner.y + 0.25f, 0);
                _isMoving = true;
            }
        }
    }
    public void updateCurBoard(GameBoard gb)
    {
        _gameBoard = gb;
    }
} 