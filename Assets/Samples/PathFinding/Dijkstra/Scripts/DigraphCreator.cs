using Algorithm;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Dijkstra
{
    public struct AdjacentMatrixUI
    {
        public int VertexIndex;
        public GameObject UI;

    }

    public class AdjacentMatrixLineUI
    {
        public int SourceIndex;
        public int TargetIndex;
        public GameObject UI;

    }


    public class DigraphCreator : MonoBehaviour
    {
        [SerializeField]
        private int _vertexCount;
        [SerializeField]
        private int _edgeCount;
        [SerializeField]
        private int _startVertex;
        [SerializeField]
        private int _endVertex;

        private List<AdjacentMatrixUI> _nodeList = new List<AdjacentMatrixUI>();
        private List<AdjacentMatrixLineUI> _lineList = new List<AdjacentMatrixLineUI>();

        // Start is called before the first frame update
        void Start()
        {
            if (_edgeCount < _vertexCount)
                Debug.LogError("至少保证每个顶点都有边!");
            CreateGrahp();
        }

        // Update is called once per frame
        void Update()
        {

        }

        void CreateGrahp()
        {
            AdjacentMatrix matrixData = new AdjacentMatrix(false, _vertexCount, _edgeCount);
            GameObject nodeObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/PathFinding/Dijkstra/Prefab/PathNode.prefab");
            GameObject lineObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/PathFinding/Dijkstra/Prefab/PathLine.prefab");

            //创建节点UI
            for (int i = 0; i < _vertexCount; ++i)
            {
                AdjacentMatrixUI nodeUI = new AdjacentMatrixUI();
                nodeUI.VertexIndex = i;
                nodeUI.UI = GameObject.Instantiate<GameObject>(nodeObj);
                nodeUI.UI.transform.position = new Vector3(i / 2 * 2, i % 2 * 2, 0.1f);
                _nodeList.Add(nodeUI);
                TextMeshPro textUI = nodeUI.UI.transform.Find("NodeIndexText")?.GetComponent<TextMeshPro>();
                textUI.text = i.ToString();
            }
            //创建连线
            for (int i = 0; i < matrixData.Edge.GetLength(0); ++i)
            {
                for (int j = 0; j < matrixData.Edge.GetLength(1); ++j)
                {
                    if (!matrixData.IsEdgeExists(i, j))
                        continue;
                    AdjacentMatrixLineUI previous = _lineList.Find(x => x.TargetIndex == i && x.SourceIndex == j);
                    if (previous == null)
                    {
                        AdjacentMatrixLineUI lineUI = new AdjacentMatrixLineUI();
                        lineUI.SourceIndex = i;
                        lineUI.TargetIndex = j;
                        lineUI.UI = GameObject.Instantiate<GameObject>(lineObj);
                        _lineList.Add(lineUI);
                        AdjacentMatrixUI node1 = _nodeList.Find(x => x.VertexIndex == i);
                        AdjacentMatrixUI node2 = _nodeList.Find(x => x.VertexIndex == j);
                        DrawLine(lineUI.UI.GetComponent<LineRenderer>(), node1, node2, matrixData.Edge[i, j]);
                    }
                    else
                    {
                        AdjacentMatrixLineUI lineUI = new AdjacentMatrixLineUI();
                        lineUI.SourceIndex = i;
                        lineUI.TargetIndex = j;
                        lineUI.UI = previous.UI;
                        _lineList.Add(lineUI);
                    }
                }
            }

            //最短路径计算
            int[] d = new int[matrixData.numV];
            int[] p = new int[matrixData.numV];
            DijkstraAlgorithm.ShortPathDijkstra(matrixData, _startVertex, p, d);
            int currIndex = _endVertex;
            if (_endVertex < p.Length)
            {
                while (currIndex != _startVertex)
                {
                    int tmpIndex = currIndex;
                    currIndex = p[currIndex];
                    if (currIndex == -1)
                    {
                        Debug.LogError($"{_startVertex}到{_endVertex}未找到有效路径!");
                        break;
                    }
                    AdjacentMatrixLineUI lineUI = _lineList.Find(x => x.SourceIndex == currIndex && x.TargetIndex == tmpIndex);
                    if (lineUI != null)
                        ChangeLineColor(lineUI.UI.GetComponent<LineRenderer>());

                }
            }

        }

        void DrawLine(LineRenderer lineRender, AdjacentMatrixUI node1, AdjacentMatrixUI node2, int edgeVal)
        {
            lineRender.SetPosition(0, node1.UI.transform.position);
            lineRender.SetPosition(1, node2.UI.transform.position);
            TextMeshPro textUI = lineRender.transform.Find("PathLineText")?.GetComponent<TextMeshPro>();
            textUI.text = edgeVal.ToString();
            Vector3 newPos = (node1.UI.transform.position + node2.UI.transform.position) / 2;
            newPos.z = -0.1f;
            textUI.transform.position = newPos;

        }

        void ChangeLineColor(LineRenderer lineRender)
        {
            lineRender.startColor = Color.red;
        }

    }

}

