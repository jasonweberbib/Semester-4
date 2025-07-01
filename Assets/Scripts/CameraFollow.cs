// BEGINN DES KORREKTEN CAMERAFOLLOW.CS INHALTS
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target; // Das Transform, dem die Kamera folgen soll (cameraFollowTarget von NetPlayer)

    // Setze diese Werte im Inspector des CameraFollow-Skripts auf deiner Main Camera
    // um den gewünschten Abstand und Winkel und Winkel zur Kamera zu erhalten.
    public Vector3 localOffset = new Vector3(0f, 2.5f, -6f); // X, Y (Höhe), Z (Tiefe hinter dem Target)
    public float lookAtHeightOffset = 1.0f; // Wie hoch über dem Target die Kamera \"schaut\"

    /// <summary>
    /// Setzt das Ziel, dem die Kamera folgen soll.
    /// </summary>
    /// <param name=\"followTarget\">Das Transform, das die Kamera verfolgen soll.</param>
    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
        // Beim Setzen des Targets sofort die Kamera positionieren, um einen Ruck zu vermeiden
        if (target != null)
        {
            transform.position = target.position + target.TransformDirection(localOffset);
            transform.LookAt(target.position + Vector3.up * lookAtHeightOffset);
        }
    }

    /// <summary>
    /// Wird nach allen Update-Methoden aufgerufen, um die Kamera-Positionierung zu aktualisieren.
    /// </summary>
    void LateUpdate()
    {
        if (target == null) return; // Wenn kein Target gesetzt ist, nichts tun

        // Die Kamera übernimmt die Rotation des Targets (die vom NetPlayer durch Maus gesteuert wird)
        transform.rotation = target.rotation;

        // Dann wird der lokale Offset angewendet, um die Kamera hinter und über dem Target zu platzieren
        transform.position = target.position + target.TransformDirection(localOffset);

        // Optional: Die Kamera explizit auf einen Punkt am Target schauen lassen.
        // Das ist nützlich, wenn du willst, dass die Kamera immer auf den Spieler fokussiert ist.
        transform.LookAt(target.position + Vector3.up * lookAtHeightOffset);
    }
}
// ENDE DES KORREKTEN CAMERAFOLLOW.CS INHALTS