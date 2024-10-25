

using Unity.Mathematics;

namespace BoidsECSSimulator
{
    public static class CommonFunction
    {
        public static float3 SteerTowards(float3 vector, float maxSpeed, float3 velocity, float maxSteerForce)
        {
            //目标方向和当前速度差值以得到所需的加速度向量
            float3 v = Normalize(vector) * maxSpeed - velocity;
            return ClampMagnitude(v, maxSteerForce);
        }

        public static float3 Normalize(float3 value)
        {
            float num = (float)math.sqrt(value.x * value.x + value.y * value.y + value.z * value.z);
            if (num > 1E-05f)
                return value / num;
            return float3.zero;
        }

        public static float3 ClampMagnitude(float3 vector, float maxLength)
        {
            float num = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
            if (num > maxLength * maxLength)
            {
                float num2 = (float)math.sqrt(num);
                if (num2 > 0)
                {
                    float num3 = vector.x / num2;
                    float num4 = vector.y / num2;
                    float num5 = vector.z / num2;
                    return new float3(num3 * maxLength, num4 * maxLength, num5 * maxLength);
                }
            }
            return vector;
        }

    }

}
