using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NodeActor : MonoBehaviour
{
    public string nodeId;
    [SerializeField] private NodeColor nodeColor = NodeColor.Red;
    [SerializeField] private List<RegionId> nodeRegions = new List<RegionId>();

    private GraphManager _mgr;

    private bool isReachable = false;

    [Header("Test Mesh")]
    public Mesh reachableMesh;
    public Mesh unreachableMesh;

    [SerializeField] private GameObject activatedVisual;


#if UNITY_EDITOR
    [Header("Editor")]
    [SerializeField] private bool showNodeIdLabel = false;
    [SerializeField] private Color nodeIdLabelColor = Color.white;
    [SerializeField] private float nodeIdLabelYOffset = 0.35f;
#endif

    public NodeColor NodeColor => nodeColor;
    public IReadOnlyList<RegionId> NodeRegions => nodeRegions;


#if UNITY_EDITOR
    public void SetShowNodeIdLabel(bool show)
    {
        showNodeIdLabel = show;
    }

    private void OnDrawGizmos()
    {
        if (!showNodeIdLabel) return;
        if (string.IsNullOrEmpty(nodeId)) return;

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = nodeIdLabelColor }
        };

        var pos = transform.position + Vector3.up * nodeIdLabelYOffset;
        Handles.Label(pos, nodeId, style);
    }
#endif


    public void Init(GraphManager mgr, NodeDef nodeDef)
    {
        _mgr = mgr;
        //nodeId = id;
        gameObject.name = $"Node_{nodeId}";
        nodeColor = nodeDef.color;
        nodeRegions = new List<RegionId>(nodeDef.allRegions);

        switch (nodeColor) { 
            case NodeColor.Red:
                GetComponentInChildren<Renderer>().material = mgr.redMat;
                break;
            case NodeColor.Yellow:
                GetComponentInChildren<Renderer>().material = mgr.yellowMat;
                break;
            case NodeColor.Green:
                GetComponentInChildren<Renderer>().material = mgr.greenMat;
                break;
        }
    }

    public void ResetVisual(GraphManager mgr = null)
    {
        // 逻辑状态
        isReachable = false;

        // Mesh 回到不可达
        var mf = GetComponentInChildren<MeshFilter>();
        if (mf != null && unreachableMesh != null)
        {
            mf.mesh = unreachableMesh;
        }

        // Activated 关掉
        if (activatedVisual != null)
        {
            activatedVisual.SetActive(false);
        }

        // 选中缩放复原
        transform.localScale = Vector3.one;

        // 材质回默认（如果给了 mgr & defaultMat）
        if (mgr != null && mgr.defaultMat != null)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                r.material = mgr.defaultMat;
            }
        }
    }

    private void OnMouseDown()
    {
        //// 简单方案：Collider + Camera 有 Physics Raycaster（默认够用）
        //if (_mgr != null)
        //    _mgr.OnNodeClicked(nodeId);

        if (nodeId == "Node_End") {
            _mgr.OnNodeProceeded(nodeId);
            return;
        }

        if (_mgr != null && isReachable) { 
            _mgr.OnNodeProceeded(nodeId);
        }
    }

    public void SetNodeReachable(bool reachable)
    {
        //Debug.Log($"Node {nodeId} reachable state set to {reachable}");
        var mf = GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            mf.mesh = reachable ? reachableMesh : unreachableMesh;
        }
        isReachable = reachable;
    }

    public void SetNodeActivated(bool activated)
    { 
        //Debug.Log($"Node {nodeId} activated state set to {activated}");
        activatedVisual.SetActive(activated);
    }

    public void SetNodeCurSelected(bool curSelected)
    { 
        this.transform.localScale = curSelected ? Vector3.one * 1.5f : Vector3.one;
    }
}
