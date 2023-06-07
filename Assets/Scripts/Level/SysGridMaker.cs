using Path;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


namespace Level
{
    public partial class SysGridMaker : SystemBase
    {
        private static float3 _lodDistance;
        private static float _levelDuration;
        private static float4x4 _cameraLtw;


        #region Internals Vars

        private static GridMakerComponent _gridMakerComponent;
        private static float _timeRunning;
        private Entity _prefabEntity;
        private bool _validStart;
        private bool _alreadyStarted;

        #endregion


        private static LoDData _loDBakeData;

        private struct LoDData
        {
            public NativeArray<MaterialMeshInfo> MeshInfoDown;
            public NativeArray<MaterialMeshInfo> MeshInfoUp;
            public NativeArray<MaterialMeshInfo> MeshInfoUpRight;
            public NativeArray<MaterialMeshInfo> MeshInfoUpForward;
            public NativeArray<MaterialMeshInfo> MeshInfoUpTop;

            public void Init()
            {
                MeshInfoDown = new NativeArray<MaterialMeshInfo>(2, Allocator.Persistent);
                MeshInfoUp = new NativeArray<MaterialMeshInfo>(2, Allocator.Persistent);
                MeshInfoUpRight = new NativeArray<MaterialMeshInfo>(2, Allocator.Persistent);
                MeshInfoUpForward = new NativeArray<MaterialMeshInfo>(2, Allocator.Persistent);
                MeshInfoUpTop = new NativeArray<MaterialMeshInfo>(2, Allocator.Persistent);
            }
        }

        #region Events Functions

        protected override void OnStartRunning()
        {
            _lodDistance = new float3(15, 25, 0);
            if (!_validStart)
            {
                SystemManager.TrueStart += EnableSystem;
                Enabled = false;
                return;
            }
            if (_alreadyStarted) return; // this is to be able to deactivate the system when pause the game
            
            
            SystemManager.PauseEvent += PauseSystem;
            SystemManager.EcsParams += OnValidate;
            SystemManager.EcsParamsRuntime += OnRuntimeValidate;

            if (!SystemAPI.HasSingleton<PillarSpawnerComponent>())
            {
                Debug.LogError("There is not PilarSpawnerComponent");
                Enabled = false;
                return;
            }
            if (!SystemAPI.HasSingleton<GridMakerComponent>())
            {
                Debug.LogError("GridMakerComponent is Null");
                Enabled = false;
                return;
            }

            
            
            _gridMakerComponent = SystemAPI.GetSingleton<GridMakerComponent>();

            
            
            
            
            SetLodData();
            EntityCleanUp();


            _timeRunning = _gridMakerComponent.MapTransitionSpeed;
            var halfScale = _gridMakerComponent.GridScale / 2 - 0.5f;
            var startPos = _gridMakerComponent.LmPosition - new Vector3(halfScale, 0, halfScale);
            
            for (var i = 0; i < 64; i++)
            for (var j = 0; j < 64; j++)
                CreatePilarEntity(new int2(i, j), new Vector3(i, 0, j) + startPos, ref _prefabEntity);

            EntityManager.DestroyEntity(_prefabEntity);
            _alreadyStarted = true;
            
            UpdateDesiredHeight();
        }


        protected override void OnUpdate()
        {
            if (!Enabled) return;

            _timeRunning += SystemAPI.Time.DeltaTime;
            if (_timeRunning >= _levelDuration)
                UpdateDesiredHeight();
            LodSystem();
        }

        private void EnableSystem(bool active) => Enabled = _validStart = active;
        private void PauseSystem(bool active) => Enabled = active;


        private void OnValidate(SystemManager.SysEcsParameters active) => _lodDistance = active.LodDistance;
        private void OnRuntimeValidate(SystemManager.SysEcsRuntimeParams pRuntimeParams)
        {
            _cameraLtw = pRuntimeParams.CameraLtw;
            _levelDuration = pRuntimeParams.LevelDuration;
        }

        #endregion
        
        
        


        


        #region PopulateGrid

        private void EntityCleanUp()
        {
            var entity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<PilarComponent>(_prefabEntity));
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<LocalToWorld>(_prefabEntity));

            var matMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(_prefabEntity);
            EntityManager.AddComponentData(entity, matMeshInfo);
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<RenderBounds>(_prefabEntity));
            
            EntityManager.AddComponentData(entity, EntityManager.GetComponentData<PhysicsCollider>(_prefabEntity));
            EntityManager.AddSharedComponent(entity, EntityManager.GetSharedComponent<PhysicsWorldIndex>(_prefabEntity));
            EntityManager.AddSharedComponent(entity, EntityManager.GetSharedComponent<RenderFilterSettings>(_prefabEntity));
            EntityManager.AddSharedComponentManaged(entity, EntityManager.GetSharedComponentManaged<RenderMeshArray>(_prefabEntity));
            
            

            FrequencyBandAnalyser.Instance.boxyMat = EntityManager.GetSharedComponentManaged<RenderMeshArray>(_prefabEntity).GetMaterial(matMeshInfo);
            
            
            EntityManager.AddComponent<WorldToLocal_Tag>(entity);

            EntityManager.DestroyEntity(_prefabEntity);
            _prefabEntity = entity;
        }

        private static int GetIndex(int x, int y) => x * 64 + y;

        private void CreatePilarEntity(int2 pilarCord, float3 position, ref Entity prefab)
        {
            var entity = EntityManager.Instantiate(prefab);
            EntityManager.SetComponentData(entity, new LocalToWorld { Value = float4x4.identity.WithPositionSet(position) });
            EntityManager.SetComponentData(entity, new PilarComponent { Cords = pilarCord });
        }

        #endregion



        private void LodSystem()
        {
            var pillarPositionBaked = new NativeArray<float>(4096, Allocator.TempJob);
            //--------------
            // Bake current pillar height
            Entities
            .ForEach((ref LocalToWorld localToWorld, in PilarComponent pillarComponent) =>
                {
                    pillarPositionBaked[GetIndex(pillarComponent.Cords.x, pillarComponent.Cords.y)] = localToWorld.Position.y;
                }).Run();
            
            //-------------------
            
            var timeRunning = _timeRunning * _gridMakerComponent.MapTransitionSpeed;
            var cameraLtw = new LocalToWorld { Value = _cameraLtw };
            var lodDistance = _lodDistance;
            var lodBakeData = _loDBakeData;
            var gridHeight = _gridMakerComponent.GridHeight;
            
            Entities
                .WithReadOnly(pillarPositionBaked)
                .WithReadOnly(lodBakeData)
                .ForEach((ref LocalToWorld localToWorld, ref PilarComponent pillarComponent, ref MaterialMeshInfo materialMeshInfo) =>
                {

                    //--------------
                    // HeightJob
                    if (timeRunning < 2f)
                    {
                        float height;

                        if (timeRunning > 1)
                            height = pillarComponent.OldPos = pillarComponent.CurrentPos = pillarComponent.DesiredPos;
                        else
                            pillarComponent.CurrentPos = height = Mathf.Lerp(pillarComponent.OldPos,
                                pillarComponent.DesiredPos, timeRunning);

                        localToWorld.SetPositionY(height);
                        pillarComponent.IsHighest = height > 0;
                    }
                    //--------------


                    // inverse the scale to match the orientation
                    localToWorld.SetVectorRight((localToWorld.Position.x < cameraLtw.Position.x ? Float3.right : Float3.left) * 1.02f);
                    localToWorld.SetVectorForward((localToWorld.Position.z < cameraLtw.Position.z ? Float3.forward : Float3.back) * 1.02f);
                    //--------------




                    //--------------
                    // LOD to distance
                    var dist = Vector3.Distance(cameraLtw.Position, localToWorld.Position);
                    var lodIndex = dist < lodDistance.x ? 0 : 1; 

           


                    //--------------
                    // Cull System
                    if (!pillarComponent.IsHighest)
                        materialMeshInfo = lodBakeData.MeshInfoDown[lodIndex];
                    else
                    {
                        var showRightFace = true;
                        var showForwardFace = true;
                        var indexZ = localToWorld.Position.z < cameraLtw.Position.z ? 1 : -1;

                        if (pillarComponent.Cords.y + indexZ is >= 0 and <= 63)
                        {
                            var forwardPilar = pillarPositionBaked[GetIndex(pillarComponent.Cords.x, pillarComponent.Cords.y + indexZ)];

                            if (forwardPilar >= localToWorld.Position.y + .1f || forwardPilar > gridHeight - .1f)
                                showForwardFace = false;
                        }

                        var indexX = localToWorld.Position.x < cameraLtw.Position.x ? 1 : -1;
                        if (pillarComponent.Cords.x + indexX is >= 0 and <= 63)
                        {
                            var rightPillar = pillarPositionBaked[GetIndex(pillarComponent.Cords.x + indexX, pillarComponent.Cords.y)];
                            if (rightPillar >= localToWorld.Position.y + .1f
                                || rightPillar >= gridHeight - .1f)
                                showRightFace = false;
                        }


                        if (showForwardFace && showRightFace)
                            materialMeshInfo = lodBakeData.MeshInfoUp[lodIndex];
                        else if (showRightFace)
                            materialMeshInfo = lodBakeData.MeshInfoUpRight[lodIndex];
                        else if (showForwardFace)
                            materialMeshInfo = lodBakeData.MeshInfoUpForward[lodIndex];
                        else
                        {
                            var cornerPillar = pillarPositionBaked[GetIndex(pillarComponent.Cords.x + indexX, pillarComponent.Cords.y + indexZ)];
                            if (cornerPillar >= localToWorld.Position.y + .1f
                                || cornerPillar >= gridHeight - .1f)
                                materialMeshInfo = lodBakeData.MeshInfoDown[lodIndex];
                            else
                                materialMeshInfo = lodBakeData.MeshInfoUpTop[lodIndex];
                        }
                    }
                }).ScheduleParallel();
            Dependency.Complete();
            pillarPositionBaked.Dispose();
        }







        private void UpdateDesiredHeight()
        {
            _timeRunning = 0;


            var texture = FrequencyBandAnalyser.GetCurrentLevel().GetNextLevel();
            var gridHeight = _gridMakerComponent.GridHeight;
            Entities
                .WithReadOnly(texture)
                .ForEach((ref PilarComponent pilarComponent) =>
                {
                    var index = GetIndex(pilarComponent.Cords.x, pilarComponent.Cords.y);
                    pilarComponent.DesiredPos = texture[index] * gridHeight;
                    pilarComponent.OldPos = pilarComponent.CurrentPos;
                    pilarComponent.IsHighest = texture[index] > 0;
                }).ScheduleParallel();

            GridManager2D.UpdateValidNodes(FrequencyBandAnalyser.GetCurrentLevel().GetCurrentLevel());
            SysAgent.Instance.UpdatePathJobsLevel();
        }


        
        private void SetLodData()
        {
            var pilarSpawner = SystemAPI.GetSingleton<PillarSpawnerComponent>();
            _prefabEntity = pilarSpawner.PrefabEntity;

           

            _loDBakeData = new LoDData();
            _loDBakeData.Init();
            _loDBakeData.MeshInfoDown[0] = EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarDownLod0);
            _loDBakeData.MeshInfoDown[1] = EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarDownLod1);


            _loDBakeData.MeshInfoUp[0] = EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpLod0);
            _loDBakeData.MeshInfoUp[1] = EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpLod1);

            _loDBakeData.MeshInfoUpRight[0] =
                EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpRightLod0);
            _loDBakeData.MeshInfoUpRight[1] =
                EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpRightLod1);

            _loDBakeData.MeshInfoUpForward[0] =
                EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpForwardLod0);
            _loDBakeData.MeshInfoUpForward[1] =
                EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpForwardLod1);


            _loDBakeData.MeshInfoUpTop[0] =
                EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpTopLod0);
            _loDBakeData.MeshInfoUpTop[1] =
                EntityManager.GetComponentData<MaterialMeshInfo>(pilarSpawner.PilarUpTopLod1);
        }

    }


}
