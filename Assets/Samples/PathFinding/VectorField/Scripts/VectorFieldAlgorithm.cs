

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using VectorField;

namespace Algorithm
{
    public class VectorFieldAlgorithm
    {
        private static VectorFieldAlgorithm _instance;
        private float _maxDistance;

        public static VectorFieldAlgorithm Instance()
        {
            if (_instance == null)
            {
                _instance = new VectorFieldAlgorithm();
            }
            return _instance;
        }

        /// <summary>
        /// 生成热力图
        /// </summary>
        public void GenerateHeatMap(VectorFieldDataNode endNode, VectorFieldDataNode[,] dataList)
        {
            Profiler.BeginSample("GenerateHeatMap");

            _maxDistance = Mathf.Max(endNode.Node.X, endNode.Node.Z, Mathf.Abs(endNode.Node.X - dataList.GetLength(0)), Mathf.Abs(endNode.Node.Z - dataList.GetLength(1)));
            dataList[endNode.Node.X, endNode.Node.Z].SetDistance(0, _maxDistance);
            endNode.IsInit = true;
            //GenerateHeatMapRecursive(ref endNode, dataList, ref dataList[endNode.X, endNode.Z]);

            //广度优先遍历
            Queue<VectorFieldDataNode> neighborsList = new Queue<VectorFieldDataNode>(8);
            neighborsList.Enqueue(endNode);
            while (neighborsList.Count > 0)
            {
                VectorFieldDataNode currNode = neighborsList.Dequeue();
                //如果当前节点是障碍物, 则不考虑由该节点能前往其他节点
                if (!currNode.Node.Reachable)
                    continue;

                //递归计算所有相邻节点距离(生成热力图)
                for (int i = -1; i <= 1; ++i)
                {
                    for (int j = -1; j <= 1; ++j)
                    {
                        if (i == 0 && j == 0)
                            continue;
                        //是否越界
                        int x = currNode.Node.X + i;
                        int y = currNode.Node.Z + j;
                        if (x < 0 || x >= dataList.GetLength(0) ||
                            y < 0 || y >= dataList.GetLength(1))
                            continue;
                        if (endNode.Node == dataList[x, y].Node)
                            continue;

                        //计算距离
                        //是否可抵达
                        bool canInit = true;
                        if (!CanReach(currNode, dataList[x, y], dataList))
                        {
                            if (!dataList[x, y].Node.Reachable)
                                dataList[x, y].SetDistance(float.MaxValue, _maxDistance);
                            else
                                canInit = false; //无法通过障碍达到的格子, 但是它自己又不是障碍, 此时不能给它初始化
                        }
                        else
                        {
                            Vector2 vec = new Vector2(i, j);
                            float newDis = EuclideanDistance(currNode, dataList[x, y]) + currNode.Distance;
                            if(!dataList[x, y].IsInit || newDis < dataList[x, y].Distance)
                                dataList[x, y].SetDistance(newDis, _maxDistance);
                        }

                        if (canInit && !dataList[x, y].IsInit)
                        {
                            dataList[x, y].IsInit = true;
                            neighborsList.Enqueue(dataList[x, y]);
                        }

                    }

                }

                //计算当前节点向量
                var smallestV = Vector3.zero;
                var kernelV = Vector3.zero;
                float minDistance = float.MaxValue;
                bool bContainObstacle = false;
                for (int i = -1; i <= 1; ++i)
                {
                    for (int j = -1; j <= 1; ++j)
                    {
                        if (i == 0 && j == 0)
                            continue;
                        int x = currNode.Node.X + i;
                        int y = currNode.Node.Z + j;
                        //边界
                        if (x < 0 || x >= dataList.GetLength(0) ||
                            y < 0 || y >= dataList.GetLength(1))
                            continue;

                        if (!CanReach(currNode, dataList[x, y], dataList))
                        {
                            bContainObstacle = true;
                            continue;
                        }
                        //最小
                        if (dataList[x, y].Distance < minDistance)
                        {
                            smallestV = new Vector3(i, 0, j);
                            minDistance = dataList[x, y].Distance;
                        }
                        //3x3卷积
                        var dic = new Vector3(i, 0, j);
                        float factor = 1f / dataList[x, y].Distance; //最短距离视为1
                        kernelV += dic.normalized * factor;
                    }
                }
                //根据周围是否有障碍, 选择不同策略?
                currNode.Direction = bContainObstacle ? smallestV.normalized : kernelV.normalized;
                float newAngle = Mathf.Atan2(currNode.Direction.z, currNode.Direction.x) * Mathf.Rad2Deg;
                currNode.Node.AngleText.text = $"{currNode.Direction}, {newAngle.ToString("0.#")}";
                currNode.Node.ArrowObj.transform.localRotation = Quaternion.Euler(0, 0, newAngle);

            }
            Debug.Log("生成完毕");
            Profiler.EndSample();
        }

        /// <summary>
        /// 是否可达
        /// </summary>
        /// <param name="currNode"></param>
        /// <param name="targetNode"></param>
        /// <param name="dataList"></param>
        /// <returns></returns>
        private bool CanReach(VectorFieldDataNode currNode, VectorFieldDataNode targetNode, VectorFieldDataNode[,] dataList)
        {
            if (!targetNode.Node.Reachable)
                return false;
            //对角则考虑两边是否可达
            if (Mathf.Abs(currNode.Node.X - targetNode.Node.X) == 1 &&
                Mathf.Abs(currNode.Node.Z - targetNode.Node.Z) == 1)
            {
                if (!dataList[currNode.Node.X, targetNode.Node.Z].Node.Reachable && !dataList[targetNode.Node.X, currNode.Node.Z].Node.Reachable)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 欧氏距离
        /// </summary>
        /// <returns></returns>
        private float EuclideanDistance(VectorFieldDataNode a, VectorFieldDataNode b)
        {
            if (a.Node.X == b.Node.X || a.Node.Z == b.Node.Z)
                return 1f;
            return 1.4142f;
        }


    }

}

