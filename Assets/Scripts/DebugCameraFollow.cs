using UnityEngine;

public class DebugCameraFollow : MonoBehaviour
{
    [Header("Tracking Settings")]
    public Transform target;

    [Header("Smoothing")]
    public float smoothSpeed = 10f;

    private Vector3 offset;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("DebugCameraFollow: No target assigned!");
            return;
        }

        // Calculate and cache the initial distance vector from the target
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate the ideal resting position based on your cached angle/offset
        Vector3 targetPosition = target.position + offset;

        // Smoothly slide into position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}