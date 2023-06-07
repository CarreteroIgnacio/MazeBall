using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace CCC
{

    //[UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class CameraTrackSystem : SystemBase
    {
        public static CameraTrackSystem Instance;
        protected override void OnStartRunning() => Instance = this;

        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((ref LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, in PlayerComponent playerComponent) =>
                {
                    CameraTrack.Instance.SetPosition(localToWorld.Position, localToWorld.Rotation, physicsVelocity.Angular);
                    
                    
                    CameraTrack.Instance.SetSpeedTrailDirection(physicsVelocity.Linear);
                }).Run();
        }
    }
}