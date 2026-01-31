using UnityEngine;
using System;



public class NodeVisualController : MonoBehaviour
{
    [SerializeField] private NodeColorDatabase nodeColorDatabase;

    // Shader property IDs（按你的 Shader Graph Reference 改）
    static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
    static readonly int PulseAmpId = Shader.PropertyToID("_MapStrength");
    static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");

    public StateStyle[] styles;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private VisualState _currentState;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    public void SetState(VisualState state)
    {
        _currentState = state;

        if (_renderer == null) return;

        // 1) 找到对应样式
        if (!TryGetStyle(state, out var style))
        {
            // 找不到就回退 default
            if (!TryGetStyle(VisualState.Default, out style))
                return;
        }

        // 2) 需要换材质就换（注意用 sharedMaterial，避免实例化）
        if (style.material != null)
        {
            if (_renderer.sharedMaterial != style.material)
                _renderer.sharedMaterial = style.material;
        }

        // 3) 用 MPB 覆盖参数（同材质/不同材质都可以，只要属性存在）
        _renderer.GetPropertyBlock(_mpb);

        // 下面这些 SetXXX：只有当 shader 有这个属性时才有效（没有就忽略）
        _mpb.SetColor(EmissionColorId, style.emissionColor);
        _mpb.SetFloat(GlowStrengthId, style.glowStrength);
        _mpb.SetFloat(PulseSpeedId, style.pulseSpeed);
        _mpb.SetFloat(PulseAmpId, style.pulseAmp);
        _mpb.SetFloat(FresnelPowerId, style.fresnelPower);

        _renderer.SetPropertyBlock(_mpb);
    }

    private bool TryGetStyle(VisualState state, out StateStyle style)
    {
        for (int i = 0; i < styles.Length; i++)
        {
            if (styles[i].state == state)
            {
                style = styles[i];
                return true;
            }
        }
        style = default;
        return false;
    }
}

[Serializable]
public enum VisualState
{
    Default,
    Reachable,
    Selected,
    Activated
}

[Serializable]
public struct StateStyle
{
    public VisualState state;

    [Header("Material (optional)")]
    public Material material; // 如果为空 = 不换材质，只改参数

    [Header("MPB Overrides")]
    public Color emissionColor;   // HDR 也行
    public float glowStrength;    // _GlowStrength
    public float pulseSpeed;      // _PulseSpeed
    public float pulseAmp;        // _MapStrength (or amplitude)
    public float fresnelPower;    // 如果你也做了这个参数
}