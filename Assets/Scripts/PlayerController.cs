using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float WalkSpeed = 5.5f;
    [SerializeField] private float RunSpeed = 5.5f;

    [SerializeField] private float JumpForce = 8.0f;
    [SerializeField] private float Gravity = 20.0f;

    [SerializeField] private float LookSensitivity = 0.2f;
    [SerializeField] private float LookAngleLimit = 90.0f;

    private Camera mainCamera;
    private CharacterController characterController;

    private InputAction moveInput;
    private InputAction runInput;

    private InputAction jumpInput;
    private bool jumped = false;

    private float currentMoveSpeed = 0.0f;
    private Vector3 moveDirection = Vector3.zero;
    private float lookAngle = 0.0f;

    private void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();

        moveInput = InputSystem.actions.FindAction("Move");
        runInput = InputSystem.actions.FindAction("Sprint");

        jumpInput = InputSystem.actions.FindAction("Jump");
        jumpInput.started += Jumped;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentMoveSpeed = WalkSpeed;
    }

    private void Update()
    {
        Vector2 moveVector = moveInput.ReadValue<Vector2>();
        Vector2 mouseDelta = new Vector2(Mouse.current.delta.x.ReadValue(), Mouse.current.delta.y.ReadValue());

        // Once the player leaves the ground, reset the flag so holding the jump button doesn't cause jumps midair
        if (!characterController.isGrounded)
            jumped = false;

        currentMoveSpeed = runInput.IsPressed() ? RunSpeed : WalkSpeed;
        HandleMovement(moveVector);
        HandleLooking(mouseDelta);
    }

    private void HandleMovement(Vector2 moveVector)
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float oldY = moveDirection.y;

        // Multiply vector components directly: x for right/left, y for forward/backward
        moveDirection = (forward * moveVector.y * currentMoveSpeed) + (right * moveVector.x * currentMoveSpeed);

        // If the jump button was pressed and the player is standing on the ground, apply the jump force. Otherwise keep the previous vertical movement
        moveDirection.y = (jumped && characterController.isGrounded) ? JumpForce : oldY;

        // Gradually decreases the vertical speed of the player midair
        if (!characterController.isGrounded)
            moveDirection.y -= Gravity * Time.deltaTime;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void Jumped(InputAction.CallbackContext _)
    {
        jumped = true;
    }

    private void HandleLooking(Vector2 mouseDelta)
    {
        // Multiplied by LookSensitivity so we can control how fast the camera moves
        lookAngle += -mouseDelta.y * LookSensitivity;
        lookAngle = Mathf.Clamp(lookAngle, -LookAngleLimit, LookAngleLimit);

        mainCamera.transform.localRotation = Quaternion.Euler(lookAngle, 0, 0);
        transform.rotation *= Quaternion.Euler(0, mouseDelta.x * LookSensitivity, 0);
    }
}