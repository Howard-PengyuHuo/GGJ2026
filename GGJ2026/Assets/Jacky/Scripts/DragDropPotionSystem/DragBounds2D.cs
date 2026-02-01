using UnityEngine;

public class DragBounds2D : MonoBehaviour
{
    [Header("Bounds Source")]
    public BoxCollider2D boundsCollider;   // 用它定义矩形区域（world space）
    public Vector2 minBounds = new Vector2(-3f, -3f);
    public Vector2 maxBounds = new Vector2(3f, 3f);
    public bool useColliderBounds = true;

    [Header("Ground / Table")]
    public float groundY = -2f;

    [Header("Padding (keep sprite inside)")]
    public float padding = 0f; // 给边界留一点余量

    public Bounds GetWorldBounds()
    {
        if (useColliderBounds && boundsCollider != null)
            return boundsCollider.bounds;

        // 手写 bounds
        var b = new Bounds();
        b.SetMinMax(new Vector3(minBounds.x, minBounds.y, 0f), new Vector3(maxBounds.x, maxBounds.y, 0f));
        return b;
    }

    /// <summary>
    /// 把物体位置限制在区域内，同时应用桌面最低Y。
    /// extents：物体半宽半高（用于“边缘不越界”）
    /// </summary>
    public Vector3 ClampPosition(Vector3 desiredWorldPos, Vector2 extents)
    {
        Bounds b = GetWorldBounds();

        float minX = b.min.x + extents.x + padding;
        float maxX = b.max.x - extents.x - padding;

        float minY = b.min.y + extents.y + padding;
        float maxY = b.max.y - extents.y - padding;

        desiredWorldPos.x = Mathf.Clamp(desiredWorldPos.x, minX, maxX);
        desiredWorldPos.y = Mathf.Clamp(desiredWorldPos.y, minY, maxY);

        // 桌面限制（同样考虑 extents，确保底边不穿桌子）
        float minAllowedYByGround = groundY + extents.y;
        if (desiredWorldPos.y < minAllowedYByGround)
            desiredWorldPos.y = minAllowedYByGround;

        return desiredWorldPos;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Bounds b = GetWorldBounds();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.center, b.size);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(b.min.x, groundY, 0f), new Vector3(b.max.x, groundY, 0f));
    }
#endif
}
