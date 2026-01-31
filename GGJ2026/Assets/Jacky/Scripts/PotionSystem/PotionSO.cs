using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PotionSO", menuName = "Scriptable Objects/PotionSO")]
public class PotionSO : ScriptableObject
{
    [Header("Identity")]
    public string potionId;          // ⭐ 唯一 key
    public string displayName;
    [TextArea] public string description;

    [Header("UI")]
    public Sprite icon;
    public Color uiTint = Color.white;

    [Header("Rules")]
    public List<NodeColor> allowedColors = new();
    [Min(1)] public int maxSteps = 1;
    public bool consumeOnMove = true;

    public bool Allows(NodeColor color)
    {
        return allowedColors.Contains(color);
    }
}
