using UnityEngine;

public class ArcballRotate : MonoBehaviour
{
    public Transform brainRoot;

    [Header("Rotate")]
    public float rotateSpeed = 0.25f;
    public bool requireAlt = false;

    [Header("Reset")]
    public KeyCode resetKey = KeyCode.R;         // 按 R 归位
    public bool smoothReset = false;             // 是否平滑归位
    public float resetLerpSpeed = 12f;           // 平滑速度（越大越快）

    private Vector3 _lastMouse;
    private bool _dragging;

    private Quaternion _initialRotation;         // ⭐ 开始时记录
    private bool _resetting;

    public static bool IsDraggingRotate { get; private set; }

    void Awake()
    {
        if (brainRoot != null)
            _initialRotation = brainRoot.rotation;
    }

    void Start()
    {
        // 保险：如果你是运行时才赋值 brainRoot
        if (brainRoot != null && _initialRotation == default)
            _initialRotation = brainRoot.rotation;
    }

    void Update()
    {
        if (brainRoot == null) return;

        // --- Reset hotkey ---
        if (Input.GetKeyDown(resetKey))
        {
            if (!smoothReset)
            {
                brainRoot.rotation = _initialRotation;
                _resetting = false;
            }
            else
            {
                _resetting = true;
            }
        }

        // 平滑归位（优先级高于拖拽）
        if (_resetting)
        {
            brainRoot.rotation = Quaternion.Slerp(
                brainRoot.rotation,
                _initialRotation,
                1f - Mathf.Exp(-resetLerpSpeed * Time.deltaTime)
            );

            // 足够接近就停止
            if (Quaternion.Angle(brainRoot.rotation, _initialRotation) < 0.1f)
            {
                brainRoot.rotation = _initialRotation;
                _resetting = false;
            }
            return;
        }

        bool altOk = !requireAlt || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        // --- Begin drag ---
        if (Input.GetMouseButtonDown(2) && altOk)
        {
            _dragging = true;
            IsDraggingRotate = true;
            _lastMouse = Input.mousePosition;
        }

        // --- End drag ---
        if (Input.GetMouseButtonUp(2))
        {
            _dragging = false;
            IsDraggingRotate = false;
        }

        if (!_dragging) return;

        Vector3 mouse = Input.mousePosition;
        Vector3 delta = _lastMouse - mouse;
        _lastMouse = mouse;

        Camera cam = Camera.main;
        if (cam == null) return;

        float yaw = delta.x * rotateSpeed;
        float pitch = -delta.y * rotateSpeed;

        // 只旋转 brainRoot（不动 camera）
        brainRoot.Rotate(Vector3.up, yaw, Space.World);
        brainRoot.Rotate(cam.transform.right, pitch, Space.World);
    }

    // 如果你希望“外部”也能触发归位，比如 UI 按钮
    public void ResetNow()
    {
        if (brainRoot == null) return;
        brainRoot.rotation = _initialRotation;
        _resetting = false;
    }

    public void ResetSmooth()
    {
        if (brainRoot == null) return;
        _resetting = true;
    }
}
