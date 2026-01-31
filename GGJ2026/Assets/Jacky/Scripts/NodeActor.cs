using System.Collections.Generic;
using UnityEngine;

public class NodeActor : MonoBehaviour
{
    public string nodeId;
    [SerializeField] private NodeColor nodeColor = NodeColor.Red;
    [SerializeField] private List<RegionId> nodeRegions = new List<RegionId>();

    private GraphManager _mgr;

    [Header("Test Mesh")]
    public Mesh reachableMesh;
    public Mesh unreachableMesh;

    [SerializeField] private GameObject connectedVisual;
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

    private void OnMouseDown()
    {
        // 简单方案：Collider + Camera 有 Physics Raycaster（默认够用）
        if (_mgr != null)
            _mgr.OnNodeClicked(nodeId);
    }

    public void SetNodeReachable(RegionId curActiateRegion)
    { 
        bool isReachable = nodeRegions.Contains(curActiateRegion);
        var mf = GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            mf.mesh = isReachable ? reachableMesh : unreachableMesh;
        }
    }

    public void SetNodeConnected(bool connected)
    { 
        if (connectedVisual != null)
        {
            connectedVisual.SetActive(connected);
        }
    }
}
