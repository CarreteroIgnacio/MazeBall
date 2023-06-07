using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

namespace Path
{ 
    /*
     * Fucking DOTS!!!! quede con brain dmg para resolver este algorimo (tecnicamnte el 3D xD) este fue un chiste
     * es simplemente un A* pero hecho con jobs por lo que aunq en los agentes se recalcula bastante seguido el path,
     * la performance no se afecta en nada
     *
     * Atravez de PathResult es que se obtiene la lista del job para contruir el path
     *
     * PD: para dar una idea, aca los neighbourOffsetArray son 4 (8 considerando las esquinas). En el 3D son 6-14-26 :)
    */
    public static class Path2D
    {
        private const int MoveStraightCost = 10;
        private const int MoveDiagonalCost = 14;

        private static readonly int2[] NeighbourOffset2 =
        {
            new (1, 0),
            new (-1, 0),
            new (0, 1),
            new (0, -1),
        };



        public static NativeArray<float2> GetPathWorldSpace2D(float3 position, float3 target, out int nodeAmount, GridDataStruct gridData, NativeArray<Node> pathArray)   
        {
            
            var starPoint = GridManager2D.GetClosestPointWorldSpace(position, gridData, pathArray);
            
            if (target is { x: 0, z: 0 })
                target = GridManager2D.GetRandomValidNode(pathArray);


            NativeList<int2> pathResult = new NativeList<int2>(Allocator.Temp);
            int nya = 0;
        
            FindPath(starPoint,
                GridManager2D.GetClosestPointWorldSpace(GridManager2D.GetRandomValidNode(pathArray), gridData, pathArray),
                gridData.GridSize,
                pathArray,
                    ref pathResult);
                /*
                pathResult = GetPathJob(starPoint, 
                    GridManager2D.GetClosestPointWorldSpace(GridManager2D.GetRandomValidNode()),
                    GridManager2D.GridData.GridSize,
                    GridManager2D.PathNodeArray);*/





            if (pathResult.Length > 2) CompressPathArray(ref pathResult);




            //foreach (var node in pathResult) Debug.Log("Path " + node);

            //foreach (var node in compressed) Debug.Log("Compressed " + node);


            //nodeAmount = compressed.Length;
            nodeAmount = pathResult.Length < 8 ? pathResult.Length : 8;

            pathResult.ReverseList();

            var pathWorldSpace = new NativeList<float2>(Allocator.Temp);

            foreach (var t in pathResult)
                pathWorldSpace.Add(
                    pathArray[CalculateIndex(t)]
                        .WorldPosition.RemoveY());

            //pathWorldSpace.Reverse();
            /*
            if(pathWorldSpace.Length > 0)
                pathWorldSpace.RemoveAt(0);*/
            
            
            var nyak = pathWorldSpace.Trim(8, Allocator.Temp);
            pathWorldSpace.Dispose();
            return nyak;

        }

        /// <summary>
        ///  Only keep the node that will make u turn
        /// </summary>
        /// <param name="pathResult"></param>
        /// <returns></returns>
        private static void CompressPathArray(ref NativeList<int2> pathResult)
        {
            if (pathResult.Length < 4) return;
            var nya = new NativeList<int2>(Allocator.Temp);
            nya.Add(pathResult[0]);

            for (var i = 1; i < pathResult.Length-1; i++)
            {
                if ((pathResult[i - 1].x != pathResult[i].x || pathResult[i].x != pathResult[i + 1].x)
                    && (pathResult[i - 1].y != pathResult[i].y || pathResult[i].y != pathResult[i + 1].y))
                    nya.Add(pathResult[i]);
            }

            pathResult = nya;
        }


        private static NativeList<int2> GetPathJob(int2 starPoint, int2 endPoint, int2 gridSize, NativeArray<Node> pathNodeArray)
        {
            var findPathJob = new FindPathJob
            {
                StartPosition = starPoint,
                EndPosition = endPoint,
                GridSize = gridSize,
                GridNodeArray = pathNodeArray,
                PathResult = new NativeList<int2>(Allocator.TempJob),
            };
            findPathJob.Run();

            return findPathJob.PathResult;
        }
        
        [BurstCompile]
        private struct FindPathJob : IJob
        {
            public int2 StartPosition;
            public int2 EndPosition;
            public int2 GridSize;
            public NativeArray<Node> GridNodeArray;
            public NativeList<int2> PathResult;

            public void Execute() => FindPath(StartPosition, EndPosition, GridSize, GridNodeArray, ref PathResult);
        }


        //-----------------------------------------------------------------------------//
  
        private static void FindPath (int2 startCord, int2 endCord, int2 gridSize, NativeArray<Node> gridNodeArray, ref NativeList<int2> pathResult)
        {
            
             for (var i = 0; i < gridNodeArray.Length; i++)
             {
                 var pathNode = gridNodeArray[i];
                 pathNode.ResetNode();
                 pathNode.SetHCost(
                     CalculateDistanceCost(pathNode.Coords, endCord));
                 gridNodeArray[i] = pathNode;
             }

             var endNodeIndex = CalculateIndex(endCord);
             var startNodeIndex = CalculateIndex(startCord);
            

            
             gridNodeArray[startNodeIndex].SetGCost(0);
            
            
             //lista de index
             var openList = new NativeList<int>(Allocator.Temp);
             var closedList = new NativeList<int>(Allocator.Temp);

            
             openList.Add(startNodeIndex);
            
             while (openList.Length > 0  ) // es una mierda esto, hay q optimizar
             {
                 var currentNodeIndex = GetLowestCostFNodeIndex(openList, ref gridNodeArray);
                 var currentNode = gridNodeArray[currentNodeIndex];

                 if (currentNodeIndex == endNodeIndex) break;

                 for (var i = 0; i < openList.Length; i++)
                 {
                     if (openList[i] != currentNodeIndex) continue;
                     openList.RemoveAtSwapBack(i);
                     break;
                 }
                
                 closedList.Add(currentNodeIndex);


                 foreach (var offset in NeighbourOffset2)
                 {
                     int2 neighbourPosition = currentNode.Coords + offset;
                     if (!IsPositionInsideGrid(neighbourPosition, gridSize)) continue ;

                     int neighbourNodeIndex = CalculateIndex(neighbourPosition);
                    
                     if (closedList.Contains(neighbourNodeIndex)) continue;

                     Node neighbour = gridNodeArray[neighbourNodeIndex];
                     if (!neighbour.IsWalkable) continue;
                    
                    

                     int tentativeGCost = currentNode.GCost + CalculateDistanceCost(currentNode.Coords, neighbourPosition);
                     if (tentativeGCost < neighbour.GCost) {
                         neighbour.SetComeIndex(currentNodeIndex);
                         neighbour.SetGCost(tentativeGCost);
                         neighbour.CalculateFCost();
                         gridNodeArray[neighbourNodeIndex] = neighbour;

                         if (!openList.Contains(neighbour.Index)) { 
                             openList.Add(neighbour.Index);
                         }
                     }
                 }
                
             }
             
             CalculatePath(endCord, gridNodeArray, ref pathResult);
             

             openList.Dispose();
             closedList.Dispose();
             
             
        }
    
        private static void CalculatePath(int2 endCord, NativeArray<Node> pathNodeArray, ref NativeList<int2> pathResult)
        {
            
            var endNodeIndex = pathNodeArray[CalculateIndex(endCord)];
            
            
            if (endNodeIndex.CameFromNodeIndex == -1) return;

            // Found a path


            pathResult.Add(endCord);
            

            var nya = 0;
            Node current = endNodeIndex;
            while (current.CameFromNodeIndex != -1) {
                Node cameFrom = pathNodeArray[current.CameFromNodeIndex];
                pathResult.Add(cameFrom.Coords);
                current = cameFrom;
                nya++;

                if (nya >= pathNodeArray.Length) current.SetComeIndex(-1);

            }
        }

        private static int CalculateIndex(int2 cords) => cords.x * 64 + cords.y;// * boxSize.x;

        private static int CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            var distanceX = Mathf.Abs(aPosition.x - bPosition.x);//
            var distanceY = Mathf.Abs(aPosition.y - bPosition.y);
            var remaining = Mathf.Abs(distanceX - distanceY);

            return MoveDiagonalCost * Mathf.Min(distanceX, distanceY) + 
                   MoveStraightCost * remaining;
        }

        private static int GetLowestCostFNodeIndex(NativeList<int> openList, ref NativeArray<Node> pathNodeArray) {
            Node lowestCostPath = pathNodeArray[openList[0]];
            for (int i = 1; i < openList.Length; i++) {
                Node nodePath = pathNodeArray[openList[i]];
                if (nodePath.FCost < lowestCostPath.FCost) {
                    lowestCostPath = nodePath;
                }
            }
            return lowestCostPath.Index;
        }
        
        
        private static bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
        {
            return 
                gridPosition is { x: >= 0, y: >= 0 } &&
                gridPosition.x < gridSize.x &&
                gridPosition.y < gridSize.y;
        }

    }
}