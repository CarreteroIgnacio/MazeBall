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


    //[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SysPlayer : SystemBase
    {
        private static readonly float FixedDeltaTime = 0.01666666f;
        public static SysPlayer Instance;
        private static readonly PhysicsCategoryTags LevelCategory = new() { Category00 = true };
        private static readonly PhysicsCategoryTags PlayerCategory = new() { Category01 = true };


        protected override void OnCreate()
        {
            Instance = this;
        }

        protected override void OnStartRunning()
        {
            if (GameManager.Instance is null)
            {
                Debug.LogError("GameManager is Null");
                Enabled = false;
                return;
            }
            GameManager.LevelReset += ResetPlayer;
            SystemManager.CollectableEvent += RepairIntegrity;
            SystemManager.PauseEvent += PauseSystem;////
        }

        private void PauseSystem(bool active) => Enabled = active;
        protected override void OnUpdate()
        {
            if(!Enabled)return;
            
            var playerInputs = InputManager.PlayerInputs;
            var jump = playerInputs.Jump;
            var dash = playerInputs.Dash;

            var dir = (playerInputs.Wasd.y * CameraTrack.Instance.transform.forward).normalized;

            var playerComp = new PlayerComponent();
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;

            
            var restitution = Mathf.Lerp(.5f, 0, playerInputs.Wasd.y);
            //Debug.Log(restitution);
            Entities
                //.WithoutBurst()
                .ForEach((ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
                    ref LocalToWorld localToWorld,
                    ref PhysicsMass physicsMass, ref PhysicsCollider physicsCollider) =>
                {

                    physicsCollider.Value.Value.SetRestitution(restitution);
                    var wtlMatrix = localToWorld.Value.Inverted();
                    var worldRot = Float3.Cross(dir, Float3.up).normalized();
                    var localRot = wtlMatrix.MultiplyVector(worldRot);

                    //
                    physicsVelocity.ApplyAngularImpulse(physicsMass,
                        localRot * (playerComponent.Speed * SystemAPI.Time.DeltaTime));

                    physicsVelocity.Angular =
                        Float3.ClampMagnitude(physicsVelocity.Angular, playerComponent.SpeedMagnitude);

                    playerComponent.JumpCurrentCd -= SystemAPI.Time.fixedDeltaTime;
                    playerComponent.DashCurrentCd -= SystemAPI.Time.fixedDeltaTime;
                    if (jump) Jump(ref playerComponent, ref physicsVelocity, ref physicsMass);
                    if (dash)
                        Dash(ref playerComponent, ref localToWorld, ref physicsVelocity, ref physicsMass, localRot);


                    LooseAir(ref playerComponent);
                    GainEnergy(ref playerComponent, ref physicsVelocity, ref localToWorld, ref collisionWorld);


                    playerComp = playerComponent;
                })
                .Run();
            
            if (playerComp.Health < 0)
            {
                playerComp.Health = 0;
                GameManager.OnGameOver();
            }

            if (playerComp.IsGrounded)
            {
                GameManager.Instance.AddPoints(playerComp.PointsThisFrame);
            }
        }


        private static void LooseAir(ref PlayerComponent playerComponent)
        {
            playerComponent.Health -= FixedDeltaTime *
                                      (1 - (playerComponent.Integrity / playerComponent.MaxIntegrity)) *
                                      playerComponent.AirLooseMultiplier;
        }

        private static void GainEnergy(ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
            ref LocalToWorld localToWorld, ref CollisionWorld collisionWorld)
        {
            playerComponent.IsGrounded = RayCastToFloor(localToWorld.Position, ref collisionWorld);
            if (!playerComponent.IsGrounded) return;

            var amount = physicsVelocity.Linear.magnitude();
            playerComponent.Energy += FixedDeltaTime * amount *
                                      playerComponent.EnergyGainMultiplier;


            if (playerComponent.Energy > playerComponent.MaxEnergy)
                playerComponent.Energy = playerComponent.MaxEnergy;

            playerComponent.PointsThisFrame = amount;

        }

        private static void Jump(ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
            ref PhysicsMass physicsMass)
        {


            if (playerComponent.Energy < playerComponent.JumpCost) return;
            if (playerComponent.JumpCurrentCd > 0) return;


            playerComponent.JumpCurrentCd = playerComponent.JumpCooldown;
            playerComponent.Energy -= playerComponent.JumpCost;

            physicsVelocity.ApplyLinearImpulse(physicsMass, new float3(0, playerComponent.JumpForce, 0));
        }

        private static void Dash(ref PlayerComponent playerComponent, ref LocalToWorld localToWorld,
            ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass, float3 dir)
        {

            if (playerComponent.Energy < playerComponent.DashCost) return;
            if (playerComponent.DashCurrentCd > 0) return;

            playerComponent.DashCurrentCd = playerComponent.DashCooldown;
            playerComponent.Energy -= playerComponent.DashCost;

            
            var worldAngular = localToWorld.Value.MultiplyVector(physicsVelocity.Angular);
            var worldForward = Float3.Cross(Float3.up, worldAngular).normalized();
            physicsVelocity.ApplyLinearImpulse(physicsMass, worldForward * playerComponent.DashForce);
        }


        public void TakeDamage(float amount, float3 agentPos)
        {
            CameraTrack.Instance.RunCameraNoise();


            

            var dir = new float3();
            var currentIntegrity = new float();// cualquiera el timer para recibir daño lo maneja el leak xd
            if (!LeakPool.Instance.CreateLeak(dir)) return;
            Entities
                .ForEach((ref PlayerComponent playerComponent, ref PhysicsVelocity physicsVelocity,
                    ref PhysicsMass physicsMass, in LocalToWorld localToWorld) =>
                {
                    dir = (agentPos - localToWorld.Position).normalized();;
                    physicsVelocity.ApplyLinearImpulse(physicsMass, -dir);
                    playerComponent.Integrity -= amount;
                    if (playerComponent.Integrity < 0)
                        playerComponent.Integrity = 0;
                    currentIntegrity = playerComponent.Integrity / playerComponent.MaxIntegrity;
                }).Run();
            
            //CanvasManager.Instance.SetIntegrityBarGUI(currentIntegrity);
        }

        public void RepairIntegrity(CollectableComponent collectableComponent)
        {
            var integrityCoef = 0f;
            Entities
                .ForEach((ref PlayerComponent playerComponent) =>
                {
                    playerComponent.Integrity += collectableComponent.Healing;
                    if (playerComponent.Integrity > playerComponent.MaxIntegrity)
                        playerComponent.Integrity = playerComponent.MaxIntegrity;


                    playerComponent.Health += collectableComponent.Air;
                    if (playerComponent.Health > playerComponent.MaxHealth)
                        playerComponent.Health = playerComponent.MaxHealth;
                    _ = playerComponent.Health / playerComponent.MaxHealth;

                    integrityCoef = playerComponent.Integrity / playerComponent.MaxIntegrity;


                    playerComponent.Energy += collectableComponent.Energy;
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

        private static bool RayCastToFloor(float3 start, ref CollisionWorld collisionWorld)
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

            
            collisionWorld.CastRay(rayInput, out var hit);

            Debug.DrawLine(start, end, Color.red);
            return hit.Entity != Entity.Null;
        }
    }
}