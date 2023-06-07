using System.Linq;
using Path;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class SysCollectible : SystemBase
{

    private static float3 _playerPos;

    private void OnRuntimeValidate(SystemManager.SysEcsRuntimeParams parameters) => _playerPos = parameters.PlayerPos;



    protected override void OnStartRunning()
    {
        var pathArray = GridManager2D.PathNodeArray;
        Entities
            .WithoutBurst()
            .ForEach((ref CollectableComponent collectableComponent, ref LocalTransform localToWorld) =>
        {
            ResetRandomPosition(ref collectableComponent, ref localToWorld, ref pathArray);
        }).Run();

        SystemManager.EcsParamsRuntime += OnRuntimeValidate;
    }


    protected override void OnUpdate()
    {
        var playerPos = _playerPos;

        var listy = new NativeArray<CollectableComponent>(3, Allocator.TempJob);

        var pathArray = GridManager2D.PathNodeArray;
        Entities
            .ForEach((int entityInQueryIndex, ref CollectableComponent collectableComponent, ref LocalTransform localToWorld) =>
            {
                var pos = localToWorld.Position;
                var dist = Float3.Distance(playerPos, pos);
                if (dist < .5f)
                {
                    listy[entityInQueryIndex] = collectableComponent;
                    ResetRandomPosition(ref collectableComponent, ref localToWorld, ref pathArray);
                }
                else if (Float3.Distance(playerPos, pos) < 3f)
                    collectableComponent.StaticPos = localToWorld.Position += Float3.Direction(localToWorld.Position, playerPos) * SystemAPI.Time.DeltaTime * 3f;
                else
                    localToWorld.Position = new float3(0, Mathf.Sin((float)SystemAPI.Time.ElapsedTime) * 2, 0) + collectableComponent.StaticPos;

            }).Run();

        foreach (var nya in listy.Where(nya => nya.IsValid))
            SystemManager.OnCollectable(nya);
    }

    

    

    private static void ResetRandomPosition(ref CollectableComponent collectableComponent, ref LocalTransform localToWorld, ref NativeArray<Node> pathArray)
    {
        //localToWorld.Position = collectableComponent.StaticPos = new float3(-16, 3, -16);
        localToWorld.Position = collectableComponent.StaticPos = GridManager2D.GetRandomValidNode(pathArray) + new float3(0, 2, 0);
    }

}
    
    
    


