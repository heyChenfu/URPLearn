using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Algorithm
{
    //邻接矩阵
    public struct AdjacentMatrix 
    {
        public bool IsDigraph; //是否有向图
        //顶点集
        public int[] Vertices;
        //边集
        public int[,] Edge;
        //顶点数 边数
        public int numV, numE;
        //无效的边值, 即两个顶点无连接s
        public const int InvalidEdgeValue = 65535;

        public AdjacentMatrix(bool isDigraph, int vertexCount, int edgeCount)
        {
            IsDigraph = isDigraph;
            Vertices = new int[100];
            Edge = new int[100, 100];
            numV = vertexCount;
            numE = edgeCount;

            for (int i = 0; i < numV; i++)
            {
                Vertices[i] = i;
            }
            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    if (i == j)
                    {
                        //一个非带权的图 0代表没有边
                        //边不指向自己 即对角线为0
                        Edge[i, j] = 0;
                    }
                    else
                    {
                        //如果是带权的图 初始化为0或者为一个不可能的值
                        Edge[i, j] = InvalidEdgeValue;
                    }
                }
            }
            //
            int tmpEdge = 0;
            while (tmpEdge < numE)
            {
                int vertex1 = Random.Range(0, numV);
                int vertex2 = Random.Range(0, numV);
                if (vertex1 != vertex2 &&
                    Edge[vertex1, vertex2] == InvalidEdgeValue)
                {
                    Edge[vertex1, vertex2] = Random.Range(1, 11);
                    //如果是无向图 矩阵对称
                    if (!IsDigraph)
                        Edge[vertex2, vertex1] = Edge[vertex1, vertex2];
                    tmpEdge++;
                }
            }

        }

        public bool IsEdgeExists(int i, int j)
        {
            if (i >= Edge.GetLength(0) || j >= Edge.GetLength(1))
                return false;
            if (Edge[i, j] != 0 && Edge[i, j] != InvalidEdgeValue)
                return true;
            return false;
        }

    }

    /// <summary>
    /// 最短路径算法 迪杰斯特拉 求有向图G 从某一个顶点开始 到其余各个顶点的最短路径P以及带权长度
    /// </summary>
    public static class DijkstraAlgorithm
    {
        /// <summary>
        /// 迪杰斯特拉最短路径算法
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="v0">初始点</param>
        /// <param name="p">源点到该节点经过的上一个节点数组</param>
        /// <param name="d">源点开始到该节点的最短路径长度</param>
        public static void ShortPathDijkstra(AdjacentMatrix matrix, int v0, int[] p, int[] d)
        {
            int k = 0;//当前节点下标
            int[] final = new int[matrix.numV]; //final[x] = 1 表示已求得的到v0的最短路径, 不再更新其路径值
            //初始化D, P, Final 数组
            for (int i = 0; i < matrix.numV; i++)
            {
                final[i] = 0;//初始化为未知状态
                d[i] = matrix.Edge[v0, i];
                if (matrix.IsEdgeExists(v0, i))
                    p[i] = v0;
                else
                    p[i] = -1;
            }
            final[v0] = 1;
            d[v0] = 0;//自己到自己的路径为0

            //主循环 求每次v0到v的最短路径
            for (int i = 1; i < matrix.numV; i++)
            {
                int min = AdjacentMatrix.InvalidEdgeValue;
                //寻找与v0距离最近的顶点
                for (int j = 0; j < matrix.numV; j++)
                {
                    if (final[j] != 1 && d[j] < min)
                    {
                        min = d[j];
                        k = j;
                    }
                }
                //V(k)已是最短路径
                final[k] = 1;
                //修正当前最短路径的距离
                //以Vk作为中转，更新以Vk为中心的邻接点到V0的距离
                for (int j = 0; j < matrix.numV; j++)
                {
                    //如果当前找到v的顶点的路径小于原来的路径长度
                    if (min + matrix.Edge[k, j] < d[j] && final[j] != 1)
                    {
                        //说明找到了更短的路径 修改D, P
                        d[j] = min + matrix.Edge[k, j];
                        p[j] = k;
                    }
                }
            }

        }

    }

}


