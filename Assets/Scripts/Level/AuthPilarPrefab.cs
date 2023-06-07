using System;
using Unity.Entities;
using UnityEngine;

namespace Level
{

    public class AuthPilarSpawner : MonoBehaviour
    {
        public GameObject prefab;
        public GameObject prefabLod;
        public GameObject urpEntityMat;
    }

    public class PilarSpawnerBaker : Baker<AuthPilarSpawner>
    {
        [Obsolete("Obsolete")]
        public override void Bake(AuthPilarSpawner authoring)
        {
            AddComponent( new PilarSpawnerComponent
            {
                PrefabEntity = GetEntity(authoring.prefab),
                PrefabEntityLod = GetEntity(authoring.prefabLod),
                UrpEntityMat = GetEntity(authoring.urpEntityMat),
            });
        }
    }
    
}