using CCC;
using Path;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;


namespace Level
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SysGridMaker : SystemBase
    {

        public static SysGridMaker Instance;

        private static MaterialMeshInfo _materialMeshInfoLod0;
        private static MaterialMeshInfo _materialMeshInfoLod1;
        private MaterialMeshInfo urpMat;
        #region Internals Vars

            private static int _index;
            private float _currentTime;
            private static GridMakerComponent _gridMakerEnt;
            private static float _timeRunning;
            private const float GridOffset = 0;
            private NativeList<Entity> _allEntities;
            private static Vector3 _startPos;
            private Entity _prefabEntity;
            private bool _validStart;
            private bool _alreadyStarted;
        #endregion


        #region Events Functions


            protected override void OnCreate() => Instance = this;

            protected override void OnStartRunning()
            {
                if (!_validStart)
                {
                    Enabled = false;
                    return;
                }
                if(_alreadyStarted)return;

                _prefabEntity = SystemAPI.GetSingleton<PilarSpawnerComponent>().PrefabEntity;
                var prefabEntityLod = SystemAPI.GetSingleton<PilarSpawnerComponent>().PrefabEntityLod;
                var urpEntityMat = SystemAPI.GetSingleton<PilarSpawnerComponent>().UrpEntityMat;

                _materialMeshInfoLod0 = EntityManager.GetComponentData<MaterialMeshInfo>(_prefabEntity);
                _materialMeshInfoLod1 = EntityManager.GetComponentData<MaterialMeshInfo>(prefabEntityLod);
                urpMat = EntityManager.GetComponentData<MaterialMeshInfo>(urpEntityMat);


                
                if (GraphicsSettings.currentRenderPipeline && 
                    GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
                {
                    _materialMeshInfoLod0.Material = urpMat.Material;
                    _materialMeshInfoLod1.Material = urpMat.Material;
                }
                
              
                
                
                _gridMakerEnt = SystemAPI.GetSingleton<GridMakerComponent>();
                _timeRunning = _gridMakerEnt.MapTransitionSpeed;
                EntityCleanUp();

                var halfScale = _gridMakerEnt.GridScale / 2  - 0.5f;
                _startPos = _gridMakerEnt.LmPosition - new Vector3(halfScale, 0, halfScale);

                _allEntities = new NativeList<Entity>(64 * 64, Allocator.Persistent);

                for (var i = 0; i < _gridMakerEnt.CellAmount; i++)
                    for (var j = 0; j < _gridMakerEnt.CellAmount; j++)
                        PopulateCell(new Vector2Int(i, j),
                            64);

                UpdateDesiredHeight();
                EntityManager.DestroyEntity(_prefabEntity);
                _alreadyStarted = true;
            }


            

            protected override void OnUpdate()
            {
                

                var playerPos = (float3)CameraTrack.Instance.transform.forward * 3f;
                Entities
                    .ForEach((ref LocalToWorld localToWorld, in PlayerComponent playerComponent) =>
                    {
                        playerPos += localToWorld.Position;
                    }).Run();

                _timeRunning += SystemAPI.Time.fixedDeltaTime;
                var time = _timeRunning * _gridMakerEnt.MapTransitionSpeed;


                var Lod0 = _materialMeshInfoLod0;
                var Lod1 = _materialMeshInfoLod1;
                Entities
                    .ForEach(( ref LocalToWorld localToWorld, ref PilarComponent pilar, ref MaterialMeshInfo materialMeshInfo) =>
                {
                    if (time < 2f)
                    {
                        float height;

                        if (time > 1)
                            height = pilar.OldPos = pilar.CurrentPos = pilar.DesiredPos;
                        else
                            pilar.CurrentPos = height = Mathf.Lerp(pilar.OldPos, pilar.DesiredPos, time);

                        localToWorld.Value.c3.y = height;
                    }

                    var dist = Vector3.Distance(playerPos, localToWorld.Position);
                    if (dist > 10f)
                    {
                        materialMeshInfo = Lod1;

                    }
                    else if (dist < 8f)
                    {
                        materialMeshInfo = Lod0;
                    }

                }).Run();

                if (_timeRunning >= FrequencyBandAnalyser.GetCurrentLevel().Duration) UpdateDesiredHeight();
                
            }

        
        #endregion


        public void EnableSystem(bool active) => Enabled = _validStart = active;
        public void PauseSystem(bool active) => Enabled = active;

    
        private void EntityCleanUp()
        {
            var archetype = EntityManager.CreateArchetype(
                typeof(PilarComponent),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(MaterialMeshInfo),
                typeof(PhysicsCollider),
                typeof(PhysicsWorldIndex),
                typeof(RenderFilterSettings),
                typeof(RenderMeshArray)
            );

            var entity = EntityManager.CreateEntity(archetype);


            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<PilarComponent>(_prefabEntity));
            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<LocalToWorld>(_prefabEntity));


            EntityManager.SetComponentData(entity, new RenderBounds
            {
                Value = EntityManager.GetComponentData<RenderBounds>(_prefabEntity).Value
            });

            EntityManager.SetComponentData(entity, new PhysicsCollider
            {
                Value = EntityManager.GetComponentData<PhysicsCollider>(_prefabEntity).Value
            });



            var meshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(_prefabEntity);
            EntityManager.SetComponentData(entity, meshInfo);
            
            
            EntityManager.SetComponentData(entity, EntityManager.GetComponentData<PhysicsCollider>(_prefabEntity));

            EntityManager.SetSharedComponent(entity, EntityManager.GetSharedComponent<PhysicsWorldIndex>(_prefabEntity));
            EntityManager.SetSharedComponent(entity, EntityManager.GetSharedComponent<RenderFilterSettings>(_prefabEntity));
            EntityManager.SetSharedComponentManaged(entity, EntityManager.GetSharedComponentManaged<RenderMeshArray>(_prefabEntity));

            FrequencyBandAnalyser.Instance.boxyMat =
                EntityManager.GetSharedComponentManaged<RenderMeshArray>(_prefabEntity).GetMaterial(meshInfo);
            
            
            EntityManager.AddComponent<WorldToLocal_Tag>(entity);
            EntityManager.RemoveComponent<Simulate>(entity);

            
            EntityManager.DestroyEntity(_prefabEntity);
            _prefabEntity = entity;
        }
        

        private void PopulateCell(Vector2Int trimCords, int height)
        {
            var fraction = height / _gridMakerEnt.CellAmount;
            
            for (var x = fraction * trimCords.x; x < fraction * (trimCords.x + 1); x++)
                for (var z = fraction * trimCords.y; z < fraction * (trimCords.y + 1); z++)
                {
                    var offset = x % 2 != 0 ? Vector3.zero : new Vector3(0, 0, GridOffset);
                    var pilarPos = new Vector3(x, 0, z) + offset + _startPos;

                    _allEntities.Add(CreatePilarEntity(new int2(x, z), pilarPos, ref _prefabEntity));
                }
            
        }
        
        private static int GetIndex(int x, int y, int height) => x * height + y;

        private Entity CreatePilarEntity(int2 pilarCord, float3 position, ref Entity prefab)
        {
            var entity = EntityManager.Instantiate(prefab);

            EntityManager.SetComponentData(entity, 
                new LocalToWorld
                {
                    Value = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one)
                });
            
            EntityManager.SetComponentData(entity, 
                new PilarComponent
                {
                    DesiredPos = 0,
                    Cords = pilarCord,
                    IsHigh = false
                });

            return entity;
        }


        private void UpdateDesiredHeight()
        {
            _timeRunning = 0;

            new UpdateDesiredJob
            {
                Texture = FrequencyBandAnalyser.GetCurrentLevel().GetNextLevel(),
                GridHeight = _gridMakerEnt.GridHeight,
            }.Run();

            GridManager2D.Instance.UpdateValidNodes(FrequencyBandAnalyser.GetCurrentLevel().GetCurrentLevel());
            SysAgent.Instance.UpdatePathJobsLevel();
        }
        
        
        [BurstCompile]
        private partial struct UpdateDesiredJob : IJobEntity
        {
            public NativeArray<float> Texture;
            public float GridHeight;

            private void Execute(ref PilarComponent pilarComponent)
            {
                var index = GetIndex(pilarComponent.Cords.x, pilarComponent.Cords.y, 64);
                pilarComponent.DesiredPos = Texture[index] * GridHeight;
                pilarComponent.OldPos = pilarComponent.CurrentPos;
                pilarComponent.IsHigh = Texture[index] > 0;
            }
        }
    }
}