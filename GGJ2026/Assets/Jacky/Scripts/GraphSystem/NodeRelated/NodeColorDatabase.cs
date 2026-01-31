using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeColorDatabase", menuName = "Scriptable Objects/NodeColorDatabase")]
public class NodeColorDatabase : ScriptableObject
{
    [Header("Node Colors")]
    [SerializeField] private List<NodeColorSO> nodeColors = new List<NodeColorSO>();

    // Fast lookup (built on demand)
    private Dictionary<NodeColor, NodeColorSO> _lookup;

    public IReadOnlyList<NodeColorSO> NodeColors => nodeColors;

    /// <summary>
    /// Returns the NodeColorSO for a NodeColor, or null if not found.
    /// </summary>
    public NodeColorSO Get(NodeColor color)
    {
        EnsureLookup();
        _lookup.TryGetValue(color, out var so);
        return so;
    }

    /// <summary>
    /// Tries to get the NodeColorSO for a NodeColor.
    /// </summary>
    public bool TryGet(NodeColor color, out NodeColorSO so)
    {
        EnsureLookup();
        return _lookup.TryGetValue(color, out so);
    }

    private void EnsureLookup()
    {
        if (_lookup != null) return;

        _lookup = new Dictionary<NodeColor, NodeColorSO>();

        for (int i = 0; i < nodeColors.Count; i++)
        {
            var so = nodeColors[i];
            if (so == null) continue;

            // If duplicates exist, keep the first and ignore the rest to make behavior stable.
            if (_lookup.ContainsKey(so.nodeColor))
                continue;

            _lookup.Add(so.nodeColor, so);
        }
    }
}
