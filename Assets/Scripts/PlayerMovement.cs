using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _fallSpeed = 8f;
    [SerializeField] private float _continuousMovementDelay = 0.2f;
    [SerializeField] private float _autoJumpDelay = 0.4f; // Delay before auto-jumping

    [Header("References")]
    public GameBoard _gameBoard;
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
    public bool isBufferBroken = false;

    // Auto-jump state
    private float _blockHitTime = 0f;
    private Vector3Int _blockedDirection = Vector3Int.zero;
    private bool _isWaitingForAutoJump = false;

    //Tool held
    public Tool tool = Tool.None;
    private ToolTimer toolTimer;
    private HealthManager healthSystem;

    public int dmgLow = 1;
    public int dmgHigh = 5;
    public BlockCollisionDetector blockCollisionDetector;

    void Awake()
    {
        if (gameBoardManager == null)
            gameBoardManager = FindFirstObjectByType<GameBoardManager>();
    }

    void Start()
    {
        InitializePosition();
        toolTimer = GetComponent<ToolTimer>();
        healthSystem = GetComponent<HealthManager>();
    }

    void Update()
    {
        HandleAttack();
        HandleInput();
        HandleMovement();
        CheckGravity();
    }

    public void InitializePosition()
    {
        _gameBoard = gameBoardManager.curBoard;
        blockCollisionDetector.gameBoard = gameBoardManager.curBoard;
        Vector3 pos = transform.position;
        _currentGridPosition = _gameBoard.WorldToCell(new Vector3(pos.x - GRID_OFFSET, pos.y - GRID_OFFSET, 0));
        Vector3 corner = _gameBoard.CellToWorld(_currentGridPosition);
        _targetWorldPosition = new Vector3(corner.x + GRID_OFFSET, corner.y + GRID_OFFSET, 0);
        transform.position = _targetWorldPosition;
    }

    public void StartOnTop()
    {
        _gameBoard = gameBoardManager.curBoard;
        blockCollisionDetector.gameBoard = gameBoardManager.curBoard;

        // Start at the top center of the board (y = 0 is the top)
        int startX = _gameBoard.boardWidth / 2; // Center horizontally
        int startY = 0; // Top of the board

        // Set player position
        _currentGridPosition = new Vector3Int(startX, startY, 0);
        Vector3 corner = _gameBoard.CellToWorld(_currentGridPosition);
        _targetWorldPosition = new Vector3(corner.x + GRID_OFFSET, corner.y + GRID_OFFSET, 0);
        transform.position = _targetWorldPosition;

        Debug.Log($"Player initialized at grid position: {_currentGridPosition}, world position: {_targetWorldPosition}");
    }

    void HandleAttack()
    {
        if (!InputManager.attackPressed) return;

        // Check if attack is on cooldown
        if (!toolTimer.CanAttack())
        {
            Debug.Log($"Attack on cooldown! Time remaining: {toolTimer.GetAttackCooldownRemaining():F2}s");
            return;
        }

        Vector3Int attackDirection = GetAttackDirection();
        Vector3Int targetPos = _currentGridPosition + attackDirection;

        // Attempt to break block
        bool blockBroken = BreakBlock(targetPos.x, targetPos.y);

        // Only trigger cooldown if we actually attempted an attack
        // (even if no block was broken, we still "used" our attack)
        toolTimer.OnAttackMade();
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

    bool BreakBlock(int x, int y)
    {
        if (!_gameBoard.IsValidPosition(x, y)) return false;

        BlockData blockData = _gameBoard.GetBlockData(x, y);
        if (blockData != null && blockData.blockType != BlockType.Empty)
        {
            if (blockData.blockType == BlockType.Buffer) isBufferBroken = true;
            if (blockData.blockType == BlockType.Hard)
            {
                healthSystem.TakeDamage(dmgHigh);
            }
            else
            {
                healthSystem.TakeDamage(dmgLow);
            }

            // Handle different tool effects
            if (tool == Tool.Hammer)
            {
                // Hammer breaks 3x3 area
                BreakHammerArea(x, y);
            }
            else
            {
                // Normal or pickaxe breaking
                _gameBoard.DestroyConnectedBlocks(x, y);
            }

            toolTimer.ToolTimerTick();

            // Reset auto-jump waiting if we break a block
            if (_isWaitingForAutoJump && (_blockedDirection == Vector3Int.right && x == _currentGridPosition.x + 1) ||
                (_blockedDirection == Vector3Int.left && x == _currentGridPosition.x - 1))
            {
                _isWaitingForAutoJump = false;
                _blockedDirection = Vector3Int.zero;
            }

            Debug.Log($"Breaking block at ({x}, {y}) of type: {blockData.blockType} with {tool}");
            return true;
        }
        return false;
    }

    // Helper method for hammer 3x3 breaking
    private void BreakHammerArea(int centerX, int centerY)
    {
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int y = centerY - 1; y <= centerY + 1; y++)
            {
                if (_gameBoard.IsValidPosition(x, y))
                {
                    BlockData blockData = _gameBoard.GetBlockData(x, y);
                    if (blockData != null && blockData.blockType != BlockType.Empty)
                    {
                        if (blockData.blockType == BlockType.Buffer) isBufferBroken = true;
                        _gameBoard.DestroyConnectedBlocks(x, y);
                    }
                }
            }
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
        Debug.Log("Board is currently" + _gameBoard.name);
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

    public void CollectToolItem(int x, int y)
    {
        tool = _gameBoard.GetBlockData(x, y).tool;
        _gameBoard.DestroyBlock(x, y);
        toolTimer.StartTimer(tool);
        StartCoroutine(_gameBoard.ProcessGravity());
    }

    void CheckGravity()
    {
        if (_isMoving) return;

        Vector3Int belowPos = _currentGridPosition + Vector3Int.down;

        if (isBufferBroken && -_currentGridPosition.y >= _gameBoard.boardHeight - 1)
        {
            gameBoardManager.DestroyGameBoard();
            InitializePosition();
            isBufferBroken = false;
        }

        if (CanMoveToPosition(belowPos))
        {
            // Check if there's a life item below us - collect it during fall
            BlockData blockData = _gameBoard.GetBlockData(belowPos.x, belowPos.y);
            if (blockData != null && blockData.blockType == BlockType.Life)
            {
                CollectLifeItem(belowPos.x, belowPos.y);
            }
            if (blockData != null && blockData.blockType == BlockType.Tool)
            {
                CollectToolItem(belowPos.x, belowPos.y);
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
    
    public void ResetPlayerState()
{
    Debug.Log("Resetting player state...");
    
    // Reset tool state
    tool = Tool.None;
    
    // Reset movement state
    _isMoving = false;
    _lastMoveTime = 0f;
    _lastMoveDirection = Vector3Int.zero;
    _isContinuousMovement = false;
    
    // Reset buffer state
    isBufferBroken = false;
    
    // Reset auto-jump state
    ResetAutoJumpState();
    
    // Reset tool timer if it exists
    if (toolTimer != null)
    {
        toolTimer.ResetToolTimer();
    }
    
    Debug.Log("Player state reset completed");
}
}