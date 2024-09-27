using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoidDirectionHelper
{

    static int NumViewDirections = 300;
    public static readonly Vector3[] Directions;

    /// <summary>
    /// ʹ�ûƽ������������ϵ�ļ��㷽ʽ����Ϊ��ȷ����Щ����������������澡�����ȵطֲ�
    /// </summary>
    static BoidDirectionHelper()
    {
        Directions = new Vector3[NumViewDirections];

        //�ƽ����
        float goldenRatio = 1.618f;
        //���ݻƽ�����õ��ĽǶ�����
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < NumViewDirections; i++)
        {
            float t = (float)i / NumViewDirections;
            //ͨ�������Һ���������� Z ��ƫ��ĽǶ�, ����б��
            float inclination = Mathf.Acos(1 - 2 * t);
            //���㷽λ��, ��Χ�� Z ����ת�ĽǶ�
            float azimuth = angleIncrement * i;

            //�����굽ֱ�������ת����ʽ(�������Ǽ���r = 1, ��Ϊ���ǹ��ĵ��Ƿ�������Ǿ���ľ���)
            //x = r * sin(��) * cos(��)
            //y = r * sin(��) * sin(��)
            //z = r * cos(��)
            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);
            Directions[i] = new Vector3(x, y, z);
        }
    }
}
