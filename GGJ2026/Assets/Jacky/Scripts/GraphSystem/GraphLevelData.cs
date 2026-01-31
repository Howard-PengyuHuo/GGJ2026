using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GraphLevelData", menuName = "Scriptable Objects/GraphLevelData")]
public class GraphLevelData : ScriptableObject
{
    [Header("Meta")]
    public string levelId = "BrainCase_01";
    public string displayName = "Brain Case 01";

    [Header("Start / End")]
    public string startNodeId = "Node_Start";
    public string endNodeId = "Node_End";

    [Header("Initial Regions")]
    public List<RegionId> initialActivatedRegions = new List<RegionId>();

    [Header("Graph")]
    public List<NodeDef> nodes = new List<NodeDef>();
    public List<EdgeDef> edges = new List<EdgeDef>();

    [Header("Next_Level")]
    public DialogueGraph nextLinearLevelDialogueGraph;
    public DialogueGraph nextHubAndBranchDialogueGraph;

    public bool TryGetNode(string id, out NodeDef node)
    {
        node = null;
        if (string.IsNullOrEmpty(id)) return false;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].id == id)
            {
                node = nodes[i];
                return true;
            }
        }
        return false;
    }

    public bool HasNode(string id)
    {
        return TryGetNode(id, out _);
    }

    private List<string> ConnectedNodeIds(string id)
    {
        List<string> connectedIds = new List<string>();
        if (string.IsNullOrEmpty(id)) return connectedIds;
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            if (edge == null) continue;
            if (edge.a == id)
            {
                connectedIds.Add(edge.b);
            }
            else if (edge.b == id)
            {
                connectedIds.Add(edge.a);
            }
        }
        return connectedIds;
    }

    public List<string> ReturnConnectedNodeIdsDepth(string id, int depth = 1) {
        var result = new List<string>();

        if (string.IsNullOrEmpty(id)) return result;
        if (depth <= 0) return result;
        if (depth == 1) return ConnectedNodeIds(id);

        // BFS: 从起点开始，逐层扩展，收集 1..depth 距离内的所有节点（不包含起点本身）
        var visited = new HashSet<string>();
        var currentLayer = new HashSet<string>();

        visited.Add(id);
        currentLayer.Add(id);

        for (int d = 1; d <= depth; d++)
        {
            var nextLayer = new HashSet<string>();

            foreach (var nodeId in currentLayer)
            {
                var neighbors = ConnectedNodeIds(nodeId);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    var n = neighbors[i];
                    if (string.IsNullOrEmpty(n)) continue;

                    // visited 保证去重，也避免走回头路
                    if (visited.Add(n))
                    {
                        nextLayer.Add(n);
                        result.Add(n); // 这是距离 <= depth 的节点（按层推进）
                    }
                }
            }

            if (nextLayer.Count == 0)
                break;

            currentLayer = nextLayer;
        }

        return result;
    }
}

public enum NodeColor
{
    Red = 0,
    Yellow = 1,
    Green = 2,
    Black = 3
}

public enum RegionId
{
    StartEnd,
    Temporal,
    Limbic,
    Brainstem,
    Other
}

[Serializable]
public class NodeDef
{
    public string id = "N_000";
    //public Vector3 position;
    public NodeColor color;
    public List<RegionId> allRegions = new List<RegionId>();
    //public string label;
}

[Serializable]
public class EdgeDef
{
    public string a;
    public string b;

    public EdgeDef(string a, string b)
    {
        this.a = a;
        this.b = b;
    }
}