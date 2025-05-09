using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset; // You can adjust this in the Inspector

    private void LateUpdate()
    {
        if (target == null)
        {
            // Try to find player if not set (e.g. after scene load)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            } else {
                Debug.LogWarning("CameraFollow: Target not set and Player not found.");
                return;
            }
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z); // Keep original Z
    }
}