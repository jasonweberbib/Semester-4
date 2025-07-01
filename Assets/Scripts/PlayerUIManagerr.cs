using UnityEngine;
using TMPro; // Wichtig: Für TextMeshPro
using Unity.Netcode; // Für NetworkVariable Listener (wird hier nicht mehr direkt genutzt für score, aber falls andere NetworkVariables hinzu kommen)
using UnityEngine.UI; // Für Button

public class PlayerUIManagerr : MonoBehaviour
{
    public TextMeshProUGUI scoreText; // Referenz auf dein TextMeshPro-Objekt für den Score. Dies muss im Inspector zugewiesen sein!
    public GameObject spawnPillarButton; // Referenz auf deinen Pillar Spawn Button
    public GameObject spawnBallButton;   // Referenz auf deinen Ball Spawn Button

    private NetPlayer linkedPlayer; // Referenz auf den NetPlayer, dessen UI das ist

    public void Initialize(NetPlayer player)
    {
        linkedPlayer = player;
        Debug.Log($"PlayerUIManagerr Initialize für Spieler {linkedPlayer.OwnerClientId}: Wird aufgerufen. IsOwner: {linkedPlayer.IsOwner}"); 

        if (linkedPlayer.IsOwner)
        {
            gameObject.SetActive(true);
            Debug.Log($"PlayerUIManagerr: UI Canvas für lokalen Spieler aktiviert."); 

            if (spawnPillarButton != null)
            {
                spawnPillarButton.GetComponent<Button>().onClick.AddListener(OnSpawnPillarButtonClick); 
            }
            if (spawnBallButton != null)
            {
                spawnBallButton.GetComponent<Button>().onClick.AddListener(OnSpawnBallButtonClick); 
            }
        }
        else
        {
            gameObject.SetActive(false);
            Debug.Log($"PlayerUIManagerr: UI Canvas für NICHT-lokalen Spieler deaktiviert."); 
        }
    }

    void OnDestroy()
    {
        // Diese Zeilen wurden entfernt, da der NetPlayer jetzt direkt den Score aktualisiert
        // if (linkedPlayer != null)
        // {
        //     linkedPlayer.score.OnValueChanged -= UpdateScoreDisplay; 
        // }

        if (spawnPillarButton != null)
        {
            spawnPillarButton.GetComponent<Button>().onClick.RemoveListener(OnSpawnPillarButtonClick); 
        }
        if (spawnBallButton != null)
        {
            spawnBallButton.GetComponent<Button>().onClick.RemoveListener(OnSpawnBallButtonClick); 
        }
    }

    // Diese Methode UpdateScoreDisplay ist nicht mehr notwendig und wurde entfernt,
    // da der NetPlayer direkt das 'scoreText'-Objekt hier im PlayerUIManagerr aktualisiert.
    // private void UpdateScoreDisplay(int oldScore, int newScore)
    // {
    //     if (scoreText != null)
    //     {
    //         scoreText.text = "Score: " + newScore; 
    //     }
    // }

    private void OnSpawnPillarButtonClick()
    {
        if (linkedPlayer != null && linkedPlayer.IsOwner)
        {
            linkedPlayer.SpawnPillarServerRpc(); 
        }
    }

    private void OnSpawnBallButtonClick()
    {
        if (linkedPlayer != null && linkedPlayer.IsOwner)
        {
            linkedPlayer.SpawnBallServerRpc(); 
        }
    }
}