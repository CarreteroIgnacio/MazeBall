using Unity.Mathematics;
using UnityEngine;

namespace Unity.Mathematics
{
    public static class Float3
    {
        public static readonly float3 right = new(1, 0, 0);
        public static readonly float3 up = new(0, 1, 0);
        public static readonly float3 forward = new(0, 0, 1);
        public static readonly float3 left = new(-1, 0, 0);
        public static readonly float3 down = new(0, -1, 0);
        public static readonly float3 back = new(0, 0, -1);

        public static readonly float3 zero = new(0, 0, 0);
        public static readonly float3 one = new(1, 1, 1);

        public static float3 normalized(this float3 value)
        {
            var num = value.Magnitude();
            return num > 9.999999747378752E-06 ? value / num : float3.zero;
        }

        public static void Normalize(this ref float3 value)
        {
            var num = value.Magnitude();
            value = num > 9.999999747378752E-06 ? value /= num : float3.zero;
            ;
        }

        public static float Magnitude(this float3 vector) => Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);


        public static float Distance(float3 a, float3 b)
        {
            var num1 = a.x - b.x;
            var num2 = a.y - b.y;
            var num3 = a.z - b.z;
            return Mathf.Sqrt(num1 * num1 + num2 * num2 + num3 * num3);
        }

        /// <summary> <para>Returns the orthogonal distance, (without diagonals) between X & z.</para> </summary>
        public static float DistanceOrthogonalXZ(float3 a, float3 b) => Mathf.Abs(a.x - a.y) + Mathf.Abs(a.z - b.z);

        /// <summary> <para>Returns a the direction normalized.</para> </summary>
        public static float3 Direction(float3 from, float3 to)
        {
            return (to - from).normalized();
        }

        public static float Dot(float3 lhs, float3 rhs) => lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;

        public static float Angle(float3 from, float3 to)
        {
            var num = Mathf.Sqrt(from.sqrMagnitude() * to.sqrMagnitude());
            return num < 1.0000000036274937E-15 ? 0.0f : Mathf.Acos(Mathf.Clamp(Vector3.Dot(from, to) / num, -1f, 1f)) * 57.29578f;
        }

        public static float3 Cross(float3 lhs, float3 rhs) => new (lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x);


        public static float3 ClampMagnitude(float3 vector, float maxLength)
        {
            var sqrMagnitude = vector.sqrMagnitude();
            if (sqrMagnitude <= maxLength * maxLength)
                return vector;
            var num1 = Mathf.Sqrt(sqrMagnitude);
            var num2 = vector.x / num1;
            var num3 = vector.y / num1;
            var num4 = vector.z / num1;
            return new Vector3(num2 * maxLength, num3 * maxLength, num4 * maxLength);
        }

        public static float magnitude(this float3 value) => Mathf.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z);

        public static float sqrMagnitude(this float3 value)
        {
            return value.x * value.x + value.y * value.y + value.z * value.z;
        }



        /// <summary> <para>Returns a quadrant direction from A to B.</para> </summary>
        public static float3 RelativeDirection(float3 from, float3 to) =>
            new(
                from.x < to.x ? 1 : -1,
                from.y < to.y ? 1 : -1,
                from.z < to.z ? 1 : -1
            );

        public static float3 RelativeDirectionNonY(float3 from, float3 to) =>
            new(
                from.x < to.x ? 1 : -1,
                0,
                from.z < to.z ? 1 : -1
            );



        /// <summary> <para>Change the X axis to 0.</para> </summary>
        public static void DoZeroX(this ref float3 value) => value.x = 0;

        /// <summary> <para>Returns a copy of float3 with the X axis at 0.</para> </summary>
        public static float3 WithZeroX(this float3 value)
        {
            value.x = 0;
            return value;
        }

        /// <summary> <para>Change the Y axis to 0.</para> </summary>
        public static void DoZeroY(this ref float3 value) => value.y = 0;

        /// <summary> <para>Returns a copy of float3 with the Y axis at 0.</para> </summary>
        public static float3 WithZeroY(this float3 value)
        {
            value.y = 0;
            return value;
        }
        
        public static float2 RemoveY(this float3 value) => new(value.x, value.z);
        


        /// <summary> <para>Change the Z axis to 0.</para> </summary>
        public static void DoZeroZ(this ref float3 value) => value.z = 0;

        /// <summary> <para>Returns a copy of float3 with the Z axis at 0.</para> </summary>
        public static float3 WithZeroZ(this float3 value)
        {
            value.z = 0;
            return value;
        }
    }

}
