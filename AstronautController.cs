using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class AstronautController : MonoBehaviour
{
    // =============================================================================
    // MOVEMENT SETTINGS
    // =============================================================================
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    // =============================================================================
    // JUMP SETTINGS
    // =============================================================================
    [Header("Jump Settings")]
    public float jumpForce = 5f;
    [Tooltip("Extra downward pull when falling to make jumps feel less floaty.")]
    public float extraGravity = 15f;
    [Tooltip("Defines what objects count as the floor so you don't jump off invisible triggers.")]
    public LayerMask groundMask = Physics.DefaultRaycastLayers;

    // =============================================================================
    // CAMERA SETTINGS
    // =============================================================================
    [Header("Camera Settings")]
    [Tooltip("Drag your Main Camera here. If empty, it will auto-find Camera.main")]
    public Transform playerCamera;
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, 0f);
    public float cameraDistance = 5f;
    public float minCameraDistance = 1f;
    public float mouseSensitivity = 3f;
    public float minCameraPitch = -40f;
    public float maxCameraPitch = 60f;

    // =============================================================================
    // ANIMATION
    // =============================================================================
    [Header("Animation")]
    [Tooltip("Drag your character's Animator here.")]
    public Animator animator;

    // =============================================================================
    // INTERNAL VARIABLES
    // =============================================================================
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector3 movementInput;
    private float mouseX, mouseY;
    private float cameraPitch = 0f;
    private float cameraYaw = 0f;
    private bool isGrounded;
    private bool jumpRequested;

    // =============================================================================
    // UNITY METHODS
    // =============================================================================
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Prevent the physics engine from tipping the astronaut over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Auto-assign components if left blank in the inspector
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (playerCamera == null)
        {
            if (Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("No Main Camera found! Please tag your camera as 'MainCamera' or assign it in the inspector.");
            }
        }

        // Lock and hide the mouse cursor for standard 3D controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize camera rotation based on starting view
        cameraYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        // 1. Gather Movement Input (WASD / Arrows)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movementInput = new Vector3(horizontal, 0f, vertical).normalized;

        // 2. Gather Mouse Input for looking around
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 3. Ground Check & Jump Input
        CheckGrounded();
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
        }

        // 4. Update Animations
        if (animator != null)
        {
            animator.SetFloat("Speed", movementInput.magnitude);
        }
    }

    void FixedUpdate()
    {
        // 5. Physics-based movement
        MoveAndRotate();

        // 6. Physics-based jumping
        if (jumpRequested)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }

        // 7. Apply extra gravity when falling
        if (!isGrounded && rb.velocity.y < 0)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    void LateUpdate()
    {
        // 8. Update Camera Position and Rotation
        if (playerCamera != null)
        {
            HandleCameraLook();
        }
    }

    // =============================================================================
    // MOVEMENT & ROTATION
    // =============================================================================
    private void MoveAndRotate()
    {
        if (movementInput.magnitude >= 0.1f)
        {
            // Calculate the angle based on WASD + the Camera's current forward facing direction
            float targetAngle = Mathf.Atan2(movementInput.x, movementInput.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;

            // Smoothly rotate the character model to face the target angle
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            rb.MoveRotation(Quaternion.Euler(0f, angle, 0f));

            // Move the Rigidbody forward in the calculated direction
            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // =============================================================================
    // CAMERA
    // =============================================================================
    private void HandleCameraLook()
    {
        // Update yaw (Left/Right) and pitch (Up/Down)
        cameraYaw += mouseX;
        cameraPitch -= mouseY;

        // Clamp the pitch so the camera doesn't flip completely upside down
        cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);

        // Calculate the camera's new rotation
        Quaternion camRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);

        // Calculate the camera's new position (orbiting around the player + offset)
        Vector3 focusPoint = transform.position + cameraOffset;
        Vector3 directionToCamera = -(camRotation * Vector3.forward);
        Vector3 camPosition = focusPoint + (directionToCamera * cameraDistance);

        // --- FOOLPROOF CAMERA COLLISION ---
        // Raycast backward to see if a wall is between the player and the target camera position
        if (Physics.SphereCast(focusPoint, 0.2f, directionToCamera, out RaycastHit hit, cameraDistance, groundMask))
        {
            // If we hit a wall, move the camera in closer so it doesn't clip through
            float clampedDistance = Mathf.Clamp(hit.distance, minCameraDistance, cameraDistance);
            camPosition = focusPoint + (directionToCamera * clampedDistance);
        }

        // Apply rotation and position to the Main Camera
        playerCamera.position = camPosition;
        playerCamera.rotation = camRotation;
    }

    // =============================================================================
    // GROUND CHECK
    // =============================================================================
    private void CheckGrounded()
    {
        // --- FOOLPROOF GROUND CHECK ---
        // Instead of a single thin raycast, we use a SphereCast.
        // This acts like a thick cylinder, meaning the astronaut won't get stuck unable to jump if they stand halfway off a ledge!
        float radius = capsuleCollider.radius * 0.9f;
        float castDistance = (capsuleCollider.height / 2f) - radius + 0.1f;

        isGrounded = Physics.SphereCast(capsuleCollider.bounds.center, radius, Vector3.down, out _, castDistance, groundMask);
    }
}