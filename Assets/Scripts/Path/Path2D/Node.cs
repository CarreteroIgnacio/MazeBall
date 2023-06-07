using Unity.Mathematics;
using UnityEngine;

/*
 * Quisas algunas cosas parescan raras, pero estan asi para lograr el mayor rendimiento posible. Dado q los nodos estan un native array persitens
 * no se estan inicializando todo el tiempo
 */

namespace Path
{
    public struct Node
    {
        public int2 Coords { get; }
        public Vector3 WorldPosition { get; }
        public bool IsWalkable { get; set; }

        public int Index { get; }

        public Node(int2 cor, Vector3 pos, int index)
        {
            Coords = cor;
            WorldPosition = pos;
            Index = index;

            IsWalkable = true;

            GCost = int.MaxValue;
            HCost = 0;
            FCost = 0;
            CameFromNodeIndex = -1;
        }


        public void SetIsWalkable(bool isWalkable) => IsWalkable = isWalkable;

        public int CameFromNodeIndex { get; private set; }

        public int GCost { get; private set; }

        public int HCost { get; private set; }

        public int FCost { get; private set; }

        public void CalculateFCost() => FCost = GCost + HCost;

        public void ResetNode()
        {
            GCost = int.MaxValue;
            FCost = 0;
            HCost = 0;
            CameFromNodeIndex = -1;
        }

        public void SetGCost(int cost)
        {
            GCost = cost;
            CalculateFCost();
        }

        public void SetHCost(int cost)
        {
            HCost = cost;
            CalculateFCost();
        }

        public void SetComeIndex(int nya) => CameFromNodeIndex = nya;
    }
}