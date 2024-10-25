using UnityEngine;

namespace BoidsSimulator
{
    public class BoidsMono : MonoBehaviour
    {
        [HideInInspector]
        public Vector3 Position;
        [HideInInspector]
        public Vector3 Forward;
        [HideInInspector]
        public Vector3 FlockHeading; //当前Boid感知到的所有邻居的方向总和
        [HideInInspector]
        public Vector3 FlockCentre; //当前Boid感知到的所有邻居的位置总和
        [HideInInspector]
        public Vector3 AvoidanceHeading; //当前Boid感知到的所有邻居的分离方向总和
        [HideInInspector]
        public int NumFlockmates; //当前Boid感知到的邻居数量

        BoidsDataMono _boidsData;
        Transform _target; //当前Boid目标
        Vector3 _velocity;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        public void Initialize(BoidsDataMono boidsData, Transform target)
        {
            _boidsData = boidsData;
            _target = target;

            Position = transform.position;
            Forward = transform.forward;

            float startSpeed = (_boidsData.minSpeed + _boidsData.maxSpeed) / 2;
            _velocity = transform.forward * startSpeed;

        }

        public void UpdateBoid()
        {
            Vector3 acceleration = Vector3.zero;

            if (_target != null)
            {
                Vector3 offsetToTarget = (_target.position - Position);
                acceleration = SteerTowards(offsetToTarget) * _boidsData.targetWeight;
            }

            if (NumFlockmates != 0)
            {
                //位置总和除以邻居数量得到邻居平均位置
                Vector3 centreOfFlockmates = FlockCentre / NumFlockmates;
                //当前位置指向邻居平均位置向量
                Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - Position);

                //分别得到对齐,聚合,分离
                var alignmentForce = SteerTowards(FlockHeading) * _boidsData.alignWeight;
                var cohesionForce = SteerTowards(offsetToFlockmatesCentre) * _boidsData.cohesionWeight;
                var seperationForce = SteerTowards(AvoidanceHeading) * _boidsData.seperateWeight;

                acceleration += alignmentForce;
                acceleration += cohesionForce;
                acceleration += seperationForce;
            }
            if (IsHeadingForCollision())
            {
                Vector3 collisionAvoidDir = GetAvoidDir();
                Vector3 collisionAvoidForce = SteerTowards(collisionAvoidDir) * _boidsData.avoidCollisionWeight;
                acceleration += collisionAvoidForce;
            }

            _velocity += acceleration * Time.deltaTime;
            //使用最大最小速度约束当前速度
            float speed = _velocity.magnitude;
            Vector3 dir = _velocity / speed;
            speed = Mathf.Clamp(speed, _boidsData.minSpeed, _boidsData.maxSpeed);
            _velocity = dir * speed;

            transform.position += _velocity * Time.deltaTime;
            transform.forward = dir;
            Position = transform.position;
            Forward = dir;

        }


        bool IsHeadingForCollision()
        {
            RaycastHit hit;
            if (Physics.SphereCast(
                Position, _boidsData.boundsRadius, Forward, out hit, _boidsData.collisionAvoidDst, _boidsData.obstacleMask))
            {
                return true;
            }
            return false;
        }

        Vector3 GetAvoidDir()
        {
            Vector3[] rayDirections = BoidDirectionHelper.GetDirectionsVector3S();

            for (int i = 0; i < rayDirections.Length; i++)
            {
                Vector3 dir = transform.TransformDirection(rayDirections[i]);
                Ray ray = new Ray(Position, dir);
                if (!Physics.SphereCast(ray, _boidsData.boundsRadius, _boidsData.collisionAvoidDst, _boidsData.obstacleMask))
                {
                    return dir;
                }
            }
            return Forward;
        }

        Vector3 SteerTowards(Vector3 vector)
        {
            //目标方向和当前速度差值以得到所需的加速度向量
            Vector3 v = vector.normalized * _boidsData.maxSpeed - _velocity;
            return Vector3.ClampMagnitude(v, _boidsData.maxSteerForce);
        }

    }

}
