using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int pillerscore = 25;
    private float scoreAccumulator = 0f;

    public GameObject piller;
    public GameObject ball;

    public Transform playerCamera;
    public GameObject cameraFollowTargetPrefab; // Assign this in Inspector
    private Transform cameraFollowTarget; // Spawned at runtime

    public float moveSpeed = 5f;
    public float rotationSpeed = 90f;
    public float mouseSensitivity = 2f;

    private float mouseX;
    private float mouseY;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        if (!IsOwner) return;

        if (IsOwner)
        {
            // Find and assign camera follow script
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;

                CameraFollow followScript = mainCam.GetComponent<CameraFollow>();
                if (followScript != null)
                {
                    followScript.SetTarget(transform); // Or use your camera follow target if needed
                }
            }
            else
            {
                Debug.LogError("No MainCamera found in scene.");
            }
        }


        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                playerCamera = cam.transform;
            else
                Debug.LogError("Main Camera not found.");
        }

        if (cameraFollowTargetPrefab != null)
        {
            GameObject camTarget = Instantiate(cameraFollowTargetPrefab);
            cameraFollowTarget = camTarget.transform;

            // Optional: match player's current yaw
            mouseX = transform.eulerAngles.y;
        }
        else
        {
            Debug.LogError("cameraFollowTargetPrefab not assigned.");
        }
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned || cameraFollowTarget == null || playerCamera == null) return;

        // === MOUSE LOOK (Right-click drag) ===
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseDeltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseDeltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            mouseX += mouseDeltaX;
            mouseY -= mouseDeltaY;
            mouseY = Mathf.Clamp(mouseY, -20f, 60f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // === UPDATE CAMERA POSITION ===
        cameraFollowTarget.position = transform.position + Vector3.up * 1.5f;

        Vector3 camOffset = Quaternion.Euler(mouseY, mouseX, 0) * new Vector3(0, 2f, -5f);
        playerCamera.position = cameraFollowTarget.position + camOffset;
        playerCamera.LookAt(cameraFollowTarget.position);

        // === MOVEMENT INPUT ===
        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 camForward = cameraFollowTarget.forward;
            Vector3 camRight = cameraFollowTarget.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;
            moveDir.Normalize();

            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // Rotate player to face movement direction
            if (Input.GetMouseButton(1))
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            MovingServerRPC(transform.position, transform.rotation);
        }

        // === ROTATE WITH Q/E ===
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotationInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotationInput = 1f;

        if (rotationInput != 0f)
        {
            float rotationAmount = rotationInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
            RotateServerRpc(transform.rotation);
        }

        // === SCORE ===
        if ((scoreAccumulator += Time.deltaTime) >= 1.0f)
        {
            score.Value += 1;
            scoreAccumulator -= 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateObjectServerRpc();
        }
    }

    [ServerRpc]
    public void CreateObjectServerRpc()
    {
        if (score.Value >= pillerscore)
        {
            GameObject obj = Instantiate(piller);
            obj.GetComponent<NetworkObject>().Spawn(true);
            score.Value -= 25;
        }
        if (score.Value >= 75)
        {
            GameObject obj = Instantiate(ball);
            obj.GetComponent<NetworkObject>().Spawn(true);
            score.Value -= 75;
        }
    }

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
        if (IsOwner) return;
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
}
