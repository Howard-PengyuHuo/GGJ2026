using System.Collections.Generic;
using UnityEngine;

public class GraphManager : MonoBehaviour
{
    [Header("Data")]
    public GraphLevelData levelData;

    [Header("Prefabs")]
    //public GameObject nodePrefab;          // 一个带 Collider + MeshRenderer 的球体之类
    public Material defaultMat;
    public Material reachableMat;
    public Material lockedMat;
    public Material startMat;
    public Material endMat;

    [Header("ColorMaterials")]
    public Material redMat;
    public Material yellowMat;
    public Material greenMat;

    [Header("Edge Visual")]
    public Material lineMat;
    public float lineWidth = 0.03f;

    private readonly Dictionary<string, NodeActor> _spawnedNodes = new();
    private readonly List<LineRenderer> _spawnedLines = new();

    // Runtime state (先留骨架)
    private string _currentNodeId;
    private List<string> _reachableNodeIds = new List<string>();

    [Header("AllNodes")]
    [SerializeField] private List<NodeActor> allNodes = new List<NodeActor>();

    [Header("MedicineRelated")]
    [SerializeField] private List<RegionId> activatedRegions = new List<RegionId>();
    [SerializeField] private int medicineStrength = 1;

    private void Start()
    {
        BuildLevel();
    }

    [ContextMenu("Build Level")]
    public void BuildLevel()
    {
        ClearLevel();

        if (levelData == null)
        {
            Debug.LogWarning("[GraphManager] Missing levelData.");
            return;
        }

        CollectSceneNodes();
        ApplyLevelDataToSceneNodes();
        BuildEdgesFromLevelData();

        _currentNodeId = levelData.startNodeId;
        RecomputeReachable();
    }

    private void CollectSceneNodes()
    { 
        _spawnedNodes.Clear();

        for (int i = 0; i < allNodes.Count; i++)
        {
            var actor = allNodes[i];
            if (actor == null) continue;

            if (string.IsNullOrEmpty(actor.nodeId))
            {
                Debug.LogWarning($"[GraphManager] Found NodeActor without nodeId: {actor.gameObject.name}", actor);
                continue;
            }

            //// 确保点击回调可用
            //actor.Init(this, actor.nodeId);

            if (_spawnedNodes.ContainsKey(actor.nodeId))
            {
                Debug.LogWarning($"[GraphManager] Duplicate nodeId '{actor.nodeId}' found on '{actor.gameObject.name}'. Ignored.", actor);
                continue;
            }

            _spawnedNodes.Add(actor.nodeId, actor);
        }
    }

    private void ApplyLevelDataToSceneNodes()
    {
        foreach (var kvp in _spawnedNodes)
        {
            var nodeId = kvp.Key;
            NodeActor nodeActor = kvp.Value;

            if (!levelData.TryGetNode(nodeId, out var nodeDef) || nodeDef == null)
            {
                Debug.LogWarning($"[GraphManager] Scene node '{nodeId}' not found in GraphLevelData.nodes.");
                // 不 return：场景可能有摆了但数据没配的节点
            }

            nodeActor.Init(this, nodeDef);

            // TODO: 按你的需求，把 nodeDef.color / nodeDef.allRegions 反向写回 prefab 上的组件
            // 目前你的 NodeActor 还没存 region/color，所以这里先用材质表现 color（你也可以换成 Renderer 颜色等）
            //ApplyNodeMaterial(nodeId, nodeDef);
        }
    }


    private void BuildEdgesFromLevelData()
    {
        if (levelData.edges == null) return;

        foreach (var e in levelData.edges)
        {
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.a) || string.IsNullOrEmpty(e.b)) continue;

            if (!_spawnedNodes.TryGetValue(e.a, out var aActor) || !_spawnedNodes.TryGetValue(e.b, out var bActor))
            {
                Debug.LogWarning($"[GraphManager] Edge skipped (missing node in scene): {e.a} - {e.b}");
                continue;
            }

            var lrGo = new GameObject($"Edge_{e.a}_{e.b}");
            lrGo.transform.SetParent(transform);

            var lr = lrGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.material = lineMat != null ? lineMat : new Material(Shader.Find("Sprites/Default"));

            lr.SetPosition(0, aActor.transform.position);
            lr.SetPosition(1, bActor.transform.position);

            _spawnedLines.Add(lr);
        }
    }

    [ContextMenu("Clear Level")]
    public void ClearLevel()
    {
        // 反向流程：不再销毁节点（节点是你手工摆放并保存到场景里的）
        // 只清理线
        for (int i = _spawnedLines.Count - 1; i >= 0; i--)
        {
            if (_spawnedLines[i] != null)
                DestroyImmediate(_spawnedLines[i].gameObject);
        }

        _spawnedLines.Clear();
        _spawnedNodes.Clear();
    }

    public void OnNodeClicked(string nodeId)
    {
        Debug.Log($"[Graph] Clicked node: {nodeId}");

        //List<string> connectedIds = levelData.ReturnConnectedNodeIdsDepth(_currentNodeId, medicineStrength);
        if (!_reachableNodeIds.Contains(nodeId))
        {
            Debug.Log("[Graph] Node not connected to current node.");
            return;
        }

        _currentNodeId = nodeId;

        if (levelData != null && nodeId == levelData.endNodeId)
        {
            Debug.Log("[Graph] Reached END!");
        }

        RecomputeReachable();
    }

    private void RecomputeReachable()
    {
        //foreach (var nodeId in _spawnedNodes.Keys)
        //{
        //    levelData.TryGetNode(nodeId, out var nodeDef);
        //    //ApplyNodeMaterial(nodeId, nodeDef);
        //}
        _reachableNodeIds = levelData.ReturnConnectedNodeIdsDepth(_currentNodeId, medicineStrength);
        foreach (var nodeId in _spawnedNodes.Keys)
        {
            if (_spawnedNodes.TryGetValue(nodeId, out var actor))
            {
                actor.SetNodeConnected(_reachableNodeIds.Contains(nodeId));
            }
        }
    }

    private void ApplyNodeMaterial(string nodeId, NodeDef nodeDef)
    {
        if (!_spawnedNodes.TryGetValue(nodeId, out var actor)) return;

        var rend = actor.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        //if (levelData != null && nodeId == levelData.startNodeId && startMat != null)
        //{
        //    rend.sharedMaterial = startMat;
        //    return;
        //}

        //if (levelData != null && nodeId == levelData.endNodeId && endMat != null)
        //{
        //    rend.sharedMaterial = endMat;
        //    return;
        //}

        // 你真正想要的：根据 levelData 中该 nodeId 的 color/region 去设定显示
        // 这里先维持原行为：默认材质
        if (defaultMat != null) rend.sharedMaterial = defaultMat;

        // 如果你希望立刻把 NodeColor 映射到材质，也可以在这里 switch(nodeDef.color) 做映射
        // （但你当前只有 default/reachable/locked/start/end 材质，所以先不强行加）
    }
}
