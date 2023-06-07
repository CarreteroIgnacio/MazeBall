using CCC;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

// No usar Linq Lpm



namespace Path
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SysAgent : SystemBase
    {

        #region Local Vars

            public static SysAgent Instance;

            private static readonly PhysicsCategoryTags LevelCategory = new() { Category00 = true };
            private static readonly PhysicsCategoryTags PlayerCategory = new() { Category01 = true };
            private static readonly PhysicsCategoryTags AgentCategory = new() { Category02 = true };
            private static readonly PhysicsCategoryTags AgentPlayerCategory = new() { Category01 = true, Category02 = true };

            private NativeList<Entity> _allAgents;

            private Entity _prefabAgentEntity;

            public delegate void DelegateState(
                ref AgentComponent agentComponent,
                ref LocalToWorld localToWorld,
                ref PhysicsVelocity physicsVelocity,
                ref PhysicsMass physicsMass);


            private float3 _playerPos;

            private bool _validStart;
            private bool _alreadyStarted;
        #endregion

        private static readonly float3[] SpawnPoints = {
            new(16, 5,16),
            new(-16, 5,16),
            new(16, 5,-16),
            new(-16, 5,-16)
        };

 
        #region Event Functions

            private void EnableSystem(bool active) => Enabled = _validStart = active;
            private void PauseSystem(bool active) => Enabled = active;
            protected override void OnCreate() => Instance = this;

            protected override void OnStartRunning()
            {
                /*
                if (!SystemAPI.HasSingleton<AgentSpawnerComponent>())
                {
                    Debug.LogError("There is not AgentSpawnerComponent");
                    Enabled = false;
                    return;
                }*/
                if (!SystemAPI.HasSingleton<PhysicsWorldSingleton>())
                {
                    Debug.LogError("There is not PhysicsWorldSingleton");
                    Enabled = false;
                    return;
                }
                
                
                if (!_validStart)
                {
                    SystemManager.TrueStart += EnableSystem;
                    Enabled = false;
                    return;
                }
                SystemManager.PauseEvent += PauseSystem;
                
                
                if(_alreadyStarted)return;

                GameManager.LevelChanging += SpawnEnemy;
                GameManager.LevelReset += ResetAgent;
                SystemManager.EcsParamsRuntime += OnRuntimeValidate;
                
                
                _allAgents = new NativeList<Entity>(Allocator.Persistent);
 
                _prefabAgentEntity = SystemAPI.GetSingleton<AgentSpawnerComponent>().PrefabEntity;
                EntityCleanUp();
                 
                CreateAgentEntity(new float3(16, 10,16));
                CreateAgentEntity(new float3(-16, 10,16));
                CreateAgentEntity(new float3(16, 10,-16));
                CreateAgentEntity(new float3(-16, 10,-16));
                

                Entities
                    .WithoutBurst()
                    .ForEach((ref AgentComponent agentComponent, ref LocalToWorld localToWorld, ref LocalTransform localTransform) =>
                    {
                        agentComponent.CurrentState = AgentState.FollowNode;
                        //Debug.Log(localTransform.Position);
                        GetRandomPath(ref agentComponent, localToWorld.Position, GridManager2D.GridData, GridManager2D.PathNodeArray);
                    }).Run();

                GizmosManager.Instance.DrawGizmos -= MyGizmos;
                GizmosManager.Instance.DrawGizmos += MyGizmos;

                _alreadyStarted = true;
                
                //EntityManager.DestroyEntity(_prefabAgentEntity);
                
                
                ResetAgent();
            }


            protected override void OnUpdate()
            {
                if(!Enabled)return;


                var agentAttack = new NativeArray<AgentComponent>(AgentAmount+1, Allocator.TempJob);
                var playerPos = _playerPos;

                var pathArray = GridManager2D.PathNodeArray;
                var gridData = GridManager2D.GridData;
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;


                var count = 0;//
                Entities
                    //.WithoutBurst()
                    .ForEach(( int entityInQueryIndex,
                        ref AgentComponent agentComponent, ref LocalToWorld localToWorld,
                        ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass, ref LocalTransform localTransform) =>
                    {
                        
                        if (Float3.Distance(playerPos, localToWorld.Position) < agentComponent.DamageRadius)
                        {
                            agentComponent.Position = localToWorld.Position;
                            agentAttack[count] = agentComponent;
                            physicsVelocity.ApplyLinearImpulse(physicsMass,  Float3.Direction(playerPos, localToWorld.Position));
                        }//

                       
                        
                        switch (agentComponent.CurrentState)
                        {
                            case AgentState.ChasePlayer:
                                ChasePlayerState(playerPos, SystemAPI.Time.DeltaTime, gridData, ref pathArray, ref agentComponent, ref localToWorld, ref physicsVelocity, ref physicsMass, ref localTransform, ref collisionWorld);
                                break;
                            case AgentState.FollowNode:
                                FollowNodeState(playerPos, SystemAPI.Time.DeltaTime, gridData, ref pathArray,  ref agentComponent, ref localToWorld, ref physicsVelocity, ref physicsMass, ref localTransform, ref collisionWorld);
                                break;

                            case AgentState.Null:
                                break;
                        }



                        count++;
                    }).Run();

                
                foreach (var agentComponent in agentAttack)
                {
                    if(agentComponent.IsValid)
                        SysPlayer.Instance.TakeDamage(agentComponent.Damage, agentComponent.Position);
                }



                agentAttack.Dispose();
                //Debug.Log(agentComponent.NodePath.c0.x + "   " + nodes[0]);

                //var path = agentComponent.NodePath.ToArrayFloat2(Allocator.Temp);

                /*
                for (var i = 1; i < agentComponent.NodeAmount; i++) 
                    Debug.DrawLine(
                        path[i - 1].AddY(.5f) ,
                        path[i].AddY(.5f) ,
                        Color.Lerp(Color.green, Color.red, i/(float)agentComponent.NodeAmount));
                
                Debug.DrawLine(path[agentComponent.IndexPath].AddY(.5f), path[agentComponent.IndexPath].AddY(3), Color.magenta);*/

            }

            protected override void OnDestroy()
            {
                if (GizmosManager.Instance is not null)
                    GizmosManager.Instance.DrawGizmos -= MyGizmos;
            }

            #endregion
 

        private void OnRuntimeValidate(SystemManager.SysEcsRuntimeParams pRuntimeParams) => _playerPos = pRuntimeParams.PlayerPos;

        private void ResetAgent()
        {
            
            var count = 0;
            Entities
                .WithoutBurst()
                .ForEach((ref LocalTransform localTransform,
                    ref PhysicsVelocity physicsVelocity, in AgentComponent agentComponent) =>
                {
                    localTransform.Position = SpawnPoints[count];
                    localTransform.Rotation = quaternion.identity;
                    
                    physicsVelocity.Angular = float3.zero;
                    physicsVelocity.Linear = float3.zero;
                    
                    count++;
                    if (count >= SpawnPoints.Length) count = 0;
                }).Run();
           
        }

        //------------------------------------------------------------------------------------------------------------//
        //------------------------------------------------------------------------------------------------------------//


        private static void ChasePlayerState(float3 playerPos, float deltaTime, GridDataStruct gridData, ref NativeArray<Node> pathArray, ref AgentComponent agentComponent, ref LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass, ref LocalTransform localTransform, ref CollisionWorld collisionWorld)
        {
         
            if (CanSeePlayer(playerPos, agentComponent, localToWorld, ref collisionWorld))
                MoveTowards(playerPos, deltaTime, ref agentComponent, ref localToWorld, ref physicsVelocity, ref physicsMass);
            else
            {
                GetRandomPath(ref agentComponent, localTransform.Position, gridData, pathArray);
                agentComponent.CurrentState = AgentState.FollowNode;
            }
        }

        private static void FollowNodeState(float3 playerPos, float deltaTime, GridDataStruct gridData, ref NativeArray<Node> pathArray, ref AgentComponent agentComponent, ref LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass, ref LocalTransform localTransform, ref CollisionWorld collisionWorld)
        {
            if (CanSeePlayer(playerPos, agentComponent, localToWorld, ref collisionWorld))
            {
                agentComponent.CurrentState = AgentState.ChasePlayer;
                return;
            }
            
            if (agentComponent.IndexPath >= agentComponent.NodeAmount )
            {
                GetRandomPath(ref agentComponent, localTransform.Position, gridData, pathArray);
                return;
            }

            var pathList = agentComponent.NodePath.ToArrayFloat2(Allocator.Temp);
            var currentNode = CalculateCurrentNode(ref agentComponent, localTransform.Position, pathList, physicsVelocity.Linear, ref collisionWorld, gridData, pathArray);
            
            MoveTowards(currentNode, deltaTime, ref agentComponent, ref localToWorld, ref physicsVelocity, ref physicsMass);


            pathList.Dispose();


        }



        private static float3 CalculateCurrentNode(ref AgentComponent agentComponent, float3 position, NativeArray<float2> pathList, float3 lineal, ref CollisionWorld collisionWorld, GridDataStruct gridData, NativeArray<Node> pathArray)
        {
            if (Float2.Distance(position.RemoveY(), pathList[agentComponent.IndexPath]) < .5f) agentComponent.IndexPath++;


            if (agentComponent.IndexPath >= agentComponent.NodeAmount)
            {
                GetRandomPath(ref agentComponent, position, gridData, pathArray);
                return position;
            }

            if (agentComponent.IndexPath > 0 && !RayCastToNode(position, pathList[agentComponent.IndexPath].AddY(0.5f), ref collisionWorld))
            {
                agentComponent.IndexPath--;
            }
            
            //Debug.Log(agentComponent.NodePath.ToArrayFloat2(Allocator.Temp)[agentComponent.IndexPath].AddY(.5f));

            var pathlist = agentComponent.NodePath.ToArrayFloat2(Allocator.Temp);
            var value = pathlist[agentComponent.IndexPath].AddY(.5f);
            pathlist.Dispose();

            return value;

        }

        
        private static void MoveTowards(float3 toPosition, float deltaTime, ref AgentComponent agentComponent, ref LocalToWorld localToWorld, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass)
        {

            
            var direction = Float3.Direction(localToWorld.Position, toPosition);

            var nyan = (direction.x > direction.z ? direction.WithZeroX().WithZeroY() : direction.WithZeroY().WithZeroZ()) *2;

            direction += nyan; 

            Debug.DrawLine(localToWorld.Position, localToWorld.Position + nyan, Color.green);

            var wtlMatrix =  localToWorld.Value.inverse();
            var worldRot = Float3.Cross(direction, Float3.up).normalized();
            var localRot = wtlMatrix.MultiplyVector(worldRot);


            
            
            var nya = Mathf.InverseLerp(-1, 0.45f, Float3.Dot(direction, physicsVelocity.Linear));
            var braking = Mathf.Lerp(0.75f, 1, nya);
            
            physicsVelocity.ApplyAngularImpulse(physicsMass, localRot * (agentComponent.Speed * deltaTime * braking) );
            physicsVelocity.Angular = Float3.ClampMagnitude(physicsVelocity.Angular, agentComponent.SpeedMagnitude);

            physicsVelocity.Linear.x *= braking;//
            physicsVelocity.Linear.z *= braking;


            //var angle = Float3.Angle(direction, physicsVelocity.Linear);
            //var nya = Vector3.RotateTowards(physicsVelocity.Linear, direction, SystemAPI.Time.fixedDeltaTime, 10);
            //physicsVelocity.Linear = nya;
            
            //Debug.DrawLine(localToWorld.Position, localToWorld.Position+ direction * 2, Color.green);
            //Debug.DrawLine(localToWorld.Position, localToWorld.Position + physicsVelocity.Linear.normalized() * 2, Color.magenta);

        }



        private static void GetRandomPath(ref AgentComponent agentComponent, float3 position, GridDataStruct gridData, NativeArray<Node> pathArray)
        {
            
            var path = Path2D.GetPathWorldSpace2D(position, float3.zero, out var nodeAmount, gridData, pathArray);

            
            agentComponent.NodePath = Float4x4.ToMatrix(path);
            agentComponent.NodeAmount = nodeAmount;
            agentComponent.IndexPath = 0;

            path.Dispose();
        }


        private void GetAllPathRandom( GridDataStruct gridData, NativeArray<Node> pathArray)
        {
            Entities
                .WithoutBurst()
                .ForEach((ref AgentComponent agentComponent, ref LocalToWorld localToWorld, ref LocalTransform localTransform) =>
                {
                    GetRandomPath(ref agentComponent, localTransform.Position, gridData, pathArray);
                }).Run();
            
        }
        
        public void UpdatePathJobsLevel() => GetAllPathRandom(GridManager2D.GridData, GridManager2D.PathNodeArray);


        private static bool CanSeePlayer(float3 playerPos, AgentComponent agentComponent, LocalToWorld localToWorld, ref CollisionWorld collisionWorld)
        {
            //var dir = Float3.Direction(localToWorld.Position, playerPos);
            return RayCastToNode(localToWorld.Position, playerPos, ref collisionWorld);
                //Float3.Distance(localToWorld.Position, playerPos) <= agentComponent.ViewDist &&
                //RayCastToPlayer(localToWorld.Position, dir, agentComponent.ViewDist, ref collisionWorld) &&
        }

        private static bool RayCastToPlayer(float3 start, float3 direction, float distance, ref CollisionWorld collisionWorld)
        {
            
            var colFilter = new CollisionFilter {
                BelongsTo = AgentCategory.Value,
                CollidesWith = AgentPlayerCategory.Value,
                GroupIndex = 0
            };

            var rayInput = new RaycastInput {
                Start = start,
                End = direction * distance,
                Filter = colFilter
            };

            collisionWorld.CastRay(rayInput, out var hit);
            //var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld;

            return  hit.Entity != Entity.Null;
        }

        //--------------------------------------------------------------//
        //--------------------------------------------------------------//


        private void EntityCleanUp()
        {
            var entity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<AgentComponent>(_prefabAgentEntity));

            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<LocalToWorld>(_prefabAgentEntity));
            EntityManager.AddComponentData(entity, new LocalTransform
            {
                Position = new float3( 0, 7.5f,0),
                Rotation = quaternion.identity,
                Scale = 1
            });

            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<RenderBounds>(_prefabAgentEntity));
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<MaterialMeshInfo>(_prefabAgentEntity));
            
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<PhysicsCollider>(_prefabAgentEntity));
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<PhysicsMass>(_prefabAgentEntity));
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<PhysicsVelocity>(_prefabAgentEntity));
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<PhysicsDamping>(_prefabAgentEntity));

            EntityManager.AddSharedComponent(entity, EntityManager.GetSharedComponent<PhysicsWorldIndex>(_prefabAgentEntity));
            EntityManager.AddSharedComponent(entity, EntityManager.GetSharedComponent<RenderFilterSettings>(_prefabAgentEntity));
            EntityManager.AddSharedComponentManaged(entity, EntityManager.GetSharedComponentManaged<RenderMeshArray>(_prefabAgentEntity));
            
            EntityManager.AddComponent<WorldToLocal_Tag>(entity);
            EntityManager.AddComponent<Simulate>(entity);

            EntityManager.DestroyEntity(_prefabAgentEntity);
            _prefabAgentEntity = entity;

        }

        private int AgentAmount;
        private void CreateAgentEntity(float3 position)
        {
            //EntityManager.SetComponentData(_prefabAgentEntity,new LocalTransform { Position = position });
            var entity = EntityManager.Instantiate(_prefabAgentEntity);
            AgentAmount++;
            /*
            var agentComponent = EntityManager.GetComponentData<AgentComponent>(entity);
            agentComponent.IsValid = true;
            EntityManager.SetComponentData(entity,agentComponent);*/
   
            
            
            _allAgents.Add(entity);
        }
        private void SpawnEnemy() => CreateAgentEntity(new float3(1, 5, 0));


        private static bool RayCastToNode(float3 start, float3 end, ref CollisionWorld collisionWorld)
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

            
            collisionWorld.CastRay(rayInput, out var hit);

            Debug.DrawLine(start,end, Color.red);
            return hit.Entity == Entity.Null;
        }

        
        
        
        
        
        
        #region Gizmos

            private static readonly Color GizmoColor = Color.white;

            private void MyGizmos()
            {
                return;
                /*
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
                        Gizmos.DrawLine(oldPos, nya.AddY(.5f));
                        oldPos = nya.AddY(.5f);
                    }
                    
                    Gizmos.DrawWireSphere(localToWorld.Position, agent.DamageRadius  );
                }*/
            }

        #endregion
    }
}
