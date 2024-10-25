using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

public class BoidDirectionHelper
{
    static readonly int NumViewDirections = 300;
    //黄金比例
    static readonly float GoldenRatio = 1.618f;

    /// <summary>
    /// 使用黄金比例和球坐标系的计算方式，是为了确保这些方向向量在球体表面尽量均匀地分布
    /// </summary>
    public static Vector3[] GetDirectionsVector3S()
    {
        var directions = new Vector3[NumViewDirections];

        //根据黄金比例得到的角度增量
        float angleIncrement = Mathf.PI * 2 * GoldenRatio;
        for (int i = 0; i < NumViewDirections; i++)
        {
            Tuple<float, float, float> singleDirection = GetSingleDirection(i, angleIncrement);
            directions[i] = new Vector3(singleDirection.Item1, singleDirection.Item2, singleDirection.Item3);
        }
        return directions;
    }
    
    public static NativeArray<float3> GetDirectionsFloat3S()
    {
        NativeArray<float3> directions = new NativeArray<float3>(
            NumViewDirections, Allocator.TempJob, NativeArrayOptions.ClearMemory);
        
        //根据黄金比例得到的角度增量
        float angleIncrement = Mathf.PI * 2 * GoldenRatio;
        for (int i = 0; i < NumViewDirections; i++)
        {
            Tuple<float, float, float> singleDirection = GetSingleDirection(i, angleIncrement);
            directions[i] = new float3(singleDirection.Item1, singleDirection.Item2, singleDirection.Item3);
        }
        return directions;
    }

    private static Tuple<float, float, float> GetSingleDirection(int i, float angleIncrement)
    {
        float t = (float)i / NumViewDirections;
        //通过反余弦函数计算出从 Z 轴偏离的角度, 即倾斜角
        float inclination = Mathf.Acos(1 - 2 * t);
        //计算方位角, 即围绕 Z 轴旋转的角度
        float azimuth = angleIncrement * i;

        //球坐标到直角坐标的转换公式(这里我们假设r = 1, 因为我们关心的是方向而不是具体的距离)
        //x = r * sin(θ) * cos(φ)
        //y = r * sin(θ) * sin(φ)
        //z = r * cos(θ)
        float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
        float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
        float z = Mathf.Cos(inclination);
        return new Tuple<float, float, float>(x, y, z);
    }
    
}
