using System.Collections.Generic;
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
     * PD: para dar una idea, aca los neighbourOffsetArray son 4. En el 3d son 26 :)
    */
    public static class Path2D
    {
        private const int MoveStraightCost = 10;
        private const int MoveDiagonalCost = 14;

        public static NativeArray<Vector3> GetPath(Vector3 position, Vector3 target)
        {
            var starPoint = GridManager2D.GetClosestPointWorldSpace(position);
            var endPoint = GridManager2D.GetClosestPointWorldSpace(target);

//            Debug.Log(starPoint + " " + endPoint);
            var path = new NativeList<int>(Allocator.TempJob);
            var pathNodeArray = GridManager2D.PathNodeArray;
            
            var neighbourOffsetArray = 
                new NativeArray<int2>(4, Allocator.Persistent)
                {
                    [0] = new int2(1, 0),
                    [1] = new int2(-1, 0),
                    [2] = new int2(0, 1),
                    [3] = new int2(0, -1),
                };
            
            
            
            var findPathJob = new FindPathJob
            {
                StartPosition = starPoint,
                EndPosition = endPoint,
                GridSize = GridManager2D.GridSize,
                PathNodeArray = pathNodeArray,
                NeighbourOffset = neighbourOffsetArray,
                PathResult = path,
            };
            var jobQueue = findPathJob.Schedule();
            jobQueue.Complete();

            var nodeList = new List<Vector3>();

            foreach (var node in findPathJob.PathResult) 
                nodeList.Add(pathNodeArray[node].WorldPosition);
            path.Dispose();
            neighbourOffsetArray.Dispose();
            
            nodeList.Reverse();
            if(nodeList.Count > 0)
                nodeList.Remove(nodeList[0]);
            
            var native = new NativeArray<Vector3>(nodeList.ToArray(), Allocator.Persistent);
            
            
            return native;
        }


        //-----------------------------------------------------------------------------//

        [BurstCompile]
        private struct FindPathJob : IJob 
        {

            public int2 StartPosition;
            public int2 EndPosition;
            public int2 GridSize;
            public NativeArray<Node> PathNodeArray;
            public NativeArray<int2> NeighbourOffset;
            public NativeList<int> PathResult;

            public void Execute()
            {
                for (var i = 0; i < PathNodeArray.Length; i++)
                {
                    var pathNode = PathNodeArray[i];
                    pathNode.ResetNode();
                    pathNode.SetHCost(
                        CalculateDistanceCost(pathNode.Coords, EndPosition));
                    PathNodeArray[i] = pathNode;
                }

                var endNodeIndex = CalculateIndex(EndPosition, GridSize);
                var startNodeIndex = CalculateIndex(StartPosition, GridSize);
                
                PathNodeArray[startNodeIndex].SetGCost(0);

                
                
                
                //lista de index
                var openList = new NativeList<int>(Allocator.Temp);
                var closedList = new NativeList<int>(Allocator.Temp);

                
                openList.Add(startNodeIndex);
                
                while (openList.Length > 0  ) // es una mierda esto, hay q optimizar
                {
                    var currentNodeIndex = GetLowestCostFNodeIndex(openList, PathNodeArray);
                    var currentNode = PathNodeArray[currentNodeIndex];

                    if (currentNodeIndex == endNodeIndex) break;

                    for (var i = 0; i < openList.Length; i++)
                    {
                        if (openList[i] != currentNodeIndex) continue;
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                    
                    closedList.Add(currentNodeIndex);


                    foreach (var neighbourOffset in NeighbourOffset)
                    {
                        int2 neighbourPosition = currentNode.Coords + neighbourOffset;
                        if (!IsPositionInsideGrid(neighbourPosition, GridSize)) continue ;

                        int neighbourNodeIndex = CalculateIndex(neighbourPosition, GridSize);
                        
                        if (closedList.Contains(neighbourNodeIndex)) continue;

                        Node neighbour = PathNodeArray[neighbourNodeIndex];
                        if (!neighbour.IsWalkable) continue;
                        
                        

                        int tentativeGCost = currentNode.GCost + CalculateDistanceCost(currentNode.Coords, neighbourPosition);
                        if (tentativeGCost < neighbour.GCost) {
                            neighbour.SetComeIndex(currentNodeIndex);
                            neighbour.SetGCost(tentativeGCost);
                            neighbour.CalculateFCost();
                            PathNodeArray[neighbourNodeIndex] = neighbour;

                            if (!openList.Contains(neighbour.Index)) { 
                                openList.Add(neighbour.Index);
                            }
                        }
                    }
                    
                }
                var endNode = PathNodeArray[endNodeIndex];

                if (endNode.CameFromNodeIndex != -1)
                    CalculatePath(endNode);
                //else
                    //Debug.Log("Didn't find a path!");

                openList.Dispose();
                closedList.Dispose();
            }
            
            private NativeList<int> CalculatePath( Node end)
            {
                if (end.CameFromNodeIndex == -1) // Couldn't find a path!
                    return new NativeList<int>(Allocator.Temp);

                // Found a path
                //var path = new NativeList<int>(Allocator.Temp);
                PathResult.Add(end.Index);
                

                var nya = 0;
                Node current = end;
                while (current.CameFromNodeIndex != -1) {
                    Node cameFrom = PathNodeArray[current.CameFromNodeIndex];
                    PathResult.Add(cameFrom.Index);
                    current = cameFrom;
                    nya++;

                    if (nya >= PathNodeArray.Length) current.SetComeIndex(-1);

                }
                return PathResult;
            }

            private static int CalculateIndex(int2 cords, int2 boxSize) => cords.x * 64 + cords.y;// * boxSize.x;

            private static int CalculateDistanceCost(int2 aPosition, int2 bPosition)
            {
                var distanceX = Mathf.Abs(aPosition.x - bPosition.x);//
                var distanceY = Mathf.Abs(aPosition.y - bPosition.y);
                var remaining = Mathf.Abs(distanceX - distanceY);

                return MoveDiagonalCost * Mathf.Min(distanceX, distanceY) + 
                       MoveStraightCost * remaining;
            }

            private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<Node> pathNodeArray) {
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
                    gridPosition.x >= 0 &&
                    gridPosition.y >= 0 &&
                    gridPosition.x < gridSize.x &&
                    gridPosition.y < gridSize.y;
            }

            
        }

    }
}