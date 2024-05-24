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
        public NativeArray<Vector3> TargetPoints;
        public NativeArray<Matrix4x4> Matrices;
        public NativeArray<PackedMatrix> Obj2WorldArr;
        public NativeArray<PackedMatrix> World2ObjArr;

        public Unity.Mathematics.Random random;
        public float DeltaTime;

        public void Execute(int index)
        {
            Vector3 curPos = Matrices[index].GetPosition();
            Vector3 dir = TargetPoints[index] - curPos;
            if (Unity.Mathematics.math.lengthsq(dir) < 0.4f)
            {
                Vector3 newTargetPos = TargetPoints[index];
                random.InitState((uint)(DateTime.Now.Ticks + index));
                newTargetPos.x = random.NextFloat(RandomPostionRange.x, RandomPostionRange.y);
                newTargetPos.z = random.NextFloat(RandomPostionRange.z, RandomPostionRange.w);
                TargetPoints[index] = newTargetPos;
                //Debug.Log($"新的随机位置为:{newTargetPos}");
            }
            dir = math.normalizesafe(TargetPoints[index] - curPos, Vector3.forward);
            curPos += dir * DeltaTime;

            var mat = Matrices[index];
            mat.SetTRS(curPos, Quaternion.LookRotation(dir), Vector3.one);
            Matrices[index] = mat;

            PackedMatrix ow = Obj2WorldArr[index];
            ow.SetData(mat);
            Obj2WorldArr[index] = ow;
            PackedMatrix wo = World2ObjArr[index];
            wo.SetData(mat.inverse);
            World2ObjArr[index] = wo;

        }

    }

}

