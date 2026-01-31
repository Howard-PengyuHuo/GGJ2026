using UnityEngine;

[CreateAssetMenu(fileName = "NodeColorSO", menuName = "Scriptable Objects/NodeColorSO")]
public class NodeColorSO : ScriptableObject
{
    public NodeColor nodeColor;
    public Material nodeGlowingMaterial;
}
