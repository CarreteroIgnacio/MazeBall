using System;
using Unity.Entities;
using UnityEngine;


namespace Level
{
    public class AuthPilar : MonoBehaviour
    {
        }


    public class PilarBaker : Baker<AuthPilar>
    {
        [Obsolete("Obsolete")]
        public override void Bake(AuthPilar authoring)
        {
            AddComponent(new PilarComponent());
        }
    }
    
 
}

