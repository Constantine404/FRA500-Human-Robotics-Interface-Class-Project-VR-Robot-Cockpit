using UnityEngine; 

public class FollowHead : MonoBehaviour
{
    public Transform head;
    public float distance = 1.0f;
    public float heightOffset = 0.3f;

    void LateUpdate()
    {
        // ===== POSITION =====
        Vector3 flatForward = head.forward;
        flatForward.y = 0;
        flatForward.z = 0;
        flatForward.Normalize();

        Vector3 targetPos = head.position
                          + flatForward * distance
                          + Vector3.up * heightOffset;

        transform.position = targetPos;
    }
}