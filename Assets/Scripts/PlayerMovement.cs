using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _fallSpeed = 8f;
    [SerializeField] private float _continuousMovementDelay = 0.2f;
    [SerializeField] private float _autoJumpDelay = 0.4f; // Delay before auto-jumping

    [Header("References")]
    private GameBoard _gameBoard;
    private GameBoardManager gameBoardManager;

    private const float GRID_OFFSET = 0.5f;
    private const float MOVEMENT_THRESHOLD = 0.5f;
    private const float POSITION_TOLERANCE = 0.01f;

    private Vector3Int _currentGridPosition;
    private Vector3 _targetWorldPosition;
    private bool _isMoving = false;

    // Movement state
    private float _lastMoveTime = 0f;
    private Vector3Int _lastMoveDirection = Vector3Int.zero;
    private bool _isContinuousMovement = false;

    // Auto-jump state
    private float _blockHitTime = 0f;
    private Vector3Int _blockedDirection = Vector3Int.zero;
    private bool _isWaitingForAutoJump = false;
    //Tool held
    public Tool tool = Tool.None;

    void Awake()
    {
        if (gameBoardManager == null)
            gameBoardManager = FindFirstObjectByType<GameBoardManager>();
    }

    void Start()
    {
        InitializePosition();
    }

    void Update()
    {
        HandleAttack();
        HandleInput();
        HandleMovement();
        CheckGravity();
    }

    private void InitializePosition()
    {
        _gameBoard = gameBoardManager.curBoard;
        Vector3 pos = transform.position;
        _currentGridPosition = _gameBoard.WorldToCell(new Vector3(pos.x - GRID_OFFSET, pos.y - GRID_OFFSET, 0));

        Vector3 corner = _gameBoard.CellToWorld(_currentGridPosition);
        _targetWorldPosition = new Vector3(corner.x + GRID_OFFSET, corner.y + GRID_OFFSET, 0);
        transform.position = _targetWorldPosition;
    }

    void HandleAttack()
    {
        if (!InputManager.attackPressed) return;

        Vector3Int attackDirection = GetAttackDirection();
        Vector3Int targetPos = _currentGridPosition + attackDirection;
        BreakBlock(targetPos.x, targetPos.y);
    }

    private Vector3Int GetAttackDirection()
    {
        Vector2 input = InputManager.movement;

        // Check movement input first
        if (input.magnitude > MOVEMENT_THRESHOLD)
        {
            return GetDirectionFromInput(input);
        }

        // Use mouse/touch position
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 worldDirection = mousePos - transform.position;

        if (Mathf.Abs(worldDirection.x) > MOVEMENT_THRESHOLD || Mathf.Abs(worldDirection.y) > MOVEMENT_THRESHOLD)
        {
            return GetDirectionFromVector(worldDirection);
        }

        return Vector3Int.down; // Default
    }

    private Vector3Int GetDirectionFromInput(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0 ? Vector3Int.right : Vector3Int.left;
        return input.y > 0 ? Vector3Int.up : Vector3Int.down;
    }

    private Vector3Int GetDirectionFromVector(Vector3 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? Vector3Int.right : Vector3Int.left;
        return direction.y > 0 ? Vector3Int.up : Vector3Int.down;
    }

    void BreakBlock(int x, int y)
    {
        if (!_gameBoard.IsValidPosition(x, y)) return;

        BlockData blockData = _gameBoard.GetBlockData(x, y);
        if (blockData != null && blockData.blockType != BlockType.Empty && blockData.blockType != BlockType.Buffer)
        {
            _gameBoard.DestroyConnectedBlocks(x, y);

            // Reset auto-jump waiting if we break a block
            if (_isWaitingForAutoJump && (_blockedDirection == Vector3Int.right && x == _currentGridPosition.x + 1) ||
                (_blockedDirection == Vector3Int.left && x == _currentGridPosition.x - 1))
            {
                _isWaitingForAutoJump = false;
                _blockedDirection = Vector3Int.zero;
            }

            Debug.Log($"Breaking block at ({x}, {y}) of type: {blockData.blockType}");
        }
    }

    void HandleInput()
    {
        Vector2 input = InputManager.movement;

        if (input.magnitude > MOVEMENT_THRESHOLD)
        {
            Vector3Int direction = GetDirectionFromInput(input);
            HandleDirectionalInput(direction);
        }
        else
        {
            StopContinuousMovement();
        }

        // Handle auto-jump timing
        CheckAutoJump();
    }

    private void HandleDirectionalInput(Vector3Int direction)
    {
        bool isFirstMove = !_isContinuousMovement || _lastMoveDirection != direction;
        bool canMove = !_isMoving && (isFirstMove || Time.time - _lastMoveTime >= _continuousMovementDelay);

        if (canMove)
        {
            if (TryMove(direction))
            {
                // Successful move - reset auto-jump state
                ResetAutoJumpState();
            }
            else if (IsHorizontalDirection(direction))
            {
                // Blocked horizontal movement - start auto-jump timer
                StartAutoJumpTimer(direction);
            }

            _lastMoveTime = Time.time;
            _lastMoveDirection = direction;
            _isContinuousMovement = true;
        }
    }

    private void StopContinuousMovement()
    {
        _isContinuousMovement = false;
        _lastMoveDirection = Vector3Int.zero;
        ResetAutoJumpState();
    }

    private void StartAutoJumpTimer(Vector3Int direction)
    {
        if (!_isWaitingForAutoJump || _blockedDirection != direction)
        {
            _isWaitingForAutoJump = true;
            _blockedDirection = direction;
            _blockHitTime = Time.time;
        }
    }

    private void CheckAutoJump()
    {
        if (_isWaitingForAutoJump && Time.time - _blockHitTime >= _autoJumpDelay)
        {
            if (TryAutoJump(_blockedDirection))
            {
                Debug.Log($"Auto-jumped in direction: {_blockedDirection}");
            }
            ResetAutoJumpState();
        }
    }

    private void ResetAutoJumpState()
    {
        _isWaitingForAutoJump = false;
        _blockedDirection = Vector3Int.zero;
        _blockHitTime = 0f;
    }

    private bool IsHorizontalDirection(Vector3Int direction)
    {
        return direction == Vector3Int.left || direction == Vector3Int.right;
    }

    void HandleMovement()
    {
        if (!_isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, _targetWorldPosition, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetWorldPosition) < POSITION_TOLERANCE)
        {
            transform.position = _targetWorldPosition;
            _isMoving = false;
        }
    }

    System.Collections.IEnumerator FallToTarget()
    {
        while (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetWorldPosition, _fallSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _targetWorldPosition) < POSITION_TOLERANCE)
            {
                transform.position = _targetWorldPosition;
                _isMoving = false;
            }

            yield return null;
        }
    }
    bool TryAutoJump(Vector3Int horizontalDirection)
    {
        if (!IsHorizontalDirection(horizontalDirection)) return false;

        Vector3Int spaceAbovePlayer = _currentGridPosition + Vector3Int.up;
        Vector3Int jumpTarget = _currentGridPosition + horizontalDirection + Vector3Int.up;

        // Check if there's space above player and the jump target is empty
        if (IsEmptySpace(spaceAbovePlayer) && IsEmptySpace(jumpTarget))
        {
            MoveToPosition(jumpTarget);
            return true;
        }

        return false;
    }
        void MoveToPosition(Vector3Int gridPosition)
    {
        _currentGridPosition = gridPosition;
        Vector3 corner = _gameBoard.CellToWorld(_currentGridPosition);
        _targetWorldPosition = new Vector3(corner.x + GRID_OFFSET, corner.y + GRID_OFFSET, 0);
        _isMoving = true;
    }
    public Vector3Int GetCurrentGridPosition()
    {
        return _currentGridPosition;
    }
    public void updateCurBoard(GameBoard gb)
    {
        _gameBoard = gb;
    }
    bool TryMove(Vector3Int direction)
    {
        Vector3Int newPos = _currentGridPosition + direction;

        if (!_gameBoard.IsValidPosition(newPos.x, newPos.y))
            return false;

        BlockData blockData = _gameBoard.GetBlockData(newPos.x, newPos.y);

        if (blockData == null)
            return false;

        // life item --> collect it and allow movement
        if (blockData.blockType == BlockType.Life)
        {
            CollectLifeItem(newPos.x, newPos.y);
            MoveToPosition(newPos);
            return true;
        }

        // empty space --> allow movement
        if (blockData.blockType == BlockType.Empty)
        {
            MoveToPosition(newPos);
            return true;
        }
        // tool --> collect it and allow movement
        if (blockData.blockType == BlockType.Tool)
        {
            CollectToolItem(newPos.x, newPos.y);
            MoveToPosition(newPos);
            return true;
        }

        // All other block types no movement
            return false;
    }

    // Add this new method to handle life item collection:
    private void CollectLifeItem(int x, int y)
    {
        // Get health system and heal player
        HealthManager healthSystem = GetComponent<HealthManager>();
        if (healthSystem != null)
        {
            healthSystem.OnLifeItemCollected();
        }

        // Remove the life item from the game board
        _gameBoard.DestroyBlock(x, y);

        // Start gravity processing to make blocks fall
        StartCoroutine(_gameBoard.ProcessGravity());

        Debug.Log($"Collected life item at ({x}, {y})!");

        SoundManager.Instance.PlaySound2D("PicturePickup");
        print("powerpickup sound");
    }
    //TODO: add timer to tools
    private void CollectToolItem(int x, int y)
    {
        tool = _gameBoard.GetBlockData(x, y).tool;
        _gameBoard.DestroyBlock(x, y);
        StartCoroutine(_gameBoard.ProcessGravity());
    }
    void CheckGravity()
    {
        if (_isMoving) return;

        Vector3Int belowPos = _currentGridPosition + Vector3Int.down;

        if (CanMoveToPosition(belowPos))
        {
            // Check if there's a life item below us - collect it during fall
            BlockData blockData = _gameBoard.GetBlockData(belowPos.x, belowPos.y);
            if (blockData != null && blockData.blockType == BlockType.Life)
            {
                CollectLifeItem(belowPos.x, belowPos.y);
            }

            MoveToPosition(belowPos);
            StartCoroutine(FallToTarget());
        }
    }
    private bool CanMoveToPosition(Vector3Int position)
    {
        if (!_gameBoard.IsValidPosition(position.x, position.y)) return false;

        BlockData blockData = _gameBoard.GetBlockData(position.x, position.y);
        if (blockData == null) return false;

        // Can move to empty spaces or life items
        return blockData.blockType == BlockType.Empty || blockData.blockType == BlockType.Life;
    }
    private bool IsEmptySpace(Vector3Int position)
    {
        return CanMoveToPosition(position);
    }
}