using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance { get; private set; }

    [Header("Data")]
    public GraphLevelData levelData;

    [Header("Reference")]
    [SerializeField] private DialogueSystem dialogueSystem;

    [Header("ColorMaterials")]
    public Material defaultMat;
    public Material redMat;
    public Material yellowMat;
    public Material greenMat;

    [Header("Edge Visual")]
    public Material lineMat;
    public float lineWidth = 0.03f;
    [SerializeField] private Transform lineRendererTransform;

    [Header("AllNodes")]
    [SerializeField] private List<NodeActor> allNodes = new List<NodeActor>();

    private readonly Dictionary<string, NodeActor> _spawnedNodes = new();
    private readonly List<LineRenderer> _spawnedLines = new();

    // Runtime state (先留骨架)
    [Header("RunTime Debug")]
    [SerializeField] private string _currentNodeId;
    [SerializeField] private List<string> _reachableNodeIds = new List<string>();

    [Header("MedicineRelated")]
    [SerializeField] private List<RegionId> activatedRegions = new List<RegionId>();
    [SerializeField] private PotionSO selectedPotionSO;

    // ================= Events =================
    public Action<List<RegionId>> OnActivatedRegionsChanged;


    // ================= Editor Tools =================
#if UNITY_EDITOR

    private void SetSceneNodeIdLabels(bool show)
    {
        for (int i = 0; i < allNodes.Count; i++)
        {
            var n = allNodes[i];
            if (n == null) continue;
            n.SetShowNodeIdLabel(show);
            EditorUtility.SetDirty(n);
        }

        // 让 SceneView 立即重绘（否则可能要动一下相机才刷新）
        SceneView.RepaintAll();
    }


    [ContextMenu("Editor Tools/Init Level (BuildLevel)")]
    private void Editor_InitLevel()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[GraphManager] Editor_InitLevel is intended for Edit Mode.", this);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Init Level");
        BuildLevel();

        // Init 后打开 NodeId 显示
        SetSceneNodeIdLabels(true);

        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Editor Tools/Clear Level")]
    private void Editor_ClearLevel()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[GraphManager] Editor_ClearLevel is intended for Edit Mode.", this);
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Clear Level");
        ClearLevel();

        // Clear 后关闭 NodeId 显示
        SetSceneNodeIdLabels(false);

        EditorUtility.SetDirty(this);
    }
#endif


    private void OnEnable()
    {
        //OnActivatedRegionsChanged += HandleActivatedRegionsChanged;

        //var inventoryManager = PotionInventoryManager.Instance;
        //if (inventoryManager != null)
        //{
        //    inventoryManager.OnSelectedPotionChanged += OnSelectedPotionChanged;
        //}
        Invoke(nameof(SubscribeAll), 0.1f);
    }

    private void SubscribeAll()
    {
        var inventoryManager = PotionInventoryManager.Instance;
        if (inventoryManager != null)
        {
            inventoryManager.OnSelectedPotionChanged += OnSelectedPotionChanged;
        }
    }

    private void OnDisable()
    {
        //OnActivatedRegionsChanged -= HandleActivatedRegionsChanged;

        var inventoryManager = PotionInventoryManager.Instance;
        if (inventoryManager != null)
        {
            inventoryManager.OnSelectedPotionChanged -= OnSelectedPotionChanged;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }


    private void Start()
    {
        //SetActivatedRegion(new List<RegionId> { RegionId.Temporal });
        //BuildLevel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) { 
            SetActivatedRegion(new List<RegionId> { RegionId.Temporal });
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            SetActivatedRegion(new List<RegionId> { RegionId.Limbic });
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            SetActivatedRegion(new List<RegionId> { RegionId.Brainstem });
        }
    }


    public void BuildLevelWLevelData(GraphLevelData newLevel) {
        if (levelData != newLevel)
        {
            levelData = newLevel;
            ClearLevel();
            BuildLevel();
        }
        else
        { 
            Debug.Log("[GraphManager] BuildLevelWLevelData: levelData is the same as current, build again.");
            ClearLevel();
            BuildLevel();
        }
        
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

    #region BuildLevel Steps
    private void CollectSceneNodes()
    { 
        _spawnedNodes.Clear();

        for (int i = 0; i < allNodes.Count; i++)
        {
            var actor = allNodes[i];
            if (actor == null) continue;

            actor.gameObject.SetActive(true);

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
        //if (levelData.edges == null) return;

        //foreach (var e in levelData.edges)
        //{
        //    if (e == null) continue;
        //    if (string.IsNullOrEmpty(e.a) || string.IsNullOrEmpty(e.b)) continue;

        //    if (!_spawnedNodes.TryGetValue(e.a, out var aActor) || !_spawnedNodes.TryGetValue(e.b, out var bActor))
        //    {
        //        Debug.LogWarning($"[GraphManager] Edge skipped (missing node in scene): {e.a} - {e.b}");
        //        continue;
        //    }

        //    var lrGo = new GameObject($"Edge_{e.a}_{e.b}");
        //    if (lineRendererTransform == null)
        //    {
        //        Debug.LogWarning("[GraphManager] lineRendererTransform is not assigned. Using GraphManager's transform as parent.");
        //        lrGo.transform.SetParent(this.transform);
        //    }
        //    else {
        //        lrGo.transform.SetParent(lineRendererTransform);
        //    }

        //    var lr = lrGo.AddComponent<LineRenderer>();
        //    lr.positionCount = 2;
        //    lr.startWidth = lineWidth;
        //    lr.endWidth = lineWidth;
        //    lr.useWorldSpace = true;
        //    lr.material = lineMat != null ? lineMat : new Material(Shader.Find("Sprites/Default"));

        //    lr.SetPosition(0, aActor.transform.position);
        //    lr.SetPosition(1, bActor.transform.position);

        //    _spawnedLines.Add(lr);
        //}

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

            Transform parent = lineRendererTransform != null ? lineRendererTransform : this.transform;
            lrGo.transform.SetParent(parent, worldPositionStays: false);

            var lr = lrGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;

            // ✅ Key: use local space so parent rotation doesn't require per-frame updates.
            lr.useWorldSpace = false;

            lr.material = lineMat != null ? lineMat : new Material(Shader.Find("Sprites/Default"));

            // ✅ Key: write endpoints in parent's local space.
            Vector3 aLocal = parent.InverseTransformPoint(aActor.transform.position);
            Vector3 bLocal = parent.InverseTransformPoint(bActor.transform.position);

            lr.SetPosition(0, aLocal);
            lr.SetPosition(1, bLocal);

            _spawnedLines.Add(lr);
        }
    }

    #endregion

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

        // 节点回到“未初始化/不可达/未激活”的外观
        for (int i = 0; i < allNodes.Count; i++)
        {
            var n = allNodes[i];
            if (n == null) continue;
            n.ResetVisual(this);

            n.gameObject.SetActive(false);
        }

        _currentNodeId = null;
        _reachableNodeIds.Clear();

        SetActivatedRegion(new List<RegionId>());
    }

    // Called by NodeActor when clicked
    public void OnNodeProceeded(string nodeId)
    {
        Debug.Log($"[Graph] Clicked node: {nodeId}");

        //List<string> connectedIds = levelData.ReturnConnectedNodeIdsDepth(_currentNodeId, medicineStrength);
        if (!_reachableNodeIds.Contains(nodeId))
        {
            Debug.LogWarning("[Graph] Node not connected to current node. But Technically Should in NodeActor");
            return;
        }

        _currentNodeId = nodeId;

        var inventoryManager = PotionInventoryManager.Instance;
        if (inventoryManager != null)
        {
            var potionId = inventoryManager.SelectedPotionId;
            inventoryManager.TryConsume(potionId);
        }

        if (levelData != null && nodeId == levelData.endNodeId)
        {
            Debug.Log("[Graph] Reached END!");

            ClearLevel();

            dialogueSystem.StopDialogue();
            if (levelData.nextLinearLevelDialogueGraph == null) { 
                dialogueSystem.PlayNPC(levelData.nextHubAndBranchDialogueGraph);
            }
            else
            {
                dialogueSystem.Play(levelData.nextLinearLevelDialogueGraph, () => { 
                    dialogueSystem.PlayNPC(levelData.nextHubAndBranchDialogueGraph);
                });
            }
        }

        RecomputeReachable();
    }

    // 根据当前节点、药剂等状态，重新计算可达节点;三种情况下重新计算：
    // 1. 当前节点变化 => OnNodeProceeded
    // 2. 选择的药剂变化 => OnSelectedPotionChanged订阅
    // 3. 激活的脑区变化 => SetActivatedRegion
    private void RecomputeReachable()
    {
        if (levelData == null || string.IsNullOrEmpty(_currentNodeId))
            return;

        if (selectedPotionSO == null)
        {
            // 没选择药剂时：全部当作不可用（只显示距离？或者全关掉）
            _reachableNodeIds = levelData.ReturnConnectedNodeIdsDepth(_currentNodeId, 1);
        }
        else
        {
            _reachableNodeIds = levelData.ReturnConnectedNodeIdsDepth(_currentNodeId, selectedPotionSO.maxSteps);
        }

        foreach (var kvp in _spawnedNodes)
        { 
            var actor = kvp.Value;
            bool inRange = _reachableNodeIds.Contains(actor.nodeId);
            bool colorOk = selectedPotionSO != null && selectedPotionSO.allowedColors.Contains(actor.NodeColor);

            bool regionOk = false;
            foreach (var r in actor.NodeRegions)
            {
                if (activatedRegions.Contains(r))
                {
                    regionOk = true;
                    break;
                }
            }

            actor.SetNodeActivated(regionOk);
            actor.SetNodeReachable(inRange && colorOk && regionOk);
            actor.SetNodeCurSelected(actor.nodeId == _currentNodeId);
        }
    }

    // 设置当前激活的脑区列表, 会触发重新计算可达节点,可被text系统调用
    public void SetActivatedRegion(List<RegionId> activatedRegions)
    { 
        this.activatedRegions = activatedRegions;
        RecomputeReachable();
        OnActivatedRegionsChanged?.Invoke(activatedRegions);
    }

    private void OnSelectedPotionChanged(string potionId)
    {
        var inventoryManager = PotionInventoryManager.Instance;
        if (inventoryManager == null) return;

        var potionDef = inventoryManager.GetPotionDef(potionId);
        if (potionDef == null) return;

        selectedPotionSO = potionDef;
        RecomputeReachable();
    }

    //public bool IsNodeReachable(string nodeId) { 
    //    if (string.IsNullOrEmpty(nodeId)) return false;
    //    return activatedRegions.Contains(nodeId);
    //}
}
