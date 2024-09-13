
using System.Collections.Generic;
using UnityEngine;

namespace SoftBodySimulator
{
    public struct CollisionLine 
    {
        public Vector2 Point1;
        public Vector2 Point2;
    }

    public struct CollisionData
    {
        //碰撞归一化方向
        public Vector2 CollisionNormal;
        //碰撞穿透深度
        public float Depth;
    }

    /// <summary>
    /// 2D软体模拟
    /// </summary>
    public class SoftBodySimulatorMono : MonoBehaviour
    {
        [SerializeField]
        GameObject _instantiatePrefab;
        [SerializeField]
        LineRenderer _collisionLine;
        [SerializeField]
        Vector2 _gravity;
        [SerializeField, Range(0, 1)]
        float _elasticity;
        [SerializeField]
        float _friction;

        List<SoftBodyPointMono> _pointList = new();
        List<ConstraintLineMono> _constraintLineList = new();
        List<CollisionLine> _lineList = new();
        SoftBodyPointMono _selectPoint;

        // Start is called before the first frame update
        void Start()
        {
            for (int i = 0; i < 1; ++i)
            {
                var obj = Instantiate(_instantiatePrefab, transform);
                obj.transform.position = new Vector3(Random.Range(-6, 6), Random.Range(-3, 3), 0);
                var pointArr = obj.GetComponentsInChildren<SoftBodyPointMono>();
                for (int j = 0; j < pointArr.Length; ++j)
                {
                    _pointList.Add(pointArr[j]);
                }
                var constraintLineArr = obj.GetComponentsInChildren<ConstraintLineMono>();
                for (int j = 0; j < constraintLineArr.Length; ++j)
                {
                    _constraintLineList.Add(constraintLineArr[j]);
                }
            }
            for (int i = 0; i < _collisionLine.positionCount - 1; i++)
            {
                var pos1 = _collisionLine.GetPosition(i);
                var pos2 = _collisionLine.GetPosition(i + 1);
                _lineList.Add(new CollisionLine() { Point1 = pos1, Point2 = pos2});
            }

        }

        // Update is called once per frame
        void Update()
        {
            //velocity integration
            for (int i = 0; i < _pointList.Count; ++i)
            {
                _pointList[i].Velocity += _gravity * Time.deltaTime;
            }
            //collision resolution
            for (int i = 0; i < _pointList.Count; ++i)
            {
                CollisionData collisionData = FindCollision(_pointList[i]);
                if (collisionData.Depth < 0)
                    continue;
                // resolve the constraint
                var collisionVec = collisionData.CollisionNormal * collisionData.Depth;
                _pointList[i].transform.position += new Vector3(collisionVec.x, collisionVec.y, 0);

                //计算法向速度(vn)和切向速度(vt)
                var vn = collisionData.CollisionNormal * Vector2.Dot(collisionData.CollisionNormal, _pointList[i].Velocity);
                var vt = _pointList[i].Velocity - vn;
                //法向速度通过弹性进行反转和缩放, 模拟了碰撞的弹性
                vn = -_elasticity * vn;
                //摩擦使点在平行于表面的方向上减速, 模拟阻力效应。
                vt *= Mathf.Exp(-_friction * Time.deltaTime);
                // add up the new velocity
                _pointList[i].Velocity = vn + vt;
            }
            //constraint
            for (int i = 0; i < _constraintLineList.Count; ++i)
            {
                Vector2 distance = _constraintLineList[i].Point[0].transform.position - _constraintLineList[i].Point[1].transform.position;
                float distanceLen = distance.magnitude;
                if (Mathf.Abs(distanceLen) <= float.Epsilon)
                    continue;
                Vector2 requiredDistance = distance * (_constraintLineList[i].Distance / distanceLen);
                //添加阻尼以实现软约束
                float dampingFactor = 1 - Mathf.Exp(-_constraintLineList[i].ConstraintDamping * Time.deltaTime);
                //Vector2 offset = (requiredDistance - distanceVec) * dampingFactor;
                //Vector3 offset3 = new Vector3(offset.x / 2f, offset.y / 2f, 0);
                //对约束对象施加弹簧力
                Vector2 force = _constraintLineList[i].SpringForce * (requiredDistance - distance) * Time.deltaTime;
                _constraintLineList[i].Point[0].Velocity += force;
                _constraintLineList[i].Point[1].Velocity -= force;

            }
            //modify position
            for (int i = 0; i < _pointList.Count; ++i)
            {
                Vector2 newPos = _pointList[i].Velocity * Time.deltaTime;
                _pointList[i].transform.position += new Vector3(newPos.x, newPos.y, 0);
            }

            //mouse
            if (Input.GetMouseButton(0))
            {
                if (_selectPoint == null)
                {
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPos.z = 0;
                    for (int i = 0; i < _pointList.Count; ++i)
                    {
                        Vector3 distance = _pointList[i].transform.position - mouseWorldPos;
                        distance.z = 0;
                        if (distance.magnitude < _pointList[i].Radius)
                        {
                            _selectPoint = _pointList[i];
                            _pointList[i].transform.position = mouseWorldPos;
                            break;
                        }
                    }
                }
            }
            else
            {
                _selectPoint = null;
            }
            if (_selectPoint != null)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;
                _selectPoint.transform.position = mouseWorldPos;
                //Debug.Log($"修改选中物体位置{_selectPoint.transform.position}");
            }

        }

        /// <summary>
        /// 查找穿透深度最大的障碍物
        /// </summary>
        /// <returns></returns>
        private CollisionData FindCollision(SoftBodyPointMono point)
        {
            CollisionData result = new CollisionData() { Depth = -1};
            for (int j = 0; j < _lineList.Count; ++j)
            {
                CollisionData collisionData = CollisionLineDepth(point, _lineList[j]);
                if (collisionData.Depth >= result.Depth)
                    result = collisionData;
            }
            return result;
        }

        private CollisionData CollisionLineDepth(SoftBodyPointMono point, CollisionLine line)
        {
            Vector2 pointPos = point.transform.position;
            // 线段向量
            Vector2 lineDir = line.Point2 - line.Point1;
            // 圆心到线段起点的向量
            Vector2 centerToLineStart = pointPos - line.Point1;

            // 线段的长度平方
            float lineLengthSqr = lineDir.sqrMagnitude;
            if (lineLengthSqr == 0)
            {
                Debug.LogError("p1和p2重合，线段不存在");
                return default;
            }

            // 计算圆心在线段上的投影比例t, t是一个标量,表示投影点在线段P1P2上从P1开始的比例
            float t = Vector2.Dot(centerToLineStart, lineDir) / lineLengthSqr;
            // 限制t的值在[0, 1]，确保投影点在线段上
            t = Mathf.Clamp01(t);
            // 计算线段上离圆心最近的点
            Vector2 closestPoint = line.Point1 + t * lineDir;
            // 计算最近点与圆心的距离
            float distanceToCircle = Vector2.Distance(closestPoint, pointPos);
            Vector2 collisionNormal = pointPos - closestPoint;
            collisionNormal.Normalize();
            // 如果距离小于或等于圆的半径，表示相交
            float depth = point.Radius - distanceToCircle;
            return new CollisionData() { CollisionNormal = collisionNormal,  Depth = depth };
        }

    }

}

