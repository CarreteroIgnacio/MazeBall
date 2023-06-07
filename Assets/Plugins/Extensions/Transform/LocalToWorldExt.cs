using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Transforms
{
    public static class LocalToWorldExt
    {
        
        public static void SetPosition(this ref LocalToWorld localToWorld, float3 value)
            => localToWorld.Value.c3 = new float4(value.x, value.y, value.z, 1);
        
        public static void SetPositionX(this ref LocalToWorld localToWorld, float value) => 
            localToWorld.Value.c3.x = value;
        
        public static void SetPositionY(this ref LocalToWorld localToWorld, float value) => 
            localToWorld.Value.c3.y = value;
        
        public static void SetPositionZ(this ref LocalToWorld localToWorld, float value) => 
            localToWorld.Value.c3.z = value;



        
        public static void SetVectorRight(this ref LocalToWorld localToWorld, float3 value) =>
            localToWorld.Value.c0 = new float4(value.x, value.y, value.z, 0);
        
        public static void SetVectorUp(this ref LocalToWorld localToWorld, float3 value) =>
            localToWorld.Value.c1 = new float4(value.x, value.y, value.z, 0);
        
        public static void SetVectorForward(this ref LocalToWorld localToWorld, float3 value) =>
            localToWorld.Value.c2 = new float4(value.x, value.y, value.z, 0);
        

    }
}