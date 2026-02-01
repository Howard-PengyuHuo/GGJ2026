using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DraggableFalling2D : MonoBehaviour
{
    [Header("Reference")]
    public DragBounds2D bounds; // 可以不填，自动 FindObjectOfType

    [Header("Gravity")]
    public float gravity = 20f;
    public float maxFallSpeed = 15f;

    [Header("Drag")]
    public float followSpeed = 30f;

    private Camera cam;
    private Collider2D _col;

    private bool isDragging;
    private Vector3 grabOffset;
    private float fallSpeed;

    private SpriteRenderer sr;

    private void Awake()
    {
        cam = Camera.main;
        //if (cam == null)
        //    cam = FindObjectOfType<Camera>();

        sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();

        //if (bounds == null)
        //    bounds = FindObjectOfType<DragBounds2D>();
    }

    private void Update()
    {
        if (bounds == null || cam == null) return;

        // --- mouse down: start drag if hit THIS object ---
        if (Input.GetMouseButtonDown(0))
        {
            if (HitThisObject())
            {
                isDragging = true;
                fallSpeed = 0f;

                Vector3 mouseWorld = GetMouseWorld();
                grabOffset = transform.position - mouseWorld;
            }
        }

        // --- mouse up: stop drag ---
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        Vector2 extents = GetExtents2D();

        if (!isDragging)
        {
            fallSpeed += gravity * Time.deltaTime;
            fallSpeed = Mathf.Min(fallSpeed, maxFallSpeed);

            Vector3 desired = transform.position;
            desired.y -= fallSpeed * Time.deltaTime;

            Vector3 clamped = bounds.ClampPosition(desired, extents);

            // 被地面顶回去 -> 清零速度
            if (clamped.y > desired.y)
                fallSpeed = 0f;

            transform.position = clamped;
        }
        else
        {
            Vector3 mouseWorld = GetMouseWorld();
            Vector3 target = mouseWorld + grabOffset;

            Vector3 desired = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);
            transform.position = bounds.ClampPosition(desired, extents);
        }
    }

    private bool HitThisObject()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

        return hit.collider != null && hit.collider == _col;
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 m = Input.mousePosition;
        float z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        m.z = z;
        return cam.ScreenToWorldPoint(m);
    }

    private Vector2 GetExtents2D()
    {
        if (sr != null && sr.sprite != null)
        {
            var b = sr.bounds; // world space
            return new Vector2(b.extents.x, b.extents.y);
        }

        var cb = _col.bounds;
        return new Vector2(cb.extents.x, cb.extents.y);
    }
}