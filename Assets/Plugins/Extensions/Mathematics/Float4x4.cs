using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Mathematics
{
    public static class Float4x4
    {

        public static float3 GetPosition(this float4x4 value)
            => new(value.c3.x,value.c3.y,value.c3.z);

        
        /// <summary> <para>Set the position of the matrix.</para> </summary>
        public static void SetPosition(this ref float4x4 matrix, float3 value)
            => matrix.c3 = new float4(value.x, value.y, value.z, 1);
        
        /// <summary> <para>Return a copy of the matrix with the position set as.</para> </summary>
        public static float4x4 WithPositionSet(this float4x4 matrix, float3 position)
        {
            matrix.SetPosition(position);
            return matrix;
        }


        /// <summary> <para>It is just math.inverse.</para> </summary>
        public static float4x4 inverse(this ref float4x4 matrix) => math.inverse(matrix);
        
        /// <summary> <para>Return a copy of the inverted matrix.</para> </summary>
        public static float4x4 Inverted(this float4x4 matrix) => math.inverse(matrix);

        public static float3 MultiplyVector(this ref float4x4 matrix, float3 vector)
        {
            return new float3(
                (matrix.c0.x * vector.x + matrix.c1.x * vector.y + matrix.c2.x * vector.z),
                (matrix.c0.y * vector.x + matrix.c1.y * vector.y + matrix.c2.y * vector.z),
                (matrix.c0.z * vector.x + matrix.c1.z * vector.y + matrix.c2.z* vector.z)
            );
        }


        public static float4 GetRow(this ref float4x4 matrix, int index)
        {
            return index switch
            {
                0 => new float4(matrix.c0.x, matrix.c1.x, matrix.c2.x, matrix.c3.x),
                1 => new float4(matrix.c0.y, matrix.c1.y, matrix.c2.y, matrix.c3.y),
                2 => new float4(matrix.c0.z, matrix.c1.z, matrix.c2.z, matrix.c3.z),
                3 => new float4(matrix.c0.w, matrix.c1.w, matrix.c2.w, matrix.c3.w),
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
        }
     
        public static NativeArray<float> ToArray(this ref float4x4 matrix, Allocator allocator)
        {
            return new NativeArray<float>(16, allocator)
            {
                [0] = matrix.c0.x,
                [1] = matrix.c0.y,
                [2] = matrix.c0.z,
                [3] = matrix.c0.w,

                [4] = matrix.c1.x,
                [5] = matrix.c1.y,
                [6] = matrix.c1.z,
                [7] = matrix.c1.w,
                
                [8] = matrix.c2.x,
                [9] = matrix.c2.y,
                [10] = matrix.c2.z,
                [11] = matrix.c2.w,
                
                [12] = matrix.c3.x,
                [13] = matrix.c3.y,
                [14] = matrix.c3.z,
                [15] = matrix.c3.w,
            };
        }
                
        public static float4x4 ToMatrix(NativeArray<float> array)
        {
            return new float4x4
            {
                c0 = new float4(array[0],array[1], array[2], array[3]),
                c1 = new float4(array[4],array[5], array[6], array[7]),
                c2 = new float4(array[8],array[9], array[10], array[11]),
                c3 = new float4(array[12],array[13], array[14], array[15]),
            };
        }
        
        
        
        public static NativeArray<float2> ToArrayFloat2(this ref float4x4 matrix, Allocator allocator)
        {
            return new NativeArray<float2>(8, allocator)
            {
                [0] = new (matrix.c0.x, matrix.c0.y),
                [1] = new (matrix.c0.z, matrix.c0.w),
                [2] = new (matrix.c1.x, matrix.c1.y),
                [3] = new (matrix.c1.z, matrix.c1.w),

                [4] = new (matrix.c2.x, matrix.c2.y),
                [5] = new (matrix.c2.z, matrix.c2.w),
                [6] = new (matrix.c3.x, matrix.c3.y),
                [7] = new (matrix.c3.z, matrix.c3.w),
          } ;
        }
        
        public static float4x4 ToMatrix(NativeArray<float2> array)
        {
            return new float4x4
            {
                c0 = new float4(array[0].x,array[0].y, array[1].x, array[1].y),
                c1 = new float4(array[2].x,array[2].y, array[3].x, array[3].y),
                c2 = new float4(array[4].x,array[4].y, array[5].x, array[5].y),
                c3 = new float4(array[6].x,array[6].y, array[7].x, array[7].y),
            };
        }

        
        public static NativeArray<float4> ToArrayFloat4(this ref float4x4 matrix)
        {
            return new NativeArray<float4>
            {
                [0] = matrix.c0,
                [1] = matrix.c1,
                [2] = matrix.c2,
                [3] = matrix.c3,
            };
        }
    }
}
