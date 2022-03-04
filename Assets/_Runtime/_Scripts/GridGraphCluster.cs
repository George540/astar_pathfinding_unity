using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[SelectionBase]
public class GridGraphCluster : MonoBehaviour
{
    [SerializeField] public List<GridGraphNode> _nodeCollection = new List<GridGraphNode>();
    [SerializeField] public List<GridGraphCluster> _adjacentClusters = new List<GridGraphCluster>();
    public float _clusterCost;
    
    private GridGraph graph;
    
    private void Awake()
    {
        foreach (var node in GetComponentsInChildren<GridGraphNode>())
        {
            if (!_nodeCollection.Contains(node))
            {
                _nodeCollection.Add(node);
                node._cluster = this;
            }
        }
    }

    private GridGraph Graph
    {
        get
        {
            if (graph == null)
                graph = GetComponentInParent<GridGraph>();
            
            return graph;
        }
    }

    private void OnDestroy()
    {
        if (Graph != null)
        {
            Graph.Remove(this);
        }
    }
    
#if UNITY_EDITOR
    public Color _nodeGizmoColor = new Color(Color.white.r, Color.white.g, Color.white.b, 0.5f);
#endif
}
