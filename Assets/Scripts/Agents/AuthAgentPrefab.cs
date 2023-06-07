using System;
using Unity.Entities;
using UnityEngine;

namespace Level
{

    public class AuthAgentPrefab : MonoBehaviour
    {
        public GameObject prefab;
        //public GameObject prefabLod;
    }

    public class AgentSpawnerBaker : Baker<AuthAgentPrefab>
    {
        [Obsolete("Obsolete")]
        public override void Bake(AuthAgentPrefab authoring)
        {
            AddComponent( new AgentSpawnerComponent()
            {
                PrefabEntity = GetEntity(authoring.prefab),
                //PrefabEntityLod = GetEntity(authoring.prefabLod),
            });
        }
    }
    
}