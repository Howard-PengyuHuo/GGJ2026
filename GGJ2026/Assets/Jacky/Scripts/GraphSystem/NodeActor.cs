using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NodeActor : MonoBehaviour
{
    public string nodeId;
    [SerializeField] private NodeColor nodeColor = NodeColor.Red;
    [SerializeField] private List<RegionId> nodeRegions = new List<RegionId>();

    private GraphManager _mgr;

    //private bool isReachable = false;

    [Header("Test Mesh")]
    public Mesh reachableMesh;
    public Mesh unreachableMesh;

    [SerializeField] private GameObject activatedVisual;

    private NodeVisualController _nodeVisualController;

    private bool isReachable = false;   

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

    public void Start()
    {
        _nodeVisualController = GetComponent<NodeVisualController>();
    }


    public void Init(GraphManager mgr, NodeDef nodeDef)
    {
        _mgr = mgr;
        gameObject.name = $"Node_{nodeId}";
        // nodeColor = nodeDef.color;
        // nodeRegions = new List<RegionId>(nodeDef.allRegions);
        //
        // switch (nodeColor) { 
        //     case NodeColor.Red:
        //         GetComponentInChildren<Renderer>().material = mgr.redMat;
        //         break;
        //     case NodeColor.Yellow:
        //         GetComponentInChildren<Renderer>().material = mgr.yellowMat;
        //         break;
        //     case NodeColor.Green:
        //         GetComponentInChildren<Renderer>().material = mgr.greenMat;
        //         break;
        // }
    }

    public void ResetVisual(GraphManager mgr = null)
    {
        // Mesh �ص����ɴ�
        var mf = GetComponentInChildren<MeshFilter>();
        if (mf != null && unreachableMesh != null)
        {
            mf.mesh = unreachableMesh;
        }

        // Activated �ص�
        if (activatedVisual != null)
        {
            activatedVisual.SetActive(false);
        }

        // ѡ�����Ÿ�ԭ
        transform.localScale = Vector3.one;

        // ���ʻ�Ĭ�ϣ�������� mgr & defaultMat��
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

        //if (_nodeVisualController == null) return;
        //if (reachable)
        //{
        //    _nodeVisualController.SetState(VisualState.Reachable);
        //}
        //else { 
        //    _nodeVisualController.SetState(VisualState.Default);
        //}
    }

    public void SetNodeActivated(bool activated)
    { 
        //Debug.Log($"Node {nodeId} activated state set to {activated}");
        activatedVisual.SetActive(activated);

        //if (_nodeVisualController == null) return;
        //if (activated)
        //{
        //    _nodeVisualController.SetState(VisualState.Activated);
        //}
        //else
        //{
        //    _nodeVisualController.SetState(VisualState.Default);
        //}
    }

    public void SetNodeCurSelected(bool curSelected)
    { 
        this.transform.localScale = curSelected ? Vector3.one * 1.5f : Vector3.one;

        //if (_nodeVisualController == null) return;
        //if (curSelected)
        //{
        //    _nodeVisualController.SetState(VisualState.Selected);
        //}
        //else
        //{
        //    _nodeVisualController.SetState(VisualState.Default);
        //}
    }
}
