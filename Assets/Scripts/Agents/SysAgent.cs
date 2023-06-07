using System.Collections.Generic;
using CCC;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;


namespace Path
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SysAgent : SystemBase
    {

        #region Local Vars

            public static SysAgent Instance;

            private static readonly PhysicsCategoryTags LevelCategory = new() { Category00 = true };
            private static readonly PhysicsCategoryTags AgentCategory = new() { Category02 = true };
            private static readonly PhysicsCategoryTags LevelPlayerCategory = new() { Category00 = true, Category01 = true };

            private NativeList<Entity> _allAgents;

            private Entity _prefabAgentEntity;

            public delegate void Nekon();
            public delegate void DelegateState(
                ref AgentComponent agentComponent,
                ref LocalToWorld localToWorld,
                ref PhysicsVelocity physicsVelocity,
                ref PhysicsMass physicsMass);

            private static Dictionary<int, EntityDelegate> _allDelegates;

            private float3 _playerPos;

            private bool _validStart;
            private bool _alreadyStarted;
        #endregion

        


        public class EntityDelegate // As Class work fine :D no performance issue
        {
            public Entity AgentEntity;
            public DelegateState CurrentState;
            public NativeList<Vector3> PathList;
            public float3 Position;

            public void PathJob(float3 position, float3 target)
            {
                PathList.Clear();
                PathList.AddRange(Path2D.GetPath(position, target));
            }
        }

        #region Event Functions

            public void EnableSystem(bool active) => Enabled = _validStart = active;
            public void PauseSystem(bool active) => Enabled = active;

            protected override void OnCreate()
            {
                Instance = this;

                if(_allDelegates == null)
                    _allDelegates = new Dictionary<int, EntityDelegate>();
                else
                    _allDelegates.Clear();
            }

            protected override void OnStartRunning()
            {
                if (!_validStart)
                {
                    Enabled = false;
                    return;
                }
                if(_alreadyStarted)return;

                GameManager.Instance.LevelChanging += SpawnEnemy;
                GameManager.Instance.LevelReset += ResetAgent;
                
                
                _allAgents = new NativeList<Entity>(Allocator.Persistent);
                _prefabAgentEntity = SystemAPI.GetSingleton<AgentSpawnerComponent>().PrefabEntity;
                //EntityCleanUp();

                SpawnEnemy(new float3(16, 10,16));
                SpawnEnemy(new float3(-16, 10,16));
                SpawnEnemy(new float3(16, 10,-16));
                SpawnEnemy(new float3(-16, 10,-16));

                var count = 0;

                Entities
                    .WithoutBurst()
                    .ForEach((Entity entity, ref AgentComponent agentComponent, ref LocalToWorld localToWorld) =>
                    {
                        agentComponent.EntityID = count;
                        _allDelegates.Add(count, new EntityDelegate());

                        _allDelegates[count] = new EntityDelegate
                        {
                            AgentEntity = entity,
                            PathList = new NativeList<Vector3>(Allocator.Persistent),
                        };
                        if (CanSeePlayer(agentComponent, localToWorld))
                            _allDelegates[count].CurrentState += ChasePlayerState;
                        else
                        {
                            GetRandomPath(agentComponent, localToWorld);
                            _allDelegates[count].CurrentState = null;
                            _allDelegates[count].CurrentState += FollowNodeState;
                        }

                        count++;

                    }).Run();

                GizmosManager.Instance.DrawGizmos -= MyGizmos;
                GizmosManager.Instance.DrawGizmos += MyGizmos;

                _alreadyStarted = true;
            }


            protected override void OnUpdate()
            {
                Entities
                    .WithoutBurst()
                    .ForEach((ref LocalToWorld localToWorld, in PlayerComponent playerComponent) =>
                    {
                        _playerPos = localToWorld.Position;
                    }).Run();



                Entities
                    .WithoutBurst()
                    .ForEach((ref AgentComponent agentComponent, ref LocalToWorld localToWorld,
                        ref PhysicsVelocity physicsVelocity,
                        ref PhysicsMass physicsMass) =>
                    {
                        if (Vector3.Distance(_playerPos, localToWorld.Position) < agentComponent.DamageRadius)
                        {
                            SysPlayer.Instance.TakeDamage(agentComponent.Damage, localToWorld.Position, _playerPos );
                            var dir = ((Vector3)(localToWorld.Position - _playerPos )).normalized;
                            physicsVelocity.ApplyLinearImpulse(physicsMass, dir / 2);
                        }
                    }).Run();

                
                Entities
                    .WithoutBurst()
                    .ForEach((ref AgentComponent agentComponent, ref LocalToWorld localToWorld,
                        ref PhysicsVelocity physicsVelocity,
                        ref PhysicsMass physicsMass) =>
                    {
                        _allDelegates[agentComponent.EntityID].Position = localToWorld.Position;
                        _allDelegates[agentComponent.EntityID].CurrentState(ref agentComponent, ref localToWorld,
                            ref physicsVelocity, ref physicsMass);

                    }).Run();
            }



            protected override void OnDestroy() => GizmosManager.Instance.DrawGizmos -= MyGizmos;

        #endregion

        private static readonly float3[] _spawnPoints = {
            new(16, 10,16),
            new(-16, 10,16),
            new(16, 10,-16),
            new(-16, 10,-16)
        };


        private void ResetAgent()
        {
            
            var count = 0;
            Entities
                .WithoutBurst()
                .ForEach((ref LocalTransform localTransform,
                    ref PhysicsVelocity physicsVelocity, in AgentComponent agentComponent) =>
                {
                    localTransform.Position = _spawnPoints[count];
                    localTransform.Rotation = quaternion.identity;
                    
                    physicsVelocity.Angular = float3.zero;
                    physicsVelocity.Linear = float3.zero;
                    
                    count++;
                }).Run();
           
        }

        //------------------------------------------------------------------------------------------------------------//
        //------------------------------------------------------------------------------------------------------------//


        private void ChasePlayerState(ref AgentComponent agentComponent, ref LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass)
        {
            if (CanSeePlayer(agentComponent, localToWorld))
                MoveTowards(_playerPos, ref agentComponent, ref localToWorld, ref physicsVelocity, ref physicsMass);
            else
            {
                _allDelegates[agentComponent.EntityID].PathJob(localToWorld.Position, _playerPos);
                _allDelegates[agentComponent.EntityID].CurrentState = null;
                _allDelegates[agentComponent.EntityID].CurrentState += FollowNodeState;
            }
        }


        private void FollowNodeState(ref AgentComponent agentComponent, ref LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass)
        {
            if (CanSeePlayer(agentComponent, localToWorld))
            {
                _allDelegates[agentComponent.EntityID].CurrentState = null;
                _allDelegates[agentComponent.EntityID].CurrentState += ChasePlayerState;
                _allDelegates[agentComponent.EntityID].PathList.Clear();
                return;
            }

            if (_allDelegates[agentComponent.EntityID].PathList.Length < 2)
            {
                GetRandomPath(agentComponent, localToWorld);
                return;
            }

            var currentNode = CalculateCurrentNode(localToWorld.Position, _allDelegates[agentComponent.EntityID].PathList, physicsVelocity.Linear );
            MoveTowards(currentNode, ref agentComponent, ref localToWorld, ref physicsVelocity, ref physicsMass);

        }



        private Vector3 CalculateCurrentNode(Vector3 position, NativeList<Vector3> pathList, Vector3 velocity)
        {
            Vector3 closeTarget;
            position.y = pathList[0].y;// para ignorar una dimension

            
            var distNextNode = Vector3.Distance(position, pathList[0]);
            if ((distNextNode <  0.5f) || (distNextNode > Vector3.Distance(position, pathList[1]))
                || CheckInertiaDot(position, velocity, pathList[0], pathList[1]))
            {
                closeTarget = pathList[1];
                pathList.RemoveAt(0);
            }
            else
                closeTarget = pathList[0];

            return closeTarget;
        }

        private bool CheckInertiaDot(Vector3 position, Vector3 velocity, Vector3 node1, Vector3 node2)
        {
            if (RayCastToNode(position, node2)) return false;

            var dir = (node2 - position).normalized;
            return Vector3.Dot(velocity.normalized, dir) > .95f;
        }


        private bool RayCastToNode(Vector3 start, Vector3 end)
        {
            end.y = start.y += .5f;
            var colFilter = new CollisionFilter {
                BelongsTo = AgentCategory.Value,
                CollidesWith = LevelCategory.Value,
                GroupIndex = 0
            };


            var rayInput = new RaycastInput {
                Start = start,
                End = end,
                Filter = colFilter
            };

            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;
            collisionWorld.CastRay(rayInput, out var hit);

            Debug.DrawLine(start,end, Color.red);
            return EntityManager.HasComponent<PilarComponent>(hit.Entity);
        }



        private void MoveTowards(float3 toPosition,
            ref AgentComponent agentComponent,
            ref LocalToWorld localToWorld,
            ref PhysicsVelocity physicsVelocity,
            ref PhysicsMass physicsMass)
        {


            var direction = ((Vector3)(toPosition - localToWorld.Position)).normalized;
            MoveTowardsPhysics(direction, agentComponent.Speed, agentComponent.SpeedMagnitude, ref physicsVelocity, ref physicsMass, ref localToWorld);


        }

        private void MoveTowardsPhysics(Vector3 direction, float speed, float speedMagnitude,
            ref PhysicsVelocity physicsVelocity,
            ref PhysicsMass physicsMass,
            ref LocalToWorld localToWorld)
        {

            var wtlMatrix = ((Matrix4x4)localToWorld.Value).inverse;
            var worldRot = Vector3.Cross(direction, Vector3.up).normalized;

            var localRot = wtlMatrix.MultiplyVector(worldRot);
            physicsVelocity.ApplyAngularImpulse(physicsMass, localRot * (speed * SystemAPI.Time.DeltaTime) );

            physicsVelocity.Angular = Vector3.ClampMagnitude(physicsVelocity.Angular, speedMagnitude);

        }



        private static void GetRandomPath(AgentComponent agentComponent, LocalToWorld localToWorld)
        {
            _allDelegates[agentComponent.EntityID].PathJob(localToWorld.Position,
                GridManager2D.Instance.GetValidNode());
        }

        private static void GetRandomPath()
        {
            foreach (var entityDelegate in _allDelegates.Values)
            {
                entityDelegate.PathJob(entityDelegate.Position,
                    GridManager2D.Instance.GetValidNode());
            }
        }
        
        public void UpdatePathJobsLevel() => GetRandomPath();


        private bool CanSeePlayer(AgentComponent agentComponent, LocalToWorld localToWorld)
        {
            var dir = (Vector3)(_playerPos - localToWorld.Position);
            return 
                Vector3.Distance(localToWorld.Position, _playerPos) <= agentComponent.ViewDist &&
                RayCastToPlayer(localToWorld.Position, dir, agentComponent.ViewDist);
        }



        private bool RayCastToPlayer(Vector3 start, Vector3 direction, float distance)
        {
            var colFilter = new CollisionFilter {
                BelongsTo = AgentCategory.Value,
                CollidesWith = LevelPlayerCategory.Value,
                GroupIndex = 0
            };

            var rayInput = new RaycastInput {
                Start = start,
                End = direction * distance,
                Filter = colFilter
            };
            
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;
            
            return collisionWorld.CastRay(rayInput, out var hit) && 
                   EntityManager.HasComponent<PlayerComponent>(hit.Entity);
        }

        //--------------------------------------------------------------//
        //--------------------------------------------------------------//


          private void EntityCleanUp()
        {
            var archetype = EntityManager.CreateArchetype(
                typeof(AgentComponent),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                //typeof(WorldRenderBounds),
                //typeof(ChunkWorldRenderBounds),
                typeof(MaterialMeshInfo),
                typeof(PhysicsCollider),
                typeof(PhysicsDamping),
                //typeof(PhysicsGravityFactor),
                typeof(PhysicsVelocity),
                typeof(PhysicsMass),
                typeof(PhysicsWorldIndex),
                typeof(WorldToLocal_Tag),
                typeof(RenderFilterSettings),
                typeof(RenderMeshArray)
            );

            var entity = EntityManager.CreateEntity(archetype);


            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<AgentComponent>(_prefabAgentEntity));
            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<LocalToWorld>(_prefabAgentEntity));


            EntityManager.SetComponentData(entity, new RenderBounds
            {
                Value = EntityManager.GetComponentData<RenderBounds>(_prefabAgentEntity).Value
            });

            EntityManager.SetComponentData(entity, new PhysicsCollider
            {
                Value = EntityManager.GetComponentData<PhysicsCollider>(_prefabAgentEntity).Value
            });

            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<MaterialMeshInfo>(_prefabAgentEntity));
            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<PhysicsCollider>(_prefabAgentEntity));
            
            //EntityManager.SetComponentData(entity, EntityManager.GetComponentData<PhysicsGravityFactor>(_prefabAgentEntity));
            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<PhysicsVelocity>(_prefabAgentEntity));
            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<PhysicsMass>(_prefabAgentEntity));
            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<PhysicsDamping>(_prefabAgentEntity));

            EntityManager.SetSharedComponent(entity, EntityManager.GetSharedComponent<PhysicsWorldIndex>(_prefabAgentEntity));
            EntityManager.SetSharedComponent(entity, EntityManager.GetSharedComponent<RenderFilterSettings>(_prefabAgentEntity));
            EntityManager.SetSharedComponentManaged(entity, EntityManager.GetSharedComponentManaged<RenderMeshArray>(_prefabAgentEntity));
//
            
            EntityManager.AddComponent<WorldToLocal_Tag>(entity);
            EntityManager.AddComponent<Simulate>(entity);


            /*

            EntityManager.RemoveComponent<PerInstanceCullingTag>(_prefabAgentEntity);
            EntityManager.RemoveComponent<LinkedEntityGroup>(_prefabAgentEntity);
            EntityManager.RemoveComponent<BlendProbeTag>(_prefabAgentEntity);
            EntityManager.RemoveComponent<PhysicsColliderKeyEntityPair>(_prefabAgentEntity);
            EntityManager.RemoveComponent<EntityGuid>(_prefabAgentEntity);
            EntityManager.RemoveComponent<Simulate>(_prefabAgentEntity);
            EntityManager.RemoveComponent<LocalTransform>(_prefabAgentEntity);
            */

            // qda tremendo eh :v
            //EntityManager.RemoveComponent<WorldToLocal_Tag>(_prefabAgentEntity);

            EntityManager.DestroyEntity(_prefabAgentEntity);
            _prefabAgentEntity = entity;

        }
        
          
          private void CreateAgentEntity(float3 position, ref Entity prefab)
          {
              //position = new float3(Random.Range(-31, 31)),10, Random.Range(-31, 31));
              var entity = EntityManager.Instantiate(prefab);

              /*
              EntityManager.SetComponentData(entity, 
                  new LocalTransform
                  {
                      Position = position
                  });
                  
            */
              //EntityManager.RemoveComponent<LocalTransform>(entity);
              Matrix4x4 trs = Matrix4x4.identity;

              trs.m30 = position.x;
              trs.m31 = position.y;
              trs.m32 = position.z;
              
              
              EntityManager.SetComponentData(entity, 
                  new LocalToWorld
                  {
                      Value = Matrix4x4.TRS(position,Quaternion.identity, Vector3.one)
                  });

              
        
              //var nya = EntityManager.GetComponentData<LocalToWorld>(entity);
              //Debug.Log(nya.Position);
              _allAgents.Add(entity);
          }

          public void SpawnEnemy(float3 position) => CreateAgentEntity(position, ref _prefabAgentEntity);

          public void SpawnEnemy() => SpawnEnemy(new float3(0, 10, 0));
        
        
        
        #region Gizmos

            private static readonly Color GizmoColor = Color.white;

            private void MyGizmos()
            {
                Gizmos.color = GizmoColor;

                foreach (var entityDelegate in _allDelegates.Values)
                {
                    var localToWorld = EntityManager.GetComponentData<LocalToWorld>(entityDelegate.AgentEntity);
                    var agent = EntityManager.GetComponentData<AgentComponent>(entityDelegate.AgentEntity);

                    if (!entityDelegate.PathList.IsCreated) return;
                    if (entityDelegate.PathList.Length == 0) return;

                    
                    var oldPos = localToWorld.Position;

                    foreach (var nya in entityDelegate.PathList)
                    {
                        Gizmos.DrawLine(oldPos, nya);
                        oldPos = nya;
                    }
                    
                    Gizmos.DrawWireSphere(localToWorld.Position, agent.DamageRadius  );
                }
            }

        #endregion
    }
}
