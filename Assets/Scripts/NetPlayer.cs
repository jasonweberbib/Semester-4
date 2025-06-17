using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.Animations;
using UnityEngine;

public class NetPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int pillerscore = 25;
    private float scoreAccumulator = 0f;
    public GameObject piller;
    public GameObject ball;
    public Transform playerCamera; // Die Kamera, die am Spieler hängt
    public float moveSpeed = 5f;
    public float rotationSpeed = 90f; // Grad pro Sekunde



    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        if (score.Value < 0)
        {
            score.Value = 0;
        }

        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Kamera-Richtung nehmen, aber nur auf Y-Ebene
            Vector3 camForward = playerCamera.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = playerCamera.right;
            camRight.y = 0;
            camRight.Normalize();

            // Richtung relativ zur Kamera
            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;
            moveDir.Normalize();

            // Position bewegen
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // Rotation anpassen (zur Bewegungsrichtung drehen)
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Netzwerk sync
            MovingServerRPC(transform.position, transform.rotation);
        }


        if ((scoreAccumulator += Time.deltaTime) >= 1.0f)
        {
            score.Value += 1;
            scoreAccumulator -= 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateObjectServerRpc();
        }

        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotationInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotationInput = 1f;

        if (rotationInput != 0f)
        {
            float rotationAmount = rotationInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
            RotateServerRpc(transform.rotation);
        }

        if (playerCamera != null)
        {
            playerCamera.position = transform.position + new Vector3(0.36f, 0.622f, -1.671f); // z.B. Schulterhöhe
            playerCamera.rotation = Quaternion.Euler(10f, transform.eulerAngles.y, 0); // Leichter Blick nach unten
        }

    }

    // [ServerRpc (RequierOwnership = false)]
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