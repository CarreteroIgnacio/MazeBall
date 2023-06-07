using System;

using Unity.Entities;
using UnityEngine;


namespace CCC
{
    public class AuthPlayer : MonoBehaviour
    {
        [Header("Stats Settings")]
            public float speed;
            public float speedMagnitude;
            public float health = 90;
            public float maxHealth = 100;
        
        [Header("Integrity Settings")]
            public float integrity;
            public float maxIntegrity;
            public float airLooseMultiplier;

        [Header("Energy Settings")]
            public float energy;
            public float maxEnergy;
            public float energyGainMultiplier;
        
        [Header("Dash Settings")]
            public float dashForce;
            public float dashCost;
            public float dashColdown;
        
        [Header("Jump Settings")]
            public float jumpForce;
            public float jumpCost;
            public float jumpColdown;
        
    }


    public class PlayerBaker : Baker<AuthPlayer>
    {
        [Obsolete("Obsolete")]
        public override void Bake(AuthPlayer authoring)
        {
            AddComponent(new PlayerComponent
            {
                
                Speed = authoring.speed,
                SpeedMagnitude = authoring.speedMagnitude,
                Health = authoring.health,
                MaxHealth = authoring.maxHealth,
                
                Integrity = authoring.integrity,
                MaxIntegrity = authoring.maxIntegrity,
                AirLooseMultiplier = authoring.airLooseMultiplier,
                
                Energy = authoring.energy,
                MaxEnergy = authoring.maxEnergy,
                EnergyGainMultiplier = authoring.energyGainMultiplier,
                    
                DashForce = authoring.dashForce,
                DashCost = authoring.dashCost,
                DashColdown = authoring.dashColdown,
                DashCurrentCd = 0,
                
                JumpForce = authoring.jumpForce,
                JumpCost = authoring.jumpCost,
                JumpColdown = authoring.jumpColdown,
                JumpCurrentCd = 0,
                
                
                
            });
        }
    }
}