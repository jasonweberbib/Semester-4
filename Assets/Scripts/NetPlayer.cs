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
    // public float rotationSpeed = 360f; // Nicht mehr direkt für Spielerrotation verwendet
    public float mouseSensitivity = 2f;

    private float mouseX; // Für die horizontale Mausbewegung (Yaw) - wird jetzt auf cameraFollowTarget angewendet
    private float mouseY; // Für die vertikale Mausbewegung (Pitch) - wird auf cameraFollowTarget angewendet

    private bool isSetupComplete = false; // Flag, um zu verhindern, dass das Setup mehrfach läuft
    
    void Awake()
    {
        // Debug.Log("[NetPlayer] Awake called.");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkSpawn. IsOwner: {IsOwner}, IsHost: {IsHost}, Current Scene: {SceneManager.GetActiveScene().name}.");

        if (IsOwner)
        {
            Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkSpawn: Dies ist der lokale Spieler.");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
                Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkSpawn: OnLoadComplete Listener registriert.");
            }
            else
            {
                Debug.LogError($"[NetPlayer:{OwnerClientId}] OnNetworkSpawn: NetworkManager oder SceneManager ist null. Kann OnLoadComplete nicht registrieren.");
            }

            if (SceneManager.GetActiveScene().name == "Level" && !isSetupComplete) 
            {
                Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkSpawn: Bereits in der 'Level'-Szene. Starte Setup sofort.");
                StartCoroutine(SetupCameraAndUI());
            }

            score.OnValueChanged += UpdateScoreLocalDisplay;
            UpdateScoreLocalDisplay(0, score.Value); 
        }
        else
        {
            Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkSpawn: Dies ist ein Remote-Spieler. Kein lokales UI/Kamera-Setup.");
        }
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkDespawn.");

        if (IsOwner)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
                Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkDespawn: OnLoadComplete Listener deregistriert.");
            }

            score.OnValueChanged -= UpdateScoreLocalDisplay;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkDespawn: Cursor freigegeben.");

            if (currentUI != null)
            {
                Destroy(currentUI.gameObject);
                Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkDespawn: HUD zerstört.");
            }
            if (cameraFollowTarget != null && cameraFollowTarget.gameObject != null)
            {
                Destroy(cameraFollowTarget.gameObject);
                Debug.Log($"[NetPlayer:{OwnerClientId}] OnNetworkDespawn: CameraFollowTarget zerstört.");
            }
        }
        base.OnNetworkDespawn();
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (IsOwner && sceneName == "Level" && !isSetupComplete) 
        {
            Debug.Log($"[NetPlayer:{OwnerClientId}] OnSceneLoadComplete: Szene '{sceneName}' vollständig geladen. Starte Kamera/UI Setup.");
            StartCoroutine(SetupCameraAndUI());
        }
    }

    private IEnumerator SetupCameraAndUI()
    {
        if (isSetupComplete) 
        {
            Debug.LogWarning($"[NetPlayer:{OwnerClientId}] SetupCameraAndUI: Setup bereits abgeschlossen. Breche ab.");
            yield break;
        }

        Debug.Log($"[NetPlayer:{OwnerClientId}] Starte Einrichtungsprozess für Kamera und UI.");

        // === Setup HUD ===
        Debug.Log($"[NetPlayer:{OwnerClientId}] Starte HUD Setup.");
        if (playerUIPrefab != null)
        {
            GameObject playerUIGameObject = Instantiate(playerUIPrefab);
            currentUI = playerUIGameObject.GetComponent<PlayerUIManagerr>();

            if (currentUI != null)
            {
                currentUI.Initialize(this); 
                Debug.Log($"[NetPlayer:{OwnerClientId}] UI Manager gefunden. Initialisiere UI.");
            }
            else
            {
                Debug.LogError($"[NetPlayer:{OwnerClientId}] PlayerUIPrefab ({playerUIPrefab.name}) hat kein PlayerUIManagerr Skript! Bitte überprüfen.");
            }
        }
        else
        {
            Debug.LogError($"[NetPlayer:{OwnerClientId}] PlayerUIPrefab nicht zugewiesen! Bitte im Inspector zuweisen.");
        }

        // === Setup Camera ===
        Debug.Log($"[NetPlayer:{OwnerClientId}] Starte Kamera Setup.");
        Camera mainCam = null;
        int attempts = 0;
        int maxAttempts = 20; 
        float delayBetweenAttempts = 0.1f; 

        while (mainCam == null && attempts < maxAttempts)
        {
            mainCam = Camera.main; 
            if (mainCam == null)
            {
                yield return new WaitForSeconds(delayBetweenAttempts);
                attempts++;
            }
        }

        if (mainCam == null)
        {
            Debug.LogError($"[NetPlayer:{OwnerClientId}] Main Camera konnte nach {maxAttempts} Versuchen nicht gefunden werden. Kamera wird nicht eingerichtet. Stelle sicher, dass eine Kamera mit dem Tag 'MainCamera' in der Szene ist.");
            isSetupComplete = true; 
            yield break; 
        }

        Debug.Log($"[NetPlayer:{OwnerClientId}] Main Camera gefunden.");
        
        // Instanziiere cameraFollowTarget und mache es zum Kind des NetPlayer-Objekts.
        if (cameraFollowTargetPrefab != null)
        {
            // IMPORTANT: Instantiate WITHOUT `transform` as parent.
            // The cameraFollowTarget will be a separate object in the scene.
            // Its position will be explicitly set to follow the player's position.
            cameraFollowTarget = Instantiate(cameraFollowTargetPrefab).transform; 
            cameraFollowTarget.name = $"PlayerCamTarget_{OwnerClientId}"; 
            
            // Setze die initiale Position des cameraFollowTarget auf die des Spielers plus Offset
            cameraFollowTarget.position = transform.position + Vector3.up * 1.5f; 
            cameraFollowTarget.rotation = Quaternion.identity; 
            
            Debug.Log($"[NetPlayer:{OwnerClientId}] CameraFollowTarget instanziiert: {cameraFollowTarget.name}");
        }
        else
        {
            Debug.LogError($"[NetPlayer:{OwnerClientId}] cameraFollowTargetPrefab ist NICHT zugewiesen! Bitte im Inspector zuweisen.");
            isSetupComplete = true; 
            yield break; 
        }

        CameraFollow followScript = mainCam.GetComponent<CameraFollow>();
        if (followScript == null)
        {
            Debug.LogError($"[NetPlayer:{OwnerClientId}] Main Camera hat kein CameraFollow Skript! Füge es hinzu oder stelle sicher, dass CameraSetupHelper es hinzufügt.");
            followScript = mainCam.gameObject.AddComponent<CameraFollow>();
        }

        followScript.SetTarget(cameraFollowTarget);
        Debug.Log($"[NetPlayer:{OwnerClientId}] CameraFollow Script Target gesetzt zu {cameraFollowTarget.name}.");

        // Initialisiere Mausposition basierend auf der initialen Blickrichtung des cameraFollowTarget
        // Dies verhindert Sprünge beim Start
        mouseX = cameraFollowTarget.eulerAngles.y; 
        mouseY = cameraFollowTarget.localEulerAngles.x; // Beachte localEulerAngles für Pitch

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log($"[NetPlayer:{OwnerClientId}] Kamera-Setup abgeschlossen. Cursor freigegeben für UI-Interaktion.");
        
        isSetupComplete = true; 
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned || !isSetupComplete || cameraFollowTarget == null) return;

        // === MAUS-BLICK (Kamera-Rotation, nur wenn rechte Maustaste gedrückt) ===
        if (Input.GetMouseButton(1)) 
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseDeltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseDeltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // AKKUMULIERE die Mausbewegung auf mouseX und mouseY
            mouseX += mouseDeltaX;
            mouseY -= mouseDeltaY; // Maus Y invertiert
            mouseY = Mathf.Clamp(mouseY, -45f, 60f); // Vertikale Kamerabegrenzung

            // IMPORTANT: Der NetPlayer (PlayerRoot) selbst rotiert NICHT mehr durch die Maus.
            // Nur das cameraFollowTarget rotiert horizontal (Yaw) und vertikal (Pitch).
            cameraFollowTarget.rotation = Quaternion.Euler(mouseY, mouseX, 0); 
        }
        else 
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // === Spieler-Position immer an cameraFollowTarget anpassen ===
        // Das cameraFollowTarget sollte immer auf der Position des Spielers sein.
        // Das ist der zentrale Punkt für die Kamera.
        cameraFollowTarget.position = transform.position + Vector3.up * 1.5f; // Offset nach oben

        // === BEWEGUNGSINPUT (Relativ zur KAMERABlickrichtung) ===
        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Bewegungsrichtung relativ zur HORIZONTALEN Richtung des cameraFollowTarget (Kamera) berechnen
            // Wir ignorieren hier die Pitch-Rotation des cameraFollowTarget für die Bewegung.
            Vector3 camForward = cameraFollowTarget.forward;
            Vector3 camRight = cameraFollowTarget.right;

            camForward.y = 0; // Vertikale Komponente entfernen für Bewegung auf der XZ-Ebene
            camRight.y = 0;   // Vertikale Komponente entfernen
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;
            moveDir.Normalize();

            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        
        // RPC wird IMMER gesendet, um Position und Rotation des Players zu synchronisieren.
        // Wir senden die aktuelle Position des Players und die Rotation des cameraFollowTargets.
        // Die Rotation des Players selbst bleibt unberührt.
        MovingServerRPC(transform.position, cameraFollowTarget.rotation);
    }

    // NetPlayer.cs
    private void UpdateScoreLocalDisplay(int oldScore, int newScore)
    {
        if (currentUI != null && currentUI.scoreText != null)
        {
            currentUI.scoreText.text = "Score: " + newScore;
        }
    }

    // --- RPCs für das Spawnen von Objekten ---
    [ServerRpc]
    public void SpawnPillarServerRpc()
    {
        if (piller == null)
        {
            Debug.LogError("Pillar prefab is not assigned in NetPlayer!");
            return;
        }
        if (score.Value >= pillerscore)
        {
            // Instanziiere Pillar in Blickrichtung des Spielers (der sich hier nicht dreht, also feste Richtung oder default forward)
            // Oder wir können die Rotation des cameraFollowTarget nehmen, um die Richtung des Spawnens zu bestimmen.
            // Ich nehme die Rotation des Players, da die Spawn-Position relativ zur Player-Position ist.
            GameObject obj = Instantiate(piller, transform.position + transform.forward * 2f + transform.up, Quaternion.identity); // Player-Rotation (initial 0,0,0)
            NetworkObject networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn(true);

            PillarScoreBonus pillarBonus = obj.GetComponent<PillarScoreBonus>();
            if (pillarBonus != null)
            {
                pillarBonus.spawnerPlayerId.Value = OwnerClientId;
            }
            else
            {
                Debug.LogWarning("Spawned Pillar does not have a PillarScoreBonus component! Make sure it's on the Pillar prefab.");
            }

            score.Value -= pillerscore;
        }
        else
        {
            Debug.LogWarning($"[NetPlayer:{OwnerClientId}] Nicht genug Score, um Pillar zu spawnen. Benötigt: {pillerscore}, Hat: {score.Value}");
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
            // Ball wird in die Blickrichtung der Kamera gespawnt.
            // Nutze die Rotation des cameraFollowTarget
            GameObject obj = Instantiate(ball, transform.position + cameraFollowTarget.forward * 2f + transform.up, cameraFollowTarget.rotation);
            NetworkObject networkObj = obj.GetComponent<NetworkObject>();
            networkObj.Spawn(true);
            score.Value -= ballscore;
        }
        else
        {
            Debug.LogWarning($"[NetPlayer:{OwnerClientId}] Nicht genug Score, um Ball zu spawnen. Benötigt: {ballscore}, Hat: {score.Value}");
        }
    }
    // --- Ende RPCs für das Spawnen von Objekten ---

    [ServerRpc]
    void MovingServerRPC(Vector3 position, Quaternion cameraTargetRotation)
    {
        // Wenn dieses RPC vom Host-Spieler (IsOwner=true) gesendet wird, muss der Server
        // seine eigenen lokalen Werte nicht überschreiben, da sie bereits aktuell sind.
        if (!IsOwner) 
        {
            transform.position = position;
        }
        // WICHTIG: Die Rotation des Players selbst wird NICHT gesetzt.
        // Nur die Rotation des cameraFollowTarget (für Remote-Clients) muss synchronisiert werden.
        // Da das cameraFollowTarget auf Remote-Clients nicht existiert, müssen wir diese
        // Rotation an die Remote-Players senden, damit deren Kameras korrekt folgen können.
        // Dies erfordert eine separate RPC für die Kamera-Rotation.
        
        MovingClientRPC(position, cameraTargetRotation);
    }

    [ClientRpc]
    void MovingClientRPC(Vector3 position, Quaternion cameraTargetRotation)
    {
        if (IsOwner) return; // Der Besitzer hat sich bereits lokal bewegt, kein Server-Update nötig
        
        transform.position = position;
        
        // Remote-Clients müssen die Rotation der Kamera des Besitzers erhalten, damit ihre Kameras
        // (die ja dem cameraFollowTarget des Besitzers folgen würden, wenn es existierte) korrekt ausgerichtet sind.
        // Da das cameraFollowTarget nur auf dem Besitzer-Client existiert, müssen wir hier
        // die Rotation des PlayerRoot für Remote-Clients auf die Rotation der "Kamera" setzen,
        // damit die Remote-Kamera (falls sie eine feste Kamera ist oder selbst einem anderen Target folgt)
        // zumindest die Blickrichtung des Remote-Players repräsentiert.
        // ABER: Wenn es eine 3rd-Person-Ansicht ist, ist die Rotation des Players für Remote-Clients
        // weniger wichtig als die Rotation der Kamera.

        // Da der NetPlayer selbst nicht rotiert, und die Kamera einem unsichtbaren Target folgt,
        // können wir hier nur die Position synchronisieren. Die Rotation des remotePlayers wird nicht von der Maus gesteuert.
        // Stattdessen brauchen wir eine separate RPC für die Kamera-Rotation, die dann auf dem Remote-Client
        // die Rotation des cameraFollowTarget simuliert oder direkt an die Kamera weitergibt.

        // Für jetzt lassen wir die Rotation des Remote-Spielers unangetastet, da sie nicht von der Maus gesteuert wird.
        // Das PlayerRoot bleibt also auf dem Remote-Client in seiner Standardausrichtung.
        // Die Remote-Kamera (falls sie existiert und dem Remote-Player folgen soll) würde dann diese fehlende Rotation bemerken.
        // Let's reintroduce RotateClientRpc for remote cameras to know the owner's camera direction.
    }


    // Da der Spieler selbst nicht rotiert, aber die Kamera-Richtung trotzdem synchronisiert werden muss,
    // um die Blickrichtung des Remote-Spielers darzustellen (z.B. für Spawns oder visuelle Cues),
    // verwenden wir die RotateServerRpc/ClientRpc wieder, aber ausschließlich für die KAmera-Richtung.
    [ServerRpc]
    void SyncCameraRotationServerRpc(Quaternion cameraRotation)
    {
        SyncCameraRotationClientRpc(cameraRotation);
    }

    [ClientRpc]
    void SyncCameraRotationClientRpc(Quaternion cameraRotation)
    {
        if (IsOwner) return; // Besitzer hat diese Information bereits lokal.

        // Auf Remote-Clients: Hier können wir das PlayerRoot-Objekt so drehen,
        // dass es die horizontale Blickrichtung der Kamera des Besitzers widerspiegelt.
        // Das eigentliche PlayerRoot-Objekt (transform) dreht sich dann, aber nur um die Y-Achse
        // und nur auf Remote-Clients, um die Ausrichtung des Besitzers zu zeigen.
        // Die x- und z-Komponente der Rotation (Pitch und Roll) werden ignoriert, da der Player nur Yaw hat.
        transform.rotation = Quaternion.Euler(0, cameraRotation.eulerAngles.y, 0);
    }
}