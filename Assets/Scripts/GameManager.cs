using UnityEngine;
using Unity.Netcode; // Import behalten, auch wenn nicht direkt genutzt

public class GameManager : MonoBehaviour
{
    // Dieser GameManager ist für die Verwaltung des Spielzustands nach dem Netzwerkstart zuständig.
    // Die Netzwerk-Startlogik (Host/Client starten) wird vom MainMenuManager gehandhabt.

    void Start()
    {
        // Diese Methode bleibt hier leer oder wird für spielspezifische Initialisierungen genutzt,
        // die NICHT den NetworkManager starten.
        Debug.Log("GameManager Start Methode wurde aufgerufen.");
    }

    void Update()
    {
        // Diese Methode bleibt hier leer oder wird für fortlaufende Spiel-Logik genutzt.
    }
}