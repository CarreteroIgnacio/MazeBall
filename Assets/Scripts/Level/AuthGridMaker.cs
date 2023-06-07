using Unity.Entities;
using UnityEngine;

namespace Level
{
    public class AuthGridMaker : MonoBehaviour
    {
        public int cellAmount;
        public float gridScale;
        public float gridHeight;

        public float mapTransitionSpeed = .1f;

        private void OnValidate()
        {
            cellAmount = Mathf.Abs(cellAmount);
            gridScale = Mathf.Abs(gridScale);
            gridHeight = Mathf.Abs(gridHeight);
        }

        public static AuthGridMaker Instance;
        private void Awake()
        {
            Instance = this;
        }
    }

    
    public class GridMakerBaker : Baker<AuthGridMaker>
    {
        public override void Bake(AuthGridMaker authoring)
        {

            AddComponent( new GridMakerComponent
            {
                CellAmount = authoring.cellAmount,
                GridScale = authoring.gridScale,
                GridHeight = authoring.gridHeight,
                LmPosition = authoring.transform.position,
                MapTransitionSpeed = authoring.mapTransitionSpeed
            });


        }
    }
}
