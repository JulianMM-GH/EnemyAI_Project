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

        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}