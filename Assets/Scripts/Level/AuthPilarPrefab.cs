using System;
using Unity.Entities;
using UnityEngine;

namespace Level
{

    public class AuthPilarSpawner : MonoBehaviour
    {
        [Header("Physics Config")]
        public GameObject prefab;
        
        [Header("Face Only Top")]
        public GameObject pilarDownLod0;
        public GameObject pilarDownLod1;
        
        [Header("Face BothFace")]
        public GameObject pilarUpLod0;
        public GameObject pilarUpLod1;
        
        [Header("Face Right")]
        public GameObject pilarUpRightLod0;
        public GameObject pilarUpRightLod1;
     
        [Header("Face Forward")]
        public GameObject pilarUpForwardLod0;
        public GameObject pilarUpForwardLod1;
        
        [Header("Only Top Up")]
        public GameObject pilarUpTopLod0;
        public GameObject pilarUpTopLod1;
    }

    public class PilarSpawnerBaker : Baker<AuthPilarSpawner>
    {
        [Obsolete("Obsolete")]
        public override void Bake(AuthPilarSpawner authoring)
        {
            AddComponent( new PillarSpawnerComponent
            {
                PrefabEntity = GetEntity(authoring.prefab),
                PilarDownLod0 = GetEntity(authoring.pilarDownLod0),
                PilarDownLod1 = GetEntity(authoring.pilarDownLod1),
                
                PilarUpLod0 = GetEntity(authoring.pilarUpLod0),
                PilarUpLod1 = GetEntity(authoring.pilarUpLod1),
                
                PilarUpRightLod0 = GetEntity(authoring.pilarUpRightLod0),
                PilarUpRightLod1 = GetEntity(authoring.pilarUpRightLod1),
                
                PilarUpForwardLod0 = GetEntity(authoring.pilarUpForwardLod0),
                PilarUpForwardLod1 = GetEntity(authoring.pilarUpForwardLod1),
                
                PilarUpTopLod0 = GetEntity(authoring.pilarUpTopLod0),
                PilarUpTopLod1 = GetEntity(authoring.pilarUpTopLod1),
            });
        }
    }
    
}