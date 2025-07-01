using UnityEngine;

/// <summary>
/// Hilfs-Skript um sicherzustellen, dass die Main Camera korrekt eingerichtet ist.
/// Dieses Skript sollte auf der Main Camera in der Spielszene platziert werden.
/// </summary>
public class CameraSetupHelper : MonoBehaviour
{
    void Awake()
    {
        // Stelle sicher, dass diese Kamera den MainCamera Tag hat
        if (!gameObject.CompareTag("MainCamera"))
        {
            Debug.LogWarning($"Camera '{gameObject.name}' does not have 'MainCamera' tag. Adding it now.");
            gameObject.tag = "MainCamera";
        }

        // Stelle sicher, dass das CameraFollow Skript vorhanden ist
        CameraFollow followScript = GetComponent<CameraFollow>();
        if (followScript == null)
        {
            Debug.LogWarning($"Camera '{gameObject.name}' does not have CameraFollow script. Adding it now.");
            gameObject.AddComponent<CameraFollow>();
        }

        Debug.Log($"Camera '{gameObject.name}' is properly set up with MainCamera tag and CameraFollow script.");
    }
}