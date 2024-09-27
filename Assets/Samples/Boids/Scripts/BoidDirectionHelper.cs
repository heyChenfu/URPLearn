using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoidDirectionHelper
{

    static int NumViewDirections = 300;
    public static readonly Vector3[] Directions;

    /// <summary>
    /// 使用黄金比例和球坐标系的计算方式，是为了确保这些方向向量在球体表面尽量均匀地分布
    /// </summary>
    static BoidDirectionHelper()
    {
        Directions = new Vector3[NumViewDirections];

        //黄金比例
        float goldenRatio = 1.618f;
        //根据黄金比例得到的角度增量
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < NumViewDirections; i++)
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
            Directions[i] = new Vector3(x, y, z);
        }
    }
}
