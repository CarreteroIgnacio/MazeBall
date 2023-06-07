using Inputs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace CCC
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class SysPlayerGUI : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((ref PlayerComponent playerComponent) => { CanvasManager.Instance.SetGUI(playerComponent); })
                .Run();


        }
    }


    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SysPlayer : SystemBase
    {
        private static readonly float FixedDeltaTime = 0.01666666f;
        public static SysPlayer Instance;
        private static readonly PhysicsCategoryTags LevelCategory = new() { Category00 = true };
        private static readonly PhysicsCategoryTags PlayerCategory = new() { Category01 = true };

        public static PlayerComponent playerComponentInstance;

        protected override void OnCreate()
        {
            Instance = this;
        }

        protected override void OnStartRunning()
        {
            GameManager.Instance.LevelReset += ResetPlayer;
        }

        protected override void OnUpdate()
        {

            var playerInputs = InputManager.PlayerInputs;
            var jump = playerInputs.Jump;
            var dash = playerInputs.Dash;

            var dir = (playerInputs.Wasd.y * CameraTrack.Instance.transform.forward).normalized;

            var playerComp = new PlayerComponent();


            var restitution = Mathf.Lerp(.5f, 0, playerInputs.Wasd.y);
            //Debug.Log(restitution);
            Entities
                .WithoutBurst()
                .ForEach((ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
                    ref LocalToWorld localToWorld,
                    ref PhysicsMass physicsMass, ref PhysicsCollider physicsCollider) =>
                {

                    physicsCollider.Value.Value.SetRestitution(restitution);
                    var wtlMatrix = ((Matrix4x4)localToWorld.Value).inverse;
                    var worldRot = Vector3.Cross(dir, Vector3.up).normalized;
                    var localRot = wtlMatrix.MultiplyVector(worldRot);

                    //
                    physicsVelocity.ApplyAngularImpulse(physicsMass,
                        localRot * (playerComponent.Speed * SystemAPI.Time.DeltaTime));

                    physicsVelocity.Angular =
                        Vector3.ClampMagnitude(physicsVelocity.Angular, playerComponent.SpeedMagnitude);

                    playerComponent.JumpCurrentCd -= FixedDeltaTime;
                    playerComponent.DashCurrentCd -= FixedDeltaTime;
                    if (jump) Jump(ref playerComponent, ref physicsVelocity, ref physicsMass);
                    if (dash)
                        Dash(ref playerComponent, ref localToWorld, ref physicsVelocity, ref physicsMass, localRot);


                    LooseAir(ref playerComponent);
                    GainEnergy(ref playerComponent, ref physicsVelocity, ref localToWorld);


                    playerComp = playerComponent;
                })
                .Run();

            playerComponentInstance = playerComp;
            //CanvasManager.Instance.SetGUI(playerComp);
        }


        private static void LooseAir(ref PlayerComponent playerComponent)
        {
            playerComponent.Health -= FixedDeltaTime *
                                      (1 - (playerComponent.Integrity / playerComponent.MaxIntegrity)) *
                                      playerComponent.AirLooseMultiplier;


            if (playerComponent.Health < 0)
            {
                playerComponent.Health = 0;
                GameManager.Instance.OnGameOver();
            }
        }

        private void GainEnergy(ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
            ref LocalToWorld localToWorld)
        {
            if (!RayCastToFloor(localToWorld.Position)) return;

            var amount = ((Vector3)(physicsVelocity.Linear)).magnitude;
            playerComponent.Energy += FixedDeltaTime * amount *
                                      playerComponent.EnergyGainMultiplier;


            if (playerComponent.Energy > playerComponent.MaxEnergy)
                playerComponent.Energy = playerComponent.MaxEnergy;
            
            GameManager.Instance.AddPoints(amount);
        }

        private static void Jump(ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
            ref PhysicsMass physicsMass)
        {


            if (playerComponent.Energy < playerComponent.JumpCost) return;
            if (playerComponent.JumpCurrentCd > 0) return;


            playerComponent.JumpCurrentCd = playerComponent.JumpColdown;
            playerComponent.Energy -= playerComponent.JumpCost;

            physicsVelocity.ApplyLinearImpulse(physicsMass, new float3(0, playerComponent.JumpForce, 0));
        }

        private static void Dash(ref PlayerComponent playerComponent, ref LocalToWorld localToWorld,
            ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass, Vector3 dir)
        {

            if (playerComponent.Energy < playerComponent.DashCost) return;
            if (playerComponent.DashCurrentCd > 0) return;

            playerComponent.DashCurrentCd = playerComponent.DashColdown;
            playerComponent.Energy -= playerComponent.DashCost;


            Matrix4x4 ltwMatrix = localToWorld.Value;
            var worldAngular = ltwMatrix.MultiplyVector(physicsVelocity.Angular);
            var worldForward = Vector3.Cross(Vector3.up, worldAngular).normalized;
            //worldForward.y += .05f;
            physicsVelocity.ApplyLinearImpulse(physicsMass, worldForward * playerComponent.DashForce);
        }


        public void TakeDamage(float amount, Vector3 agentPos, Vector3 playerPos)
        {
            CameraTrack.Instance.RunCameraNoise();


            var dir = agentPos - playerPos;
            dir.Normalize();
            if (!LeakPool.Instance.CreateLeak(playerPos, dir)) return;

            var currentIntegrity = new float();
            Entities
                .ForEach((ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
                    ref PhysicsMass physicsMass) =>
                {
                    physicsVelocity.ApplyLinearImpulse(physicsMass, -dir / 2);
                    playerComponent.Integrity -= amount;
                    if (playerComponent.Integrity < 0)
                        playerComponent.Integrity = 0;
                    currentIntegrity = playerComponent.Integrity / playerComponent.MaxIntegrity;
                }).Run();
            //CanvasManager.Instance.SetIntegrityBarGUI(currentIntegrity);
        }

        public void RepairIntegrity(float repair, float air, float energy)
        {
            var integrityCoef = 0f;
            Entities
                .ForEach((ref PlayerComponent playerComponent) =>
                {
                    playerComponent.Integrity += repair;
                    if (playerComponent.Integrity > playerComponent.MaxIntegrity)
                        playerComponent.Integrity = playerComponent.MaxIntegrity;


                    playerComponent.Health += air;
                    if (playerComponent.Health > playerComponent.MaxHealth)
                        playerComponent.Health = playerComponent.MaxHealth;
                    _ = playerComponent.Health / playerComponent.MaxHealth;

                    integrityCoef = playerComponent.Integrity / playerComponent.MaxIntegrity;


                    playerComponent.Energy += energy;
                    if (playerComponent.Energy > playerComponent.MaxEnergy)
                        playerComponent.Energy = playerComponent.MaxEnergy;

                }).Run();
            LeakPool.Instance.RepairLeaks(integrityCoef);
        }


        private void ResetPlayer()
        {
            
            
            Entities
                .WithoutBurst()
                .ForEach((ref LocalTransform localTransform,
                    ref PhysicsVelocity physicsVelocity, ref PlayerComponent playerComponent) =>
                {
                    localTransform.Position = new float3(0,16,0);
                    localTransform.Rotation = quaternion.identity;
                    
                    physicsVelocity.Angular = float3.zero;
                    physicsVelocity.Linear = float3.zero;

                    playerComponent.Health = playerComponent.MaxHealth;
                    playerComponent.Integrity = playerComponent.MaxIntegrity;
                    playerComponent.Energy = 0;

                }).Run();
            LeakPool.Instance.RepairLeaks(1);
           
        }

        private bool RayCastToFloor(float3 start)
        {
            var end = start - new float3(0, .6f, 0);
            var colFilter = new CollisionFilter
            {
                BelongsTo = PlayerCategory.Value,
                CollidesWith = LevelCategory.Value,
                GroupIndex = 0
            };


            var rayInput = new RaycastInput
            {
                Start = start,
                End = end,
                Filter = colFilter
            };

            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;
            collisionWorld.CastRay(rayInput, out var hit);

            Debug.DrawLine(start, end, Color.red);
            return EntityManager.HasComponent<PilarComponent>(hit.Entity);
        }
    }
}