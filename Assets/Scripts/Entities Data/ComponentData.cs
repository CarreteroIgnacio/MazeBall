using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

    public struct PilarComponent : IComponentData
    {
        public float DesiredPos;
        public float OldPos;
        public float CurrentPos;
        public int2 Cords;
        public bool IsHighest;
    }//
    
    
    public struct PillarSpawnerComponent : IComponentData
    {
        public Entity PrefabEntity;
        
        public Entity PilarDownLod0;
        public Entity PilarDownLod1;
        
        public Entity PilarUpLod0;
        public Entity PilarUpLod1;
        
        public Entity PilarUpRightLod0;
        public Entity PilarUpRightLod1;
        
        public Entity PilarUpForwardLod0;
        public Entity PilarUpForwardLod1;
        
        public Entity PilarUpTopLod0;
        public Entity PilarUpTopLod1;
    }

    public struct AgentSpawnerComponent : IComponentData
    {
        public Entity PrefabEntity;
        //public Entity PrefabEntityLod;
    }


    public struct CollectableComponent : IComponentData
    {
        public int Points;
        public float3 StaticPos;
        public float Healing;
        public float Air;
        public float Energy;
        public bool IsValid;
    }
    
    public struct GridMakerComponent : IComponentData
    {
        public int CellAmount;
        public float GridScale;
        public float GridHeight;
        public Vector3 LmPosition;
        public float MapTransitionSpeed;
    }


    public struct PlayerComponent : IComponentData
    {
        public float Speed;
        public float Health;
        public float MaxHealth;
        public float SpeedMagnitude;
            
        public float Integrity;
        public float MaxIntegrity;
        public float AirLooseMultiplier;

        public float Energy;
        public float MaxEnergy;
        public float EnergyGainMultiplier;
            
        public float DashForce;
        public float DashCost;
        public float DashCooldown;
        public float DashCurrentCd;
            
        public float JumpForce;
        public float JumpCost;
        public float JumpCooldown;
        public float JumpCurrentCd;
        
        public bool IsGrounded;
        public float PointsThisFrame;
    }
    
    public struct AgentComponent : IComponentData
{
    public float DamageRadius;
    public float Damage;
        
    public int EntityID;
    public float Speed;

    public float ViewAngle;
    public float ViewDist;

    public int IndexPath;
    public int NodeAmount;
    public float4x4 NodePath;

    public bool IsValid;

    public float3 Position;
    //public Color GizmosColor;s

    public LayerMask LevelMask;
    public float KeepDistance;
    public float SpeedMagnitude;
    public AgentState CurrentState;
}

public enum AgentState
{
    Null,
    ChasePlayer,
    FollowNode,
}