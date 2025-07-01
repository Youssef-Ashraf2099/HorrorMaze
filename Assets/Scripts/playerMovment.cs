using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class playerMovment : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Audio")]
    public AudioSource movementAudioSource;
    public AudioClip walkingSound;
    public AudioClip jumpSound;

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
        HandleAudio();
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
                if (movementAudioSource != null && jumpSound != null)
                {
                    movementAudioSource.PlayOneShot(jumpSound);
                }
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
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Rotate player left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    void HandleAudio()
    {
        bool isMoving = moveInput.magnitude > 0.1f;

        if (controller.isGrounded && isMoving)
        {
            if (movementAudioSource != null && walkingSound != null && !movementAudioSource.isPlaying)
            {
                movementAudioSource.clip = walkingSound;
                movementAudioSource.loop = true;
                movementAudioSource.Play();
            }
        }
        else
        {
            if (movementAudioSource != null && movementAudioSource.clip == walkingSound && movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }
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

    // Inline PlayerInputActions class
    private class PlayerInputActions
    {
        public readonly InputAction Move;
        public readonly InputAction Look;
        public readonly InputAction Jump;

        public PlayerInputActions()
        {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = new InputAction("Look", InputActionType.Value);
            Look.AddBinding("<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick");

            Jump = new InputAction("Jump", InputActionType.Button);
            Jump.AddBinding("<Keyboard>/space");
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
}