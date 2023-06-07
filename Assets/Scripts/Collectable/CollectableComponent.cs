using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace Collectable
{
    public struct CollectableComponent : IComponentData
    {
        public int Points;
        public float3 StaticPos;
        public float Healing;
        public float Air;
        public float Energy;
    }
}