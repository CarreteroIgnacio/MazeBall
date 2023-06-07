using System.Collections;
using System.Collections.Generic;   
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class FrequencyBandAnalyser : MonoBehaviour
{

        
    public static FrequencyBandAnalyser Instance;
    
    [Header("Shader")]
    public Material boxyMat;
    public Material wallMat;

    [Header("Transitions")]
    public float transitionTime;
    [Range(0, 1)] public float globalSongDuration;
    [Range(0,1)] public float globalVolume;
    

    [Header("Level List")]
    public int currentLevelIndex;
    public List<MapLevelScriptable> levelScriptableList;

    [Header("Audio Source")]
    public int currentAudioSourceIndex;
    public AudioSource[] audioSourceArray = new AudioSource[2];
    public float bandHighMul = 10f;
    
    // ---------------
    
    private static List<MapSettings> _mapSettingsList;
    
    private static readonly int EmissiveAcute = Shader.PropertyToID("_EmissiveAcute");
    private static readonly int EmissiveGrave = Shader.PropertyToID("_EmissiveGrave");
    private const int FrequencyBins = 4096;

    private float[] _sampleBuffer;
    private NativeArray<float> _samples;
    

    #region Events Funcitons

        private void Awake() => Instance = this;

        private void Start()
        {
            _sampleBuffer = new float[FrequencyBins];
            _samples = new NativeArray<float>(FrequencyBins, Allocator.Persistent);
            
            
            _mapSettingsList = new List<MapSettings>();

            foreach (var level in levelScriptableList)
                _mapSettingsList.Add(new MapSettings(level));

            GetNextLevel( true );
        }
        
        private void Update()
        {
            audioSourceArray[currentAudioSourceIndex].GetSpectrumData(_sampleBuffer, 0, FFTWindow.Rectangular);

            
            var joby = new BandAnalyzerJob
            {
                SampleBuffer = new NativeArray<float>(_sampleBuffer ,Allocator.TempJob),
                Samples = _samples,
            
                MatrixValues = new NativeArray<float4x4>(16, Allocator.TempJob),
                Vec4ShaderMul = bandHighMul,
                SmoothDownRate = levelScriptableList[currentLevelIndex].smoothDownRate,
                DeltaTime = Time.deltaTime
            };
            joby.Run();

            
            for (var i = 0; i < joby.MatrixValues.Length; i++)
                wallMat.SetMatrix($"_MatrixSector_{i}", joby.MatrixValues[i]);

            
            //_samples = joby.Samples;
            //joby.Samples.Dispose();
            joby.SampleBuffer.Dispose();
            joby.MatrixValues.Dispose();
        }

        #endregion


    private IEnumerator LevelChanger(float time)
    {
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
        GameManager.OnLevelChanging();
        StartCoroutine(LevelChanger(time));
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
            
        
            boxyMat.SetColor(EmissiveAcute, 
                Color.Lerp(levelScriptableList[oldLevelIndex].acuteColorShader, levelScriptableList[currentLevelIndex].acuteColorShader, lerp));
            boxyMat.SetColor(EmissiveGrave, 
                Color.Lerp(levelScriptableList[oldLevelIndex].graveColorShader, levelScriptableList[currentLevelIndex].graveColorShader, lerp));
            
            
            wallMat.SetColor(EmissiveAcute, 
                Color.Lerp(levelScriptableList[oldLevelIndex].acuteColorShader, levelScriptableList[currentLevelIndex].acuteColorShader, lerp));
            wallMat.SetColor(EmissiveGrave, 
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

    


    [BurstCompile]
    private struct BandAnalyzerJob : IJob
    {
        public NativeArray<float> Samples;
        public NativeArray<float4x4> MatrixValues;
        public float Vec4ShaderMul;
        public NativeArray<float> SampleBuffer;
        public float SmoothDownRate;
        public float DeltaTime;
        
        

        public void Execute()
        {
            var freqBands256 = new NativeArray<float>(256, Allocator.Temp);
            for (var i = 0; i < Samples.Length; i++)
            {
                if (SampleBuffer[i] > Samples[i])
                    Samples[i] = SampleBuffer[i];
                else
                    Samples[i] = Mathf.Lerp(Samples[i], SampleBuffer[i], DeltaTime * SmoothDownRate);
            }
            
            
            // I can integrate this function in the matrix, but is already in burst, no performance difference y super boilerplate
            UpdateFreqBands64(ref freqBands256);
            
            // Construct Matrix4x4 Array
            for (var i = 0; i < 256; i += 16)
            {
                var nya = new Matrix4x4();
                for (var j = 0; j < 16; j += 4)
                {
                    var sector = new Vector4(
                        freqBands256[i + j],
                        freqBands256[i + j + 1],
                        freqBands256[i + j + 2],
                        freqBands256[i + j + 3]
                    );
                    nya.SetRow(j/4, sector * Vec4ShaderMul);

                }
                MatrixValues[i/16] = nya;
            }

            freqBands256.Dispose();
        }

  
        private void UpdateFreqBands64(ref NativeArray<float> freqBands256)
        {

            var count = 0;
            var sampleCount = 1;
            var power = 0;


            for (var i = 0; i < 256; i++)
            {
                float average = 0;

                if (i is 64 or 24*4 or 32*4 or 40*4 or 48*4 or 56*4)
                {
                    power++;
                    sampleCount = (int)Mathf.Pow(2, power);
                    if (power == 3)
                        sampleCount -= 2;
                }


                for (var j = 0; j < sampleCount; j++)
                {
                    //count = Mathf.Clamp(count, 0, 511);
                    average += Samples[count] * (count + 1);

                    count++;
                }

                average /= count;
                
                //var nya = samples[i] + samples[i+1] + samples[i+2] + samples[i+3];
                
                
                freqBands256[i] = average;
            }
        }

    }
}
