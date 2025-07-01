using UnityEngine;
using UnityEngine.SceneManagement; // Für Szenenwechsel
using Unity.Netcode; // Für Netzwerkstartmethoden (Host/Client)
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Game Scene Name")]
    [Tooltip("Der Name der Level-Szene, zu der gewechselt werden soll.")]
    public string gameSceneName = "SampleScene"; // Stelle sicher, dass dies der genaue Name deiner Level-Szene ist

    [Header("UI Elements")]
    [Tooltip("Optional: Buttons um sie während des Verbindungsprozesses zu deaktivieren")]
    public GameObject hostButton;
    public GameObject joinButton;
    public GameObject loadingText; // Optional: Ladetext anzeigen

    private bool isConnecting = false;

    // Methode für den "Host Game" Button
    public void OnClickHostGame()
    {
        if (isConnecting) return; // Verhindere mehrfache Klicks
        
        Debug.Log("Hosting Game...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton ist null! Bitte stelle sicher, dass ein NetworkManager in der Szene ist und DontDestroyOnLoad gesetzt ist.");
            return;
        }

        isConnecting = true;
        SetButtonsInteractable(false);
        ShowLoadingText("Starting Host...");

        // Einfacher Ansatz: Direkt hosten und dann Szene laden
        bool success = NetworkManager.Singleton.StartHost();
        
        if (success)
        {
            // Kurz warten und dann Szene laden
            StartCoroutine(LoadSceneAfterDelay());
        }
        else
        {
            Debug.LogError("Failed to start host!");
            OnConnectionFailed();
        }
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Kurz warten bis Host bereit ist
        
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host ready, loading game scene...");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Host not ready after delay!");
            OnConnectionFailed();
        }
    }



    // Methode für den "Join Game" Button
    public void OnClickJoinGame()
    {
        if (isConnecting) return; // Verhindere mehrfache Klicks
        
        Debug.Log("Joining Game...");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton ist null!");
            return;
        }

        isConnecting = true;
        SetButtonsInteractable(false);
        ShowLoadingText("Connecting to Host...");

        // Einfacher Client-Start
        bool success = NetworkManager.Singleton.StartClient();
        
        if (!success)
        {
            Debug.LogError("Failed to start client!");
            OnConnectionFailed();
        }
        else
        {
            // Timeout falls Verbindung fehlschlägt
            StartCoroutine(ConnectionTimeout(10f));
        }
    }

    // Timeout für Client-Verbindung
    private IEnumerator ConnectionTimeout(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        
        // Prüfen ob wir immer noch versuchen zu verbinden
        if (isConnecting && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient))
        {
            Debug.LogWarning("Connection timeout!");
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            OnConnectionFailed();
        }
    }

    // Methode die aufgerufen wird wenn eine Verbindung fehlschlägt
    private void OnConnectionFailed()
    {
        Debug.LogWarning("Connection failed or timed out.");
        
        isConnecting = false;
        SetButtonsInteractable(true);
        HideLoadingText();
    }

    // Hilfsmethoden für UI-Updates
    private void SetButtonsInteractable(bool interactable)
    {
        if (hostButton != null)
        {
            var button = hostButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.interactable = interactable;
        }
        if (joinButton != null)
        {
            var button = joinButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.interactable = interactable;
        }
    }

    private void ShowLoadingText(string message)
    {
        if (loadingText != null)
        {
            loadingText.SetActive(true);
            
            // Versuche zuerst UI.Text
            var uiText = loadingText.GetComponent<UnityEngine.UI.Text>();
            if (uiText != null)
            {
                uiText.text = message;
                return;
            }
            
            // Falls das nicht funktioniert, versuche TextMeshPro
            var tmpText = loadingText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = message;
            }
        }
    }

    private void HideLoadingText()
    {
        if (loadingText != null)
            loadingText.SetActive(false);
    }

    // Methode für den "Quit Game" Button
    public void OnClickQuitGame()
    {
        Debug.Log("Quitting Game...");
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // Beendet den Play Mode im Editor
        #else
                Application.Quit(); // Beendet die Anwendung im Build
        #endif
    }

    // Methode für den "Settings" Button (noch nicht implementiert)
    public void OnClickSettings()
    {
        Debug.Log("Settings button clicked - Not yet implemented.");
    }

    // Cleanup beim Zerstören des Objects
    void OnDestroy()
    {
        // Sicherheitshalber alle Coroutines stoppen
        StopAllCoroutines();
    }
}