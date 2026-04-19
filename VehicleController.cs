using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Vehicle controller that handles both car and spaceship movement.
/// Vehicle ID 1 = Car (WASD: forward/back/left/right)
/// Vehicle ID 2 = Spaceship (WASD: forward/back + rotation, mouse for aiming)
/// </summary>
public class VehicleController : MonoBehaviour
{
    [Header("Vehicle Settings")]
    [Tooltip("1 = Car, 2 = Spaceship")]
    public int vehicleID = 1;

    [Header("Car Settings (Vehicle ID 1)")]
    public float carSpeed = 20f;
    public float carTurnSpeed = 100f;

    [Header("Spaceship Settings (Vehicle ID 2)")]
    public float spaceshipSpeed = 30f;
    public float spaceshipRotationSpeed = 90f;
    public float spaceshipMouseSensitivity = 3f;
    public float spaceshipVerticalSpeed = 20f;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode enterExitKey = KeyCode.E;

    // Current movement input
    private float verticalInput;   // W/S or Up/Down
    private float horizontalInput; // A/D or Left/Right
    private float vertical2Input;  // Q/E or Up/Down for spaceship altitude
    private Vector2 mouseInput;    // Mouse X/Y for spaceship

    // Vehicle state
    private bool isOccupied = false;
    private Transform playerTransform = null;

    // References
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("No Rigidbody found. Adding one automatically.");
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        // Validate vehicle ID
        if (vehicleID != 1 && vehicleID != 2)
        {
            Debug.LogWarning($"Invalid vehicle ID: {vehicleID}. Defaulting to Car (ID 1).");
            vehicleID = 1;
        }

        Debug.Log($"Vehicle initialized as: {(vehicleID == 1 ? "Car" : "Spaceship")}");
    }

    private void Update()
    {
        // Handle entering/exiting vehicle
        if (Input.GetKeyDown(enterExitKey))
        {
            if (isOccupied)
            {
                ExitVehicle();
            }
            else
            {
                TryEnterVehicle();
            }
        }

        // Only process movement if vehicle is occupied
        if (!isOccupied) return;

        // Get input values
        GetInput();

        // Apply movement based on vehicle type
        if (vehicleID == 1)
        {
            HandleCarMovement();
        }
        else if (vehicleID == 2)
        {
            HandleSpaceshipMovement();
        }
    }

    /// <summary>
    /// Try to find a player nearby and enter the vehicle.
    /// </summary>
    private void TryEnterVehicle()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("No object with 'Player' tag found.");
            return;
        }

        float distance = Vector3.Distance(player.transform.position, transform.position);
        if (distance <= interactionRange)
        {
            EnterVehicle(player.transform);
        }
        else
        {
            Debug.Log($"Player too far to enter vehicle. Distance: {distance:F1}, Range: {interactionRange}");
        }
    }

    /// <summary>
    /// Make player a child of the vehicle and start controlling it.
    /// </summary>
    private void EnterVehicle(Transform player)
    {
        playerTransform = player;
        
        // Make player a child of the vehicle
        playerTransform.SetParent(transform);
        
        isOccupied = true;
        Debug.Log($"Entered {GetVehicleTypeName()}. Press {enterExitKey} to exit.");
    }

    /// <summary>
    /// Remove player from vehicle and restore control.
    /// </summary>
    private void ExitVehicle()
    {
        if (playerTransform == null) return;

        // Restore player to scene root (removes parent)
        playerTransform.SetParent(null);

        Debug.Log($"Exited {GetVehicleTypeName()}.");
        
        playerTransform = null;
        isOccupied = false;
    }

    /// <summary>
    /// Reads all input values from keyboard and mouse.
    /// </summary>
    private void GetInput()
    {
        // Keyboard input (WASD or Arrow keys)
        verticalInput = Input.GetAxis("Vertical");   // W/S or Up/Down
        horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right

        // Space/C for spaceship altitude (up/down)
        if (vehicleID == 2)
        {
            if (Input.GetKey(KeyCode.Space)) vertical2Input = 1f;      // Up
            else if (Input.GetKey(KeyCode.C)) vertical2Input = -1f;    // Down
            else if (Input.GetKey(KeyCode.UpArrow)) vertical2Input = 1f;
            else if (Input.GetKey(KeyCode.DownArrow)) vertical2Input = -1f;
            else vertical2Input = 0f;

            // Mouse input for spaceship
            mouseInput.x = Input.GetAxis("Mouse X");
            mouseInput.y = Input.GetAxis("Mouse Y");
        }
    }

    /// <summary>
    /// Car movement: W/S = forward/back, A/D = turn left/right
    /// </summary>
    private void HandleCarMovement()
    {
        // Forward/Backward movement
        Vector3 moveDirection = transform.forward * verticalInput * carSpeed * Time.deltaTime;
        transform.position += moveDirection;

        // Left/Right rotation
        float turnAmount = horizontalInput * carTurnSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, turnAmount);
    }

    /// <summary>
    /// Spaceship movement:
    /// - W/S = forward/backward
    /// - Q/E = up/down (altitude)
    /// - A/D = rotate left/right (yaw)
    /// - Mouse X = additional yaw control
    /// - Mouse Y = pitch control (look up/down)
    /// </summary>
    private void HandleSpaceshipMovement()
    {
        // Forward/Backward movement
        Vector3 moveDirection = transform.forward * verticalInput * spaceshipSpeed * Time.deltaTime;
        transform.position += moveDirection;

        // Up/Down movement (Q/E keys)
        Vector3 verticalDirection = Vector3.up * vertical2Input * spaceshipVerticalSpeed * Time.deltaTime;
        transform.position += verticalDirection;

        // Rotation: A/D keys rotate (yaw), Mouse controls pitch and additional yaw
        float yawRotation = horizontalInput * spaceshipRotationSpeed * Time.deltaTime;
        float mouseYaw = mouseInput.x * spaceshipMouseSensitivity;
        float mousePitch = mouseInput.y * spaceshipMouseSensitivity;

        // Apply rotations
        // Yaw (left/right) - from keys and mouse
        transform.Rotate(Vector3.up, yawRotation + mouseYaw);

        // Pitch (up/down) - from mouse only
        transform.Rotate(Vector3.right, -mousePitch);
    }

    /// <summary>
    /// Public method to set vehicle ID at runtime.
    /// </summary>
    public void SetVehicleID(int newVehicleID)
    {
        if (newVehicleID == 1 || newVehicleID == 2)
        {
            vehicleID = newVehicleID;
            Debug.Log($"Switched to: {(vehicleID == 1 ? "Car" : "Spaceship")}");
        }
        else
        {
            Debug.LogError($"Invalid vehicle ID: {newVehicleID}. Must be 1 or 2.");
        }
    }

    /// <summary>
    /// Check if current vehicle is flyable (spaceship is flyable, car is not).
    /// </summary>
    public bool IsFlyable()
    {
        return vehicleID == 2;
    }

    /// <summary>
    /// Get the name of the current vehicle type.
    /// </summary>
    public string GetVehicleTypeName()
    {
        return vehicleID == 1 ? "Car" : "Spaceship";
    }

    /// <summary>
    /// Check if vehicle is currently occupied by a player.
    /// </summary>
    public bool IsOccupied()
    {
        return isOccupied;
    }
}

/*
 * SUMMARY: This script provides a unified vehicle controller for both cars and spaceships in Unity 3D.
 * It detects vehicle type via vehicleID (1 = car, 2 = spaceship) and applies appropriate movement controls.
 * Players can enter vehicles by pressing E when within interaction range, making them a child of the vehicle.
 * Car uses WASD for movement while spaceship adds Space/C for altitude and mouse for directional aiming.
 */