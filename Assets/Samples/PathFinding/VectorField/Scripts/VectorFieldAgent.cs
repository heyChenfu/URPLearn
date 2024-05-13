using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VectorField
{
    public class VectorFieldAgent : MonoBehaviour
    {
        [SerializeField]
        private float _acceleration;

        private VectorFieldDataNode[,] _pathArr;
        private VectorFieldDataNode _endNode;
        private Rigidbody _rigidbody;
        private Vector3 _currentVelocity;

        // Start is called before the first frame update
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_pathArr != null)
            {
                //判断当前物体所在格子(判断复杂应简化, 位置除以网格总大小快速计算当前所在格子索引?)
                VectorFieldDataNode currNode = null;
                for (int i = 0; i < _pathArr.GetLength(0); ++i)
                {
                    for (int j = 0; j < _pathArr.GetLength(1); ++j)
                    {
                        if (!_pathArr[i, j].Node.Reachable)
                            continue;
                        if (_pathArr[i, j].Node.BCollider.bounds.Contains(
                            new Vector3(_rigidbody.position.x, _pathArr[i, j].Node.BCollider.bounds.center.y, _rigidbody.position.z)))
                        {
                            currNode = _pathArr[i, j];
                        }
                    }
                }

                if(_endNode == currNode)
                {
                    _currentVelocity = _endNode.Node.NodeObj.transform.position - transform.position;
                    Vector2 horizentalDis = new Vector2(_currentVelocity.x, _currentVelocity.z);
                    if (horizentalDis.sqrMagnitude < 0.1)
                    {
                        StopMove();
                    }
                    else
                    {
                        _rigidbody.velocity = _currentVelocity;
                        transform.forward = _currentVelocity;
                    }
                    return;
                }
                if (currNode == null)
                    return;
                if (currNode.Direction.Equals(Vector3.zero))
                {
                    StopMove();
                    return;
                }
                _currentVelocity = currNode.Direction * _acceleration;
                _rigidbody.velocity = _currentVelocity;
                transform.forward = _currentVelocity;
            }

        }

        public void SetPath(VectorFieldDataNode[,] newPath, VectorFieldDataNode endNode)
        {
            _pathArr = newPath;
            _endNode = endNode;
        }

        private void StopMove()
        {
            _pathArr = null;
            _rigidbody.Sleep();
        }

    }

}
