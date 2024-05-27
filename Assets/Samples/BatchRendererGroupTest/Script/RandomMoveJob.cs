using RVO;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BatchRendererGroupTest
{
    public struct RandomMoveJob : IJobParallelFor
    {
        public float4 RandomPostionRange;
        //public NativeArray<Vector3> TargetPoints;
        //public NativeArray<Matrix4x4> Matrices;
        public NativeArray<AgentData> AgentDataArr;
        public NativeArray<PackedMatrix> Obj2WorldArr;
        public NativeArray<PackedMatrix> World2ObjArr;

        public Unity.Mathematics.Random random;
        public float DeltaTime;

        public void Execute(int index)
        {
            AgentData agentData = AgentDataArr[index];
            RVO.Vector2 rvoPos = Simulator.Instance.getAgentPosition(agentData.AgentId);
            Vector3 curPos = new Vector3(rvoPos.x(), 0, rvoPos.y());
            Vector3 dir = agentData.TargetPosition - curPos;
            Vector3 newTargetPos = agentData.TargetPosition;

            if (Unity.Mathematics.math.lengthsq(dir) < 0.2f)
            {
                //随机新的位置
                random.InitState((uint)(DateTime.Now.Ticks + index));
                newTargetPos.x = random.NextFloat(RandomPostionRange.x, RandomPostionRange.y);
                newTargetPos.z = random.NextFloat(RandomPostionRange.z, RandomPostionRange.w);
                agentData.TargetPosition = newTargetPos;
                //Debug.Log($"新的随机位置为:{newTargetPos}");
            }
            //移动
            //dir = math.normalizesafe(newTargetPos - curPos, Vector3.forward);
            //curPos += dir * DeltaTime;
            //RVO移动
            float moveSpeed = 0.08f;
            RVO.Vector2 goalVector = new RVO.Vector2(newTargetPos.x, newTargetPos.z) - rvoPos;
            //if (RVOMath.absSq(goalVector) > 1.0f)
                goalVector = RVOMath.normalize(goalVector);
            //添加随机扰动
            float angle = (float)random.NextDouble() * 2.0f * (float)Math.PI;
            float dist = (float)random.NextDouble() * 0.0001f;
            Simulator.Instance.setAgentPrefVelocity(agentData.AgentId, goalVector * moveSpeed +
                    dist * new RVO.Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));

            agentData.Matrix.SetTRS(curPos, Quaternion.LookRotation(new Vector3(goalVector.x(), 0, goalVector.y())), Vector3.one);
            AgentDataArr[index] = agentData;

            Obj2WorldArr[index] = new PackedMatrix(agentData.Matrix);
            World2ObjArr[index] = new PackedMatrix(agentData.Matrix.inverse);

        }

    }

}

