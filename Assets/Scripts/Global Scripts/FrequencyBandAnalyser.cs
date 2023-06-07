using System.Collections;
using System.Collections.Generic;   
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Random = UnityEngine.Random;

public class FrequencyBandAnalyser : MonoBehaviour
{

        
    public static FrequencyBandAnalyser Instance;
    
    [Header("Shader")]
    public Material boxyMat;

    [Header("Transitions")]
    public float transitionTime;
    [Range(0, 1)] public float globalSongDuration;
    [Range(0,1)] public float globalVolume;
    

    [Header("Lights")]
    public HDAdditionalLightData northLight;
    public HDAdditionalLightData eastLight;
    public HDAdditionalLightData southLight;
    public HDAdditionalLightData westLight;



    [Header("Level List")]
    public int currentLevelIndex;
    public List<MapLevelScriptable> levelScriptableList;

    [Header("Audio Source")]
    public int currentAudioSourceIndex;
    public AudioSource[] audioSourceArray = new AudioSource[2];
    
    // ---------------
    
    private static List<MapSettings> _mapSettingsList;
    
    private static readonly int EmissiveAcute = Shader.PropertyToID("_EmissiveAcute");
    private static readonly int EmissiveGrave = Shader.PropertyToID("_EmissiveGrave");
    private const int FrequencyBins = 512;

    private float[] _samples;
    private float[] _sampleBuffer;
    
    
    
    #region Events Funcitons

        private void Awake() => Instance = this;

        private void Start()
        {
            _samples = new float[FrequencyBins];
            _sampleBuffer = new float[FrequencyBins];
            
            _mapSettingsList = new List<MapSettings>();

            foreach (var level in levelScriptableList)
                _mapSettingsList.Add(new MapSettings(level));

            GetNextLevel( true );
        }
        
        private void Update()
        {
            audioSourceArray[currentAudioSourceIndex].GetSpectrumData(_sampleBuffer, 0, FFTWindow.BlackmanHarris);
            
            for (var i = 0; i < _samples.Length; i++)
            {
                if (_sampleBuffer[i] > _samples[i])
                    _samples[i] = _sampleBuffer[i];
                else
                    _samples[i] = Mathf.Lerp(_samples[i], _sampleBuffer[i], Time.deltaTime * levelScriptableList[currentLevelIndex].smoothDownRate);
            }
            UpdateFreqBands64();

            
        }
        
    #endregion


    private IEnumerator LevelChanger(float time)
    {
        //var time = ;
        yield return new WaitForSeconds(time);
        GetNextLevel(false);
    }
    public static MapSettings GetCurrentLevel() => _mapSettingsList[Instance.currentLevelIndex];

    
    private void GetNextLevel(bool started)
    {
        var time = levelScriptableList[currentLevelIndex].clip.length * globalSongDuration;
        var ran = Random.Range(0, levelScriptableList[currentLevelIndex].clip.length - time - 1f);
        
        var oldSource = currentAudioSourceIndex;
        currentAudioSourceIndex = currentAudioSourceIndex > 0 ? 0 : 1;
        
        var oldLevelIndex = currentLevelIndex;


        int newIndex;
        while (true)
        {
            newIndex = Random.Range(0, levelScriptableList.Count);
            if (currentLevelIndex != newIndex) break;
        }
        currentLevelIndex = newIndex;

        audioSourceArray[currentAudioSourceIndex].clip = levelScriptableList[currentLevelIndex].clip;
        audioSourceArray[currentAudioSourceIndex].time = ran;
        audioSourceArray[currentAudioSourceIndex].Play();
        StartCoroutine(FadeSong(oldLevelIndex, oldSource, started));
        StartCoroutine(LevelChanger(time));
        
        // tendria q cambiar toda la AI de los agents para poder usar el burst compile, xq sino se va al carajo la performcnce
        //GameManager.Instance.OnLevelChanging();
    }


    public void PauseMusic(bool nya)
    {
        if (nya)
        {
            audioSourceArray[0].Pause();
            audioSourceArray[1].Pause();
        }
        else
        {
            audioSourceArray[0].Play();
            audioSourceArray[1].Play();
        }
        
        if(audioSourceArray[0].volume == 0)audioSourceArray[0].Stop();
        if(audioSourceArray[1].volume == 0)audioSourceArray[1].Stop();
    }

    private IEnumerator FadeSong(int oldLevelIndex, int oldSource, bool started)
    {
        var nya = 0f;
        while (nya < transitionTime)
        {
                audioSourceArray[oldSource].volume = started ? 0 :
                    Mathf.Lerp(levelScriptableList[oldLevelIndex].maxVolume, 0, nya / transitionTime) * globalVolume;
            audioSourceArray[currentAudioSourceIndex].volume = Mathf.Lerp(0,levelScriptableList[currentLevelIndex].maxVolume, nya/transitionTime) * globalVolume;
            
            
            var lerp = Mathf.Lerp(0, 1, nya/transitionTime);
            
            northLight.color = Color.Lerp(levelScriptableList[oldLevelIndex].northColor, levelScriptableList[currentLevelIndex].northColor, lerp);
            eastLight.color = Color.Lerp(levelScriptableList[oldLevelIndex].eastColor, levelScriptableList[currentLevelIndex].eastColor, lerp);
            southLight.color = Color.Lerp(levelScriptableList[oldLevelIndex].southColor, levelScriptableList[currentLevelIndex].southColor, lerp);
            westLight.color = Color.Lerp(levelScriptableList[oldLevelIndex].westColor, levelScriptableList[currentLevelIndex].westColor, lerp);
            
        
            boxyMat.SetColor(EmissiveAcute, 
                Color.Lerp(levelScriptableList[oldLevelIndex].acuteColorShader, levelScriptableList[currentLevelIndex].acuteColorShader, lerp));
            boxyMat.SetColor(EmissiveGrave, 
                Color.Lerp(levelScriptableList[oldLevelIndex].graveColorShader, levelScriptableList[currentLevelIndex].graveColorShader, lerp));
            
            
            nya += Time.deltaTime;
            yield return 0;
        }
        
        audioSourceArray[oldSource].Stop();
        audioSourceArray[currentAudioSourceIndex].volume = levelScriptableList[currentLevelIndex].maxVolume * globalVolume;
    }

    private void OnValidate()
    {
        audioSourceArray[currentAudioSourceIndex].volume = levelScriptableList[currentLevelIndex].maxVolume * globalVolume;
    }

    public void SetCurrentVolume(float vol)
    {
        globalVolume = vol;
        audioSourceArray[currentAudioSourceIndex].volume = levelScriptableList[currentLevelIndex].maxVolume * globalVolume;
    }
    
    private void UpdateFreqBands64()
    {

        
        var joby = new BandAnalyzerJob
        {
            finalValues = new NativeArray<float4>(1,Allocator.TempJob),
            samples = new NativeArray<float>(_samples ,Allocator.TempJob)
        };
        joby.Run();
        
        
        //----  
        northLight.intensity = Mathf.Lerp(
            northLight.intensity,
            levelScriptableList[currentLevelIndex].intencityMul * joby.finalValues[0].x,
            levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime);

        eastLight.intensity = Mathf.Lerp(
            eastLight.intensity,
            levelScriptableList[currentLevelIndex].intencityMul * joby.finalValues[0].y,
            levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime);
        
        southLight.intensity = Mathf.Lerp(
            southLight.intensity,
            levelScriptableList[currentLevelIndex].intencityMul * joby.finalValues[0].z,
            levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime);
        
        westLight.intensity = Mathf.Lerp(
            westLight.intensity,
            levelScriptableList[currentLevelIndex].intencityMul * joby.finalValues[0].w,
            levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime);
        //----    
    
        
        
        //----
        northLight.range = Mathf.Clamp(
                            Mathf.Lerp(
                                northLight.range,
                                levelScriptableList[currentLevelIndex].rangeMul * joby.finalValues[0].x,
                                levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime),
                            0,32);
        
        eastLight.range = Mathf.Clamp(
                            Mathf.Lerp(
                                eastLight.range,
                                levelScriptableList[currentLevelIndex].rangeMul * joby.finalValues[0].y,
                                levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime),
                            0,32);
                    
        southLight.range = Mathf.Clamp(
                            Mathf.Lerp(
                                southLight.range,
                                levelScriptableList[currentLevelIndex].rangeMul * joby.finalValues[0].z,
                                levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime),
                            0,32);
    
        westLight.range = Mathf.Clamp(
                            Mathf.Lerp(
                                westLight.range,
                                levelScriptableList[currentLevelIndex].rangeMul * joby.finalValues[0].w,
                                levelScriptableList[currentLevelIndex].speedLerp * Time.deltaTime),
                            0,32);
        //----


        northLight.SetAreaLightSize(new Vector2( Mathf.Lerp(0,64, joby.finalValues[0].x * levelScriptableList[currentLevelIndex].shapeMul) , .01f));
        eastLight.SetAreaLightSize(new Vector2( Mathf.Lerp(0,64, joby.finalValues[0].y * levelScriptableList[currentLevelIndex].shapeMul) , .01f));
        southLight.SetAreaLightSize(new Vector2( Mathf.Lerp(0,64, joby.finalValues[0].z * levelScriptableList[currentLevelIndex].shapeMul) , .01f));
        westLight.SetAreaLightSize(new Vector2( Mathf.Lerp(0,64, joby.finalValues[0].z * levelScriptableList[currentLevelIndex].shapeMul) , .01f));
    }

    


    [BurstCompile]
    private struct BandAnalyzerJob : IJob
    {
        public NativeArray<float4> finalValues;
        public NativeArray<float> samples;
        public void Execute()
        {
            var currentValues = new float4
            {
                x = CalculateBand(0, 8),
                y = CalculateBand(20, 28),
                z = CalculateBand(36, 44),
                w = CalculateBand(56, 64)
            };

            finalValues[0] = currentValues;
        }

        private float CalculateBand(int from, int to)
        {
            var average = 0f;
            var count = 0;
            var power = 0;
            var sampleCount = 1;

            for (var i = from; i < to; i++)
            {
                if (i is 16 or 24 or 32 or 40 or 48 or 56)
                {
                    power++;
                    sampleCount = (int)Mathf.Pow(2, power);
                    if (power == 3)
                        sampleCount -= 2;
                }
                for (var j = 0; j < sampleCount; j++)
                {
                    average += samples[count] * (count + 1);
                    count++;
                }
            }
            return average;
        }
        
    }
}
