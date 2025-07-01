using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetPlayer : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int pillerscore = 25;
    public int ballscore = 75; // Kosten für das Spawnen eines Balls
    private float scoreAccumulator = 0f;

    public GameObject piller; // Prefab für den Pillar
    public GameObject ball;   // Prefab für den Ball

    public GameObject playerUIPrefab; // Hier das Canvas Prefab zuweisen
    private PlayerUIManagerr currentUI; // Referenz zum instanziierten UI Manager

    public GameObject cameraFollowTargetPrefab; // Hier ein leeres GameObject Prefab zuweisen
    private Transform cameraFollowTarget; // Wird zur Laufzeit gespawnt

    public float moveSpeed = 5f;
    public float rotationSpeed = 360f; // Schnellere Rotation für Maus-Steuerung
    public float mouseSensitivity = 2f;

    private float mouseX; // Für die horizontale Mausbewegung (Yaw)
    private float mouseY; // Für die vertikale Mausbewegung (Pitch)

    private bool isInGameScene = false; // Flag um zu prüfen ob wir in der Spielszene sind

    void Awake()
    {
        // Der Cursor-Status wird in Start() und Update() verwaltet.
        // Keine feste Zuweisung hier, da das Verhalten von der Maussteuerung abhängt.
        
        // Prüfe ob wir in der Spielszene sind
        CheckIfInGameScene();
    }

    private void CheckIfInGameScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        isInGameScene = (currentScene != "MainMenu" && currentScene != "Menu"); // Passe die Menü-Szenennamen an
        Debug.Log($"NetPlayer: Current scene is {currentScene}, isInGameScene: {isInGameScene}");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return; // Nur der Besitzer führt die folgende Logik aus

        // Prüfe erneut die Szene beim Network Spawn
        CheckIfInGameScene();
        
        if (!isInGameScene)
        {
            Debug.Log($"NetPlayer OnNetworkSpawn for Client {OwnerClientId}: Not in game scene, skipping setup.");
            return;
        }

        Debug.Log($"NetPlayer OnNetworkSpawn for Client {OwnerClientId}: Starting setup in game scene.");
        
        // Warte etwas länger bevor Setup beginnt, um sicherzustellen dass die Szene vollständig geladen ist
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        // Warte mehrere Frames um sicherzustellen dass alles geladen ist
        yield return new WaitForSeconds(0.5f);
        
        // Erneut prüfen ob wir noch in der richtigen Szene sind
        CheckIfInGameScene();
        if (!isInGameScene) yield break;

        // --- Kamera-Setup für den lokalen Spieler ---
        yield return StartCoroutine(SetupCameraAfterDelay());

        // --- UI Instantiation und Initialisierung für den lokalen Spieler ---
        if (playerUIPrefab != null && isInGameScene)
        {
            GameObject playerUIInstance = Instantiate(playerUIPrefab);
            currentUI = playerUIInstance.GetComponent<PlayerUIManagerr>();

            if (currentUI != null)
            {
                Debug.Log($"NetPlayer DelayedSetup for Client {OwnerClientId}: UI Manager gefunden. Initialisiere UI.");
                currentUI.Initialize(this); // Initialisiere den UI Manager mit einer Referenz auf DIESEN NetPlayer
            }
            else
            {
                Debug.LogError($"NetPlayer DelayedSetup for Client {OwnerClientId}: PlayerUIPrefab hat kein PlayerUIManagerr Skript!");
            }
        }
        else
        {
            Debug.LogError($"NetPlayer DelayedSetup for Client {OwnerClientId}: PlayerUIPrefab nicht zugewiesen oder nicht in Spielszene!");
        }

        // Standardmäßig ist der Cursor ungesperrt und sichtbar für UI-Interaktion
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Stelle sicher, dass die UI des Spielers nur vom Besitzer zerstört wird,
        // wenn der Spieler das Netzwerk verlässt oder zerstört wird.
        if (IsOwner && currentUI != null)
        {
            Destroy(currentUI.gameObject);
        }

        // Zerstöre auch das cameraFollowTarget des Besitzers
        if (IsOwner && cameraFollowTarget != null)
        {
            Destroy(cameraFollowTarget.gameObject);
        }
    }

    // Coroutine für die Kamera-Einrichtung
    IEnumerator SetupCameraAfterDelay()
    {
        Camera mainCam = null;
        int attempts = 0;
        int maxAttempts = 10;

        // Versuche mehrmals die Main Camera zu finden
        while (mainCam == null && attempts < maxAttempts)
        {
            mainCam = Camera.main; // Hier wird die Kamera über den Tag gesucht
            
            if (mainCam == null)
            {
                Debug.Log($"Attempt {attempts + 1}: Main Camera not found yet, waiting...");
                yield return new WaitForSeconds(0.2f);
                attempts++;
            }
        }

        if (mainCam == null)
        {
            Debug.LogError("Main Camera could not be found after multiple attempts. Bitte stelle sicher, dass deine Kamera den 'MainCamera' Tag hat und in der Spielszene vorhanden ist.");
            yield break;
        }

        CameraFollow followScript = mainCam.GetComponent<CameraFollow>();
        if (followScript == null)
        {
            Debug.LogError("Main Camera does not have a CameraFollow script attached. Bitte füge es hinzu!");
            yield break; // Coroutine beenden
        }

        if (cameraFollowTargetPrefab != null)
        {
            GameObject camTargetObject = Instantiate(cameraFollowTargetPrefab);
            cameraFollowTarget = camTargetObject.transform;
            cameraFollowTarget.name = $"PlayerCamTarget_{OwnerClientId}"; // Für besseres Debugging

            followScript.SetTarget(cameraFollowTarget);
            mouseX = transform.eulerAngles.y;
            
            Debug.Log($"Camera setup completed successfully for Client {OwnerClientId}");
        }
        else
        {
            Debug.LogError("cameraFollowTargetPrefab not assigned. Bitte weise es im Inspector zu.");
        }
    }

    void Update()
    {
        // Nur der Besitzer des Objekts sollte die Steuerung und die Kamera kontrollieren
        // Und nur wenn wir in der Spielszene sind
        if (!IsOwner || !IsSpawned || cameraFollowTarget == null || !isInGameScene) return;

        // === MAUS-BLICK (Kamera-Rotation - nur wenn die rechte Maustaste gedrückt ist) ===
        if (Input.GetMouseButton(1)) // Prüfen, ob die rechte Maustaste gedrückt gehalten wird
        {
            Cursor.lockState = CursorLockMode.Locked; // Cursor für Kamerasteuerung sperren
            Cursor.visible = false;                   // Cursor ausblenden

            // Maus-Input holen und auf mouseX/mouseY anwenden
            float mouseDeltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseDeltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            mouseX += mouseDeltaX;
            mouseY -= mouseDeltaY;
            mouseY = Mathf.Clamp(mouseY, -45f, 60f); // Vertikalen Blickwinkel begrenzen (Pitch)
        }
        else // Rechte Maustaste ist NICHT gedrückt
        {
            Cursor.lockState = CursorLockMode.None; // Cursor für UI-Interaktion freigeben
            Cursor.visible = true;                  // Cursor anzeigen
        }

        // Aktualisiere die Position des cameraFollowTarget (normalerweise auf Kopfhöhe des Spielers)
        // Dies ist der Punkt, um den die Kamera kreisen wird.
        cameraFollowTarget.position = transform.position + Vector3.up * 1.5f;

        // Wende mouseX und mouseY auf die Rotation des cameraFollowTarget an.
        // Die Rotation dieses Targets wird dann vom CameraFollow-Skript verwendet.
        cameraFollowTarget.rotation = Quaternion.Euler(mouseY, mouseX, 0);

        // === BEWEGUNGSINPUT (Relativ zur Kamera) ===
        // GetAxisRaw für sofortigen Input ohne Smoothing
        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        // Nur bewegen und rotieren, wenn signifikanter Input vorhanden ist
        if (inputDir.magnitude >= 0.1f)
        {
            // Die Vorwärts- und Rechts-Vektoren der Kamera holen, Y-Komponente ignorieren für horizontale Bewegung
            // Dies stellt sicher, dass die Bewegung immer auf der Bodenebene relativ zur horizontalen Ansicht der Kamera erfolgt
            Vector3 camForward = cameraFollowTarget.forward;
            Vector3 camRight = cameraFollowTarget.right;
            camForward.y = 0; // Vektor auf die horizontale Ebene abflachen
            camRight.y = 0;   // Vektor auf die horizontale Ebene abflachen
            camForward.Normalize(); // Nach dem Abflachen normalisieren, um die korrekte Geschwindigkeit beizubehalten
            camRight.Normalize();   // Nach dem Abflachen normalisieren, um die korrekte Geschwindigkeit beizubehalten

            // Bewegungsrichtung relativ zur Kamera-Ausrichtung berechnen
            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;
            moveDir.Normalize(); // Für konsistente Geschwindigkeit in alle Richtungen

            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // Spieler zur horizontalen Richtung der Kamera ausrichten (ihr Vorwärtsvektor)
            // Der Spieler dreht sich immer dorthin, wohin die Kamera schaut (horizontal)
            Quaternion targetPlayerRotation = Quaternion.LookRotation(new Vector3(camForward.x, 0, camForward.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetPlayerRotation, rotationSpeed * Time.deltaTime);

            MovingServerRPC(transform.position, transform.rotation); // Position und Rotation synchronisieren
        }
        else
        {
            // Auch wenn sich der Spieler nicht bewegt, sicherstellen, dass seine Rotation der Yaw-Rotation der Kamera entspricht
            Vector3 camForward = cameraFollowTarget.forward;
            Quaternion targetPlayerRotation = Quaternion.LookRotation(new Vector3(camForward.x, 0, camForward.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetPlayerRotation, rotationSpeed * Time.deltaTime);
            // Auch im Ruhezustand synchronisieren, damit die Rotation auf anderen Clients korrekt ist
            MovingServerRPC(transform.position, transform.rotation);
        }

        // === SCORE-AKKUMULATION ===
        if ((scoreAccumulator += Time.deltaTime) >= 1.0f)
        {
            score.Value += 1;
            scoreAccumulator -= 1.0f;
        }
    }

    // --- RPCs für das Spawnen von Objekten ---
    // Diese Methoden werden von den UI-Buttons im PlayerUIManagerr aufgerufen.
    [ServerRpc]
    public void SpawnPillarServerRpc()
    {
        if (score.Value >= pillerscore)
        {
            // Instanziiere den Pillar an der aktuellen Position und Rotation des Spielers
            GameObject obj = Instantiate(piller, transform.position, transform.rotation);
            NetworkObject networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn(true); // Spawne das Objekt im Netzwerk

            // Hole die PillarScoreBonus Komponente und setze die spawnerPlayerId
            PillarScoreBonus pillarBonus = obj.GetComponent<PillarScoreBonus>();
            if (pillarBonus != null)
            {
                pillarBonus.spawnerPlayerId.Value = OwnerClientId; // Setze die NetworkObjectId des spawnernden Spielers
            }
            else
            {
                Debug.LogWarning("Spawned Pillar does not have a PillarScoreBonus component!");
            }

            score.Value -= pillerscore; // Kosten abziehen
        }
        else
        {
            Debug.LogWarning($"Nicht genug Score, um Pillar zu spawnen. Benötigt: {pillerscore}, Hat: {score.Value}");
        }
    }

    [ServerRpc]
    public void SpawnBallServerRpc()
    {
        if (ball == null)
        {
            Debug.LogError("Ball prefab is not assigned in NetPlayer!");
            return;
        }

        if (score.Value >= ballscore)
        {
            // Instanziiere den Ball an der aktuellen Position und Rotation des Spielers
            GameObject obj = Instantiate(ball, transform.position, transform.rotation);
            NetworkObject networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn(true); // Spawne das Objekt im Netzwerk
            score.Value -= ballscore; // Kosten abziehen
        }
        else
        {
            Debug.LogWarning($"Nicht genug Score, um Ball zu spawnen. Benötigt: {ballscore}, Hat: {score.Value}");
        }
    }
    // --- Ende RPCs für das Spawnen von Objekten ---

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
        if (IsOwner) return; // Der Besitzer hat sich bereits lokal bewegt, kein Server-Update nötig
        transform.position = position;
        transform.rotation = rotation;
    }

    // RotateServerRpc wird weiterhin verwendet, um die Spielerrotation (ausgelöst durch die Kameraausrichtung) zu synchronisieren
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