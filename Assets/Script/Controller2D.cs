using UnityEngine;

[RequireComponent (typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour
{
    [SerializeField] LayerMask collisionMask;

    const float skinWidth = 0.015f;
    [SerializeField] int horizontalRayCount = 4;
    [SerializeField] int verticalRayCount = 4;

    float horizontalRaySpacing;
    float verticalRaySpacing;

    BoxCollider2D col;
    RayCastOrigins rayCastOrigins;
    Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        CalculateRaySpacing();
    }

    public void Move(Vector3 velocity)
    {
        UpdateRayCastOrigin();
        VerticalCollision(ref velocity);
        rb.MovePosition(velocity);
    }

    void VerticalCollision(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayCastOrigins.bottomLeft + Vector2.right * verticalRaySpacing * i, Vector2.up * -2, Color.red);

            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;
            }
        }
    }

    void UpdateRayCastOrigin()
    {
        Bounds bounds = col.bounds;
        rayCastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        rayCastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
        rayCastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        rayCastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
    }

    void CalculateRaySpacing()
    {
        Bounds bounds = col.bounds;
        bounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    struct RayCastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
}
