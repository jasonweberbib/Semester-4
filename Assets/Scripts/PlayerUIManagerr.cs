using UnityEngine;
using TMPro; // Wichtig: Für TextMeshPro
using Unity.Netcode; // Für NetworkVariable Listener

public class PlayerUIManagerr : MonoBehaviour
{
    public TextMeshProUGUI scoreText; // Referenz auf dein TextMeshPro-Objekt für den Score
    public GameObject spawnPillarButton; // Referenz auf deinen Pillar Spawn Button
    public GameObject spawnBallButton;   // Referenz auf deinen Ball Spawn Button

    private NetPlayer linkedPlayer; // Referenz auf den NetPlayer, dessen UI das ist

    // Diese Methode wird vom NetPlayer aufgerufen, um die UI zu initialisieren
    public void Initialize(NetPlayer player)
    {
        linkedPlayer = player;
        Debug.Log($"PlayerUIManagerr Initialize für Spieler {linkedPlayer.OwnerClientId}: Wird aufgerufen. IsOwner: {linkedPlayer.IsOwner}"); // Hinzufügen


        // Sicherstellen, dass das UI Canvas nur für den lokalen Spieler aktiv ist
        if (linkedPlayer.IsOwner)
        {
            gameObject.SetActive(true); // Aktiviere das gesamte UI Canvas
            Debug.Log($"PlayerUIManagerr: UI Canvas für lokalen Spieler aktiviert."); // Hinzufügen

            // Füge Listener für die Buttons hinzu
            if (spawnPillarButton != null)
            {
                spawnPillarButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnSpawnPillarButtonClick);
            }
            if (spawnBallButton != null)
            {
                spawnBallButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnSpawnBallButtonClick);
            }

            // Füge einen Listener für die NetworkVariable hinzu, um den Score zu aktualisieren
            linkedPlayer.score.OnValueChanged += UpdateScoreDisplay;
            UpdateScoreDisplay(0, linkedPlayer.score.Value); // Initialen Score sofort anzeigen
        }
        else
        {
            // UI für nicht-lokale Spieler deaktivieren
            gameObject.SetActive(false);
            Debug.Log($"PlayerUIManagerr: UI Canvas für NICHT-lokalen Spieler deaktiviert."); // Hinzufügen
        }
    }

    void OnDestroy()
    {
        // Sicherstellen, dass der Listener entfernt wird, wenn das UI-Objekt zerstört wird
        if (linkedPlayer != null)
        {
            linkedPlayer.score.OnValueChanged -= UpdateScoreDisplay;
        }

        // Button-Listener entfernen, um Memory Leaks zu vermeiden
        if (spawnPillarButton != null)
        {
            spawnPillarButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(OnSpawnPillarButtonClick);
        }
        if (spawnBallButton != null)
        {
            spawnBallButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveListener(OnSpawnBallButtonClick);
        }
    }

    // Callback-Funktion für den Score-Update
    private void UpdateScoreDisplay(int oldScore, int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + newScore;
        }
    }

    // Callback-Funktion für den Pillar Spawn Button
    private void OnSpawnPillarButtonClick()
    {
        if (linkedPlayer != null && linkedPlayer.IsOwner)
        {
            linkedPlayer.SpawnPillarServerRpc();
        }
    }

    // Callback-Funktion für den Ball Spawn Button
    private void OnSpawnBallButtonClick()
    {
        if (linkedPlayer != null && linkedPlayer.IsOwner)
        {
            linkedPlayer.SpawnBallServerRpc();
        }
    }
}