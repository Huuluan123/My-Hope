using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour, IRoomElement
{
    [Header("Settings")]
    public float speed = 2f;
    public Transform[] waypoints;
    public float waitTimeAtWaypoint = 0.5f;

    private int currentWaypointIndex = 0;
    private float waitTimer;
    private Rigidbody2D rb;

    // Biến ghi nhớ trí ban đầu
    private Vector2 initialPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ép bệ đỡ thành Kinematic để không bị rớt do trọng lực
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Lưu lại vị trí khi game vừa bắt đầu
        initialPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        if (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Transform target = waypoints[currentWaypointIndex];

        // Tính toán di chuyển và ép Rigidbody đi theo
        Vector2 newPos = Vector2.MoveTowards(rb.position, target.position, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // Kiểm tra xem đã đến đích chưa
        if (Vector2.Distance(rb.position, target.position) < 0.01f)
        {
            waitTimer = waitTimeAtWaypoint;
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length) currentWaypointIndex = 0;
        }
    }

    public void ResetState()
    {
        // Trả bệ đỡ về đúng vị trí xuất phát
        transform.position = initialPosition;
        rb.position = initialPosition;

        // Reset lại trí nhớ về điểm đến
        currentWaypointIndex = 0;
        waitTimer = 0;
    }
}