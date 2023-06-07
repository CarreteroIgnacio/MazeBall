using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

    public struct PilarComponent : IComponentData
    {
        public float DesiredPos;
        public float OldPos;
        public float CurrentPos;
        public int2 Cords;
        public bool IsHigh;
    }
    
    
    public struct PilarSpawnerComponent : IComponentData
    {
        public Entity PrefabEntity;
        public Entity PrefabEntityLod;
        public Entity UrpEntityMat;
    }

    public struct AgentSpawnerComponent : IComponentData
    {
        public Entity PrefabEntity;
        //public Entity PrefabEntityLod;
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
        public float DashColdown;
        public float DashCurrentCd;
            
        public float JumpForce;
        public float JumpCost;
        public float JumpColdown;
        public float JumpCurrentCd;
    }
    
    public struct AgentComponent : IComponentData
{
    public float DamageRadius;
    public float Damage;
        
    public int EntityID;
    public float Speed;

    public float ViewAngle;
    public float ViewDist;

    //public Color GizmosColor;s

    public LayerMask LevelMask;
    public float KeepDistance;
    public float SpeedMagnitude;
}