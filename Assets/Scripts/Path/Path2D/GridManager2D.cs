using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public struct GridDataStruct
{
    public float3 Position;
    public float3 StartPoint;
    public int2 GridSize;
    public float NodeDistance;
    public float NodeSize;
    public LayerMask CollideMask;
}

namespace Path
{
    /*
     * El objetivo de esta clase es poder crear comodamente una grilla que detecte colisiones para invalidad los puntos
     * y exponerlos es un native array para que el path job pueda tomarlos facil, (muchos tutoriales la rehacen a al grilla cada vez)
     * A su vez tiene la capacidad de dado un vector 3 darte el punto en la grilla mas cercano. L
     * Lo cual te permite una gran independencia del tamaño de la grilla, segun su nodeDistance
     * como tambien tenes el NodeSize para determinar las colisiones, entonces dependiendo la situacion puede ser al maximo
     * o solo del tamaño del agent
     *
     * Por ultimo la gran maravilla los gizmos! y que esta grilla tiene la capacidad de recalcularse en runtime!
     * donde se puede ver claramente el tamaño de esta como tambien los puntos validos e invalidos por separado
     *
     * PD: Se llama 2D xq simplifique el codigo del juego de Motores donde tengo una A* en 3D en DOTS
    */
    public class GridManager2D : MonoBehaviour
    {

        #region Attributes

            [SerializeField] public int2 gridSize;

            [Tooltip("si esta activo recalcula la grilla con cada modificacion")]
            [SerializeField] private bool recalculateGrid;

            [Header("TestNodes")]
            [SerializeField] private float nodeDistance;
            [SerializeField] private float nodeSize;
            [SerializeField] private LayerMask collideWith;

        #endregion

        #region Serialized Vars
            
            public static NativeArray<Node> PathNodeArray;
            //private static Node[][] GridNodes { get; set; }
            
        #endregion
        
        
        public static GridDataStruct GridData;

        

        #region Events
            private void Awake()
            {
               
                var position = transform.position;
                GridData = new GridDataStruct
                {
                    Position = position,
                    StartPoint = position - new Vector3(gridSize.x - nodeDistance ,0,gridSize.y - nodeDistance) *.5f ,
                    GridSize = gridSize,
                    NodeDistance = nodeDistance,
                    NodeSize = nodeSize,
                    CollideMask = collideWith
                };
                InitializeGrid(GridData);
            }

            private void OnDisable() { if(PathNodeArray.IsCreated) PathNodeArray.Dispose(); }

            private void OnValidate()
            {
                
                if (gridSize.x < 1) gridSize.x = 1;
                if (gridSize.y < 1) gridSize.y = 1;


                nodeDistance = Mathf.Abs(nodeDistance);
                nodeSize =  Mathf.Abs(nodeSize);

                if (nodeSize > nodeDistance)
                    nodeSize = nodeDistance;

                if (!recalculateGrid) return;
                    /*
                var position = transform.position;
                GridData =  new GridDataStruct
                {
                    Position = position,
                    StartPoint = position - new Vector3(-gridSize.x,0,-gridSize.y) *.5f,
                    GridSize = gridSize,
                    NodeDistance = nodeDistance,
                    NodeSize = nodeSize,
                    CollideMask = collideWith
                };
                InitializeGrid(GridData);*/
            }

        #endregion


        private static void InitializeGrid(GridDataStruct gridData)
        {
            if (PathNodeArray.IsCreated) PathNodeArray.Dispose();
            
            PathNodeArray = new NativeArray<Node>(gridData.GridSize.x * gridData.GridSize.y, Allocator.Persistent);
            //GridNodes = new Node[gridData.GridSize.x][];
            for (var x = 0; x < gridData.GridSize.x; x++)
            {
                //GridNodes[x] = new Node[gridData.GridSize.y];

                for (var y = 0; y < gridData.GridSize.y; y++)
                {
                    var pos = gridData.StartPoint + new float3(x, 0, y) * gridData.NodeDistance;
                    var index = GetIndex( x, y, gridData.GridSize.x);
                    var node = new Node(new int2(x, y), pos , index);

                    if (Physics.CheckSphere(pos, gridData.NodeSize / 2, gridData.CollideMask))
                        node.SetIsWalkable(false);

                    //GridNodes[x][y] = node;
                    PathNodeArray[node.Index] = node;
                }
            }
    
        }
        private static int GetIndex(int x, int y, int width) => x * width + y;

        public static int2 GetClosestPointWorldSpace(float3 position, GridDataStruct gridData, NativeArray<Node> pathArray)
        {

            var pos = position - gridData.StartPoint;

            
            var percentageX = Mathf.Clamp01(pos.x / gridData.GridSize.x);
            var percentageY = Mathf.Clamp01(pos.z / gridData.GridSize.y);
            var x = Mathf.Clamp(Mathf.RoundToInt(percentageX * gridData.GridSize.x), 0, gridData.GridSize.x - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(percentageY * gridData.GridSize.y), 0, gridData.GridSize.y - 1);
            var result = pathArray[GetIndex(x,y,gridData.GridSize.x)];
            while (!result.IsWalkable)
            {
                var freePoints = new NativeList<Node>(Allocator.Temp);
                for (var p = -1; p <= 1; p++)
                for (var q = -1; q <= 1; q++)
                {
                    if (x == p && y == q) continue;

                    var i = x + p;
                    var j = y + q;
                    if (i > -1 && i < gridData.GridSize.x &&
                        j > -1 && j < gridData.GridSize.y)
                    {
                        if (pathArray[GetIndex(x+p,y+q,gridData.GridSize.x)].IsWalkable)
                            freePoints.Add(pathArray[GetIndex(x+p,y+q,gridData.GridSize.x)]);
                    }
                }

                var distance = Mathf.Infinity;
                foreach (var t in freePoints)
                {
                    var dist = (t.WorldPosition - position).sqrMagnitude();
                    if (!(dist < distance)) continue;
                        result = t;
                        distance = dist;
                }
            }
            return result.Coords;
        }

        public static float3 GetRandomValidNode(NativeArray<Node> pathArray)
        {
            while (true)
            {
             var nodeCord= UnityEngine.Random.Range(0, pathArray.Length - 1);
             if (pathArray[nodeCord].IsWalkable)
                 return pathArray[nodeCord].WorldPosition;
            }
        }

        public static void UpdateValidNodes(NativeArray<float> levelSettingsArrayEntity)
        {
            for (var i = 0; i < levelSettingsArrayEntity.Length; i++)
            {
                var pathNode = PathNodeArray[i];
                pathNode.IsWalkable = levelSettingsArrayEntity[i] < 1;
                PathNodeArray[i] = pathNode;
            }
        }

        #region Gizmos




        /*
         maravillosa cajita en vez de hacer la clasica de hacer un box por cada punto, que equiaelent por ej si fuera 20x20x20 a 8000 box!!!
         te explota el editor, y enicmano se ve una raja
         hago box del tamaño maximo, entonces la grid solo se ve en las caras del cubo, es facil de ver, y encima solo usaria +- 30 box :3 
         
         PD: Soy demaciado bueno para hacer gizmos (?
         
         */



        [Header("Box Gizmos")] [Header("")] [Tooltip("High Performance Impact :c")]
        public bool showBoxGizmo;

        [SerializeField] private Color32 boxColor = new(255, 0, 255, 255);
        [SerializeField] private Color32 gridColor = new(255, 255, 255, 10);

        private void OnDrawGizmos() => MyGizmos();
        private void OnDrawGizmosSelected() => MyGizmosSelected();

        [Header("View TestNodes")] [Tooltip("High Performance Impact :c")]
        public bool seeValids;

        public Color32 validColor = new(255, 255, 255, 50);
        public Color32 invalidColor = new(255, 0, 0, 150);
        public bool seeInvalids;


        private void MyGizmos()
        {
            if (!showBoxGizmo) return;
            Gizmos.color = boxColor;
            var boxSize = (new Vector3(gridSize.x,0, gridSize.y)) * nodeDistance;
            var position = transform.position;
            Gizmos.DrawWireCube(position, boxSize + Vector3.up);

            //---- ---- ---- ---- ----//

            Gizmos.color = gridColor;

            var halfNodeDist = nodeDistance / 2;
            var tf = position;

            tf.x -= gridSize.x * halfNodeDist - halfNodeDist;


            var cubeSize = new Vector3(nodeDistance,1, boxSize.z);

            for (var x = 0; x < gridSize.x; x += 2)
            {
                Gizmos.DrawWireCube(tf, cubeSize);
                tf.x += nodeDistance * 2;
            }



            cubeSize = new Vector3(boxSize.x,1, nodeDistance);

            tf = position;
            tf.z -= gridSize.y * halfNodeDist - halfNodeDist;

            for (var z = 0; z < gridSize.y; z += 2)
            {
                Gizmos.DrawWireCube(tf, cubeSize);
                tf.z += nodeDistance * 2;
            }
        }

        private void MyGizmosSelected()
        {
            if (!seeInvalids && !seeValids) return;
            //for (var x = 0; x < gridSize.x; x++)
            //for (var y = 0; y < gridSize.y; y++)
            //if (GridNodes == null) return;

            foreach (var node in PathNodeArray)
            {
                if (!PathNodeArray.IsCreated) return;
                //var node = GridNodes[x][y];

                if (node.IsWalkable)
                {
                    if (!seeValids) continue;
                    Gizmos.color = validColor;//
                    Gizmos.DrawSphere(node.WorldPosition, nodeSize / 2);
                }
                else
                {
                    if (!seeInvalids) continue;
                    Gizmos.color = invalidColor;
                    Gizmos.DrawSphere(node.WorldPosition, nodeSize / 2);
                }
            }
        }

        #endregion



    }
}