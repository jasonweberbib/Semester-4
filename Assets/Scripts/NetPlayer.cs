using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int pillerscore = 25; // Cost for spawning a pillar
    public int ballscore = 75;   // Cost for spawning a ball (added this for clarity)
    private float scoreAccumulator = 0f;

    public GameObject piller;
    public GameObject ball;

    // playerCamera is now mostly for reference to the main camera, CameraFollow script does the work
    public Transform playerCamera; 
    public GameObject cameraFollowTargetPrefab; // Assign this in Inspector (a simple empty GameObject prefab)
    private Transform cameraFollowTarget; // Spawned at runtime, acts as the pivot for the camera

    public float moveSpeed = 5f;
    public float rotationSpeed = 360f; // Increased rotation speed for snappier player rotation
    public float mouseSensitivity = 2f;

    private float mouseX;
    private float mouseY;

    [Header("UI")] // New Header for UI specific variables
    public GameObject playerUIPrefab; // Assign your Canvas prefab here in the Inspector

    private PlayerUIManagerr currentUI; // Reference to the instantiated UI manager

    void Awake()
    {
        // Cursor state will be managed in Start() and Update() based on right-click
    }

    void Start()
    {
        if (!IsOwner) return;

        // Find and assign playerCamera (the actual Main Camera in the scene)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerCamera = mainCam.transform;
        }
        else
        {
            Debug.LogError("No MainCamera found in scene. Please ensure your camera has the 'MainCamera' tag.");
        }

        if (cameraFollowTargetPrefab != null)
        {
            // Instantiate the camera follow target for this specific player
            GameObject camTarget = Instantiate(cameraFollowTargetPrefab);
            cameraFollowTarget = camTarget.transform;

            // Set the spawned cameraFollowTarget as the target for the main camera's CameraFollow script
            if (mainCam != null)
            {
                CameraFollow followScript = mainCam.GetComponent<CameraFollow>();
                if (followScript != null)
                {
                    followScript.SetTarget(cameraFollowTarget); // Pass the new target to CameraFollow
                }
                else
                {
                    Debug.LogError("Main Camera does not have a CameraFollow script attached. Please add it.");
                }
            }

            // Initialize mouseX with the player's current yaw to prevent jump on start
            mouseX = transform.eulerAngles.y;
        }
        else
        {
            Debug.LogError("cameraFollowTargetPrefab not assigned. Please assign it in the Inspector.");
        }

        // --- UI Instantiation and Initialization ---
        if (playerUIPrefab != null)
        {
            // Instantiate the UI prefab only for the local owner
            GameObject playerUIInstance = Instantiate(playerUIPrefab);
            currentUI = playerUIInstance.GetComponent<PlayerUIManagerr>();

            if (currentUI != null)
            {
                currentUI.Initialize(this); // Initialize the UI Manager with a reference to THIS NetPlayer
            }
            else
            {
                Debug.LogError("PlayerUIPrefab does not have a PlayerUIManagerr script!");
            }
            // Optional: Make sure the UI persists across scene loads if your player does
            // DontDestroyOnLoad(playerUIInstance); 
        }
        else
        {
            Debug.LogError("PlayerUIPrefab not assigned in NetPlayer. Please assign the UI Canvas prefab.");
        }

        // Initially, the cursor is unlocked and visible for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // Only owner should control their player and camera
        if (!IsOwner || !IsSpawned || cameraFollowTarget == null || playerCamera == null) return;

        // === MOUSE LOOK (Camera Rotation - only when right mouse button is pressed) ===
        if (Input.GetMouseButton(1)) // Check if right mouse button is held down
        {
            Cursor.lockState = CursorLockMode.Locked; // Lock cursor for camera control
            Cursor.visible = false;                   // Hide cursor

            // Get mouse input and apply to mouseX/mouseY
            float mouseDeltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseDeltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            mouseX += mouseDeltaX;
            mouseY -= mouseDeltaY;
            mouseY = Mathf.Clamp(mouseY, -45f, 60f); // Clamped to allow more downward look
        }
        else // Right mouse button is NOT held down
        {
            Cursor.lockState = CursorLockMode.None; // Unlock cursor for UI interaction
            Cursor.visible = true;                  // Show cursor
        }

        // Apply mouseX and mouseY to the cameraFollowTarget's rotation.
        // This target's rotation will then be used by CameraFollow script and for player movement/rotation.
        // This line is outside the if(Input.GetMouseButton(1)) block
        // so that the cameraFollowTarget's rotation is always updated,
        // even if the mouse is not currently controlling the camera.
        // This is important for smooth player rotation and movement relative to the camera.
        cameraFollowTarget.rotation = Quaternion.Euler(mouseY, mouseX, 0);

        // Update camera follow target position (usually at the player's head level)
        // This is the point around which the camera will orbit
        cameraFollowTarget.position = transform.position + Vector3.up * 1.5f;

        // === MOVEMENT INPUT (Relative to Camera) ===
        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        // Only move and rotate if there's significant input
        if (inputDir.magnitude >= 0.1f)
        {
            // Get camera's forward and right vectors, ignoring Y component for horizontal movement
            // This ensures movement is always on the ground plane relative to camera's horizontal view
            Vector3 camForward = cameraFollowTarget.forward;
            Vector3 camRight = cameraFollowTarget.right;
            camForward.y = 0; // Flatten the vector to the horizontal plane
            camRight.y = 0;   // Flatten the vector to the horizontal plane
            camForward.Normalize(); // Normalize after flattening to maintain correct speed
            camRight.Normalize();   // Normalize after flattening to maintain correct speed

            // Calculate movement direction relative to camera's orientation
            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;
            moveDir.Normalize(); // Ensure consistent speed in all directions

            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // Rotate player to face the camera's horizontal direction (its forward vector)
            // The player's forward (Z-axis) will align with the camera's horizontal forward
            Quaternion targetPlayerRotation = Quaternion.LookRotation(new Vector3(camForward.x, 0, camForward.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetPlayerRotation, rotationSpeed * Time.deltaTime);

            MovingServerRPC(transform.position, transform.rotation); // Sync position and rotation
        }
        else
        {
            // Even if not moving, ensure player's rotation matches camera's yaw
            Vector3 camForward = cameraFollowTarget.forward;
            Quaternion targetPlayerRotation = Quaternion.LookRotation(new Vector3(camForward.x, 0, camForward.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetPlayerRotation, rotationSpeed * Time.deltaTime);
            MovingServerRPC(transform.position, transform.rotation); // Sync rotation even when idle
        }

        // === ROTATE WITH Q/E (Optional: This might conflict with camera-based rotation) ===
        // If you want Q/E to manually rotate the player *independent* of camera, keep this.
        // For a typical third-person game, this might be removed, or Q/E re-purposed (e.g., strafing without turning).
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotationInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotationInput = 1f;

        if (rotationInput != 0f)
        {
            float rotationAmount = rotationInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0); // Directly rotate the player
            RotateServerRpc(transform.rotation); // Sync this manual rotation
        }

        // === SCORE ACCUMULATION ===
        if ((scoreAccumulator += Time.deltaTime) >= 1.0f)
        {
            score.Value += 1;
            scoreAccumulator -= 1.0f;
        }
    }

    // --- RPCs for Spawning Objects (Updated to pass spawner ID) ---
    [ServerRpc]
    public void SpawnPillarServerRpc()
    {
        if (score.Value >= pillerscore)
        {
            // Instantiate at the player's current position and rotation
            GameObject obj = Instantiate(piller, transform.position, transform.rotation);
            NetworkObject networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn(true);
            
            // Get the PillarScoreBonus component and set the spawnerPlayerId
            PillarScoreBonus pillarBonus = obj.GetComponent<PillarScoreBonus>();
            if (pillarBonus != null)
            {
                pillarBonus.spawnerPlayerId.Value = OwnerClientId; // Set the NetworkObjectId of the player who spawned it
            }
            else
            {
                Debug.LogWarning("Spawned Pillar does not have a PillarScoreBonus component!");
            }

            score.Value -= pillerscore; // Deduct the correct score
        }
        else
        {
            Debug.LogWarning($"Not enough score to spawn Pillar. Needed: {pillerscore}, Has: {score.Value}");
        }
    }

    [ServerRpc]
    public void SpawnBallServerRpc()
    {
        if (score.Value >= ballscore)
        {
            // Instantiate at the player's current position and rotation
            GameObject obj = Instantiate(ball, transform.position, transform.rotation);
            obj.GetComponent<NetworkObject>().Spawn(true);
            score.Value -= ballscore; // Deduct the correct score
        }
        else
        {
            Debug.LogWarning($"Not enough score to spawn Ball. Needed: {ballscore}, Has: {score.Value}");
        }
    }
    // --- End RPCs for Spawning Objects ---


    [ServerRpc]
    void MovingServerRPC(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        MovingClientRPC(position, rotation);
    }

    [ClientRpc]
    void MovingClientRPC(Vector3 position, Quaternion rotation)
    {
        if (IsOwner) return; // Owner already moved locally, no need to apply server update
        transform.position = position;
        transform.rotation = rotation;
    }

    [ServerRpc]
    void RotateServerRpc(Quaternion newRotation)
    {
        transform.rotation = newRotation;
        RotateClientRpc(newRotation);
    }

    [ClientRpc]
    void RotateClientRpc(Quaternion newRotation)
    {
        if (IsOwner) return;
        transform.rotation = newRotation;
    }

    // Added to clean up UI when player object is destroyed (e.g., when disconnecting)
    public override void OnDestroy()
    {
        base.OnDestroy(); // Call the base NetworkBehaviour OnDestroy

        if (IsOwner && currentUI != null)
        {
            Destroy(currentUI.gameObject); // Destroy the UI instance when the owning player is destroyed
        }
    }
}