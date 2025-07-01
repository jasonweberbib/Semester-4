using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Für TextMeshPro

public class NetPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int pillerscore = 25;
    public int ballscore = 75; // Kosten für das Spawnen eines Balls
    private float scoreAccumulator = 0f;

    public GameObject piller; // Prefab für den Pillar
    public GameObject ball;   // Prefab für den Ball

    [Header("UI Setup")]
    public GameObject playerUIPrefab; // Hier das HUD Canvas Prefab zuweisen (mit PlayerUIManagerr Script drauf)
    private PlayerUIManagerr currentUI; // Referenz zum instanziierten UI Manager

    [Header("Camera Setup")]
    public GameObject cameraFollowTargetPrefab; // Hier ein leeres GameObject Prefab zuweisen (z.B. _CameraFollowTarget)
    private Transform cameraFollowTarget; // Wird zur Laufzeit gespawnt

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f; // Schnellere Rotation für Maus-Steuerung
    public float mouseSensitivity = 2f;

    private float mouseX; // Für die horizontale Mausbewegung (Yaw)
    private float mouseY; // Für die vertikale Mausbewegung (Pitch)

    private bool isSetupComplete = false; // Flag, um zu verhindern, dass das Setup mehrfach läuft

    void Awake()
    {
        // Debug.Log("NetPlayer Awake called."); // Hinzugefügt für Debugging
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"NetPlayer OnNetworkSpawn für Client {OwnerClientId}. IsOwner: {IsOwner}. IsHost: {IsHost}. IsServer: {IsServer}."); //

        if (IsOwner)
        {
            Debug.Log($"NetPlayer OnNetworkSpawn für Client {OwnerClientId}: Dies ist der lokale Spieler."); //

            // WICHTIGE ÄNDERUNG: Registriere einen Listener für den Szenen-Lade-Abschluss
            // Dies ist der zuverlässigste Weg, um auf das vollständige Laden einer Szene zu warten.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
                Debug.Log($"NetPlayer OnNetworkSpawn für Client {OwnerClientId}: OnLoadComplete Listener registriert."); //
            }
            else
            {
                Debug.LogError($"NetPlayer OnNetworkSpawn für Client {OwnerClientId}: NetworkManager oder SceneManager ist null. Kann OnLoadComplete nicht registrieren."); //
            }

            // Prüfen, ob wir bereits in der Spielszene sind (z.B. wenn der Spieler-Prefab direkt in der Level-Szene platziert ist)
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == "Level" && !isSetupComplete) // Annahme: Deine Spielszene heißt "Level"
            {
                Debug.Log($"NetPlayer OnNetworkSpawn für Client {OwnerClientId}: Bereits in Spielszene '{currentSceneName}'. Starte Setup sofort."); //
                StartCoroutine(SetupCameraAndUI());
            }
            else
            {
                Debug.Log($"NetPlayer OnNetworkSpawn für Client {OwnerClientId}: Aktuelle Szene ist '{currentSceneName}'. Warte auf Szenenwechsel zum Setup."); //
            }
            
            // Score Listener für den lokalen Spieler
            score.OnValueChanged += UpdateScoreLocalDisplay; //
            UpdateScoreLocalDisplay(0, score.Value); // Initialer Score-Display
        }
        else
        {
            Debug.Log($"NetPlayer OnNetworkSpawn für Client {OwnerClientId}: Dies ist ein Remote-Spieler."); //
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            // WICHTIGE ÄNDERUNG: Deregistriere den Listener beim Despawn
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
                Debug.Log($"NetPlayer OnNetworkDespawn für Client {OwnerClientId}: OnLoadComplete Listener deregistriert."); //
            }

            score.OnValueChanged -= UpdateScoreLocalDisplay; //
            Cursor.lockState = CursorLockMode.None; //
            Cursor.visible = true; //
            Debug.Log($"NetPlayer OnNetworkDespawn für Client {OwnerClientId}: Cursor freigegeben."); //

            if (currentUI != null)
            {
                Destroy(currentUI.gameObject); //
                Debug.Log($"NetPlayer OnNetworkDespawn für Client {OwnerClientId}: HUD zerstört."); //
            }
            if (cameraFollowTarget != null && cameraFollowTarget.gameObject != null)
            {
                Destroy(cameraFollowTarget.gameObject); //
                Debug.Log($"NetPlayer OnNetworkDespawn für Client {OwnerClientId}: CameraFollowTarget zerstört."); //
            }
        }
        base.OnNetworkDespawn(); // Basisklassen-Aufruf beibehalten
    }

    // Callback-Methode für den NetworkManager.SceneManager.OnLoadComplete Event
    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        // Nur für den Owner und wenn das Setup noch nicht erfolgt ist und es die Spielszene ist
        if (IsOwner && !isSetupComplete && sceneName == "Level") // Annahme: Deine Spielszene heißt "Level"
        {
            Debug.Log($"NetPlayer OnSceneLoadComplete für Client {OwnerClientId}: Szene '{sceneName}' vollständig geladen. Starte Kamera/UI Setup."); //
            StartCoroutine(SetupCameraAndUI());
        }
        else if (IsOwner)
        {
            Debug.Log($"NetPlayer OnSceneLoadComplete für Client {OwnerClientId}: Szene '{sceneName}' geladen, aber entweder nicht die Spielszene oder Setup schon abgeschlossen."); //
        }
    }

    private IEnumerator SetupCameraAndUI()
    {
        if (isSetupComplete) // Verhindere doppeltes Setup
        {
            Debug.LogWarning($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Setup bereits abgeschlossen. Breche ab."); //
            yield break;
        }

        Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Starte Einrichtungsprozess."); //

        // Setup HUD
        Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Starte HUD Setup."); //
        if (playerUIPrefab != null)
        {
            GameObject playerUIGameObject = Instantiate(playerUIPrefab); //
            currentUI = playerUIGameObject.GetComponent<PlayerUIManagerr>(); //

            if (currentUI != null)
            {
                currentUI.Initialize(this); // 'this' ist eine Referenz auf diesen NetPlayer
                Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: UI Manager gefunden. Initialisiere UI."); //
            }
            else
            {
                Debug.LogError($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: PlayerUIPrefab ({playerUIPrefab.name}) hat kein PlayerUIManagerr Skript!"); //
            }
        }
        else
        {
            Debug.LogError($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: PlayerUIPrefab nicht zugewiesen! Bitte im Inspector zuweisen."); //
        }

        // Setup Camera (verzögert, um sicherzustellen, dass Camera.main verfügbar ist)
        Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Starte Kamera Setup."); //
        Camera mainCam = null;
        int attempts = 0;
        int maxAttempts = 10;
        float delayBetweenAttempts = 0.2f; // 200ms

        while (mainCam == null && attempts < maxAttempts)
        {
            mainCam = Camera.main; // Hier wird die Kamera über den Tag gesucht
            
            if (mainCam == null)
            {
                Debug.LogWarning($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Versuch {attempts + 1}: Main Camera nicht gefunden, warte..."); //
                yield return new WaitForSeconds(delayBetweenAttempts); //
                attempts++; //
            }
        }

        if (mainCam == null)
        {
            Debug.LogError($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Main Camera konnte nach {maxAttempts} Versuchen nicht gefunden werden. Kamera wird nicht eingerichtet."); //
            isSetupComplete = true; // Markiere Setup als abgeschlossen, auch wenn es fehlschlug, um Endlosschleifen zu vermeiden
            yield break; // Abbrechen, wenn die Kamera nicht gefunden wurde
        }

        Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Main Camera gefunden."); //
        
        // Instanziiere cameraFollowTarget
        if (cameraFollowTargetPrefab != null)
        {
            cameraFollowTarget = Instantiate(cameraFollowTargetPrefab).transform; //
            cameraFollowTarget.name = $"PlayerCamTarget_{OwnerClientId}"; // Für besseres Debugging
            Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: CameraFollowTarget instanziiert: {cameraFollowTarget.name}"); //
        }
        else
        {
            Debug.LogError($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: cameraFollowTargetPrefab ist NICHT zugewiesen! Bitte im Inspector zuweisen."); //
            isSetupComplete = true; // Markiere Setup als abgeschlossen
            yield break; // Abbrechen, wenn kein Prefab zugewiesen ist
        }

        CameraFollow followScript = mainCam.GetComponent<CameraFollow>(); //
        if (followScript == null)
        {
            Debug.LogError($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Main Camera hat kein CameraFollow Skript! Füge es hinzu."); //
            followScript = mainCam.gameObject.AddComponent<CameraFollow>(); //
        }

        followScript.SetTarget(cameraFollowTarget); //
        Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: CameraFollow Script Target gesetzt zu {cameraFollowTarget.name}."); //

        mouseX = transform.eulerAngles.y; //
        mouseY = 0f; // Startet ohne vertikale Neigung

        Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Kamera-Setup abgeschlossen."); //

        Cursor.lockState = CursorLockMode.None; //
        Cursor.visible = true; //
        Debug.Log($"NetPlayer SetupCameraAndUI für Client {OwnerClientId}: Cursor freigegeben für UI-Interaktion."); //
        
        isSetupComplete = true; // Markiere, dass das Setup erfolgreich abgeschlossen wurde
    }

    void Update()
    {
        // Nur der Besitzer des Objekts sollte die Steuerung und die Kamera kontrollieren
        // Und nur wenn das Setup abgeschlossen ist
        if (!IsOwner || !IsSpawned || !isSetupComplete || cameraFollowTarget == null) return; //

        // === MAUS-BLICK (Kamera-Rotation - nur wenn die rechte Maustaste gedrückt ist) ===
        if (Input.GetMouseButton(1)) // Prüfen, ob die rechte Maustaste gedrückt gehalten wird
        {
            Cursor.lockState = CursorLockMode.Locked; // Cursor für Kamerasteuerung sperren
            Cursor.visible = false;                   // Cursor ausblenden

            float mouseDeltaX = Input.GetAxis("Mouse X") * mouseSensitivity; //
            float mouseDeltaY = Input.GetAxis("Mouse Y") * mouseSensitivity; //

            mouseX += mouseDeltaX; //
            mouseY -= mouseDeltaY; //
            mouseY = Mathf.Clamp(mouseY, -45f, 60f); // Vertikalen Blickwinkel begrenzen (Pitch)
        }
        else // Rechte Maustaste ist NICHT gedrückt
        {
            Cursor.lockState = CursorLockMode.None; // Cursor für UI-Interaktion freigeben
            Cursor.visible = true;                  // Cursor anzeigen
        }

        cameraFollowTarget.position = transform.position + Vector3.up * 1.5f; //
        cameraFollowTarget.rotation = Quaternion.Euler(mouseY, mouseX, 0); //

        // === BEWEGUNGSINPUT (Relativ zur Kamera) ===
        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized; //

        if (inputDir.magnitude >= 0.1f) //
        {
            Vector3 camForward = cameraFollowTarget.forward; //
            Vector3 camRight = cameraFollowTarget.right; //
            camForward.y = 0; //
            camRight.y = 0;   //
            camForward.Normalize(); //
            camRight.Normalize();   //

            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x; //
            moveDir.Normalize(); //

            transform.position += moveDir * moveSpeed * Time.deltaTime; //

            Quaternion targetPlayerRotation = Quaternion.LookRotation(new Vector3(camForward.x, 0, camForward.z)); //
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetPlayerRotation, rotationSpeed * Time.deltaTime); //

            MovingServerRPC(transform.position, transform.rotation); //
        }
        else
        {
            Vector3 camForward = cameraFollowTarget.forward; //
            Quaternion targetPlayerRotation = Quaternion.LookRotation(new Vector3(camForward.x, 0, camForward.z)); //
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetPlayerRotation, rotationSpeed * Time.deltaTime); //
            MovingServerRPC(transform.position, transform.rotation); //
        }

        // === SCORE-AKKUMULATION ===
        if ((scoreAccumulator += Time.deltaTime) >= 1.0f) //
        {
            score.Value += 1; //
            scoreAccumulator -= 1.0f; //
        }
    }

    // Entferne CheckIfInGameScene(), da wir jetzt den SceneManager.OnLoadComplete Event verwenden.
    // private void CheckIfInGameScene() { ... }

    private void UpdateScoreLocalDisplay(int oldScore, int newScore)
    {
        if (currentUI != null && currentUI.scoreText != null) //
        {
            currentUI.scoreText.text = "Score: " + newScore; //
        }
        else
        {
            // Debug.LogWarning($"UpdateScoreLocalDisplay: currentUI oder scoreText ist null für Client {OwnerClientId}."); //
        }
    }

    // --- RPCs für das Spawnen von Objekten ---
    [ServerRpc]
    public void SpawnPillarServerRpc()
    {
        if (piller == null) //
        {
            Debug.LogError("Pillar prefab is not assigned in NetPlayer!"); //
            return;
        }
        if (score.Value >= pillerscore) //
        {
            GameObject obj = Instantiate(piller, transform.position + transform.forward * 2f + transform.up, Quaternion.identity); //
            NetworkObject networkObj = obj.GetComponent<NetworkObject>(); //
            networkObj.Spawn(true); // Spawne das Objekt im Netzwerk

            PillarScoreBonus pillarBonus = obj.GetComponent<PillarScoreBonus>(); //
            if (pillarBonus != null) //
            {
                pillarBonus.spawnerPlayerId.Value = OwnerClientId; //
            }
            else
            {
                Debug.LogWarning("Spawned Pillar does not have a PillarScoreBonus component! Make sure it's on the Pillar prefab."); //
            }

            score.Value -= pillerscore; // Kosten abziehen
        }
        else
        {
            Debug.LogWarning($"Nicht genug Score, um Pillar zu spawnen. Benötigt: {pillerscore}, Hat: {score.Value}"); //
        }
    }

    [ServerRpc]
    public void SpawnBallServerRpc()
    {
        if (ball == null) //
        {
            Debug.LogError("Ball prefab is not assigned in NetPlayer!"); //
            return;
        }

        if (score.Value >= ballscore) //
        {
            GameObject obj = Instantiate(ball, transform.position + transform.forward * 2f + transform.up, transform.rotation); //
            NetworkObject networkObj = obj.GetComponent<NetworkObject>(); //
            networkObj.Spawn(true); // Spawne das Objekt im Netzwerk
            score.Value -= ballscore; // Kosten abziehen
        }
        else
        {
            Debug.LogWarning($"Nicht genug Score, um Ball zu spawnen. Benötigt: {ballscore}, Hat: {score.Value}"); //
        }
    }
    // --- Ende RPCs für das Spawnen von Objekten ---

    [ServerRpc]
    void MovingServerRPC(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner) //
        {
            transform.position = position; //
        }
        transform.rotation = rotation; //

        MovingClientRPC(position, rotation); //
    }

    [ClientRpc]
    void MovingClientRPC(Vector3 position, Quaternion rotation)
    {
        if (IsOwner) return; //
        transform.position = position; //
        transform.rotation = rotation; //
    }

    [ServerRpc]
    void RotateServerRpc(Quaternion newRotation)
    {
        transform.rotation = newRotation; //
        RotateClientRpc(newRotation); //
    }

    [ClientRpc]
    void RotateClientRpc(Quaternion newRotation)
    {
        if (IsOwner) return; //
        transform.rotation = newRotation; //
    }
}