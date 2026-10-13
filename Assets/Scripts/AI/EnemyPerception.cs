
using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Vision Settings")]
    [SerializeField] private float visionRange = 10f;

    [SerializeField, Range(0f, 360f)]
    private float fieldOfView = 120f;

    [Header("Raycast Settings")]
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float targetHeight = 1.0f;
    [SerializeField] private LayerMask obstacleMask;

    public Transform Player => player;

    // Memastikan script berjalan
    private void Start()
    {
        Debug.Log("[Perception] EnemyPerception AKTIF!", this);

        if (player == null)
        {
            Debug.LogWarning(
                "[Perception] Player belum dihubungkan!",
                this
            );
        }
    }



    private void Update()
{
    bool visible = CanSeePlayer();

    Debug.DrawRay(
        transform.position + Vector3.up * eyeHeight,
        (player != null
            ? (player.position + Vector3.up * targetHeight
               - (transform.position + Vector3.up * eyeHeight)).normalized
            : transform.forward) * visionRange,
        visible ? Color.green : Color.red
    );
}

    public bool CanSeePlayer()
    {
        Debug.Log("[Perception] CanSeePlayer() dipanggil", this);

        if (player == null)
            return false;

        // 1. Posisi mata Enemy dan target Player
        Vector3 eyePosition =
            transform.position + Vector3.up * eyeHeight;

        Vector3 targetPosition =
            player.position + Vector3.up * targetHeight;

        Vector3 direction = targetPosition - eyePosition;
        float distance = direction.magnitude;

        // 2. Periksa jarak pandang
        if (distance > visionRange)
        {
            Debug.Log(
                $"[Perception] Player di luar Vision Range. " +
                $"Distance: {distance:F2}",
                this
            );

            return false;
        }

        // 3. Periksa sudut penglihatan
        float angle = Vector3.Angle(
            transform.forward,
            direction.normalized
        );

        if (angle > fieldOfView * 0.5f)
        {
            Debug.Log(
                $"[Perception] Player di luar FOV. " +
                $"Angle: {angle:F2}",
                this
            );

            return false;
        }

        // 4. Periksa obstacle menggunakan Raycast
        bool obstacleHit = Physics.Raycast(
            eyePosition,
            direction.normalized,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        Debug.Log(
            $"[Perception] Distance: {distance:F2}, " +
            $"Angle: {angle:F2}, " +
            $"Obstacle Hit: {obstacleHit}",
            this
        );

        // Player terlihat jika tidak terhalang obstacle
        return !obstacleHit;
    }

    private void OnDrawGizmosSelected()
    {
        // Vision Range
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            visionRange
        );

        // Batas kiri dan kanan FOV
        Vector3 leftBoundary = Quaternion.Euler(
            0f, -fieldOfView * 0.5f, 0f
        ) * transform.forward;

        Vector3 rightBoundary = Quaternion.Euler(
            0f, fieldOfView * 0.5f, 0f
        ) * transform.forward;

        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(
            transform.position,
            leftBoundary * visionRange
        );

        Gizmos.DrawRay(
            transform.position,
            rightBoundary * visionRange
        );

        // Garis pandang menuju Player
        if (player == null)
            return;

        Vector3 eyePosition =
            transform.position + Vector3.up * eyeHeight;

        Vector3 targetPosition =
            player.position + Vector3.up * targetHeight;

        Vector3 direction = targetPosition - eyePosition;
        float distance = direction.magnitude;

        bool visible = false;

        if (distance <= visionRange)
        {
            float angle = Vector3.Angle(
                transform.forward,
                direction.normalized
            );

            if (angle <= fieldOfView * 0.5f)
            {
                bool obstacleHit = Physics.Raycast(
                    eyePosition,
                    direction.normalized,
                    distance,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                visible = !obstacleHit;
            }
        }

        Gizmos.color = visible ? Color.green : Color.red;

        Gizmos.DrawLine(
            eyePosition,
            targetPosition
        );
    }


}
