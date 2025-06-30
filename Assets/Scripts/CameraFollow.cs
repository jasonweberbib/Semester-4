using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target; 
    private Vector3 offset = new Vector3(0, 2f, -5f); 
    private float pitch = 0.5f; // <--- Changed this value. Experiment with 0f to 2f.

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + target.rotation * offset; 
        transform.position = desiredPos;

        transform.LookAt(target.position + Vector3.up * pitch); 
    }
}