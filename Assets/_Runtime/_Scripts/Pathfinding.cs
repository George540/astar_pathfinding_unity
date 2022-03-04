using System;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

public enum AStarType { Clusters, Manhattan };
public class Pathfinding : MonoBehaviour
{
    public bool _debug;
    [SerializeField] private GridGraph _graph;

    public AStarType _aStarType;
    //public delegate float Heuristic(Transform start, Transform end);

    public GridGraphNode _startNode;
    public GridGraphNode _goalNode;
    public GameObject _openPointPrefab;
    public GameObject _closedPointPrefab;
    public GameObject _pathPointPrefab;
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (_camera is { } && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity, LayerMask.GetMask("Node")))
            {
                if (_startNode != null && _goalNode != null)
                {
                    _startNode = null;
                    _goalNode = null;
                    ClearPoints();
                    ClearClusters();
                }

                if (_startNode == null)
                {
                    _startNode = hit.collider.gameObject.GetComponent<GridGraphNode>();
                }
                else if (_goalNode == null)
                {
                    _goalNode = hit.collider.gameObject.GetComponent<GridGraphNode>();

                    // TODO: use an admissible heuristic and pass it to the FindPath function
                    var path = new List<GridGraphNode>();
                    var pathCluster = new List<GridGraphCluster>();
                    if (_aStarType == AStarType.Manhattan)
                    {
                        path = FindNodePath(_startNode, _goalNode);
                    }
                    else if (_aStarType == AStarType.Clusters)
                    {
                        pathCluster = FindClusterPath(_startNode, _goalNode);
                        var pathNodes = new List<GridGraphNode>();
                        foreach (var cluster in pathCluster)
                        {
                            pathNodes.AddRange(cluster._nodeCollection);
                        }
                        path = FindNodePath(_startNode, _goalNode, pathNodes);
                    }
                }
            }
            else
            {
                _startNode = null;
                _goalNode = null;
                ClearPoints();
                ClearClusters();
            }
        }
    }

    private List<GridGraphNode> FindNodePath(GridGraphNode start, GridGraphNode goal, List<GridGraphNode> clusterNodes = null, bool isAdmissible = true)
    {
        if (_graph == null) return new List<GridGraphNode>();

        // if no heuristic is provided then set heuristic = 0
        // if (heuristic == null) heuristic = (Transform s, Transform e) => 0;
        

        List<GridGraphNode> path = null;
        var solutionFound = false;

        // dictionary to keep track of g(n) values (movement costs)
        var gnDict = new Dictionary<GridGraphNode, float> {{start, default}};

        // dictionary to keep track of f(n) values (movement cost + heuristic)
        var fnDict = new Dictionary<GridGraphNode, float>
        {
            {start, Heuristic(start.transform, goal.transform) + gnDict[start]}
        };

        // dictionary to keep track of our path (came_from)
        var pathDict = new Dictionary<GridGraphNode, GridGraphNode> {{start, null}};

        var openList = new List<GridGraphNode> {start};

        var closedODict = new OrderedDictionary(); // use hash set?

        while (openList.Count > 0)
        {
            // mimic priority queue and remove from the back of the open list (lowest fn value)
            var current = openList[openList.Count - 1];
            openList.RemoveAt(openList.Count - 1);

            closedODict[current] = true;

            // early exit
            if (current == goal && isAdmissible)
            {
                solutionFound = true;
                break;
            }

            if (closedODict.Contains(goal))
            {
                // early exit strategy if heuristic is not admissible (try to avoid this if possible)
                float gGoal = gnDict[goal];
                bool pathIsTheShortest = true;

                foreach (GridGraphNode entry in openList)
                {
                    if (gGoal > gnDict[entry])
                    {
                        pathIsTheShortest = false;
                        break;
                    }
                }

                if (pathIsTheShortest) break;
            }

            var neighbors = _graph.GetNeighbors(current);
            foreach (var neighbor in neighbors)
            {
                var movementCost = ManhattanDistance(current.transform, neighbor.transform);
                
                // For A* Clusters, skip if node does not exist in cluster path
                if (clusterNodes != null && !clusterNodes.Contains(neighbor)) continue;
                
                // TODO

                // if neighbor is in closed list then skip
                if (closedODict.Contains(neighbor)) continue;

                // find gNeighbor (g_next)
                var gNeighbor = gnDict[current] + movementCost;

                // if needed: update tables, calculate fn, and update open_list using FakePQListInsert() function
                if (!gnDict.ContainsKey(neighbor) || gNeighbor < gnDict[neighbor])
                {
                    gnDict[neighbor] = gNeighbor;
                    fnDict[neighbor] = gnDict[neighbor] + Heuristic(neighbor.transform, goal.transform);
                    FakePQListInsert(openList, fnDict, neighbor);
                    pathDict[neighbor] = current;
                }
            }
        }

        // if the closed list contains the goal node then we have found a solution
        if (!solutionFound && closedODict.Contains(goal))
            solutionFound = true;

        if (solutionFound)
        {
            // TODO
            // create the path by traversing the previous nodes in the pathDict
            // starting at the goal and finishing at the start
            var current = goal;
            path = new List<GridGraphNode>();

            while (current != start)
            {
                path = path.Append(current).ToList();
                current = pathDict[current];
            }
            path = path.Append(start).ToList();
            
            // reverse the path since we started adding nodes from the goal 
            path.Reverse();
        }

        if (_debug)
        {
            ClearPoints();

            List<Transform> openListPoints = new List<Transform>();
            foreach (GridGraphNode node in openList)
            {
                openListPoints.Add(node.transform);
            }
            SpawnPoints(openListPoints, _openPointPrefab, Color.magenta);

            List<Transform> closedListPoints = new List<Transform>();
            foreach (DictionaryEntry entry in closedODict)
            {
                GridGraphNode node = (GridGraphNode) entry.Key;
                if (solutionFound && !path.Contains(node))
                    closedListPoints.Add(node.transform);
            }
            SpawnPoints(closedListPoints, _closedPointPrefab, Color.red);

            if (solutionFound)
            {
                List<Transform> pathPoints = new List<Transform>();
                foreach (GridGraphNode node in path)
                {
                    pathPoints.Add(node.transform);
                }
                SpawnPoints(pathPoints, _pathPointPrefab, Color.green);
            }
        }

        return path;
    }

    private List<GridGraphCluster> FindClusterPath(GridGraphNode start, GridGraphNode goal, bool isAdmissible = true)
    {
        if (_graph == null) return new List<GridGraphCluster>();

        // if no heuristic is provided then set heuristic = 0
        // if (heuristic == null) heuristic = (Transform s, Transform e) => 0;
        var startCluster = start._cluster;
        var goalCluster = goal._cluster;

        List<GridGraphCluster> path = null;
        var solutionFound = false;

        // dictionary to keep track of g(n) values (movement costs)
        var gnDict = new Dictionary<GridGraphCluster, float> {{startCluster, default}};

        // dictionary to keep track of f(n) values (movement cost + heuristic)
        var fnDict = new Dictionary<GridGraphCluster, float>
        {
            {startCluster, Heuristic(startCluster.transform, goalCluster.transform) + gnDict[startCluster]}
        };

        // dictionary to keep track of our path (came_from)
        var pathDict = new Dictionary<GridGraphCluster, GridGraphCluster> {{startCluster, null}};

        var openList = new List<GridGraphCluster> {startCluster};

        var closedODict = new OrderedDictionary(); // use hash set?

        while (openList.Count > 0)
        {
            // mimic priority queue and remove from the back of the open list (lowest fn value)
            var current = openList[openList.Count - 1];
            openList.RemoveAt(openList.Count - 1);

            closedODict[current] = true;

            // early exit
            if (current == goalCluster && isAdmissible)
            {
                solutionFound = true;
                break;
            }

            if (closedODict.Contains(goal))
            {
                // early exit strategy if heuristic is not admissible (try to avoid this if possible)
                float gGoal = gnDict[goalCluster];
                bool pathIsTheShortest = true;

                foreach (GridGraphCluster entry in openList)
                {
                    if (gGoal > gnDict[entry])
                    {
                        pathIsTheShortest = false;
                        break;
                    }
                }

                if (pathIsTheShortest) break;
            }

            var neighbors = _graph.GetClusterNeighbors(current);
            foreach (var neighbor in neighbors)
            {
                var movementCost = ManhattanDistance(current.transform, neighbor.transform);
                // TODO

                // if neighbor is in closed list then skip
                if (closedODict.Contains(neighbor)) continue;

                // find gNeighbor (g_next)
                var gNeighbor = gnDict[current] + movementCost;

                // if needed: update tables, calculate fn, and update open_list using FakePQListInsert() function
                if (!gnDict.ContainsKey(neighbor) || gNeighbor < gnDict[neighbor])
                {
                    gnDict[neighbor] = gNeighbor;
                    fnDict[neighbor] = gnDict[neighbor] + Heuristic(neighbor.transform, goal.transform);
                    FakePQListInsert(openList, fnDict, neighbor);
                    pathDict[neighbor] = current;
                }
            }
        }

        // if the closed list contains the goal node then we have found a solution
        if (!solutionFound && closedODict.Contains(goal))
            solutionFound = true;

        if (solutionFound)
        {
            // TODO
            // create the path by traversing the previous nodes in the pathDict
            // starting at the goal and finishing at the start
            var current = goalCluster;
            path = new List<GridGraphCluster>();

            while (current != startCluster)
            {
                path = path.Append(current).ToList();
                current = pathDict[current];
            }
            path = path.Append(startCluster).ToList();
            
            // reverse the path since we started adding nodes from the goal
            path.Reverse();
        }

        if (_debug)
        {
            ClearPoints();

            List<Transform> openListPoints = new List<Transform>();
            foreach (GridGraphCluster cluster in openList)
            {
                openListPoints.Add(cluster.transform);
                cluster.GetComponent<Renderer>().material.color = new Color(0.2830189f, 1.0f, 1.0f, 0.3058824f);
            }

            List<Transform> closedListPoints = new List<Transform>();
            foreach (DictionaryEntry entry in closedODict)
            {
                GridGraphCluster cluster = (GridGraphCluster) entry.Key;
                if (solutionFound && !path.Contains(cluster))
                {
                    closedListPoints.Add(cluster.transform);
                    cluster.GetComponent<Renderer>().material.color = new Color(1.0f, 0.2830189f, 0.2830189f, 0.3058824f);
                }
            }

            if (solutionFound)
            {
                List<Transform> pathPoints = new List<Transform>();
                foreach (GridGraphCluster cluster in path)
                {
                    pathPoints.Add(cluster.transform);
                    cluster.GetComponent<Renderer>().material.color = new Color(0.2830189f, 1.0f, 0.2830189f, 0.3058824f);
                }
            }
        }
        foreach (var cluster in _graph.clusters)
        {
            cluster.GetComponent<MeshRenderer>().enabled = _debug;
        }

        return path;
    }
    
    private void SpawnPoints(List<Transform> points, GameObject prefab, Color color)
    {
        foreach (var t in points)
        {
#if UNITY_EDITOR
            // Scene view visuals
            t.GetComponent<GridGraphNode>()._nodeGizmoColor = color;
#endif

            // Game view visuals
            GameObject obj = Instantiate(prefab, t.position, Quaternion.identity, t);
            obj.name = "DEBUG_POINT";
            obj.transform.localPosition += Vector3.up * 0.5f;
        }
    }

    private void ClearPoints()
    {
        foreach (var node in _graph.nodes)
        {
            for (var c = 0; c < node.transform.childCount; ++c)
            {
                node._nodeGizmoColor = Color.black;
            
                if (node.transform.GetChild(c).name == "DEBUG_POINT")
                {
                    Destroy(node.transform.GetChild(c).gameObject);
                }
            }
        }
    }

    private void ClearClusters()
    {
        if (_aStarType == AStarType.Clusters)
        {
            foreach (var cluster in _graph.clusters)
            {
                cluster.GetComponent<Renderer>().material.color = new Color(0.2830189f, 0.2830189f, 0.2830189f, 0.3058824f);
            }
        }
    }

    /// <summary>
    /// mimics a priority queue here by inserting at the right position using a loop
    /// not a very good solution but ok for this lab example
    /// </summary>
    /// <param name="pqList"></param>
    /// <param name="fnDict"></param>
    /// <param name="node"></param>
    private static void FakePQListInsert(List<GridGraphNode> pqList, Dictionary<GridGraphNode, float> fnDict, GridGraphNode node)
    {
        if (pqList.Count == 0)
            pqList.Add(node);
        else
        {
            for (var i = pqList.Count - 1; i >= 0; --i)
            {
                if (fnDict[pqList[i]] > fnDict[node])
                {
                    pqList.Insert(i + 1, node);
                    break;
                }

                if (i == 0)
                {
                    pqList.Insert(0, node);
                }
            }
        }
    }
    private static void FakePQListInsert(List<GridGraphCluster> pqList, Dictionary<GridGraphCluster, float> fnDict, GridGraphCluster cluster)
    {
        if (pqList.Count == 0)
            pqList.Add(cluster);
        else
        {
            for (var i = pqList.Count - 1; i >= 0; --i)
            {
                if (fnDict[pqList[i]] > fnDict[cluster])
                {
                    pqList.Insert(i + 1, cluster);
                    break;
                }

                if (i == 0)
                {
                    pqList.Insert(0, cluster);
                }
            }
        }
    }

    private float Heuristic(Transform node, Transform goal)
    {
        switch (_aStarType)
        {
            case AStarType.Manhattan:
                return ManhattanDistance(node, goal);
            case AStarType.Clusters:
                return ManhattanDistance(node, goal);
            default:
                return 1;
        }
    }

    private float ManhattanDistance(Transform node, Transform goal)
    {
        var nextPosition = node.position;
        var goalPosition = goal.position;
        return Mathf.Abs(nextPosition.x - goalPosition.x) + Mathf.Abs(nextPosition.z - goalPosition.z);
    }
}
