using UnityEngine;

namespace Unity.Mathematics
{
    public static class Float2
    {
        public static float3 AddY(this float2 value, float num = 0) => new(value.x, num,value.y);
        
        public static float Distance(float2 a, float2 b)
        {
            var num1 = a.x - b.x;
            var num2 = a.y - b.y;
            return math.sqrt(num1 * num1 + num2 * num2);
        }
        public static float DistanceOrthogonal(float2 a, float2 b) => Mathf.Abs(a.x - a.y) + Mathf.Abs(a.y - b.y);  
    }
}