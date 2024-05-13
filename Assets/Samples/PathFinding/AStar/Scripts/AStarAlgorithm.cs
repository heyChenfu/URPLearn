using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Algorithm
{
    public class AStarNode : IComparable
    {
        public int X;
        public int Y;
        public int Z;
        public bool Unreachable; //是否不可到达

        public int G; //从起点到当前节点移动代价
        public int H; //从当前节点到终点估算(忽略障碍物)
        public AStarNode ParentNode; //寻路找出的父节点

        public GameObject NodeObj;
        public Collider NodeCollider;

        public int F
        {
            get { return G + H; }
        }

        public AStarNode(GameObject obj, int gridX, int gridY, int gridZ, bool unreachable)
        {
            X = gridX;
            Y = gridY;
            Z = gridZ;
            Unreachable = unreachable;

            NodeObj = obj;
            NodeCollider = obj.GetComponent<Collider>();
            Reset();
        }

        public void Reset()
        {
            G = 0;
            H = 0;
            ParentNode = null;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) 
                return 1;

            AStarNode node = obj as AStarNode;
            if (node != null)
                return F.CompareTo(node.F);
            else
                throw new ArgumentException("Object is not a AStarNode");
        }
    }

    /// <summary>
    /// A*寻路
    /// ◆ 维护 OpenList: 每次访问OpenList，都要找出具有最小F 值的节点, 可以通过维护一个排好序的OpenList表来改进(可以使用二叉堆改进)
    /// ◆ 其他单位: 如果你想考虑其他单位，并想使他们移动时绕过彼此，我建议你的寻路程序忽略它们，再写一些新的程序来判断两个单位是否会发生碰撞
    /// ◆ 考虑在地图中使用更大的方格。这减少了寻路时需要搜索的方格数量。对长路径使用大方格，当你接近目标时使用小方格
    /// ◆ 对于很长的路径，考虑使用路径点系统
    /// ◆ 不同的地形损耗: 如果有些可抵达的地形，移动代价会更高些，沼泽，山丘，地牢的楼梯等。计算给定方格的 G 值时加上地形的代价就很容易解决了这个问题。简单的给这些方格加上一些额外的代价就可以了
    /// 
    /// </summary>
    public class AStarAlgorithm
    {
        private static AStarAlgorithm _instance;
        public static int HorizentalMoveCost = 10; //横向移动开销
        public static int SlopeMovementCost = 14; //斜向移动开销

        private AStarGrid _grid;
        /// <summary>
        /// 使用二叉堆维护openList, 快速找出最小值
        /// </summary>
        private BinaryHeap<AStarNode> _openList = new BinaryHeap<AStarNode>();
        private HashSet<AStarNode> _closeSet = new HashSet<AStarNode>();

        private List<AStarNode> _neighboursList = new List<AStarNode>();

        public static AStarAlgorithm Instance()
        {
            if(_instance == null)
            {
                _instance = new AStarAlgorithm();
            }
            return _instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="naviNodeArray">所有寻路节点二维数组</param>
        /// <param name="startNode">开始位置</param>
        /// <param name="endNode">结束位置</param>
        public List<AStarNode> ShortPathAStar(AStarGrid grid, AStarNode startNode, AStarNode endNode)
        {
            _grid = grid;
            _openList.Clear();
            _closeSet.Clear();

            _openList.Add(startNode);

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            ShortPathAStarRecursive(startNode, endNode);
            stopWatch.Stop();
            Debug.Log($"A*耗时{stopWatch.ElapsedMilliseconds}毫秒");

            List<AStarNode> path = new List<AStarNode>();
            AStarNode tmpNode = endNode;
            while(tmpNode != null)
            {
                path.Add(tmpNode);
                tmpNode = tmpNode.ParentNode;
            }
            if(!path.Contains(startNode))
            {
                //无法抵达
                path.Clear();
            }
            path.Reverse();
            return path;
        }

        private void ShortPathAStarRecursive(AStarNode startNode, AStarNode endNode)
        {
            if (_openList.Count <= 0)
                return;

            //最小堆取出首元素即为最小F值节点
            AStarNode currNode = _openList[0];

            _openList.Remove(currNode);
            _closeSet.Add(currNode);

            List<AStarNode> neighboursNode = FindNeighbours(currNode);
            for(int i = 0; i < neighboursNode.Count; ++i)
            {
                //计算新的G值
                int newG = currNode.G + GetDistance(currNode, neighboursNode[i]);
                //如果它不在 open list 中，把它加入 open list ，并且把当前方格设置为它的父亲，记录该方格的 F ， G 和 H 值
                if (_closeSet.Contains(neighboursNode[i]))
                {
                    //检查路径是否具有更低的代价, 是则将它从Closed表中移出，回到Open表中
                    if (newG < neighboursNode[i].G)
                    {
                        neighboursNode[i].ParentNode = currNode;
                        neighboursNode[i].G = newG;
                        _openList.Add(neighboursNode[i]);
                        _closeSet.Remove(neighboursNode[i]);
                    }
                }
                else if (!_openList.Contains(neighboursNode[i]))
                {
                    neighboursNode[i].ParentNode = currNode;
                    neighboursNode[i].G = newG;
                    neighboursNode[i].H = HCalcManhattan(neighboursNode[i], endNode);
                    _openList.Add(neighboursNode[i]);
                }
                else
                {
                    //检查路径是否具有更低的代价, 是的话重置父节点并且重新计算G值
                    if (newG < neighboursNode[i].G)
                    {
                        neighboursNode[i].ParentNode = currNode;
                        neighboursNode[i].G = newG;
                        //F值变化同时更新二叉堆数据
                        _openList.Update(_openList.IndexOf(neighboursNode[i]));
                    }
                }
                //是否找到终点
                if (neighboursNode[i] == endNode)
                {
                    return;
                }
            }

            ShortPathAStarRecursive(startNode, endNode);

        }

        /// <summary>
        /// 查找当前节点的相邻节点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<AStarNode> FindNeighbours(AStarNode node)
        {
            _neighboursList.Clear();

            //遍历当前方格的 8 个相邻方格的每一个方格
            for (int iIndex = -1; iIndex <= 1; iIndex++)
            {
                for (int jIndex = -1; jIndex <= 1; jIndex++)
                {
                    for (int zIndex = -1; zIndex <= 1; zIndex++)
                    {
                        //当前节点
                        if (iIndex == 0 && jIndex == 0 && zIndex == 0)
                            continue;

                        AStarNode newNode = GetNeighbourNode(node, iIndex, jIndex, zIndex);
                        if(newNode != null)
                            _neighboursList.Add(newNode);
                    }
                }
            }
            return _neighboursList;

        }

        private AStarNode GetNeighbourNode(AStarNode node, int targetX, int targetY, int targetZ)
        {
            AStarNode neighbourNode = null;
            AStarNode tmpNode = GetNode(node, targetX, targetY, targetZ);
            if (tmpNode != null && !tmpNode.Unreachable)
            {
                neighbourNode = tmpNode;
            }

            //如果是对角线移动, 则检查相邻节点。为了向对角线移动，我们需要有4个可行走的节点
            if (Mathf.Abs(targetX) == 1 && Mathf.Abs(targetZ) == 1)
            {
                AStarNode neighbour1 = GetNode(node, targetX, 0, 0);
                if (neighbour1 == null || neighbour1.Unreachable)
                    neighbourNode = null;

                AStarNode neighbour2 = GetNode(node, 0, 0, targetZ);
                if (neighbour2 == null || neighbour2.Unreachable)
                    neighbourNode = null;
            }

            return neighbourNode;
        }

        private AStarNode GetNode(AStarNode node, int targetX, int targetY, int targetZ)
        {
            if (node.X + targetX >= 0 && node.Y + targetY >= 0 && node.Z + targetZ >= 0 &&
                node.X + targetX < _grid.Grid.GetLength(0) &&
                node.Y + targetY < _grid.Grid.GetLength(1) &&
                node.Z + targetZ < _grid.Grid.GetLength(2))
                return _grid.Grid[node.X + targetX, node.Y + targetY, node.Z + targetZ];
            return null;
        }

        /// <summary>
        /// 获取两个节点间距离
        /// </summary>
        /// <param name="nodeA"></param>
        /// <param name="nodeB"></param>
        /// <returns></returns>
        private int GetDistance(AStarNode nodeA, AStarNode nodeB)
        {
            //当前节点和上一节点位置是横/竖向还是斜向
            bool bSlope = nodeA.X != nodeB.X && nodeA.Y != nodeB.Y && nodeA.Z != nodeB.Z;
            return bSlope ? SlopeMovementCost : HorizentalMoveCost;
        }

        /// <summary>
        /// H计算
        /// 这里使用 Manhattan 方法，计算从当前方格横向或纵向移动到达目标所经过的方格数，忽略对角移动。
        /// 之所以叫做 Manhattan 方法，是因为这很像统计从一个地点到另一个地点所穿过的街区数，而你不能斜向穿过街区。
        /// 重要的是，计算 H 是，要忽略路径中的障碍物。这是对剩余距离的估算值，而不是实际值，因此才称为试探法
        /// </summary>
        private int HCalcManhattan(AStarNode currNode, AStarNode endNode)
        {
            int xInterval = Mathf.Abs(endNode.X - currNode.X);
            int yInterval = Mathf.Abs(endNode.Y - currNode.Y);
            int zInterval = Mathf.Abs(endNode.Z - currNode.Z);
            return (xInterval + yInterval + zInterval) * HorizentalMoveCost;

        }

    }

}
