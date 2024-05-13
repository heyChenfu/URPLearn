using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Algorithm
{
    public class AStarGrid : MonoBehaviour
    {
        public AStarNode[,,] Grid;

        private const int _maxX = 1000;
        private const int _maxY = 3;
        private const int _maxZ = 1000;
        private float _naviNodeInterval = 1.1f;

        public AStarGrid(GameObject naviObj, int xNum, int yNum, int zNum, Material naviCommonMat, Material naviDisableMat)
        {
            Grid = new AStarNode[_maxX, _maxY, _maxZ];

            int XOffset = 0, YOffset = 0, ZOffset = 0;
            for (int i = 0; i < xNum; ++i)
            {
                for (int j = 0; j < yNum; ++j)
                {
                    for (int k = 0; k < zNum; ++k)
                    {
                        bool bUnreachable = Random.Range(0, 10) < 3;
                        GameObject obj = GameObject.Instantiate<GameObject>(naviObj);
                        obj.transform.position = new Vector3(XOffset * _naviNodeInterval, 0, ZOffset * _naviNodeInterval);
                        obj.GetComponent<MeshRenderer>().material = bUnreachable ? naviDisableMat : naviCommonMat;
                        obj.transform.Find("Text").GetComponent<TextMeshPro>().text = $"{XOffset},{ZOffset}";
                        AStarNode nodeUI = new AStarNode(obj, XOffset, YOffset, ZOffset, bUnreachable);
                        Grid[XOffset, YOffset, ZOffset] = nodeUI;
                        ZOffset++;
                    }
                    YOffset++;
                    ZOffset = 0;
                }
                XOffset++;
                YOffset = 0;
            }

        }

        public List<AStarNode> HitDetect(RaycastHit hitInfo, Vector3 agentPosition)
        {
            List<AStarNode> path = null;
            AStarNode starNode = Grid[0, 0, 0], endNode = null;

            for (int i = 0; i < Grid.GetLength(0); ++i)
            {
                for (int j = 0; j < Grid.GetLength(1); ++j)
                {
                    for (int k = 0; k < Grid.GetLength(2); ++k)
                    {
                        if (Grid[i, j, k] == null)
                            continue;
                        Grid[i, j, k].Reset();
                        if (Vector3.Distance(Grid[i, j, k].NodeObj.transform.position, agentPosition) <= Grid[i, j, k].NodeCollider.bounds.size.x)
                            starNode = Grid[i, j, k];
                        if (Grid[i, j, k].NodeObj != null && Grid[i, j, k].NodeObj.transform == hitInfo.transform)
                            endNode = Grid[i, j, k];
                    }
                }
            }

            if (endNode != null && endNode != starNode)
            {
                path = AStarAlgorithm.Instance().ShortPathAStar(this, starNode, endNode);
                LogNodeList(path);
            }

            return path;
        }

        void LogNodeList(List<AStarNode> path)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < path.Count; ++i)
            {
                sb.Append($"{{{path[i].X}, {path[i].Z}}} ");
            }
            Debug.Log(sb.ToString());
        }

    }

}

