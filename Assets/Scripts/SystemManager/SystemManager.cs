using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class SystemManager : MonoBehaviour
{
    public static SystemManager Instance;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (cameraTrack is null)
        {
            Debug.LogError("CameraTrack is Null");
            return;
        }
        StartCoroutine(EnableAllSystem());
        GameManager.TogglePauseEvent += ToggleSystems;

        OnEcsParams( new SysEcsParameters { LodDistance = lodDistance });
    }

    

    #region EnabledSystem

        public delegate void TrueStartDelegate(bool enable);
        public static event TrueStartDelegate TrueStart;
        private static void OnTrueStart(bool enable) => TrueStart?.Invoke(enable);
        
        public delegate void SecondTrueStartDelegate(bool enable);
        public static event SecondTrueStartDelegate SecondTrueStart;
        private static void OnSecondTrueStart(bool enable) => SecondTrueStart?.Invoke(enable);
        private IEnumerator EnableAllSystem()
        {
            yield return new WaitForSeconds(.75f);
            OnTrueStart(true);
            OnValidate();
            yield return new WaitForSeconds(2f);
            OnSecondTrueStart(true);
        }

        //---
        public delegate void PauseDelegate(bool enable);
        public static event PauseDelegate PauseEvent;
        private static void OnPauseEvent(bool enable) => PauseEvent?.Invoke(enable);
        
        private void ToggleSystems(bool active) => OnPauseEvent(!active);
        
    #endregion


    public delegate void EcsParamsDelegate(SysEcsParameters parameters);
    public static event EcsParamsDelegate EcsParams;
    private static void OnEcsParams(SysEcsParameters parameters) => EcsParams?.Invoke(parameters);
    public struct SysEcsParameters
    {
        public float3 LodDistance;
    }

    public Vector3 lodDistance;
    
    
    
    
    
    private void OnValidate()
    {
        OnEcsParams(
            new SysEcsParameters
            {
                LodDistance = lodDistance,

            });
    }
    
    
    
    
    
    public struct SysEcsRuntimeParams
    {
        public float LevelDuration;
        public float4x4 CameraLtw;
        public float3 PlayerPos;
    }
    
    public delegate void EcsRuntimeDelegate(SysEcsRuntimeParams parameters);
    public static event EcsRuntimeDelegate EcsParamsRuntime;
    private static void OnRuntimeEcsParams(SysEcsRuntimeParams parameters) => EcsParamsRuntime?.Invoke(parameters);

    public Transform mainCamera;
    public Transform cameraTrack;
    
    private void Update()
    {
        OnRuntimeEcsParams(new SysEcsRuntimeParams
        {
            CameraLtw = mainCamera.localToWorldMatrix,
            LevelDuration = FrequencyBandAnalyser.GetCurrentLevel().Duration,
            PlayerPos = cameraTrack.position
            
        });
    }
    
    
    
    
    public delegate void CollectibleDelegate(CollectableComponent parameters);

    public static event CollectibleDelegate CollectableEvent;
    public static void OnCollectable(CollectableComponent parameters) => CollectableEvent?.Invoke(parameters);
    


}
