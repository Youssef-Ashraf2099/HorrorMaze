using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;

public class playerMovment : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float cameraPitch = 0f;
    private float verticalVelocity = 0f;
    private bool jumpPressed = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();

        inputActions.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Jump.performed += ctx => jumpPressed = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    void HandleMovement()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        move = transform.TransformDirection(move);

        // Gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded

            if (jumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressed = false;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Rotate player left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    // Inline PlayerInputActions class
    private class PlayerInputActions
    {
        public InputAction Move { get; }
        public InputAction Look { get; }
        public InputAction Jump { get; }

        public PlayerInputActions()
        {
            Move = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            Look.AddBinding("<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick");

            Jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Jump.AddBinding("<Gamepad>/buttonSouth");
        }

        public void Enable()
        {
            Move.Enable();
            Look.Enable();
            Jump.Enable();
        }

        public void Disable()
        {
            Move.Disable();
            Look.Disable();
            Jump.Disable();
        }
    }
    public void SetInputActive(bool active)
    {
        if (active)
        {
            inputActions.Enable();
        }
        else
        {
            inputActions.Disable();
        }
    }
}
