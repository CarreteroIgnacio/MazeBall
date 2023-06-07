using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Collectable
{
    public class AuthCollectable : MonoBehaviour
    {
        public int points;
        public float healing = 50f;
        public float air = 50f;
        public float energy = 100;
    }


    public class AgentBaker : Baker<AuthCollectable>
    {
        [Obsolete("Obsolete")]
        public override void Bake(AuthCollectable authoring)
        {
            AddComponent(new CollectableComponent
            {
                Points = authoring.points,
                Healing = authoring.healing,
                Air = authoring.air,
                Energy = authoring.energy
            });
        }
    }
}