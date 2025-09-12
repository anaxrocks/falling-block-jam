using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public static Vector2 movement;
    public static bool attackPressed;
    public static bool interactionPressed;
    public static bool consumePressed;
    public static bool throwPressed;
    public static bool restartPressed;
    public static bool isHoldingMovement;

    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _attackAction;
    private InputAction _interactAction;
    private InputAction _consumeAction;
    private InputAction _throwAction;
    private InputAction _restartAction;

    void Awake()
    {
        // Singleton pattern - only allow one instance
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            InitializeInput();
        }
        else
        {
            // Destroy duplicate instances
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeInput()
    {
        _playerInput = GetComponent<PlayerInput>();

        // Null check in case component is missing
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput component not found on InputManager!");
            return;
        }

        _moveAction = _playerInput.actions["Move"];
        _attackAction = _playerInput.actions["Attack"];
        _interactAction = _playerInput.actions["Interact"];
//        _consumeAction = _playerInput.actions["Consume"];
 //       _throwAction = _playerInput.actions["Throw"];
 //       _restartAction = _playerInput.actions["Restart"];
    }

    void Update()
    {
        // Safety check - if actions are null, reinitialize
        if (_moveAction == null)
        {
            InitializeInput();
            return;
        }

        movement = _moveAction.ReadValue<Vector2>();
        attackPressed = _attackAction.WasPressedThisFrame();
        interactionPressed = _interactAction.WasPressedThisFrame();
//        consumePressed = _consumeAction.WasPressedThisFrame();
   //     throwPressed = _throwAction.WasPressedThisFrame();
    //    restartPressed = _restartAction.WasPressedThisFrame();

        isHoldingMovement = movement.magnitude > 0.1f;
    }

    void OnDestroy()
    {
        // Clean up singleton reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Print()
    {
        Debug.Log("Test");
    }
}