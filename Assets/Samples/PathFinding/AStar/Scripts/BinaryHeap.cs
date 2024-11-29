using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Algorithm
{
    /// <summary>
    /// 二叉堆, 节点以树状结构排列，其中每个节点最多有两个子节点，并且树总是完整的，
    /// 这意味着树的所有级别都是完全填充的，除了最后一层可能是从左到右填充
    /// 该二叉堆为最小堆(最小堆的性质：对于每一个节点，其值都小于或等于其子节点的值。即父节点的值总是小于等于其左右子节点的值。这样保证了堆顶（根节点）是整个堆中的最小元素)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BinaryHeap<T> where T : IComparable
    {
        private List<T> _heap;

        public BinaryHeap()
        {
            _heap = new List<T>();
        }

        public int Count
        {
            get { return _heap.Count; }
        }

        public T this[int index]
        {
            get => _heap[index];
        }

        /// <summary>
        /// 值变化重新交换数据维持最小堆
        /// </summary>
        /// <param name="index"></param>
        public void Update(int index)
        {
            if (index < 0 && index >= _heap.Count)
                return;
            //当前节点向上进行元素交换
            ParentTreeHeapify(index);
            //当前节点向下进行元素交换
            SubTreeHeapifyRecursive(index);

        }

        public void Add(T item)
        {
            _heap.Add(item);
            int currentIndex = _heap.Count - 1;

            //当前节点向上进行元素交换
            ParentTreeHeapify(currentIndex);

        }

        public void Clear()
        {
            _heap.Clear();
        }

        public bool Remove(T item)
        {
            int index = _heap.IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            // 将目标元素和最后一个元素交换并删除
            int lastIndex = _heap.Count - 1;
            Swap(index, lastIndex);
            _heap.RemoveAt(lastIndex);

            //当前节点向下进行元素交换
            SubTreeHeapifyRecursive(index);

            return true;
        }

        /// <summary>
        /// 当前节点向上对所有父节点进行元素交换
        /// </summary>
        /// <param name="index"></param>
        private void ParentTreeHeapify(int index)
        {
            while (index > 0 && _heap[ParentIndex(index)].CompareTo(_heap[index]) > 0)
            {
                Swap(index, ParentIndex(index));
                index = ParentIndex(index);
            }
        }

        /// <summary>
        /// 当前节点向下对所有子节点进行元素交换
        /// </summary>
        /// <param name="index"></param>
        private void SubTreeHeapifyRecursive(int index)
        {
            int leftIndex = LeftChildIndex(index);
            int rightIndex = RightChildIndex(index);

            //找到值最小的节点交换到父节点
            int minIndex = index;
            if (leftIndex < _heap.Count && _heap[leftIndex].CompareTo(_heap[index]) < 0)
            {
                minIndex = leftIndex;
            }

            if (rightIndex < _heap.Count && _heap[rightIndex].CompareTo(_heap[minIndex]) < 0)
            {
                minIndex = rightIndex;
            }

            if (minIndex != index)
            {
                Swap(index, minIndex);
                SubTreeHeapifyRecursive(minIndex);
            }
        }

        public bool Contains(T item)
        {
            return _heap.Contains(item);
        }

        public int IndexOf(T item)
        {
            return _heap.IndexOf(item);
        }

        /// <summary>
        /// 在完整二叉树中，索引i处的节点的父节点总是在索引(i-1)/2处
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int ParentIndex(int index)
        {
            return (index - 1) / 2;
        }

        /// <summary>
        /// 索引i的节点的左子节点和右子节点分别位于索引2i+1和2i+2
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int LeftChildIndex(int index)
        {
            return 2 * index + 1;
        }

        private int RightChildIndex(int index)
        {
            return 2 * index + 2;
        }

        private void Swap(int index1, int index2)
        {
            T temp = _heap[index1];
            _heap[index1] = _heap[index2];
            _heap[index2] = temp;
        }

    }

}
