using CCC;
using Collectable;
using Path;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class SysCollectable : SystemBase
{

    protected override void OnStartRunning()
    {
        Entities
            .WithoutBurst()
            .ForEach((ref CollectableComponent collectableComponent, ref LocalTransform localToWorld) =>
        {
            ResetRandomPosition(ref collectableComponent, ref localToWorld);
        }).Run();
    }


    protected override void OnUpdate()
    {

        var playerPos = new float3();
        Entities
            .WithoutBurst()
            .ForEach((ref LocalTransform localTransform, in PlayerComponent playerComponent) =>
            {
                playerPos = localTransform.Position;
            }).Run();
        Entities
            .WithoutBurst()
            .ForEach((ref CollectableComponent collectableComponent, ref LocalTransform localToWorld) =>
            {
                var pos = localToWorld.Position;
                //pos.y = playerPos.y;
                var dist = Vector3.Distance(playerPos, pos);
                
                
                if (dist < .5f)
                {
                    SysPlayer.Instance.RepairIntegrity(collectableComponent.Healing, collectableComponent.Air, collectableComponent.Energy);
                    ResetRandomPosition(ref collectableComponent, ref localToWorld);
                    GameManager.Instance.AddPoints(collectableComponent.Points);
                }
                else if (Vector3.Distance(playerPos, pos) < 3f)
                {
                    var dir = ((Vector3)(playerPos - localToWorld.Position)).normalized;
                    collectableComponent.StaticPos = localToWorld.Position += (float3)(dir * SystemAPI.Time.DeltaTime * 3f);
                }
                else
                {
                    localToWorld.Position = new float3(0,Mathf.Sin((float)SystemAPI.Time.ElapsedTime) * 2,0) + collectableComponent.StaticPos ;
                }
                
                
            }).Run();
    }




    private void ResetRandomPosition(ref CollectableComponent collectableComponent, ref LocalTransform localToWorld) => 
        localToWorld.Position = collectableComponent.StaticPos = GridManager2D.Instance.GetValidNode() + new float3(0,2,0);
}
