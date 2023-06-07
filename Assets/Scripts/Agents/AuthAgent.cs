using System;
using UnityEngine;
using Unity.Entities;

namespace Path
{
    public class AuthAgent : MonoBehaviour
    {
        [Header("Stats")]
        public float speed = 1;
        public float speedMagnitude = 1;

        [Header("Field of View")]
        public float viewAngle = 45f;
        public float viewDist = 2;


        [Header("Stuffs xD")][Tooltip("Must select Walls and Player Layer")]
        public LayerMask levelMask;
        public float keepDistance = 1;

        [Header("Damage Stats")] 
        public float damageRadius = 1f;
        public float damage = .1f;
    }


    public class AgentBaker : Baker<AuthAgent>
    {
        [Obsolete("Obsolete")]
        public override void Bake(AuthAgent authoring)
        {
            AddComponent(new AgentComponent
            {
                Speed = authoring.speed,
                SpeedMagnitude = authoring.speedMagnitude,
                ViewAngle = authoring.viewAngle,
                ViewDist = authoring.viewDist,
                LevelMask = authoring.levelMask,
                KeepDistance = authoring.keepDistance,
                Damage = authoring.damage,
                DamageRadius = authoring.damageRadius
            });
        }
    }
}
